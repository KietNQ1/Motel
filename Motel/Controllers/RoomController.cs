using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Motel.Repositories.Interface;
using Motel.ViewModels.Room;
using Motel.Services.Interface;

namespace Motel.Controllers
{
    // [Authorize] // TODO: Enable authorization after testing
    public class RoomController : Controller
    {
        private readonly IRoomRepository _roomRepository;
        private readonly ILogger<RoomController> _logger;
        private readonly IRoomService _roomService;
        public RoomController(IRoomRepository roomRepository, IRoomService roomService, ILogger<RoomController> logger)
        {
            _roomRepository = roomRepository;
            _roomService = roomService;
            _logger = logger;
        }

        // GET: Room/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var room = await _roomRepository.GetRoomDetailAsync(id);
                
                if (room == null)
                {
                    TempData["Error"] = "Không tìm thấy phòng.";
                    return RedirectToAction("Index", "Property");
                }

                return View(room);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading room details");
                TempData["Error"] = "Không thể tải thông tin phòng. Vui lòng thử lại sau.";
                return RedirectToAction("Index", "Property");
            }
        }

        // GET: Room/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var room = await _roomRepository.GetRoomByIdAsync(id);
                
                if (room == null)
                {
                    TempData["Error"] = "Không tìm thấy phòng.";
                    return RedirectToAction("Index", "Property");
                }

                var model = new RoomEditViewModel
                {
                    RoomId = room.RoomId,
                    RoomName = room.RoomName,
                    RentPrice = room.RentPrice,
                    MaxOccupants = room.MaxOccupants
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading room edit form");
                TempData["Error"] = "Có lỗi xảy ra. Vui lòng thử lại.";
                return RedirectToAction("Index", "Property");
            }
        }

        // POST: Room/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RoomEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var room = await _roomRepository.GetRoomByIdAsync(model.RoomId);
                
                if (room == null)
                {
                    TempData["Error"] = "Không tìm thấy phòng.";
                    return RedirectToAction("Index", "Property");
                }

                room.RentPrice = model.RentPrice;
                room.MaxOccupants = model.MaxOccupants;

                var success = await _roomRepository.UpdateRoomAsync(room);

                if (success)
                {
                    TempData["Success"] = "Cập nhật thông tin phòng thành công!";
                    return RedirectToAction(nameof(Details), new { id = model.RoomId });
                }
                else
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật. Vui lòng thử lại.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating room");
                ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật. Vui lòng thử lại.");
                return View(model);
            }
        }

        // GET: Room/Rent/5
        public async Task<IActionResult> Rent(int id)
        {
            var roomDetail = await _roomRepository.GetRoomDetailAsync(id);
            if (roomDetail == null)
            {
                TempData["Error"] = "Không tìm thấy phòng.";
                return RedirectToAction("Index", "Property");
            }

            if (roomDetail.Status != "available")
            {
                TempData["Error"] = "Phòng này không còn trống.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var model = new RentRoomViewModel
            {
                RoomId = roomDetail.RoomId,
                RoomName = roomDetail.RoomName,
                RentPrice = roomDetail.RentPrice,
                PropertyName = roomDetail.PropertyName,
                MaxOccupants = roomDetail.MaxOccupants,
                DepositAmount = roomDetail.RentPrice * 2,
                Occupants = new List<TenantInputViewModel>
        {
            new TenantInputViewModel() // 1 dòng mặc định
        },
                PrimaryIndex = 0
            };

            return View(model);
        }

        // POST: Room/Rent
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rent(RentRoomViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var landlordId = GetCurrentLandlordId();

                // TODO: khi có auth -> lấy đúng userId từ User
                var userId = 1;

                var ok = await _roomService.RentRoomAsync(model, landlordId, userId);

                if (ok)
                {
                    TempData["Success"] = $"Cho thuê phòng {model.RoomName} thành công!";
                    return RedirectToAction(nameof(Details), new { id = model.RoomId });
                }

                ModelState.AddModelError("", "Cho thuê thất bại (phòng có thể đã được thuê hoặc dữ liệu không hợp lệ).");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renting room {RoomId}", model.RoomId);
                ModelState.AddModelError("", $"Có lỗi xảy ra: {ex.Message}");
                return View(model);
            }
        }

        /// <summary>
        /// Helper: Get current landlord ID from authenticated user
        /// TODO: Implement proper authentication with claims
        /// </summary>
        private int GetCurrentLandlordId()
        {
            // TODO: Get from User.Claims when authentication is fully implemented
            return 1; // Hardcoded for development
        }
    }
}
