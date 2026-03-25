using Motel.Models;
using Motel.ViewModels.Invoice;

namespace Motel.Services.Interface;

public interface IInvoiceService
{
    Task<int> CreateInvoiceAsync(CreateInvoiceViewModel vm, CancellationToken ct = default);

    Task<Invoice?> GetInvoiceWithLinesAsync(int invoiceId, CancellationToken ct = default);

    Task<List<TransactionHistoryViewModel>> GetTransactionHistoryAsync(int landlordId, CancellationToken ct = default);

    Task<bool> SendPaymentReminderAsync(int invoiceId, int landlordId, CancellationToken ct = default);
}
