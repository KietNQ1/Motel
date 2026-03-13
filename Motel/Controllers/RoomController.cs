using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motel.Helpers;
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
        private readonly LandlordHelper _landlordHelper;
        private readonly IRoomFurnitureService _furnitureService;
       
        private readonly Motel.Data.MotelDbContext _db;
        
        public RoomController(IRoomRepository roomRepository, IRoomService roomService, ILogger<RoomController> logger, LandlordHelper landlordHelper, IRoomFurnitureService furnitureService, Motel.Data.MotelDbContext db)
        {
            _roomRepository = roomRepository;
            _roomService = roomService;
            _logger = logger;
            _landlordHelper = landlordHelper;
            _furnitureService = furnitureService;
            _db = db;
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

                room.Furnitures = (await _furnitureService.GetFurnituresForRoomAsync(id)).ToList();

                return View(room);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading room details");

                TempData["Error"] = "Không thể tải thông tin phòng.";

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

                room.RoomName = model.RoomName;
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

        // POST: Room/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int propertyId)
        {
            try
            {
                var success = await _roomRepository.DeleteRoomAsync(id);
                if (success)
                {
                    TempData["Success"] = "Xóa phòng thành công!";
                    return RedirectToAction("Details", "Property", new { id = propertyId });
                }
                
                TempData["Error"] = "Có lỗi xảy ra khi xóa phòng.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting room");
                TempData["Error"] = "Có lỗi xảy ra khi xóa phòng.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: Room/Restore/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id, int propertyId)
        {
            try
            {
                var success = await _roomRepository.RestoreRoomAsync(id);
                if (success)
                {
                    TempData["Success"] = "Khôi phục phòng thành công!";
                }
                else
                {
                    TempData["Error"] = "Có lỗi xảy ra khi khôi phục phòng.";
                }
                return RedirectToAction("Details", "Property", new { id = propertyId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring room");
                TempData["Error"] = "Có lỗi xảy ra khi khôi phục phòng.";
                return RedirectToAction("Details", "Property", new { id = propertyId });
            }
        }

        // GET: Room/CreateSingleRoom
        [HttpGet]
        public async Task<IActionResult> CreateSingleRoom(int propertyId, int floor)
        {
            var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
            
            // Check ownership
            var isOwner = await _db.Properties.AnyAsync(p => p.PropertyId == propertyId && p.LandlordId == landlordId && !p.IsDeleted);
            if (!isOwner)
            {
                TempData["Error"] = "Không tìm thấy nhà trọ hoặc bạn không có quyền.";
                return RedirectToAction("Index", "Property");
            }

            // Calculate suggested room name
            string suggestedName = $"{floor}01";
            var existingRoomsOnFloor = await _db.Rooms
                .Where(r => r.PropertyId == propertyId && r.RoomName.StartsWith(floor.ToString()))
                .Select(r => r.RoomName)
                .ToListAsync();

            if (existingRoomsOnFloor.Any())
            {
                int maxNum = 0;
                string floorPrefix = floor.ToString();

                foreach (var name in existingRoomsOnFloor)
                {
                    if (name.StartsWith(floorPrefix))
                    {
                        var numPart = name.Substring(floorPrefix.Length);
                        if (int.TryParse(numPart, out int parsed))
                        {
                            if (parsed > maxNum) maxNum = parsed;
                        }
                    }
                }

                int nextNum = maxNum + 1;
                suggestedName = floorPrefix + (nextNum < 10 ? "0" : "") + nextNum;
            }

            ViewBag.SuggestedRoomName = suggestedName;

            var model = new RoomCreateViewModel
            {
                PropertyId = propertyId,
                Floor = floor,
                RoomName = suggestedName, // Pre-fill it
                RentPrice = 2500000,      // Default basic rent if no others
                MaxOccupants = 2
            };

            return View(model);
        }

        // POST: Room/CreateSingleRoom
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSingleRoom(RoomCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model); // Stay on the same dedicated page
            }

            try
            {
                var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
                
                // 1. Check ownership
                var isOwner = await _db.Properties.AnyAsync(p => p.PropertyId == model.PropertyId && p.LandlordId == landlordId && !p.IsDeleted);
                if (!isOwner)
                {
                    TempData["Error"] = "Không tìm thấy nhà trọ hoặc bạn không có quyền.";
                    return RedirectToAction("Index", "Property");
                }

                // 2. Fetch default settings from the first room of the property (as fallback)
                var firstRoomSetting = await _db.RoomUtilitySettings
                    .Include(s => s.Room)
                    .Where(s => s.Room.PropertyId == model.PropertyId)
                    .OrderByDescending(s => s.EffectiveFrom)
                    .FirstOrDefaultAsync();

                // 3. Create the Room
                var newRoom = new Motel.Models.Room
                {
                    PropertyId = model.PropertyId,
                    RoomName = model.RoomName,
                    RentPrice = model.RentPrice,
                    Status = "available",
                    MaxOccupants = model.MaxOccupants,
                    IsDeleted = false
                };

                _db.Rooms.Add(newRoom);
                await _db.SaveChangesAsync();

                // 4. Create the utility settings for the room
                var setting = new Motel.Models.RoomUtilitySetting
                {
                    RoomId = newRoom.RoomId,
                    ElectricUnitPrice = firstRoomSetting?.ElectricUnitPrice ?? 0,
                    WaterUnitPrice = firstRoomSetting?.WaterUnitPrice ?? 0,
                    InternetFee = firstRoomSetting?.InternetFee ?? 0,
                    TrashFee = firstRoomSetting?.TrashFee ?? 0,
                    EffectiveFrom = DateOnly.FromDateTime(DateTime.Now),
                    EffectiveTo = null
                };

                _db.RoomUtilitySettings.Add(setting);
                await _db.SaveChangesAsync();

                TempData["Success"] = $"Thêm phòng '{model.RoomName}' thành công!";
                return RedirectToAction("Details", "Property", new { id = model.PropertyId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating single room");
                ModelState.AddModelError("", "Có lỗi xảy ra khi thêm phòng. Vui lòng thử lại.");
                return View(model); // Stay if error occurs
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
                InitialElectricReading = roomDetail.CurrentElectricNew,
                InitialWaterReading = roomDetail.CurrentWaterNew,
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
                var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);

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


        // Action cho Landlord thêm nội thất (GET/POST)
        [HttpGet]
        public IActionResult AddFurniture(int roomId)
        {
            // Kiểm tra ownership (giả sử qua service hoặc middleware)
            return View(new AddFurnitureViewModel { RoomId = roomId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFurniture(AddFurnitureViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var landlordId = GetCurrentLandlordId();

            if (!await _furnitureService.AddFurnitureAsync(model, landlordId))
            {
                ModelState.AddModelError("", "Không có quyền thêm nội thất cho phòng này.");
                return View(model);
            }

            TempData["Success"] = "Thêm nội thất thành công.";
            return RedirectToAction("Details", new { id = model.RoomId });
        }
    }
}
