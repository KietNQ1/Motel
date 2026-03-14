using Microsoft.AspNetCore.Mvc;
using Motel.Helpers;
using Motel.Services.Interface;
using Motel.Services.Interfaces;
using Motel.ViewModels.Contract;

namespace Motel.Controllers
{
    public class ContractController : Controller
    {
        private readonly IContractService _service;
        private readonly ILogger<ContractController> _logger;
        private readonly LandlordHelper _landlordHelper;
        private readonly IRoomFurnitureService _furnitureService;

        public ContractController(
            IContractService service,
            ILogger<ContractController> logger,
            LandlordHelper landlordHelper,
            IRoomFurnitureService furnitureService)
        {
            _service = service;
            _logger = logger;
            _landlordHelper = landlordHelper;
            _furnitureService = furnitureService;
        }

[HttpGet]
        public async Task<IActionResult> Create(int roomId)
        {
            var vm = await _service.BuildCreateViewModelAsync(await _landlordHelper.GetCurrentLandlordIdAsync(User), roomId);
            if (vm == null)
            {
                TempData["Error"] = "Không thể tạo hợp đồng (phòng không tồn tại hoặc đã có hợp đồng active).";
                return RedirectToAction("Details", "Room", new { id = roomId });
            }
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ContractCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                // rebuild tenant list
                var rebuilt = await _service.BuildCreateViewModelAsync(await _landlordHelper.GetCurrentLandlordIdAsync(User), vm.RoomId);
                if (rebuilt != null) vm.TenantOptions = rebuilt.TenantOptions;
                return View(vm);
            }

            try
            {
                var id = await _service.CreateContractAsync(await _landlordHelper.GetCurrentLandlordIdAsync(User), vm);
                TempData["Success"] = "Tạo hợp đồng thành công!";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create contract error");

                var rebuilt = await _service.BuildCreateViewModelAsync(await _landlordHelper.GetCurrentLandlordIdAsync(User), vm.RoomId);
                if (rebuilt != null) vm.TenantOptions = rebuilt.TenantOptions;

                ModelState.AddModelError("", ex.Message);
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var (contract, occ, feeSettings) = await _service.GetContractDetailsAsync(await _landlordHelper.GetCurrentLandlordIdAsync(User), id);
            if (contract == null)
            {
                TempData["Error"] = "Không tìm thấy hợp đồng.";
                return RedirectToAction("Index", "Property");
            }

            // Fetch furniture for the contracted room
            var furnitures = await _furnitureService.GetFurnituresForRoomAsync(contract.RoomId);

            ViewBag.Occupants = occ;
            ViewBag.FeeSettings = feeSettings;
            ViewBag.Furnitures = furnitures;
            return View(contract);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> End(int contractId)
        {
            try
            {
                var ok = await _service.EndContractAsync(await _landlordHelper.GetCurrentLandlordIdAsync(User), contractId);
                if (!ok)
                {
                    TempData["Error"] = "Không thể kết thúc hợp đồng.";
                    return RedirectToAction(nameof(Details), new { id = contractId });
                }
                TempData["Success"] = "Đã kết thúc hợp đồng.";
                return RedirectToAction("Index", "Property");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "End contract error");
                TempData["Error"] = "Có lỗi khi kết thúc hợp đồng.";
                return RedirectToAction(nameof(Details), new { id = contractId });
            }
        }
    }
}