using Motel.ViewModels.Dashboard;

namespace Motel.Services
{
    /// <summary>
    /// Service interface cho Dashboard - business logic layer
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// Lấy tất cả dữ liệu cần thiết cho Dashboard
        /// </summary>
        Task<DashboardIndexViewModel> GetDashboardDataAsync(int landlordId);
    }
}
