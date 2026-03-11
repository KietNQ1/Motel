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
        /// <param name="landlordId">ID của chủ nhà</param>
        /// <param name="propertyId">ID tòa nhà để lọc (null = tất cả)</param>
        Task<DashboardIndexViewModel> GetDashboardDataAsync(int landlordId, int? propertyId = null);
    }
}
