namespace Motel.ViewModels.Payment;

public class VietQrPaymentViewModel
{
    public int InvoiceId { get; set; }

    public int PaymentIntentId { get; set; }

    public decimal Amount { get; set; }

    public string TransferContent { get; set; } = string.Empty;

    public string LandlordName { get; set; } = string.Empty;

    public string BankName { get; set; } = string.Empty;

    public string BankAccountNumber { get; set; } = string.Empty;

    public string BankAccountName { get; set; } = string.Empty;

    public string BankCode { get; set; } = string.Empty;

    public string QrImageUrl { get; set; } = string.Empty;

    public string? RoomName { get; set; }

    public string? PropertyName { get; set; }

    public string? TenantName { get; set; }
}

