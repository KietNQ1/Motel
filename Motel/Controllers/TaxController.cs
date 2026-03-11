using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Services.Interface;
using Motel.ViewModels.Tax;

namespace Motel.Controllers;

/// <summary>
/// Controller for Tax Estimation Module
/// Handles rental income tax estimation for landlords
/// </summary>
[Authorize]
public class TaxController : Controller
{
    private readonly ITaxService _taxService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly MotelDbContext _context;
    private readonly ILogger<TaxController> _logger;

    public TaxController(
        ITaxService taxService,
        UserManager<ApplicationUser> userManager,
        MotelDbContext context,
        ILogger<TaxController> logger)
    {
        _taxService = taxService;
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Display tax estimation history for current landlord
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        try
        {
            var landlordId = await GetCurrentLandlordIdAsync();
            if (landlordId == 0)
            {
                TempData["Error"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Home");
            }

            var landlord = await _context.Landlords
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.LandlordId == landlordId);

            var estimations = await _taxService.GetTaxEstimationHistoryAsync(landlordId);

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
    /// Display form to calculate tax estimation for a specific year
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Calculate()
    {
        var landlordId = await GetCurrentLandlordIdAsync();
        if (landlordId == 0)
        {
            TempData["Error"] = "Bạn không có quyền truy cập trang này.";
            return RedirectToAction("Index", "Home");
        }

        // Generate list of available years (current year and past 3 years)
        var currentYear = DateTime.Now.Year;
        var availableYears = Enumerable.Range(currentYear - 3, 4).Reverse().ToList();

        var viewModel = new TaxEstimationCalculateViewModel
        {
            Year = currentYear,
            AvailableYears = availableYears
        };

        return View(viewModel);
    }

    /// <summary>
    /// Process tax estimation calculation for selected year
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculate(TaxEstimationCalculateViewModel model)
    {
        try
        {
            var landlordId = await GetCurrentLandlordIdAsync();
            if (landlordId == 0)
            {
                TempData["Error"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validate year range
            var currentYear = DateTime.Now.Year;
            if (model.Year < currentYear - 10 || model.Year > currentYear)
            {
                ModelState.AddModelError("Year", "Năm không hợp lệ.");
                return View(model);
            }

            // Calculate tax estimation
            var estimation = await _taxService.CalculateTaxEstimationAsync(landlordId, model.Year);

            TempData["Success"] = $"Đã tính toán xong ước tính thuế cho năm {model.Year}.";
            return RedirectToAction(nameof(Details), new { year = model.Year });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot calculate tax estimation");
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating tax estimation");
            ModelState.AddModelError("", "Có lỗi xảy ra khi tính toán. Vui lòng thử lại.");
            return View(model);
        }
    }

    /// <summary>
    /// Display tax estimation details for a specific year
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(int year)
    {
        try
        {
            var landlordId = await GetCurrentLandlordIdAsync();
            if (landlordId == 0)
            {
                TempData["Error"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("Index", "Home");
            }

            var estimation = await _taxService.GetTaxEstimationForYearAsync(landlordId, year);
            if (estimation == null)
            {
                TempData["Warning"] = $"Chưa có ước tính thuế cho năm {year}. Vui lòng tính toán trước.";
                return RedirectToAction(nameof(Calculate));
            }

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
    /// Helper: Get current landlord ID from authenticated user
    /// </summary>
    private async Task<int> GetCurrentLandlordIdAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
            return 0;

        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return 0;

        var landlord = await _context.Landlords
            .AsNoTracking()
            .Where(l => l.UserId == currentUser.Id && !l.IsDeleted)
            .FirstOrDefaultAsync();

        return landlord?.LandlordId ?? 0;
    }
}
