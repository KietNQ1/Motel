using Motel.Models;

namespace Motel.Repositories.Interface;

public interface IPaymentRepository
{
    Task<Invoice?> GetInvoiceAsync(int invoiceId);

    Task<PaymentIntent?> GetIntentAsync(int paymentIntentId);

    Task<PaymentIntent?> GetPendingIntentForInvoiceAsync(int invoiceId, string provider);

    Task<List<PaymentIntent>> GetVietQrRequestsForLandlordAsync(int landlordId);

    Task AddIntentAsync(PaymentIntent intent);

    Task<bool> PaymentTxnExistsAsync(string provider, string providerTxnId);

    Task AddPaymentAsync(Payment payment);

    Task SaveChangesAsync();
    Task<Payment?> GetPaymentForReceiptAsync(int paymentId);
    Task<bool> HasPendingIntentAsync(int invoiceId);
}
