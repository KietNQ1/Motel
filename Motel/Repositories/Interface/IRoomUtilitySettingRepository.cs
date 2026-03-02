using Motel.Models;

namespace Motel.Repositories.Interface;

public interface IRoomUtilitySettingRepository
{
    /// <summary>
    /// Lấy cấu hình giá có hiệu lực cho kỳ YYYYMM.
    /// </summary>
    Task<RoomUtilitySetting?> GetEffectiveAsync(int roomId, int periodMonth, CancellationToken ct = default);
}
