namespace Motel.ViewModels.Dashboard
{
    /// <summary>
    /// Thống kê tài chính cho Dashboard
    /// </summary>
    public class FinancialStatisticsViewModel
    {
        public decimal MonthlyRevenue { get; set; }
        public decimal UnpaidAmount { get; set; }
        public int UnpaidInvoiceCount { get; set; }
        public decimal CollectedAmount { get; set; }
        public decimal CollectionRate { get; set; }
        
        // Formatted properties
        public string MonthlyRevenueFormatted => FormatCurrency(MonthlyRevenue);
        public string UnpaidAmountFormatted => FormatCurrency(UnpaidAmount);
        public string CollectedAmountFormatted => FormatCurrency(CollectedAmount);
        public string CollectionRateFormatted => $"{CollectionRate:F1}%";
        public string UnpaidInvoiceText => $"{UnpaidInvoiceCount} hóa đơn";
        
        private static string FormatCurrency(decimal amount) 
            => $"{amount:N0}đ";
    }
}
