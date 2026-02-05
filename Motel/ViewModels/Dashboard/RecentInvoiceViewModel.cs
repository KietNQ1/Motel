namespace Motel.ViewModels.Dashboard
{
    /// <summary>
    /// Hóa đơn gần đây hiển thị trên Dashboard
    /// </summary>
    public class RecentInvoiceViewModel
    {
        public int InvoiceId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public int PeriodMonth { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        
        // Computed properties
        public string PeriodFormatted => $"Tháng {PeriodMonth % 100}/{PeriodMonth / 100}";
        public string AmountFormatted => $"{TotalAmount:N0}đ";
        
        public string StatusBadgeClass => Status switch
        {
            "paid" => "bg-green-50 text-green-800 dark:bg-green-500/15 dark:text-green-500",
            "unpaid" => "bg-yellow-50 text-yellow-800 dark:bg-yellow-500/15 dark:text-yellow-500",
            "cancelled" => "bg-gray-50 text-gray-800 dark:bg-gray-500/15 dark:text-gray-500",
            _ => "bg-blue-50 text-blue-800 dark:bg-blue-500/15 dark:text-blue-500"
        };
        
        public string StatusText => Status switch
        {
            "paid" => "Đã thanh toán",
            "unpaid" => "Chưa thanh toán",
            "cancelled" => "Đã hủy",
            "draft" => "Nháp",
            _ => Status
        };
    }
}
