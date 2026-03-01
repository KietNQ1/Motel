using Motel.Models;

namespace Motel.Repositories.Interface;

public interface IContractRepository
{
    Task<Contract?> GetByIdAsync(int contractId, CancellationToken ct = default);

    Task<Contract?> GetActiveByIdAsync(int contractId, CancellationToken ct = default);
}
