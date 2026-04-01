using Motel.Models;
using Motel.ViewModels.Chat;

namespace Motel.Repositories.Interface;

public interface IInvoiceLineRepository
{
    Task AddRangeAsync(IEnumerable<InvoiceLine> lines, CancellationToken ct = default);
    Task<int> GetInvoiceLineCountByLandlordAsync(int landlordId, CancellationToken ct = default);
    Task<InvoiceLineInsightDto> GetInvoiceLineInsightByLandlordAsync(int landlordId, CancellationToken ct = default);
}
