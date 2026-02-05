namespace Motel.ViewModels.Dashboard
{
    /// <summary>
    /// ViewModel chính cho Dashboard - tập hợp tất cả data cần hiển thị
    /// </summary>
    public class DashboardIndexViewModel
    {
        public RoomStatisticsViewModel RoomStats { get; set; } = new();
        public FinancialStatisticsViewModel FinancialStats { get; set; } = new();
        public List<RecentInvoiceViewModel> RecentInvoices { get; set; } = new();
        public List<ExpiringContractViewModel> ExpiringContracts { get; set; } = new();
        public MonthlyRevenueChartViewModel RevenueChart { get; set; } = new();
    }
}
