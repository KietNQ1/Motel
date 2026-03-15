using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Helpers;
using Motel.Models;
using Motel.Repositories;
using Motel.Repositories.Interface;
using Motel.Services.Interface;
using Motel.ViewModels.Common;
using Motel.ViewModels.Room;

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
        private readonly IFeeSettingRepository _feeSettingRepository;
        private readonly IFeeTypeRepository _feeTypeRepository;
        
        public RoomController(
            IRoomRepository roomRepository, 
            IRoomService roomService, 
            ILogger<RoomController> logger, 
            LandlordHelper landlordHelper, 
            IRoomFurnitureService furnitureService, 
            Motel.Data.MotelDbContext db,
            IFeeSettingRepository feeSettingRepository,
            IFeeTypeRepository feeTypeRepository)
        {
            _roomRepository = roomRepository;
            _roomService = roomService;
            _logger = logger;
            _landlordHelper = landlordHelper;
            _furnitureService = furnitureService;
            _db = db;
            _feeSettingRepository = feeSettingRepository;
            _feeTypeRepository = feeTypeRepository;
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

                // Use the same effective-fee logic as InvoiceController: room overrides property automatically
                var now = DateTime.Now;
                var yyyymm = now.Year * 100 + now.Month;
                var effectiveFees = await _feeSettingRepository.GetEffectiveForRoomAsync(room.PropertyId, id, yyyymm);

                // Track which feeTypeIds are room-level overrides (for UI badge)
                var roomFeeTypeIds = effectiveFees
                    .Where(f => f.RoomId.HasValue)
                    .Select(f => f.FeeTypeId)
                    .ToHashSet();

                // Update well-known ViewModel fields for backward compat (EstimatedTotal etc.)
                foreach (var fee in effectiveFees)
                {
                    switch (fee.FeeType?.Name?.ToLower())
                    {
                        case "electricity":
                            room.ElectricUnitPrice = fee.CalculationMethod == "meter" ? fee.UnitPrice : fee.BaseAmount;
                            break;
                        case "water":
                            room.WaterUnitPrice = fee.CalculationMethod == "meter" ? fee.UnitPrice : fee.BaseAmount;
                            break;
                        case "internet":
                            room.InternetFee = fee.BaseAmount > 0 ? fee.BaseAmount : fee.UnitPrice;
                            break;
                        case "garbage": case "rác": case "trash":
                            room.TrashFee = fee.BaseAmount > 0 ? fee.BaseAmount : fee.UnitPrice;
                            break;
                    }
                }

                ViewBag.EffectiveFees = effectiveFees;
                ViewBag.RoomFeeTypeIds = roomFeeTypeIds;
                ViewBag.OccupantCount = room.Tenants.Count > 0 ? room.Tenants.Count : 1;

                return View(room);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading room details");
                TempData["Error"] = "Không thể tải thông tin phòng.";
                return RedirectToAction("Index", "Property");
            }
        }

        [HttpGet]
        public async Task<IActionResult> DetailFurniture(int id)
        {
            var room = await _roomRepository.GetRoomDetailAsync(id);

            if (room == null)
            {
                TempData["Error"] = "Không tìm thấy phòng.";
                return RedirectToAction("Index", "Property");
            }

            var furnitures = await _furnitureService.GetFurnituresForRoomAsync(id);

            var model = new DetailFurnitureViewModel
            {
                RoomId = id,
                RoomName = room.RoomName,
                PropertyName = room.PropertyName,
                Furnitures = furnitures.ToList()
            };

            return View(model);
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

                var propertySettings = await _feeSettingRepository.GetPropertyLevelFeeSettingsAsync(room.PropertyId);
                var roomSettings = await _feeSettingRepository.GetRoomLevelFeeSettingsAsync(id);

                var feeTypes = await _feeTypeRepository.GetAllAsync();
                ViewBag.FeeTypes = feeTypes;

                var model = new RoomEditViewModel
                {
                    RoomId = room.RoomId,
                    RoomName = room.RoomName,
                    RentPrice = room.RentPrice,
                    MaxOccupants = room.MaxOccupants,
                    PropertyId = room.PropertyId,
                    PropertyName = room.Property?.Name ?? "Nhà trọ", // Use navigation property or fallback
                    PropertyLevelFeeSettings = propertySettings.Select(f => new Motel.ViewModels.Property.PropertyFeeSettingItemViewModel
                    {
                        FeeSettingId = f.FeeSettingId,
                        FeeTypeId = f.FeeTypeId,
                        FeeTypeName = f.FeeType?.Name ?? "Unknown",
                        CalculationMethod = f.CalculationMethod,
                        UnitPrice = f.UnitPrice,
                        BaseAmount = f.BaseAmount,
                        EffectiveFrom = f.EffectiveFrom,
                        EffectiveTo = f.EffectiveTo
                    }).ToList(),
                    RoomLevelFeeSettings = roomSettings.Select(f => new Motel.ViewModels.Property.PropertyFeeSettingItemViewModel
                    {
                        FeeSettingId = f.FeeSettingId,
                        FeeTypeId = f.FeeTypeId,
                        FeeTypeName = f.FeeType?.Name ?? "Unknown",
                        CalculationMethod = f.CalculationMethod,
                        UnitPrice = f.UnitPrice,
                        BaseAmount = f.BaseAmount,
                        EffectiveFrom = f.EffectiveFrom,
                        EffectiveTo = f.EffectiveTo
                    }).ToList()
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
                    return RedirectToAction(nameof(Edit), new { id = model.RoomId });
                }
                else
                {
                    TempData["Error"] = "Không có thay đổi nào được lưu.";
                    return RedirectToAction(nameof(Edit), new { id = model.RoomId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating room");
                TempData["Error"] = "Đã xảy ra lỗi khi cập nhật.";
                return RedirectToAction(nameof(Edit), new { id = model.RoomId });
            }
        }

        // POST: Room/AddFeeSetting
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFeeSetting(int roomId, int propertyId, int feeTypeId, string newFeeTypeName, string calculationMethod, decimal unitPrice, decimal baseAmount)
        {
            try
            {
                int actualFeeTypeId = feeTypeId;
                if (feeTypeId == 0 && !string.IsNullOrWhiteSpace(newFeeTypeName))
                {
                    var newFeeType = new FeeType
                    {
                        Name = newFeeTypeName,
                        Unit = "tháng",
                        Description = "Phí tự định nghĩa",
                        IsSystem = false
                    };
                    await _feeTypeRepository.AddAsync(newFeeType);
                    actualFeeTypeId = newFeeType.FeeTypeId;
                }

                // CHECK CONSTRAINT: room-level fee must have PropertyId=NULL, RoomId=roomId
                var feeSetting = new FeeSetting
                {
                    PropertyId = null,   // MUST be null for room-level fee
                    RoomId = roomId,
                    FeeTypeId = actualFeeTypeId,
                    CalculationMethod = calculationMethod,
                    UnitPrice = (calculationMethod == "meter" || calculationMethod == "per_person") ? unitPrice : 0,
                    BaseAmount = (calculationMethod == "per_room" || calculationMethod == "per_person" || calculationMethod == "fixed") ? baseAmount : 0,
                    EffectiveFrom = DateOnly.FromDateTime(DateTime.Today),
                    CreatedAt = DateTime.Now
                };

                await _feeSettingRepository.AddAsync(feeSetting);
                TempData["Success"] = "Thêm phụ phí cho phòng thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding room fee setting");
                TempData["Error"] = "Lỗi khi thêm phụ phí: " + ex.Message;
            }

            return RedirectToAction(nameof(Edit), new { id = roomId });
        }

        // POST: Room/EditFeeSetting
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFeeSetting(int feeSettingId, int roomId, int propertyId, int feeTypeId, string calculationMethod, decimal unitPrice, decimal baseAmount)
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Today);

                // Invalidate the old setting (Case 2: room already has override)
                // Case 1: room has no override yet => feeSettingId == 0, just create new
                if (feeSettingId > 0)
                {
                    await _feeSettingRepository.InvalidateFeeSettingAsync(feeSettingId, today);
                }

                // CHECK CONSTRAINT: room-level fee must have PropertyId=NULL, RoomId=roomId
                var feeSetting = new FeeSetting
                {
                    PropertyId = null,   // MUST be null for room-level fee
                    RoomId = roomId,
                    FeeTypeId = feeTypeId,
                    CalculationMethod = calculationMethod,
                    UnitPrice = (calculationMethod == "meter" || calculationMethod == "per_person") ? unitPrice : 0,
                    BaseAmount = (calculationMethod == "per_room" || calculationMethod == "per_person" || calculationMethod == "fixed") ? baseAmount : 0,
                    EffectiveFrom = today,
                    CreatedAt = DateTime.Now
                };

                await _feeSettingRepository.AddAsync(feeSetting);
                TempData["Success"] = "Cập nhật phụ phí phòng thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing room fee setting");
                TempData["Error"] = "Lỗi khi cập nhật phụ phí: " + ex.Message;
            }

            return RedirectToAction(nameof(Edit), new { id = roomId });
        }

        // POST: Room/DeleteFeeSetting
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFeeSetting(int feeSettingId, int roomId)
        {
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Today);
                await _feeSettingRepository.InvalidateFeeSettingAsync(feeSettingId, today);
                TempData["Success"] = "Xóa phụ phí phòng thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting room fee setting");
                TempData["Error"] = "Lỗi khi xóa phụ phí.";
            }

            return RedirectToAction(nameof(Edit), new { id = roomId });
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

                // 2. Create the Room
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
        public async Task<IActionResult> AddFurniture(int roomId)
        {
            var catalogs = await _db.FurnitureCatalogs.ToListAsync();
            var statuses = await _db.FurnitureStatuses.ToListAsync();

            var model = new AddFurnitureViewModel
            {
                RoomId = roomId,
                Catalogs = catalogs,
                Statuses = statuses,
                Furnitures = catalogs.Select(c => new FurnitureRowVM
                {
                    FurnitureCatalogId = c.FurnitureCatalogId
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFurniture(AddFurnitureViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateAddFurnitureViewModel(model);
                return View(model);
            }

            var landlordId = GetCurrentLandlordId();

            // Add selected existing furniture
            foreach (var item in model.Furnitures)
            {
                if (!item.Selected)
                    continue;

                var result = await _furnitureService.AddFurnitureAsync(
                    item,
                    model.RoomId,
                    landlordId
                );

                if (!result)
                {
                    ModelState.AddModelError("", "Không có quyền thêm nội thất cho phòng này.");
                    await PopulateAddFurnitureViewModel(model);
                    return View(model);
                }
            }

            // Add a new furniture item typed in by the landlord
            if (!string.IsNullOrWhiteSpace(model.NewFurnitureName))
            {
                // Create a new catalog entry for the custom furniture
                var newCatalog = new FurnitureCatalog
                {
                    Name = model.NewFurnitureName.Trim(),
                    IsActive = true
                };

                _db.FurnitureCatalogs.Add(newCatalog);
                await _db.SaveChangesAsync();

                var defaultStatusId = await _db.FurnitureStatuses
                    .Select(s => s.FurnitureStatusId)
                    .FirstOrDefaultAsync();

                var newFurnitureRow = new FurnitureRowVM
                {
                    FurnitureCatalogId = newCatalog.FurnitureCatalogId,
                    Quantity = model.NewFurnitureQuantity ?? 1,
                    FurnitureStatusId = model.NewFurnitureStatusId ?? defaultStatusId,
                    Images = model.NewFurnitureImages ?? new List<IFormFile>()
                };

                var addResult = await _furnitureService.AddFurnitureAsync(
                    newFurnitureRow,
                    model.RoomId,
                    landlordId
                );

                if (!addResult)
                {
                    ModelState.AddModelError("", "Không thể thêm nội thất mới.");
                    await PopulateAddFurnitureViewModel(model);
                    return View(model);
                }
            }

            TempData["Success"] = "Thêm nội thất thành công.";
            return RedirectToAction("Details", new { id = model.RoomId });
        }

        private async Task PopulateAddFurnitureViewModel(AddFurnitureViewModel model)
        {
            model.Catalogs = await _db.FurnitureCatalogs.ToListAsync();
            model.Statuses = await          _db.FurnitureStatuses.ToListAsync();

            if (model.Furnitures == null || !model.Furnitures.Any())
            {
                model.Furnitures = model.Catalogs.Select(c => new FurnitureRowVM
                {
                    FurnitureCatalogId = c.FurnitureCatalogId
                }).ToList();
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditFurniture(int id)
        {
            int landlordId = 1;

            var furniture = await _db.RoomFurnitures
                .FirstOrDefaultAsync(x => x.FurnitureId == id && !x.IsDeleted);

            if (furniture == null)
                return NotFound();

            var images = await _db.StoredFileReferences
                .Where(x => x.RefType == "roomfurniture" && x.RefId == id)
                .Include(x => x.StoredFile)
                .ToListAsync();

            var catalogs = await _db.FurnitureCatalogs
                .Where(x => x.IsActive)
                .ToListAsync();
            var statuses = await _db.FurnitureStatuses.ToListAsync();
            var model = new UpdateFurnitureViewModel
            {
                FurnitureId = furniture.FurnitureId,
                RoomId = furniture.RoomId,
                FurnitureCatalogId = furniture.FurnitureCatalogId,
                Quantity = furniture.Quantity,
                Description = furniture.Description,
                Catalogs = catalogs,
                Statuses = statuses,
                ExistingImages = images
                    .Select(x => x.StoredFile)
                    .ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFurniture(UpdateFurnitureViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateEditFurnitureViewModel(model);
                return View(model);
            }

            var landlordId = GetCurrentLandlordId();

            var success = await _furnitureService.UpdateFurnitureAsync(model, landlordId);

            if (!success)
            {
                ModelState.AddModelError("", "Không thể cập nhật nội thất.");
                await PopulateEditFurnitureViewModel(model);
                return View(model);
            }

            TempData["Success"] = "Cập nhật nội thất thành công.";

            return RedirectToAction("Details", new { id = model.RoomId });
        }

        private async Task PopulateEditFurnitureViewModel(UpdateFurnitureViewModel model)
        {
            model.Catalogs = await _db.FurnitureCatalogs
                .Where(x => x.IsActive)
                .ToListAsync();

            model.Statuses = await _db.FurnitureStatuses.ToListAsync();

            if (model.FurnitureId > 0)
            {
                var images = await _db.StoredFileReferences
                    .Where(x => x.RefType == "roomfurniture" && x.RefId == model.FurnitureId)
                    .Include(x => x.StoredFile)
                    .ToListAsync();

                model.ExistingImages = images
                    .Select(x => x.StoredFile)
                    .ToList();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFurniture(int furnitureId, int roomId)
        {
            var landlordId = GetCurrentLandlordId();

            var success = await _furnitureService.DeleteFurnitureAsync(furnitureId, landlordId);

            if (!success)
            {
                TempData["Error"] = "Không thể xóa nội thất.";
            }
            else
            {
                TempData["Success"] = "Xóa nội thất thành công.";
            }

            return RedirectToAction("Details", new { id = roomId });
        }
    }
}
