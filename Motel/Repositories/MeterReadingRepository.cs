using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Repositories.Interface;

namespace Motel.Repositories;

public sealed class MeterReadingRepository : IMeterReadingRepository
{
    private readonly MotelDbContext _db;

    public MeterReadingRepository(MotelDbContext db) => _db = db;

    public Task<MeterReading?> GetByRoomAndPeriodAsync(int roomId, int periodMonth, CancellationToken ct = default)
        => _db.MeterReadings
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.RoomId == roomId && m.PeriodMonth == periodMonth, ct);
}
