using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Repositories.Interface;

namespace Motel.Repositories;

public sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly MotelDbContext _db;

    public InvoiceRepository(MotelDbContext db) => _db = db;

    public Task<bool> ExistsAsync(int contractId, int periodMonth, CancellationToken ct = default)
        => _db.Invoices
            .AsNoTracking()
            .AnyAsync(i => i.ContractId == contractId && i.PeriodMonth == periodMonth, ct);

    public Task<Invoice?> GetByIdWithLinesAsync(int invoiceId, CancellationToken ct = default)
        => _db.Invoices
            .AsNoTracking()
            .Include(i => i.InvoiceLines) // nếu navigation tên khác thì sửa lại
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId, ct);

    public async Task AddAsync(Invoice invoice, CancellationToken ct = default)
    {
        _db.Invoices.Add(invoice);
        await Task.CompletedTask;
    }
}
