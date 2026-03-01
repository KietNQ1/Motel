using Motel.Models;

namespace Motel.Repositories.Interface;

public interface IMeterReadingRepository
{
    Task<MeterReading?> GetByRoomAndPeriodAsync(int roomId, int periodMonth, CancellationToken ct = default);
}
