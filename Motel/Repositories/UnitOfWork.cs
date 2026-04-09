using Motel.Data;
using Motel.Repositories.Interface;

namespace Motel.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly MotelDbContext _db;

    public UnitOfWork(MotelDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _db.SaveChangesAsync(ct);

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            await action(ct);
            await tx.CommitAsync(ct);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
