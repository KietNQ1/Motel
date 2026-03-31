using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motel.Helpers;
using Motel.Services.Interfaces;
using Motel.ViewModels.Tenant;

namespace Motel.Controllers
{
    public class TenantController : Controller
    {
        private readonly ITenantService _service;
        private readonly ILogger<TenantController> _logger;
        private readonly LandlordHelper _landlordHelper;
        private readonly IWebHostEnvironment _env;
        private readonly Motel.Data.MotelDbContext _context;

        public TenantController(
            ITenantService service, 
            ILogger<TenantController> logger,
            LandlordHelper landlordHelper,
            IWebHostEnvironment env,
            Motel.Data.MotelDbContext context)
        {
            _service = service;
            _logger = logger;
            _landlordHelper = landlordHelper;
            _env = env;
            _context = context;
        }

[HttpGet]
        public async Task<IActionResult> Index(int propertyId)
        {
            var viewModel = await _service.GetTenantsByPropertyIdAsync(await _landlordHelper.GetCurrentLandlordIdAsync(User), propertyId);
            if (viewModel == null)
            {
                TempData["Error"] = "Không tìm thấy nhà trọ hoặc bạn không có quyền truy cập.";
                return RedirectToAction("Index", "Property");
            }
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Create(int? returnRoomId = null)
            => View(new TenantCreateViewModel { ReturnRoomId = returnRoomId });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TenantCreateViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            try
            {
                var id = await _service.CreateTenantAsync(await _landlordHelper.GetCurrentLandlordIdAsync(User), vm);
                TempData["Success"] = "Tạo người thuê thành công!";

                if (vm.ReturnRoomId.HasValue)
                    return RedirectToAction("Create", "Contract", new { roomId = vm.ReturnRoomId.Value });

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create tenant error");
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo người thuê.");
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id, int? occupancyId = null)
        {
            var tenant = await _service.GetTenantDetailsAsync(await _landlordHelper.GetCurrentLandlordIdAsync(User), id);
            if (tenant == null)
            {
                TempData["Error"] = "Không tìm thấy người thuê.";
                return RedirectToAction("Index", "Property");
            }

            var (frontImage, backImage) = await _service.GetTenantCccdImagesAsync(id);
            ViewBag.FrontImage = frontImage;
            ViewBag.BackImage = backImage;

            ViewBag.OccupancyId = occupancyId;
            ViewBag.ResidenceProofImage = await _service.GetTenantResidenceProofImageAsync(id, occupancyId);

            return View(tenant);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, int? occupancyId = null)
        {
            var vm = await _service.BuildEditViewModelAsync(await _landlordHelper.GetCurrentLandlordIdAsync(User), id, occupancyId);
            if (vm == null)
            {
                TempData["Error"] = "Không tìm thấy người thuê.";
                return RedirectToAction("Index", "Property");
            }
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TenantEditViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            try
            {
                var ok = await _service.UpdateTenantAsync(await _landlordHelper.GetCurrentLandlordIdAsync(User), vm);
                if (!ok)
                {
                    TempData["Error"] = "Không tìm thấy người thuê.";
                    return RedirectToAction("Index", "Property");
                }
                TempData["Success"] = "Cập nhật người thuê thành công!";
                return RedirectToAction(nameof(Details), new { id = vm.TenantId, occupancyId = vm.OccupancyId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Edit tenant error");
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật.");
                return View(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadResidenceProof(int tenantId, int? occupancyId, IFormFile proofImage)
        {
            if (proofImage == null || proofImage.Length == 0)
            {
                TempData["Error"] = "Vui lòng chọn ảnh minh chứng hợp lệ.";
                return RedirectToAction(nameof(Details), new { id = tenantId, occupancyId });
            }

            try
            {
                var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
                var tenant = await _service.GetTenantDetailsAsync(landlordId, tenantId);
                if (tenant == null)
                {
                    TempData["Error"] = "Không tìm thấy người thuê.";
                    return RedirectToAction("Index", "Property");
                }

                var activeOccupancy = await _context.RoomOccupancies
                    .Include(o => o.Room)
                    .ThenInclude(r => r.Property)
                    .FirstOrDefaultAsync(o =>
                        o.TenantId == tenantId &&
                        o.Status == "active" &&
                        (!occupancyId.HasValue || o.OccupancyId == occupancyId.Value) &&
                        o.Room.Property.LandlordId == landlordId);

                if (activeOccupancy == null)
                {
                    TempData["Error"] = "NgÆ°á»i thuÃª hiá»‡n khÃ´ng cÃ³ thá»‘ng tin á»Ÿ hiá»‡n táº¡i Ä‘á»ƒ Ä‘Äƒng kÃ½ táº¡m trÃº.";
                    return RedirectToAction(nameof(Details), new { id = tenantId, occupancyId });
                }

                // Ensure directory exists
                string uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "residence_proofs");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                string ext = Path.GetExtension(proofImage.FileName);
                string fileName = $"proof_T{tenantId}_{DateTime.Now.Ticks}{ext}";
                string filePath = Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await proofImage.CopyToAsync(stream);
                }

                string storagePath = $"/uploads/residence_proofs/{fileName}";

                var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                int userId = int.TryParse(userIdStr, out var u) ? u : 0;
                
                // Fallback attempt to get UserId from Landlord if NameIdentifier parsing fails
                if (userId == 0)
                {
                    var landlordObj = await _context.Landlords.FindAsync(landlordId);
                    userId = landlordObj?.UserId ?? 1;
                }

                var storedFile = new Motel.Models.StoredFile
                {
                    LandlordId = landlordId,
                    FileName = proofImage.FileName,
                    MimeType = proofImage.ContentType,
                    StoragePath = storagePath,
                    UploadedAt = DateTime.Now,
                    UploadedByUserId = userId
                };
                _context.StoredFiles.Add(storedFile);
                await _context.SaveChangesAsync();

                var tenantProofRef = new Motel.Models.StoredFileReference
                {
                    StoredFileId = storedFile.StoredFileId,
                    RefType = "tenant",
                    RefId = tenantId,
                    CreatedAt = DateTime.Now
                };
                var propertyProofRef = new Motel.Models.StoredFileReference
                {
                    StoredFileId = storedFile.StoredFileId,
                    RefType = "property",
                    RefId = activeOccupancy.Room.PropertyId,
                    CreatedAt = DateTime.Now
                };
                _context.StoredFileReferences.Add(tenantProofRef);
                _context.StoredFileReferences.Add(propertyProofRef);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Cập nhật xác minh tạm trú thành công.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading residence proof");
                TempData["Error"] = "Có lỗi xảy ra khi lưu tệp tin.";
            }

            return RedirectToAction(nameof(Details), new { id = tenantId, occupancyId });
        }

        [HttpGet]
        public async Task<IActionResult> PrintCT01(int id, int? occupancyId = null)
        {
            var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
            var vm = await _service.GetCT01DataAsync(landlordId, id, occupancyId);
            
            if (vm == null)
            {
                TempData["Error"] = "Không thể lấy dữ liệu CT01.";
                return RedirectToAction("Index", "Property");
            }

            return View(vm);
        }
    }
}
