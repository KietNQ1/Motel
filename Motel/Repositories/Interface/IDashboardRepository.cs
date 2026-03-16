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
        /// Lấy thống kê tài chính theo tháng/năm (null = tháng hiện tại)
        /// </summary>
        Task<FinancialStatisticsViewModel> GetFinancialStatisticsAsync(int landlordId, int? propertyId = null, int? year = null, int? month = null);
        
        /// <summary>
        /// Lấy danh sách hóa đơn gần đây
        /// </summary>
        Task<List<RecentInvoiceViewModel>> GetRecentInvoicesAsync(int landlordId, int count, int? propertyId = null);
        
        /// <summary>
        /// Lấy danh sách hợp đồng sắp hết hạn
        /// </summary>
        Task<List<ExpiringContractViewModel>> GetExpiringContractsAsync(int landlordId, int daysAhead, int? propertyId = null);
        
        /// <summary>
        /// Lấy dữ liệu doanh thu theo tháng (year null = năm hiện tại)
        /// </summary>
        Task<MonthlyRevenueChartViewModel> GetMonthlyRevenueDataAsync(int landlordId, int monthCount, int? propertyId = null, int? year = null);
        
        /// <summary>
        /// Lấy danh sách properties của landlord
        /// </summary>
        Task<List<PropertyOptionViewModel>> GetPropertiesAsync(int landlordId);

        /// <summary>
        /// Lấy danh sách hóa đơn theo landlord (lọc property, status, kỳ)
        /// </summary>
        Task<List<Motel.ViewModels.Invoice.InvoiceListViewModel>> GetInvoiceListAsync(int landlordId, int? propertyId = null, string? status = null, int? periodMonth = null);

        /// <summary>
        /// Lấy danh sách hợp đồng theo landlord (lọc property, chỉ sắp hết hạn trong 30 ngày)
        /// </summary>
        Task<List<Motel.ViewModels.Contract.ContractListViewModel>> GetContractListAsync(int landlordId, int? propertyId = null, bool expiringOnly = false);
    }
}
