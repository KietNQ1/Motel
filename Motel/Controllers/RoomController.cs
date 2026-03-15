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
        private readonly MotelDbContext _context;


        public RoomController(IRoomRepository roomRepository, IRoomService roomService, ILogger<RoomController> logger, LandlordHelper landlordHelper,IRoomFurnitureService furnitureService, MotelDbContext context)
        {
            _roomRepository = roomRepository;
            _roomService = roomService;
            _logger = logger;
            _landlordHelper = landlordHelper;
            _furnitureService = furnitureService;
            _context = context;
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
            var catalogs = await _context.FurnitureCatalogs.ToListAsync();
            var statuses = await _context.FurnitureStatuses.ToListAsync();

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

                _context.FurnitureCatalogs.Add(newCatalog);
                await _context.SaveChangesAsync();

                var defaultStatusId = await _context.FurnitureStatuses
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
            model.Catalogs = await _context.FurnitureCatalogs.ToListAsync();
            model.Statuses = await _context.FurnitureStatuses.ToListAsync();

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

            var furniture = await _context.RoomFurnitures
                .FirstOrDefaultAsync(x => x.FurnitureId == id && !x.IsDeleted);

            if (furniture == null)
                return NotFound();

            var images = await _context.StoredFileReferences
                .Where(x => x.RefType == "roomfurniture" && x.RefId == id)
                .Include(x => x.StoredFile)
                .ToListAsync();

            var catalogs = await _context.FurnitureCatalogs
                .Where(x => x.IsActive)
                .ToListAsync();
            var statuses = await _context.FurnitureStatuses.ToListAsync();
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
            model.Catalogs = await _context.FurnitureCatalogs
                .Where(x => x.IsActive)
                .ToListAsync();

            model.Statuses = await _context.FurnitureStatuses.ToListAsync();

            if (model.FurnitureId > 0)
            {
                var images = await _context.StoredFileReferences
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
