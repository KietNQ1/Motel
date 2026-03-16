namespace Motel.Services;

public static class PaymentProviders
{
    public const string CASH = "cash";
    public const string PAYOS = "payos";
    public const string VIETQR = "vietqr";
}

public static class PaymentIntentStatus
{
    public const string Pending = "pending";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public const string Expired = "expired";
    public const string Cancelled = "cancelled";
}

public static class PaymentStatus
{
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public const string Refunded = "refunded";
    public const string Rejected = "rejected";
}
public static class InvoiceStatus
{
    public const string Unpaid = "unpaid";
    public const string Paid = "paid";
}