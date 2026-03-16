namespace Motel.ViewModels.Invoice;

/// <summary>
/// Dòng trong danh sách hóa đơn (trang Index + gửi nhắc nợ)
/// </summary>
public class InvoiceListViewModel
{
    public int InvoiceId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string? TenantEmail { get; set; }
    public string? TenantName { get; set; }
    public int PeriodMonth { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public DateTime CreatedAt { get; set; }

    public string PeriodFormatted => $"Tháng {PeriodMonth % 100}/{PeriodMonth / 100}";
    public string AmountFormatted => $"{TotalAmount:N0}đ";
    public string DueDateFormatted => DueDate.ToString("dd/MM/yyyy");

    public string StatusBadgeClass => Status switch
    {
        "paid" => "bg-green-50 text-green-800 dark:bg-green-500/15 dark:text-green-500",
        "unpaid" => "bg-amber-50 text-amber-800 dark:bg-amber-500/15 dark:text-amber-500",
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
