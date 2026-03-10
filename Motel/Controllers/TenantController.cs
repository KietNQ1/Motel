using Microsoft.AspNetCore.Mvc;
using Motel.Services.Interfaces;
using Motel.ViewModels.Tenant;

namespace Motel.Controllers
{
    public class TenantController : Controller
    {
        private readonly ITenantService _service;
        private readonly ILogger<TenantController> _logger;
        public TenantController(ITenantService service, ILogger<TenantController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private int GetCurrentLandlordId() => 1;

        [HttpGet]
        public async Task<IActionResult> Index(int propertyId)
        {
            var viewModel = await _service.GetTenantsByPropertyIdAsync(GetCurrentLandlordId(), propertyId);
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
                var id = await _service.CreateTenantAsync(GetCurrentLandlordId(), vm);
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
        public async Task<IActionResult> Details(int id)
        {
            var tenant = await _service.GetTenantDetailsAsync(GetCurrentLandlordId(), id);
            if (tenant == null)
            {
                TempData["Error"] = "Không tìm thấy người thuê.";
                return RedirectToAction("Index", "Property");
            }
            return View(tenant);
        }
    }
}