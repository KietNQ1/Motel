using Motel.ViewModels.Dashboard;

namespace Motel.Repositories
{
    /// <summary>
    /// Repository interface cho Dashboard - data access layer
    /// </summary>
    public interface IDashboardRepository
    {
        /// <summary>
        /// Lấy thống kê phòng trọ
        /// </summary>
        Task<RoomStatisticsViewModel> GetRoomStatisticsAsync(int landlordId, int? propertyId = null);
        
        /// <summary>
        /// Lấy thống kê tài chính tháng hiện tại
        /// </summary>
        Task<FinancialStatisticsViewModel> GetFinancialStatisticsAsync(int landlordId, int? propertyId = null);
        
        /// <summary>
        /// Lấy danh sách hóa đơn gần đây
        /// </summary>
        Task<List<RecentInvoiceViewModel>> GetRecentInvoicesAsync(int landlordId, int count, int? propertyId = null);
        
        /// <summary>
        /// Lấy danh sách hợp đồng sắp hết hạn
        /// </summary>
        Task<List<ExpiringContractViewModel>> GetExpiringContractsAsync(int landlordId, int daysAhead, int? propertyId = null);
        
        /// <summary>
        /// Lấy dữ liệu doanh thu theo tháng
        /// </summary>
        Task<MonthlyRevenueChartViewModel> GetMonthlyRevenueDataAsync(int landlordId, int monthCount, int? propertyId = null);
        
        /// <summary>
        /// Lấy danh sách properties của landlord
        /// </summary>
        Task<List<PropertyOptionViewModel>> GetPropertiesAsync(int landlordId);
    }
}
