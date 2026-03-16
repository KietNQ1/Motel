namespace Motel.ViewModels.Payment;

public class VietQrRequestItemViewModel
{
    public int PaymentIntentId { get; set; }
    public int InvoiceId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}

