using Motel.Models;

namespace Motel.Services.Interface;

public interface IPaymentService
{
    Task<PaymentIntent> CreateIntentAsync(int invoiceId, string provider);
    Task<Payment> ConfirmCashAsync(int paymentIntentId, int confirmedByUserId);
    Task<Payment> ConfirmVietQrAsync(int paymentIntentId, int confirmedByUserId);
}
