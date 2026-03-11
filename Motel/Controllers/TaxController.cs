using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Helpers;
using Motel.Services.Interface;
using Motel.ViewModels.Tax;

namespace Motel.Controllers;

/// <summary>
/// Controller cho Module Ước tính Thuế
/// Xử lý tính toán thuế thu nhập từ cho thuê nhà cho chủ nhà
/// </summary>
[Authorize]
public class TaxController : Controller
{
    private readonly ITaxService _taxService;
    private readonly MotelDbContext _context;
    private readonly ILogger<TaxController> _logger;
    private readonly LandlordHelper _landlordHelper;

    public TaxController(
        ITaxService taxService,
        MotelDbContext context,
        ILogger<TaxController> logger,
        LandlordHelper landlordHelper)
    {
        _taxService = taxService;
        _context = context;
        _logger = logger;
        _landlordHelper = landlordHelper;
    }

    /// <summary>
    /// Hiển thị lịch sử ước tính thuế của chủ nhà hiện tại
    /// Display tax estimation history for current landlord
    /// 
    /// Flow:
    /// 1. Lấy landlordId từ user đang đăng nhập
    /// 2. Kiểm tra quyền truy cập (phải là landlord)
    /// 3. Lấy tất cả lịch sử tính thuế từ Service
    /// 4. Hiển thị dạng danh sách (sắp xếp theo năm giảm dần)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            // Bước 1: Xác định landlordId của user hiện tại
            var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
            if (landlordId == 0)
            {
                // User không phải landlord hoặc chưa đăng nhập
                TempData["Error"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Home");
            }

            // Bước 2: Lấy thông tin landlord để hiển thị tên
            var landlord = await _context.Landlords
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.LandlordId == landlordId);

            // Bước 3: Lấy toàn bộ lịch sử tính thuế từ Service
            var estimations = await _taxService.GetTaxEstimationHistoryAsync(landlordId);

            // Bước 4: Chuyển đổi sang ViewModel để hiển thị
            var viewModel = new TaxEstimationHistoryViewModel
            {
                LandlordName = landlord?.DisplayName ?? "N/A",
                Estimations = estimations.Select(TaxEstimationDetailViewModel.FromModel).ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tax estimation history");
            TempData["Error"] = "Không thể tải lịch sử ước tính thuế.";
            return View(new TaxEstimationHistoryViewModel());
        }
    }

    /// <summary>
    /// Hiển thị form để tính thuế cho một năm cụ thể
    /// Display form to calculate tax estimation for a specific year
    /// 
    /// Flow:
    /// 1. Kiểm tra user phải là landlord
    /// 2. Tạo danh sách các năm có thể chọn (năm hiện tại và 3 năm trước)
    /// 3. Hiển thị form với dropdown năm
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Calculate()
    {
        // Bước 1: Kiểm tra quyền truy cập
        var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
        if (landlordId == 0)
        {
            TempData["Error"] = "Bạn không có quyền truy cập trang này.";
            return RedirectToAction("Index", "Home");
        }

        // Bước 2: Tạo danh sách các năm có thể chọn
        // Ví dụ: Năm hiện tại là 2026 → Hiển thị: 2026, 2025, 2024, 2023
        var currentYear = DateTime.Now.Year;
        var availableYears = Enumerable.Range(currentYear - 3, 4).Reverse().ToList();

        // Bước 3: Tạo ViewModel và hiển thị form
        var viewModel = new TaxEstimationCalculateViewModel
        {
            Year = currentYear,  // Mặc định chọn năm hiện tại
            AvailableYears = availableYears
        };

        return View(viewModel);
    }

    /// <summary>
    /// Xử lý tính toán thuế khi user submit form
    /// Process tax estimation calculation for selected year
    /// 
    /// Flow:
    /// 1. Validate landlordId và năm được chọn
    /// 2. Gọi TaxService.CalculateTaxEstimationAsync(landlordId, year)
    ///    - Service sẽ lấy tất cả hợp đồng trong năm
    ///    - Tính tổng doanh thu theo tỷ lệ thời gian
    ///    - So sánh với ngưỡng 100M
    ///    - Tính thuế VAT 5% + PIT 5% nếu vượt ngưỡng
    ///    - Lưu kết quả vào database
    /// 3. Redirect đến trang Details để xem kết quả
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculate(TaxEstimationCalculateViewModel model)
    {
        try
        {
            // Bước 1: Kiểm tra quyền truy cập
            var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
            if (landlordId == 0)
            {
                TempData["Error"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Home");
            }

            // Bước 2: Kiểm tra validation cơ bản
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Bước 3: Validate khoảng năm hợp lệ (không quá 10 năm trước, không quá năm hiện tại)
            var currentYear = DateTime.Now.Year;
            if (model.Year < currentYear - 10 || model.Year > currentYear)
            {
                ModelState.AddModelError("Year", "Năm không hợp lệ.");
                return View(model);
            }

            // Bước 4: Gọi Service để tính toán thuế
            // Service sẽ:
            //   - Lấy tất cả hợp đồng trong năm
            //   - Tính doanh thu theo tỷ lệ thời gian
            //   - So sánh với ngưỡng 100M
            //   - Tính thuế nếu vượt ngưỡng
            //   - Lưu vào database
            var estimation = await _taxService.CalculateTaxEstimationAsync(landlordId, model.Year);

            // Bước 5: Thông báo thành công và chuyển đến trang chi tiết
            TempData["Success"] = $"Đã tính toán xong ước tính thuế cho năm {model.Year}.";
            return RedirectToAction(nameof(Details), new { year = model.Year });
        }
        catch (InvalidOperationException ex)
        {
            // Lỗi business logic: không tìm thấy landlord, không có quy định thuế, etc.
            _logger.LogWarning(ex, "Cannot calculate tax estimation");
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            // Lỗi hệ thống không mong muốn
            _logger.LogError(ex, "Error calculating tax estimation");
            ModelState.AddModelError("", "Có lỗi xảy ra khi tính toán. Vui lòng thử lại.");
            return View(model);
        }
    }

    /// <summary>
    /// Hiển thị chi tiết kết quả tính thuế cho một năm cụ thể
    /// Display tax estimation details for a specific year
    /// 
    /// Flow:
    /// 1. Lấy landlordId từ user đang đăng nhập
    /// 2. Lấy TaxEstimation từ database (nếu đã tính trước đó)
    /// 3. Hiển thị chi tiết:
    ///    - Tổng doanh thu
    ///    - Doanh thu chịu thuế
    ///    - VAT amount (5%)
    ///    - PIT amount (5%)
    ///    - Tổng thuế phải nộp
    ///    - Trạng thái miễn thuế hay không
    /// 4. Nếu chưa có dữ liệu → Redirect về Calculate
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int year)
    {
        try
        {
            // Bước 1: Kiểm tra quyền truy cập
            var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
            if (landlordId == 0)
            {
                TempData["Error"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Home");
            }

            // Bước 2: Lấy kết quả tính thuế cho năm này (nếu đã tính trước đó)
            var estimation = await _taxService.GetTaxEstimationForYearAsync(landlordId, year);
            if (estimation == null)
            {
                // Chưa có dữ liệu → Yêu cầu user tính toán trước
                TempData["Warning"] = $"Chưa có ước tính thuế cho năm {year}. Vui lòng tính toán trước.";
                return RedirectToAction(nameof(Calculate));
            }

            // Bước 3: Chuyển sang ViewModel và hiển thị chi tiết
            // Sẽ hiển thị:
            //   - Tổng doanh thu
            //   - Doanh thu chịu thuế
            //   - VAT (5%)
            //   - PIT (5%)
            //   - Tổng thuế
            //   - Trạng thái miễn thuế
            //   - Ghi chú
            var viewModel = TaxEstimationDetailViewModel.FromModel(estimation);
            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tax estimation details for year {Year}", year);
            TempData["Error"] = "Không thể tải chi tiết ước tính thuế.";
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Tính toán lại ước tính thuế cho một năm cụ thể
    /// Recalculate tax estimation for a specific year
    /// 
    /// Flow:
    /// 1. Lấy landlordId từ user đang đăng nhập
    /// 2. Gọi Service để tính toán lại (sẽ overwrite dữ liệu cũ nếu có)
    /// 3. Redirect về trang Details để xem kết quả mới
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Recalculate(int year)
    {
        try
        {
            // Bước 1: Kiểm tra quyền truy cập
            var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
            if (landlordId == 0)
            {
                TempData["Error"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Home");
            }

            // Bước 2: Validate năm hợp lệ
            var currentYear = DateTime.Now.Year;
            if (year < currentYear - 10 || year > currentYear)
            {
                TempData["Error"] = "Năm không hợp lệ.";
                return RedirectToAction(nameof(Details), new { year });
            }

            // Bước 3: Gọi Service để tính toán lại
            // Service sẽ tự động update nếu đã tồn tại record cho năm này
            await _taxService.CalculateTaxEstimationAsync(landlordId, year);

            // Bước 4: Thông báo thành công và reload trang Details
            TempData["Success"] = $"Đã tính toán lại ước tính thuế cho năm {year}.";
            return RedirectToAction(nameof(Details), new { year });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot recalculate tax estimation for year {Year}", year);
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Details), new { year });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating tax estimation for year {Year}", year);
            TempData["Error"] = "Có lỗi xảy ra khi tính toán lại. Vui lòng thử lại.";
            return RedirectToAction(nameof(Details), new { year });
        }
    }
}
