
using System.Text.Json;
using Motel.Models;
using Motel.Repositories.Interface;
using Motel.Services.Interface;

namespace Motel.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _repo;

        public PaymentService(IPaymentRepository repo)
        {
            _repo = repo;
        }

        public async Task<PaymentIntent> CreateIntentAsync(int invoiceId, string provider)
        {
            provider = provider?.Trim().ToLower();

            var invoice = await _repo.GetInvoiceAsync(invoiceId);
            if (invoice == null) throw new Exception("Invoice not found");

            if (invoice.Status == "paid")
                throw new Exception("Invoice already paid");

            if (await _repo.HasPendingIntentAsync(invoiceId))
                throw new Exception("Invoice already has a pending payment intent");

            var intent = new PaymentIntent
            {
                InvoiceId = invoiceId,
                Provider = provider,
                ProviderIntentId = null,
                Amount = invoice.TotalAmount,
                Status = PaymentIntentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ExpiredAt = DateTime.UtcNow.AddDays(15)
            };

            await _repo.AddIntentAsync(intent);
            await _repo.SaveChangesAsync();
            return intent;
        }

        public async Task<Payment> ConfirmCashAsync(int paymentIntentId, int confirmedByUserId)
        {
            var intent = await _repo.GetIntentAsync(paymentIntentId);
            if (intent == null) throw new Exception("PaymentIntent not found");

            if (intent.Provider != PaymentProviders.CASH)
                throw new Exception("Only cash payment can be confirmed manually");

            if (intent.Status == PaymentIntentStatus.Succeeded)
                throw new Exception("This intent is already paid");

            if (intent.ExpiredAt.HasValue && intent.ExpiredAt.Value <= DateTime.UtcNow)
            {
                intent.Status = PaymentIntentStatus.Cancelled;
                await _repo.SaveChangesAsync();
                throw new Exception("Payment intent expired");
            }

            var txnId = $"CASH-{DateTime.UtcNow:yyyyMMddHHmmss}-INTENT{intent.PaymentIntentId}";

            if (await _repo.PaymentTxnExistsAsync(PaymentProviders.CASH, txnId))
                throw new Exception("Duplicate transaction id");

            var payment = new Payment
            {
                InvoiceId = intent.InvoiceId,
                PaymentIntentId = intent.PaymentIntentId,
                Provider = PaymentProviders.CASH,
                ProviderTxnId = txnId,
                Amount = intent.Amount,
                PaidAt = DateTime.UtcNow,
                Status = PaymentStatus.Succeeded,
                RawCallbackJson = JsonSerializer.Serialize(new
                {
                    type = "cash_confirm",
                    confirmedByUserId,
                    confirmedAt = DateTime.UtcNow
                })
            };

            intent.Status = PaymentIntentStatus.Succeeded;
            intent.Invoice.Status = "paid";

            await _repo.AddPaymentAsync(payment);
            await _repo.SaveChangesAsync();

            return payment;
        }
    }
}