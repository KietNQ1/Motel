using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Motel.Helpers;
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
        private readonly IFeeTypeRepository _feeTypeRepository;
        private readonly IFeeSettingRepository _feeSettingRepository;
        private readonly ILogger<PropertyController> _logger;
        private readonly LandlordHelper _landlordHelper;

        public PropertyController(
            IPropertyRepository propertyRepository,
            IFeeTypeRepository feeTypeRepository,
            IFeeSettingRepository feeSettingRepository,
            ILogger<PropertyController> logger,
            LandlordHelper landlordHelper)
        {
            _propertyRepository = propertyRepository;
            _feeTypeRepository = feeTypeRepository;
            _feeSettingRepository = feeSettingRepository;
            _logger = logger;
            _landlordHelper = landlordHelper;
        }

        // GET: Property/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
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
        public async Task<IActionResult> Create()
        {
            var feeTypes = await _feeTypeRepository.GetAllAsync();
            var model = new PropertyCreateViewModel();

            // Only pre-populate Electricity and Water – these are mandatory for most motels.
            // Internet, Garbage, etc. can be added later via Settings page.
            var mandatoryNames = new[] { "Electricity", "Water" };
            foreach (var feeType in feeTypes.Where(f => f.IsSystem && mandatoryNames.Contains(f.Name)))
            {
                model.FeeSettings.Add(new PropertyFeeSettingViewModel
                {
                    FeeTypeId = feeType.FeeTypeId,
                    FeeTypeName = feeType.Name,
                    IsSelected = true,
                    CalculationMethod = "meter",
                    UnitPrice = feeType.Name == "Electricity" ? 3500 : 15000,
                    BaseAmount = 0
                });
            }

            return View(model);
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
                var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);

                var property = new Property
                {
                    LandlordId = landlordId,
                    Name = model.Name,
                    Address = model.Address,
                    Description = model.Description,
                    IsDeleted = false
                };

                var propertyId = await _propertyRepository.CreatePropertyAsync(property);

                // Save dynamic fee settings
                if (model.FeeSettings != null && model.FeeSettings.Any())
                {
                    foreach (var feeSettingModel in model.FeeSettings.Where(f => f.IsSelected))
                    {
                        int actualFeeTypeId = feeSettingModel.FeeTypeId;

                        // If FeeTypeId is 0, it means it's a newly added custom fee
                        if (actualFeeTypeId == 0 && !string.IsNullOrWhiteSpace(feeSettingModel.FeeTypeName))
                        {
                            var newFeeType = new FeeType
                            {
                                Name = feeSettingModel.FeeTypeName,
                                Unit = feeSettingModel.Unit ?? "tháng",
                                Description = "Phí tự định nghĩa",
                                IsSystem = false
                            };
                            await _feeTypeRepository.AddAsync(newFeeType);
                            actualFeeTypeId = newFeeType.FeeTypeId;
                        }

                        var feeSetting = new FeeSetting
                        {
                            PropertyId = propertyId,
                            RoomId = null,
                            FeeTypeId = actualFeeTypeId,
                            CalculationMethod = feeSettingModel.CalculationMethod,
                            UnitPrice = feeSettingModel.UnitPrice,
                            BaseAmount = feeSettingModel.BaseAmount,
                            EffectiveFrom = DateOnly.FromDateTime(DateTime.Now),
                            CreatedAt = DateTime.Now
                        };
                        await _feeSettingRepository.AddAsync(feeSetting);
                    }
                }

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
                var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
                
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
                var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
                
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

                // Create rooms without passing utility parameters (handled at property level now)
                var success = await _propertyRepository.CreateRoomsAsync(
                    model.PropertyId,
                    rooms
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
                var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
                
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


        // GET: Property/Settings/5
        public async Task<IActionResult> Settings(int id)
        {
            try
            {
                var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
                
                if (!await _propertyRepository.PropertyExistsAsync(id, landlordId))
                {
                    TempData["Error"] = "Không tìm thấy nhà trọ.";
                    return RedirectToAction(nameof(Index));
                }

                var property = await _propertyRepository.GetPropertyByIdAsync(id);
                if (property == null) return NotFound();

                var feeSettings = await _feeSettingRepository.GetPropertyLevelFeeSettingsAsync(id);
                var feeTypes = await _feeTypeRepository.GetAllAsync();
                ViewBag.FeeTypes = feeTypes;

                var model = new PropertySettingsViewModel
                {
                    PropertyId = property.PropertyId,
                    Name = property.Name,
                    Address = property.Address,
                    Description = property.Description,
                    FeeSettings = feeSettings.Select(f => new PropertyFeeSettingItemViewModel
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
                _logger.LogError(ex, "Error loading property settings");
                TempData["Error"] = "Không thể tải cấu hình nhà trọ. Vui lòng thử lại sau.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: Property/UpdateSettings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSettings(PropertySettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var feeSettings = await _feeSettingRepository.GetPropertyLevelFeeSettingsAsync(model.PropertyId);
                var feeTypes = await _feeTypeRepository.GetAllAsync();
                ViewBag.FeeTypes = feeTypes;
                model.FeeSettings = feeSettings.Select(f => new PropertyFeeSettingItemViewModel
                {
                    FeeSettingId = f.FeeSettingId,
                    FeeTypeId = f.FeeTypeId,
                    FeeTypeName = f.FeeType?.Name ?? "Unknown",
                    CalculationMethod = f.CalculationMethod,
                    UnitPrice = f.UnitPrice,
                    BaseAmount = f.BaseAmount,
                    EffectiveFrom = f.EffectiveFrom,
                    EffectiveTo = f.EffectiveTo
                }).ToList();
                return View("Settings", model);
            }

            try
            {
                var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
                if (!await _propertyRepository.PropertyExistsAsync(model.PropertyId, landlordId))
                {
                    return NotFound();
                }

                var property = await _propertyRepository.GetPropertyByIdAsync(model.PropertyId);
                if (property == null) return NotFound();

                property.Name = model.Name;
                property.Address = model.Address;
                property.Description = model.Description;

                var success = await _propertyRepository.UpdatePropertyAsync(property);
                if (success)
                {
                    TempData["Success"] = "Cập nhật thông tin nhà trọ thành công!";
                }
                else
                {
                    TempData["Error"] = "Không có thay đổi nào được lưu.";
                }

                return RedirectToAction(nameof(Settings), new { id = model.PropertyId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating property info");
                TempData["Error"] = "Đã xảy ra lỗi khi cập nhật.";
                return RedirectToAction(nameof(Settings), new { id = model.PropertyId });
            }
        }

        // POST: Property/AddFeeSetting
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFeeSetting(int propertyId, int feeTypeId, string newFeeTypeName, string calculationMethod, decimal unitPrice, decimal baseAmount)
        {
            try
            {
                var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
                 if (!await _propertyRepository.PropertyExistsAsync(propertyId, landlordId))
                {
                    return NotFound();
                }

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

                var feeSetting = new FeeSetting
                {
                    PropertyId = propertyId,
                    RoomId = null,
                    FeeTypeId = actualFeeTypeId,
                    CalculationMethod = calculationMethod,
                    UnitPrice = calculationMethod == "meter" || calculationMethod == "per_person" ? unitPrice : 0,
                    BaseAmount = calculationMethod == "per_room" || calculationMethod == "per_person" || calculationMethod == "fixed" ? baseAmount : 0,
                    EffectiveFrom = DateOnly.FromDateTime(DateTime.Today),
                    CreatedAt = DateTime.Now
                };

                await _feeSettingRepository.AddAsync(feeSetting);
                TempData["Success"] = "Thêm phụ phí thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding fee setting");
                TempData["Error"] = "Lỗi khi thêm phụ phí.";
            }

            return RedirectToAction(nameof(Settings), new { id = propertyId });
        }

        // POST: Property/EditFeeSetting
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFeeSetting(int feeSettingId, int propertyId, int feeTypeId, string calculationMethod, decimal unitPrice, decimal baseAmount)
        {
            try
            {
                var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
                if (!await _propertyRepository.PropertyExistsAsync(propertyId, landlordId))
                {
                    return NotFound();
                }

                // Invalidate the old setting
                var today = DateOnly.FromDateTime(DateTime.Today);
                await _feeSettingRepository.InvalidateFeeSettingAsync(feeSettingId, today);

                // Create the new setting
                var feeSetting = new FeeSetting
                {
                    PropertyId = propertyId,
                    RoomId = null,
                    FeeTypeId = feeTypeId,
                    CalculationMethod = calculationMethod,
                    UnitPrice = calculationMethod == "meter" || calculationMethod == "per_person" ? unitPrice : 0,
                    BaseAmount = calculationMethod == "per_room" || calculationMethod == "per_person" || calculationMethod == "fixed" ? baseAmount : 0,
                    EffectiveFrom = today,
                    CreatedAt = DateTime.Now
                };

                await _feeSettingRepository.AddAsync(feeSetting);
                TempData["Success"] = "Cập nhật phụ phí thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing fee setting");
                TempData["Error"] = "Lỗi khi cập nhật phụ phí.";
            }

            return RedirectToAction(nameof(Settings), new { id = propertyId });
        }

        // POST: Property/DeleteFeeSetting
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFeeSetting(int feeSettingId, int propertyId)
        {
            try
            {
                var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
                if (!await _propertyRepository.PropertyExistsAsync(propertyId, landlordId))
                {
                    return NotFound();
                }

                var today = DateOnly.FromDateTime(DateTime.Today);
                await _feeSettingRepository.InvalidateFeeSettingAsync(feeSettingId, today);
                TempData["Success"] = "Xóa phụ phí thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting fee setting");
                TempData["Error"] = "Lỗi khi xóa phụ phí.";
            }

            return RedirectToAction(nameof(Settings), new { id = propertyId });
        }

    }
}
