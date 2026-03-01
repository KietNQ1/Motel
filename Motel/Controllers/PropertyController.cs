using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Motel.Models;
using Motel.Repositories.Interface;
using Motel.ViewModels.Property;
using Newtonsoft.Json;

namespace Motel.Controllers
{
    // [Authorize] // TODO: Enable authorization after testing
    public class PropertyController : Controller
    {
        private readonly IPropertyRepository _propertyRepository;
        private readonly ILogger<PropertyController> _logger;

        public PropertyController(
            IPropertyRepository propertyRepository,
            ILogger<PropertyController> logger)
        {
            _propertyRepository = propertyRepository;
            _logger = logger;
        }

        // GET: Property/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                var landlordId = GetCurrentLandlordId();
                var properties = await _propertyRepository.GetPropertiesByLandlordIdAsync(landlordId);
                return View(properties);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading properties");
                TempData["Error"] = "Không thể tải danh sách nhà trọ. Vui lòng thử lại sau.";
                return View(new List<PropertyListViewModel>());
            }
        }

        // GET: Property/Create
        public IActionResult Create()
        {
            return View(new PropertyCreateViewModel());
        }

        // POST: Property/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PropertyCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var landlordId = GetCurrentLandlordId();

                var property = new Property
                {
                    LandlordId = landlordId,
                    Name = model.Name,
                    Address = model.Address,
                    Description = model.Description,
                    IsDeleted = false
                };

                var propertyId = await _propertyRepository.CreatePropertyAsync(property);

                // Store utility settings in TempData for use in ConfigureRooms
                var utilitySettings = new
                {
                    ElectricUnitPrice = model.ElectricUnitPrice,
                    WaterUnitPrice = model.WaterUnitPrice,
                    InternetFee = model.InternetFee,
                    TrashFee = model.TrashFee
                };
                TempData["UtilitySettings"] = JsonConvert.SerializeObject(utilitySettings);

                TempData["Success"] = $"Tạo nhà trọ '{model.Name}' thành công! Vui lòng cấu hình phòng.";
                return RedirectToAction(nameof(ConfigureRooms), new { id = propertyId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating property");
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo nhà trọ. Vui lòng thử lại.");
                return View(model);
            }
        }

        // GET: Property/ConfigureRooms/5
        public async Task<IActionResult> ConfigureRooms(int id)
        {
            try
            {
                var landlordId = GetCurrentLandlordId();
                
                if (!await _propertyRepository.PropertyExistsAsync(id, landlordId))
                {
                    TempData["Error"] = "Không tìm thấy nhà trọ.";
                    return RedirectToAction(nameof(Index));
                }

                var property = await _propertyRepository.GetPropertyByIdAsync(id);
                if (property == null)
                {
                    TempData["Error"] = "Không tìm thấy nhà trọ.";
                    return RedirectToAction(nameof(Index));
                }

                var model = new RoomStructureViewModel
                {
                    PropertyId = id,
                    PropertyName = property.Name,
                    NumberOfFloors = 1,
                    RoomsPerFloor = new List<int> { 5 }
                };

                // Retrieve utility settings from TempData
                if (TempData["UtilitySettings"] != null)
                {
                    var utilityJson = TempData["UtilitySettings"]?.ToString();
                    if (!string.IsNullOrEmpty(utilityJson))
                    {
                        var utility = JsonConvert.DeserializeObject<dynamic>(utilityJson);
                        model.ElectricUnitPrice = utility.ElectricUnitPrice;
                        model.WaterUnitPrice = utility.WaterUnitPrice;
                        model.InternetFee = utility.InternetFee;
                        model.TrashFee = utility.TrashFee;
                    }
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ConfigureRooms");
                TempData["Error"] = "Có lỗi xảy ra. Vui lòng thử lại.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Property/ConfigureRooms
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfigureRooms(RoomStructureViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var landlordId = GetCurrentLandlordId();
                
                if (!await _propertyRepository.PropertyExistsAsync(model.PropertyId, landlordId))
                {
                    TempData["Error"] = "Không tìm thấy nhà trọ.";
                    return RedirectToAction(nameof(Index));
                }

                // Validate rooms per floor
                if (model.RoomsPerFloor == null || model.RoomsPerFloor.Count != model.NumberOfFloors)
                {
                    ModelState.AddModelError("", "Số phòng mỗi tầng không hợp lệ.");
                    return View(model);
                }

                // Generate room list with auto-numbering
                var rooms = new List<(string RoomName, decimal RentPrice, int MaxOccupants)>();
                
                for (int floor = 1; floor <= model.NumberOfFloors; floor++)
                {
                    int roomsOnFloor = model.RoomsPerFloor[floor - 1];
                    
                    for (int roomNum = 1; roomNum <= roomsOnFloor; roomNum++)
                    {
                        // Format: Floor number + Room number (e.g., 301, 201)
                        string roomName = $"{floor}{roomNum:D2}";
                        rooms.Add((roomName, model.DefaultRentPrice, 2)); // Default 2 occupants
                    }
                }

                // Create rooms with utility settings
                var success = await _propertyRepository.CreateRoomsWithUtilitiesAsync(
                    model.PropertyId,
                    rooms,
                    model.ElectricUnitPrice,
                    model.WaterUnitPrice,
                    model.InternetFee,
                    model.TrashFee
                );

                if (success)
                {
                    TempData["Success"] = $"Đã tạo {rooms.Count} phòng thành công!";
                    return RedirectToAction(nameof(Details), new { id = model.PropertyId });
                }
                else
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi tạo phòng. Vui lòng thử lại.");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring rooms");
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo phòng. Vui lòng thử lại.");
                return View(model);
            }
        }

        // GET: Property/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var landlordId = GetCurrentLandlordId();
                
                if (!await _propertyRepository.PropertyExistsAsync(id, landlordId))
                {
                    TempData["Error"] = "Không tìm thấy nhà trọ.";
                    return RedirectToAction(nameof(Index));
                }

                var property = await _propertyRepository.GetPropertyDetailsAsync(id);
                if (property == null)
                {
                    TempData["Error"] = "Không tìm thấy nhà trọ.";
                    return RedirectToAction(nameof(Index));
                }

                return View(property);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading property details");
                TempData["Error"] = "Không thể tải thông tin nhà trọ. Vui lòng thử lại sau.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Helper: Get current landlord ID from authenticated user
        /// TODO: Implement proper authentication with claims
        /// </summary>
        private int GetCurrentLandlordId()
        {
            // TODO: Get from User.Claims when authentication is fully implemented
            // Example: return int.Parse(User.FindFirst("LandlordId")?.Value ?? "0");
            return 1; // Hardcoded for development (landlord1@motel.local)
        }
    }
}
