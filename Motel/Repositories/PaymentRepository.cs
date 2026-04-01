using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Repositories.Interface;
using Motel.Services;
using Motel.ViewModels.Chat;

namespace Motel.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly MotelDbContext _db; // đổi DbContext đúng của bạn

    public PaymentRepository(MotelDbContext db)
    {
        _db = db;
    }

    public Task<Invoice?> GetInvoiceAsync(int invoiceId)
       => _db.Invoices
        .Include(i => i.InvoiceLines)
        .Include(i => i.Contract).ThenInclude(c => c.Tenant)
        .Include(i => i.Room)
            .ThenInclude(r => r.Property)
                .ThenInclude(p => p.Landlord)
                    .ThenInclude(l => l.LandlordBankAccounts)
        .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);

    public async Task<PaymentIntent?> GetIntentAsync(int paymentIntentId)
    {
        return await _db.PaymentIntents
            .Include(p => p.Payments)
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Room)
                    .ThenInclude(r => r.Property)
                        .ThenInclude(p => p.Landlord)
                            .ThenInclude(l => l.LandlordBankAccounts)
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Contract)
                    .ThenInclude(c => c.Tenant)
            .FirstOrDefaultAsync(p => p.PaymentIntentId == paymentIntentId);
    }

    public Task<PaymentIntent?> GetPendingIntentForInvoiceAsync(int invoiceId, string provider)
        => _db.PaymentIntents
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Room)
                    .ThenInclude(r => r.Property)
                        .ThenInclude(p => p.Landlord)
                            .ThenInclude(l => l.LandlordBankAccounts)
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Contract)
                    .ThenInclude(c => c.Tenant)
            .FirstOrDefaultAsync(x =>
                x.InvoiceId == invoiceId &&
                x.Provider == provider &&
                x.Status == PaymentIntentStatus.Pending &&
                (!x.ExpiredAt.HasValue || x.ExpiredAt > DateTime.UtcNow));

    public Task<bool> HasBlockingPaymentIntentForInvoiceAsync(int invoiceId)
    {
        var now = DateTime.UtcNow;
        return _db.PaymentIntents.AnyAsync(x =>
            x.InvoiceId == invoiceId &&
            (
                (x.Status == PaymentIntentStatus.Pending &&
                 (!x.ExpiredAt.HasValue || x.ExpiredAt > now)) ||
                (x.Provider == PaymentProviders.VIETQR &&
                 (!x.ExpiredAt.HasValue || x.ExpiredAt > now) &&
                 !x.Payments.Any(p =>
                     p.Status == PaymentStatus.Succeeded || p.Status == PaymentStatus.Rejected) &&
                 (x.Status == PaymentIntentStatus.AwaitingLandlord ||
                  x.Status == PaymentIntentStatus.Succeeded))
            ));
    }

    public Task<bool> HasPendingIntentAsync(int invoiceId)
    => _db.PaymentIntents.AnyAsync(x =>
        x.InvoiceId == invoiceId &&
        x.Status == PaymentIntentStatus.Pending);

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

    public async Task<List<PaymentIntent>> GetVietQrRequestsForLandlordAsync(int landlordId)
    {
        return await _db.PaymentIntents
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Room)
                    .ThenInclude(r => r.Property)
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Contract)
                    .ThenInclude(c => c.Tenant)
            .Include(p => p.Payments)
            .Where(p =>
                p.Provider == PaymentProviders.VIETQR &&
                p.Invoice.Room.Property.LandlordId == landlordId &&
                !p.Payments.Any(x => x.Status == PaymentStatus.Succeeded || x.Status == PaymentStatus.Rejected) &&
                (p.Status == PaymentIntentStatus.AwaitingLandlord ||
                 p.Status == PaymentIntentStatus.Succeeded))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<PaymentIntent>> GetPendingCashIntentsForLandlordAsync(int landlordId)
    {
        return await _db.PaymentIntents
            .AsNoTracking()
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Room)
                    .ThenInclude(r => r.Property)
            .Include(p => p.Invoice)
                .ThenInclude(i => i.Contract)
                    .ThenInclude(c => c.Tenant)
            .Where(p =>
                p.Provider == PaymentProviders.CASH &&
                p.Invoice.Room.Property.LandlordId == landlordId &&
                p.Status == PaymentIntentStatus.Pending &&
                (!p.ExpiredAt.HasValue || p.ExpiredAt > DateTime.UtcNow))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public Task SaveChangesAsync()
        => _db.SaveChangesAsync();

    public Task<Payment?> GetPaymentForReceiptAsync(int paymentId)
    => _db.Payments
        .Include(p => p.Invoice)
            .ThenInclude(i => i.InvoiceLines) // ✅ thêm
        .Include(p => p.Invoice)
            .ThenInclude(i => i.Contract)
                .ThenInclude(c => c.Tenant)
        .Include(p => p.Invoice)
            .ThenInclude(i => i.Room)
                .ThenInclude(r => r.Property)
        .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

    public Task<int> GetPaymentCountByLandlordAsync(int landlordId)
        => _db.Payments
            .AsNoTracking()
            .CountAsync(p => p.Invoice.Room.Property.LandlordId == landlordId);

    public async Task<(int VietQrPendingCount, int CashPendingCount)> GetPendingIntentCountsByLandlordAsync(int landlordId)
    {
        var pendingVietQr = await GetVietQrRequestsForLandlordAsync(landlordId);
        var pendingCash = await GetPendingCashIntentsForLandlordAsync(landlordId);
        return (pendingVietQr.Count, pendingCash.Count);
    }

    public async Task<PaymentInsightDto> GetPaymentInsightByLandlordAsync(int landlordId)
    {
        var payments = await _db.Payments
            .AsNoTracking()
            .Where(p => p.Invoice.Room.Property.LandlordId == landlordId)
            .OrderByDescending(p => p.PaidAt)
            .ToListAsync();

        var dto = new PaymentInsightDto
        {
            TotalPayments = payments.Count,
            TotalAmount = payments.Sum(p => p.Amount),
            SucceededCount = payments.Count(p => p.Status == PaymentStatus.Succeeded),
            PendingCount = payments.Count(p => p.Status == "pending"),
            FailedCount = payments.Count(p => p.Status == PaymentStatus.Failed || p.Status == PaymentStatus.Rejected),
            RecentPaymentSamples = payments
                .Take(3)
                .Select(p => $"{p.Provider.ToUpperInvariant()} | {p.Amount:N0} VND | {p.Status} | {p.PaidAt:dd/MM/yyyy HH:mm}")
                .ToList()
        };

        return dto;
    }

    public async Task<PaymentIntentInsightDto> GetPaymentIntentInsightByLandlordAsync(int landlordId)
    {
        var now = DateTime.UtcNow;
        var intents = await _db.PaymentIntents
            .AsNoTracking()
            .Where(i => i.Invoice.Room.Property.LandlordId == landlordId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        var dto = new PaymentIntentInsightDto
        {
            TotalIntents = intents.Count,
            PendingIntents = intents.Count(i => i.Status == PaymentIntentStatus.Pending),
            AwaitingLandlordIntents = intents.Count(i => i.Status == PaymentIntentStatus.AwaitingLandlord),
            ExpiredIntents = intents.Count(i => i.ExpiredAt.HasValue && i.ExpiredAt.Value <= now),
            RecentIntentSamples = intents
                .Take(3)
                .Select(i => $"{i.Provider.ToUpperInvariant()} | {i.Amount:N0} VND | {i.Status} | {i.CreatedAt:dd/MM/yyyy HH:mm}")
                .ToList()
        };

        return dto;
    }
}

