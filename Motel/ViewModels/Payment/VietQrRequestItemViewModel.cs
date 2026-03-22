namespace Motel.ViewModels.Payment;

public class VietQrRequestItemViewModel
{
    public int PaymentIntentId { get; set; }
    public int InvoiceId { get; set; }
    public string RoomName { get; set; } = "";
    public string PropertyName { get; set; } = "";
    public string TenantName { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}
