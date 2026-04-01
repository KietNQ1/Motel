using Motel.Data;
using Motel.Models;
using Motel.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Motel.ViewModels.Chat;

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

    public Task<int> GetInvoiceLineCountByLandlordAsync(int landlordId, CancellationToken ct = default)
    {
        return _db.InvoiceLines
            .AsNoTracking()
            .CountAsync(il => il.Invoice.Room.Property.LandlordId == landlordId, ct);
    }

    public async Task<InvoiceLineInsightDto> GetInvoiceLineInsightByLandlordAsync(int landlordId, CancellationToken ct = default)
    {
        var query = _db.InvoiceLines
            .AsNoTracking()
            .Where(il => il.Invoice.Room.Property.LandlordId == landlordId);

        var totalLines = await query.CountAsync(ct);
        var totalAmount = await query.SumAsync(il => il.LineTotal ?? (il.Quantity * il.UnitPrice), ct);

        var topFeeTypes = await query
            .GroupBy(il => il.FeeType.Name)
            .Select(g => new InvoiceLineFeeTypeBreakdownDto
            {
                FeeTypeName = g.Key,
                LineCount = g.Count(),
                TotalAmount = g.Sum(x => x.LineTotal ?? (x.Quantity * x.UnitPrice))
            })
            .OrderByDescending(x => x.TotalAmount)
            .ThenByDescending(x => x.LineCount)
            .Take(3)
            .ToListAsync(ct);

        return new InvoiceLineInsightDto
        {
            TotalLines = totalLines,
            TotalAmount = totalAmount,
            TopFeeTypes = topFeeTypes
        };
    }
}
