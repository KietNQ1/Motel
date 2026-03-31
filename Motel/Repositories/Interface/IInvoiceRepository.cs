using Motel.Models;

namespace Motel.Repositories.Interface
{
    public interface IInvoiceRepository
    {
        Task<bool> ExistsAsync(int contractId, int periodMonth, CancellationToken ct = default);
        Task<Invoice?> GetByIdWithLinesAsync(int invoiceId, CancellationToken ct = default);
        Task AddAsync(Invoice invoice, CancellationToken ct = default);
        Task<List<Invoice>> GetInvoicesByLandlordAsync(int landlordId);
    }
}
