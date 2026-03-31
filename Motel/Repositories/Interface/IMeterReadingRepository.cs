using Motel.Models;

namespace Motel.Repositories.Interface;

public interface IMeterReadingRepository
{
    Task<List<MeterReading>> GetMeterReadingsByUserIdAsync(int userId);
    Task<MeterReading?> GetByRoomAndPeriodAsync(int roomId, int periodMonth, CancellationToken ct = default);
    /// <summary>
    /// Lưu chỉ số điện/nước (UPSERT qua stored procedure sp_SaveMeterReading).
    /// Nếu đã có bản ghi cho phòng + tháng → UPDATE, chưa có → INSERT.
    /// </summary>
    Task SaveMeterReadingAsync(int roomId, int periodMonth,
        int electricOld, int electricNew,
        int waterOld, int waterNew,
        int recordedByUserId,
        CancellationToken ct = default);
}
