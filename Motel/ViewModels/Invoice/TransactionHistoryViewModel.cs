namespace Motel.ViewModels.Invoice;

/// <summary>
/// ViewModel cho trang lịch sử giao dịch (ánh xạ từ vw_TransactionHistory)
/// </summary>
public class TransactionHistoryViewModel
{
    public int InvoiceId { get; set; }
    public int PeriodMonth { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;  // "2026/03"

    public decimal TotalAmount { get; set; }
    public string InvoiceStatus { get; set; } = string.Empty; // unpaid|paid|cancelled
    public DateOnly DueDate { get; set; }
    public DateTime InvoiceCreatedAt { get; set; }

    // Room & Property
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public decimal RentPrice { get; set; }
    public string PropertyName { get; set; } = string.Empty;

    // Tenant
    public string TenantName { get; set; } = string.Empty;
    public string? TenantPhone { get; set; }

    // Contract
    public int ContractId { get; set; }

    // Payment
    public decimal? PaidAmount { get; set; }
    public DateTime? PaidAt { get; set; }

    // Computed
    public string StatusLabel => InvoiceStatus switch
    {
        "paid"      => "Đã thanh toán",
        "unpaid"    => "Chưa thanh toán",
        "cancelled" => "Đã hủy",
        "draft"     => "Nháp",
        _           => InvoiceStatus
    };

    public string StatusCssClass => InvoiceStatus switch
    {
        "paid"      => "text-success",
        "unpaid"    => "text-danger",
        "cancelled" => "text-secondary",
        _           => "text-warning"
    };
}
