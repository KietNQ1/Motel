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
            // 1) Lấy invoice để biết số tiền
            var invoice = await _repo.GetInvoiceAsync(invoiceId);
            if (invoice == null) throw new Exception("Invoice not found");

            // TODO: đổi đúng tên cột tổng tiền của invoice bạn
            var amount = invoice.TotalAmount;

            // 2) Tạo intent (phiên thanh toán)
            var intent = new PaymentIntent
            {
                InvoiceId = invoiceId,
                Provider = provider, // CASH/PAYOS/VNPAY/MOMO
                ProviderIntentId = null,
                Amount = amount,
                Status = PaymentIntentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ExpiredAt = DateTime.UtcNow.AddMinutes(15)
            };

            // 3) Lưu DB
            await _repo.AddIntentAsync(intent);
            await _repo.SaveChangesAsync();

            return intent;
        }

        public async Task<Payment> ConfirmCashAsync(int paymentIntentId, int confirmedByUserId)
        {
            // 1) Lấy intent
            var intent = await _repo.GetIntentAsync(paymentIntentId);
            if (intent == null) throw new Exception("PaymentIntent not found");

            // 2) Chặn confirm trùng
            if (intent.Status == PaymentIntentStatus.Success)
                throw new Exception("This intent is already paid");

            // 3) Check hết hạn (optional)
            if (intent.ExpiredAt.HasValue && intent.ExpiredAt.Value <= DateTime.UtcNow)
            {
                intent.Status = PaymentIntentStatus.Expired;
                await _repo.SaveChangesAsync();
                throw new Exception("Payment intent expired");
            }

            // 4) Tạo mã giao dịch nội bộ cho CASH
            var txnId = $"CASH-{DateTime.UtcNow:yyyyMMddHHmmss}-INTENT{intent.PaymentIntentId}";

            // 5) (Optional) chống trùng txnId
            // Thực tế txnId theo ticks/time gần như không trùng, nhưng thêm check cho chắc
            if (await _repo.PaymentTxnExistsAsync(PaymentProviders.CASH, txnId))
                throw new Exception("Duplicate transaction id");

            // 6) Tạo Payment (giao dịch thật)
            var payment = new Payment
            {
                InvoiceId = intent.InvoiceId,
                PaymentIntentId = intent.PaymentIntentId,
                Provider = PaymentProviders.CASH,
                ProviderTxnId = txnId,
                Amount = intent.Amount,
                PaidAt = DateTime.UtcNow,
                Status = PaymentStatus.Success,
                RawCallbackJson = JsonSerializer.Serialize(new
                {
                    type = "cash_confirm",
                    confirmedByUserId,
                    confirmedAt = DateTime.UtcNow
                })
            };

            // 7) Update intent
            intent.Status = PaymentIntentStatus.Success;

            // 8) Lưu payment
            await _repo.AddPaymentAsync(payment);
            await _repo.SaveChangesAsync();

            return payment;
        }
    }
}
