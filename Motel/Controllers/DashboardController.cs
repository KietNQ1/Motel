using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Motel.Helpers;
using Motel.Services;

namespace Motel.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;
        private readonly LandlordHelper _landlordHelper;

        public DashboardController(
            IDashboardService dashboardService,
            ILogger<DashboardController> logger,
            LandlordHelper landlordHelper)
        {
            _dashboardService = dashboardService;
            _logger = logger;
            _landlordHelper = landlordHelper;
        }

        /// <summary>
        /// Dashboard Index - Trang chủ với tất cả metrics
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int? propertyId = null)
        {
            try
            {
                var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
                
                if (landlordId == 0)
                {
                    _logger.LogWarning("User {Email} attempted to access dashboard but has no landlord profile", User.Identity?.Name);
                    TempData["Error"] = "Bạn không có quyền truy cập trang này. Chỉ chủ trọ mới có thể xem dashboard.";
                    return RedirectToAction("Index", "Home");
                }
                
                var viewModel = await _dashboardService.GetDashboardDataAsync(landlordId, propertyId);
                
                // Store selected propertyId to preserve dropdown selection
                ViewBag.SelectedPropertyId = propertyId;
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["Error"] = "Không thể tải dữ liệu dashboard. Vui lòng thử lại sau.";
                
                return View(new Motel.ViewModels.Dashboard.DashboardIndexViewModel());
            }
        }
    }
}
