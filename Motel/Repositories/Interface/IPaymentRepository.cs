using Motel.Models;

namespace Motel.Repositories.Interface;

public interface IPaymentRepository
{
    Task<Invoice?> GetInvoiceAsync(int invoiceId);

    Task<PaymentIntent?> GetIntentAsync(int paymentIntentId);

    Task<PaymentIntent?> GetPendingIntentForInvoiceAsync(int invoiceId, string provider);

    /// <summary>True nếu hóa đơn đã có yêu cầu thanh toán đang mở (pending chưa hết hạn, hoặc VietQR chờ chủ trọ).</summary>
    Task<bool> HasBlockingPaymentIntentForInvoiceAsync(int invoiceId);

    Task<List<PaymentIntent>> GetVietQrRequestsForLandlordAsync(int landlordId);

    Task<List<PaymentIntent>> GetPendingCashIntentsForLandlordAsync(int landlordId);

    Task AddIntentAsync(PaymentIntent intent);

    Task<bool> PaymentTxnExistsAsync(string provider, string providerTxnId);

    Task AddPaymentAsync(Payment payment);

    Task SaveChangesAsync();
    Task<Payment?> GetPaymentForReceiptAsync(int paymentId);
    Task<bool> HasPendingIntentAsync(int invoiceId);
}
