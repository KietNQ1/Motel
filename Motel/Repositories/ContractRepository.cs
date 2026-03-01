using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Repositories.Interface;

namespace Motel.Repositories;

public sealed class ContractRepository : IContractRepository
{
    private readonly MotelDbContext _db;

    public ContractRepository(MotelDbContext db) => _db = db;

    public Task<Contract?> GetByIdAsync(int contractId, CancellationToken ct = default)
        => _db.Contracts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ContractId == contractId, ct);

    public Task<Contract?> GetActiveByIdAsync(int contractId, CancellationToken ct = default)
        => _db.Contracts
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.ContractId == contractId &&
                x.Status == "active" &&
                x.IsDeleted == false, ct);
}
