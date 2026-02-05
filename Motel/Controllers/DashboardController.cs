using Microsoft.AspNetCore.Mvc;
using Motel.Services;

namespace Motel.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            IDashboardService dashboardService,
            ILogger<DashboardController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        /// <summary>
        /// Dashboard Index - Trang chủ với tất cả metrics
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var landlordId = GetCurrentLandlordId();
                var viewModel = await _dashboardService.GetDashboardDataAsync(landlordId);
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["Error"] = "Không thể tải dữ liệu dashboard. Vui lòng thử lại sau.";
                
                return View(new Motel.ViewModels.Dashboard.DashboardIndexViewModel());
            }
        }

        /// <summary>
        /// Helper: Lấy LandlordId từ authenticated user
        /// TODO: Implement proper authentication
        /// </summary>
        private int GetCurrentLandlordId()
        {
            // TODO: Get from User.Claims when authentication is implemented
            // Example: return int.Parse(User.FindFirst("LandlordId")?.Value ?? "0");
            return 1; // Hardcoded for development
        }
    }
}
