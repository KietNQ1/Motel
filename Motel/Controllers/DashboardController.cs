using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Services;

namespace Motel.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<DashboardController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly MotelDbContext _context;

        public DashboardController(
            IDashboardService dashboardService,
            ILogger<DashboardController> logger,
            UserManager<ApplicationUser> userManager,
            MotelDbContext context)
        {
            _dashboardService = dashboardService;
            _logger = logger;
            _userManager = userManager;
            _context = context;
        }

        /// <summary>
        /// Dashboard Index - Trang chủ với tất cả metrics
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var landlordId = await GetCurrentLandlordIdAsync();
                
                if (landlordId == 0)
                {
                    _logger.LogWarning("User {Email} attempted to access dashboard but has no landlord profile", User.Identity?.Name);
                    TempData["Error"] = "Bạn không có quyền truy cập trang này. Chỉ chủ trọ mới có thể xem dashboard.";
                    return RedirectToAction("Index", "Home");
                }
                
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
        /// </summary>
        private async Task<int> GetCurrentLandlordIdAsync()
        {
            if (!User.Identity?.IsAuthenticated ?? false)
                return 0;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return 0;

            var landlord = await _context.Landlords
                .Where(l => l.UserId == currentUser.Id && !l.IsDeleted)
                .FirstOrDefaultAsync();

            return landlord?.LandlordId ?? 0;
        }
    }
}
