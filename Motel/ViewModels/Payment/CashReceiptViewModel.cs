namespace Motel.ViewModels.Payments;

public class CashReceiptViewModel
{
    public int PaymentId { get; set; }
    public string ProviderTxnId { get; set; } = "";
    public DateTime PaidAt { get; set; }

    // Tenant / Room
    public string TenantName { get; set; } = "";
    public string TenantPhone { get; set; } = "";
    public string RoomName { get; set; } = "";
    public string PropertyName { get; set; } = "";

    // Amount
    public decimal TotalAmount { get; set; }

    // Display helpers
    public string PaidAtDisplay => PaidAt.ToString("dd/MM/yyyy HH:mm");
    public string TotalAmountDisplay => $"{TotalAmount:n0} VND";
}
