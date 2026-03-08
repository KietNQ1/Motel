using Motel.Data;
using Motel.Models;
using Motel.Repositories.Interface;

namespace Motel.Repositories;

public sealed class InvoiceLineRepository : IInvoiceLineRepository
{
    private readonly MotelDbContext _db;

    public InvoiceLineRepository(MotelDbContext db) => _db = db;

    public Task AddRangeAsync(IEnumerable<InvoiceLine> lines, CancellationToken ct = default)
    {
        _db.InvoiceLines.AddRange(lines);
        return Task.CompletedTask;
    }
}
