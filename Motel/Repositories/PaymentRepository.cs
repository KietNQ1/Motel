using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Repositories.Interface;

namespace Motel.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly MotelDbContext _db; // đổi DbContext đúng của bạn

    public PaymentRepository(MotelDbContext db)
    {
        _db = db;
    }

    public Task<Invoice?> GetInvoiceAsync(int invoiceId)
        => _db.Invoices.FirstOrDefaultAsync(x => x.InvoiceId == invoiceId);

    public Task<PaymentIntent?> GetIntentAsync(int paymentIntentId)
        => _db.PaymentIntents
              .Include(x => x.Invoice)
              .FirstOrDefaultAsync(x => x.PaymentIntentId == paymentIntentId);

    public async Task AddIntentAsync(PaymentIntent intent)
    {
        _db.PaymentIntents.Add(intent);
        await Task.CompletedTask;
    }

    public Task<bool> PaymentTxnExistsAsync(string provider, string providerTxnId)
        => _db.Payments.AnyAsync(p => p.Provider == provider && p.ProviderTxnId == providerTxnId);

    public async Task AddPaymentAsync(Payment payment)
    {
        _db.Payments.Add(payment);
        await Task.CompletedTask;
    }

    public Task SaveChangesAsync()
        => _db.SaveChangesAsync();

    public Task<Payment?> GetPaymentForReceiptAsync(int paymentId)
        => _db.Payments
              .Include(p => p.Invoice)
                .ThenInclude(i => i.Contract)
                  .ThenInclude(c => c.Tenant)
              .Include(p => p.Invoice)
                .ThenInclude(i => i.Contract)
                  .ThenInclude(c => c.Room)
                    .ThenInclude(r => r.Property)
              .FirstOrDefaultAsync(p => p.PaymentId == paymentId);
}

