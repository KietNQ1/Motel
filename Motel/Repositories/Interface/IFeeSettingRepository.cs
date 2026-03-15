using Motel.Models;

namespace Motel.Repositories.Interface;

public interface IFeeSettingRepository
{
    /// <summary>
    /// Returns the effective fee settings for a specific room and property for a given period.
    /// Room-level settings override property-level settings.
    /// </summary>
    Task<List<FeeSetting>> GetEffectiveForRoomAsync(int propertyId, int roomId, int periodMonth, CancellationToken ct = default);

    /// <summary>
    /// Adds a new fee setting.
    /// </summary>
    Task<FeeSetting> AddAsync(FeeSetting setting, CancellationToken ct = default);

    /// <summary>
    /// Fetches all currently effective property-level fee settings (RoomId is null, and not ended).
    /// </summary>
    Task<List<FeeSetting>> GetPropertyLevelFeeSettingsAsync(int propertyId, CancellationToken ct = default);

    /// <summary>
    /// Fetches all currently effective room-level fee settings (RoomId is not null, and not ended).
    /// </summary>
    Task<List<FeeSetting>> GetRoomLevelFeeSettingsAsync(int roomId, CancellationToken ct = default);

    /// <summary>
    /// Ends a fee setting by setting its EffectiveTo date.
    /// </summary>
    Task<bool> InvalidateFeeSettingAsync(int feeSettingId, DateOnly endDate, CancellationToken ct = default);
}
