using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Repositories.Interface;

namespace Motel.Repositories;

public sealed class FeeSettingRepository : IFeeSettingRepository
{
    private readonly MotelDbContext _db;

    public FeeSettingRepository(MotelDbContext db) => _db = db;

    public async Task<List<FeeSetting>> GetEffectiveForRoomAsync(
        int propertyId,
        int roomId,
        int periodMonth,
        CancellationToken ct = default)
    {
        var year = periodMonth / 100;
        var month = periodMonth % 100;

        var startOfMonth = new DateOnly(year, month, 1);
        var endOfMonth = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        // Fetch all applicable settings for the property and the room
        var applicableSettings = await _db.FeeSettings
            .Include(s => s.FeeType)
            .AsNoTracking()
            .Where(s => (s.RoomId == roomId) || (s.PropertyId == propertyId && s.RoomId == null))
            // Fee takes effect on or before the end of the billing month
            .Where(s => s.EffectiveFrom <= endOfMonth)
            // Fee is effective until at least the start of the billing month
            .Where(s => s.EffectiveTo == null || s.EffectiveTo >= startOfMonth)
            .ToListAsync(ct);

        // Override logic: room settings take precedence over property settings
        var effectiveSettings = applicableSettings
            .GroupBy(s => s.FeeTypeId)
            .Select(g => g.OrderByDescending(s => s.RoomId.HasValue).ThenByDescending(s => s.EffectiveFrom).First())
            .ToList();

        return effectiveSettings;
    }

    public async Task<FeeSetting> AddAsync(FeeSetting setting, CancellationToken ct = default)
    {
        _db.FeeSettings.Add(setting);
        await _db.SaveChangesAsync(ct);
        return setting;
    }

    public async Task<List<FeeSetting>> GetPropertyLevelFeeSettingsAsync(int propertyId, CancellationToken ct = default)
    {
        return await _db.FeeSettings
            .Include(s => s.FeeType)
            .Where(s => s.PropertyId == propertyId && s.RoomId == null && s.EffectiveTo == null)
            .ToListAsync(ct);
    }

    public async Task<List<FeeSetting>> GetRoomLevelFeeSettingsAsync(int roomId, CancellationToken ct = default)
    {
        return await _db.FeeSettings
            .Include(s => s.FeeType)
            .Where(s => s.RoomId == roomId && s.EffectiveTo == null)
            .ToListAsync(ct);
    }

    public async Task<bool> InvalidateFeeSettingAsync(int feeSettingId, DateOnly endDate, CancellationToken ct = default)
    {
        var setting = await _db.FeeSettings.FirstOrDefaultAsync(s => s.FeeSettingId == feeSettingId, ct);
        if (setting == null) return false;

        setting.EffectiveTo = endDate;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public Task<int> GetFeeSettingCountByLandlordAsync(int landlordId, CancellationToken ct = default)
    {
        return _db.FeeSettings
            .AsNoTracking()
            .CountAsync(fs =>
                (fs.PropertyId.HasValue && fs.Property.LandlordId == landlordId) ||
                (fs.RoomId.HasValue && fs.Room.Property.LandlordId == landlordId),
                ct);
    }

    public Task<List<FeeSetting>> GetFeeSettingsByLandlordAsync(int landlordId, CancellationToken ct = default)
    {
        return _db.FeeSettings
            .AsNoTracking()
            .Include(fs => fs.FeeType)
            .Where(fs =>
                (fs.PropertyId.HasValue && fs.Property.LandlordId == landlordId) ||
                (fs.RoomId.HasValue && fs.Room.Property.LandlordId == landlordId))
            .OrderByDescending(fs => fs.EffectiveFrom)
            .ToListAsync(ct);
    }
}
