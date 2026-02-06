namespace Motel.Services;

public static class PaymentProviders
{
    public const string CASH = "CASH";
    public const string PAYOS = "PAYOS";
    public const string VNPAY = "VNPAY";
    public const string MOMO = "MOMO";
}

public static class PaymentIntentStatus
{
    public const string Pending = "Pending";
    public const string Success = "Success";
    public const string Failed = "Failed";
    public const string Expired = "Expired";
    public const string Canceled = "Canceled";
}

public static class PaymentStatus
{
    public const string Success = "Success";
    public const string Failed = "Failed";
    public const string Refunded = "Refunded";
}
