using Motel.Models;

namespace Motel.Repositories.Interface;

public interface IInvoiceLineRepository
{
    Task AddRangeAsync(IEnumerable<InvoiceLine> lines, CancellationToken ct = default);
}
