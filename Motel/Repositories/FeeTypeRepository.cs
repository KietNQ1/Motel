using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Repositories.Interface;

namespace Motel.Repositories;

public sealed class FeeTypeRepository : IFeeTypeRepository
{
    private readonly MotelDbContext _db;

    public FeeTypeRepository(MotelDbContext db) => _db = db;

    public Task<List<FeeType>> GetAllAsync(CancellationToken ct = default)
    {
        return _db.FeeTypes.AsNoTracking().ToListAsync(ct);
    }

    public Task<FeeType?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return _db.FeeTypes.AsNoTracking().FirstOrDefaultAsync(x => x.FeeTypeId == id, ct);
    }

    public async Task<FeeType> AddAsync(FeeType feeType, CancellationToken ct = default)
    {
        _db.FeeTypes.Add(feeType);
        await _db.SaveChangesAsync(ct);
        return feeType;
    }
}
