
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

            // Cùng phương thức + pending chưa hết hạn → dùng lại (tránh tạo bản ghi trùng).
            var existing = await _repo.GetPendingIntentForInvoiceAsync(invoiceId, provider);
            if (existing != null)
                return existing;

            // Một hóa đơn chỉ một luồng thanh toán đang mở: chặn thêm khi đã có pending (phương thức khác) hoặc VietQR chờ chủ trọ.
            if (await _repo.HasBlockingPaymentIntentForInvoiceAsync(invoiceId))
                throw new Exception(
                    "Hóa đơn này đã có yêu cầu thanh toán đang xử lý. Bạn không thể tạo thêm yêu cầu cho đến khi yêu cầu hiện tại hết hạn, bị hủy hoặc được xử lý xong (ví dụ chủ trọ xác nhận / từ chối).");

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

        public async Task<Payment> ConfirmVietQrAsync(int paymentIntentId, int confirmedByUserId)
        {
            var intent = await _repo.GetIntentAsync(paymentIntentId);
            if (intent == null) throw new Exception("PaymentIntent not found");

            if (intent.Provider != PaymentProviders.VIETQR)
                throw new Exception("Only VietQR payment can be confirmed here");

            if (intent.Payments.Any(x => x.Status == PaymentStatus.Succeeded))
                throw new Exception("Giao dịch này đã được đánh dấu đã thanh toán.");

            var noFinalPayment = !intent.Payments.Any(x =>
                x.Status == PaymentStatus.Succeeded || x.Status == PaymentStatus.Rejected);
            var tenantReported = intent.Status == PaymentIntentStatus.AwaitingLandlord
                || (intent.Status == PaymentIntentStatus.Succeeded && noFinalPayment);
            if (!tenantReported)
                throw new Exception(
                    "Chưa có xác nhận từ người thuê hoặc yêu cầu không còn ở trạng thái chờ xử lý.");

            if (intent.ExpiredAt.HasValue && intent.ExpiredAt.Value <= DateTime.UtcNow)
            {
                intent.Status = PaymentIntentStatus.Expired;
                await _repo.SaveChangesAsync();
                throw new Exception("Payment intent expired");
            }

            var txnId = $"VIETQR-{DateTime.UtcNow:yyyyMMddHHmmss}-INTENT{intent.PaymentIntentId}";

            if (await _repo.PaymentTxnExistsAsync(PaymentProviders.VIETQR, txnId))
                throw new Exception("Duplicate transaction id");

            var payment = new Payment
            {
                InvoiceId = intent.InvoiceId,
                PaymentIntentId = intent.PaymentIntentId,
                Provider = PaymentProviders.VIETQR,
                ProviderTxnId = txnId,
                Amount = intent.Amount,
                PaidAt = DateTime.UtcNow,
                Status = PaymentStatus.Succeeded,
                RawCallbackJson = JsonSerializer.Serialize(new
                {
                    type = "vietqr_manual_confirm",
                    confirmedByUserId,
                    confirmedAt = DateTime.UtcNow
                })
            };

            intent.Status = PaymentIntentStatus.Succeeded;
            intent.Invoice.Status = InvoiceStatus.Paid;

            await _repo.AddPaymentAsync(payment);
            await _repo.SaveChangesAsync();

            return payment;
        }
    }
}