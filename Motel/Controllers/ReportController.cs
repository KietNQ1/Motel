using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Motel.Helpers;
using Motel.Repositories;
using Motel.ViewModels.Report;

namespace Motel.Controllers;

[Authorize]
public class ReportController : Controller
{
    private readonly IDashboardRepository _dashboardRepo;
    private readonly LandlordHelper _landlordHelper;

    public ReportController(IDashboardRepository dashboardRepo, LandlordHelper landlordHelper)
    {
        _dashboardRepo = dashboardRepo;
        _landlordHelper = landlordHelper;
    }

    /// <summary>
    /// Doanh thu chi tiết 12 tháng (liên kết từ Dashboard)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Revenue(int? propertyId, int? year)
    {
        var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
        if (landlordId == 0)
        {
            TempData["Error"] = "Bạn không có quyền truy cập.";
            return RedirectToAction("Index", "Home");
        }

        var y = year ?? DateTime.Now.Year;
        var refDate = new DateTime(y, DateTime.Now.Month, 1);
        var chart = await _dashboardRepo.GetMonthlyRevenueDataAsync(landlordId, 12, propertyId, y);
        var properties = await _dashboardRepo.GetPropertiesAsync(landlordId);

        var months = new List<MonthRevenueRow>();
        for (int i = 0; i < chart.Revenues.Count; i++)
        {
            var d = refDate.AddMonths(-11 + i);
            months.Add(new MonthRevenueRow
            {
                Month = d.Month,
                MonthLabel = $"Tháng {d.Month}/{d.Year}",
                Revenue = chart.Revenues[i]
            });
        }

        var vm = new RevenueDetailViewModel
        {
            Year = y,
            PropertyId = propertyId,
            Properties = properties.Select(p => new PropertyItemViewModel { PropertyId = p.PropertyId, Name = p.Name }).ToList(),
            Months = months
        };

        return View(vm);
    }
}
