using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Repositories.Interface;

namespace Motel.Repositories;

public sealed class RoomUtilitySettingRepository : IRoomUtilitySettingRepository
{
    private readonly MotelDbContext _db;

    public RoomUtilitySettingRepository(MotelDbContext db) => _db = db;

    public async Task<RoomUtilitySetting?> GetEffectiveAsync(
    int roomId,
    int periodMonth,
    CancellationToken ct = default)
    {
        var year = periodMonth / 100;
        var month = periodMonth % 100;

        // convert DateTime -> DateOnly
        var effectiveDate = new DateOnly(year, month, 1);

        return await _db.RoomUtilitySettings
            .AsNoTracking()
            .Where(s => s.RoomId == roomId)
            .Where(s => s.EffectiveFrom <= effectiveDate)
            .Where(s => s.EffectiveTo == null || s.EffectiveTo >= effectiveDate)
            .OrderByDescending(s => s.EffectiveFrom)
            .FirstOrDefaultAsync(ct);
    }

}
