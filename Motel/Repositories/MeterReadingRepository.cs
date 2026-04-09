using Microsoft.Data.SqlClient;
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

    public async Task SaveMeterReadingAsync(
        int roomId, int periodMonth,
        int electricOld, int electricNew,
        int waterOld, int waterNew,
        int recordedByUserId,
        CancellationToken ct = default)
    {
        // Gọi stored procedure sp_SaveMeterReading (UPSERT)
        await _db.Database.ExecuteSqlRawAsync(
            "EXEC dbo.sp_SaveMeterReading @RoomId, @PeriodMonth, @ElectricOld, @ElectricNew, @WaterOld, @WaterNew, @RecordedByUserId",
            new SqlParameter("@RoomId",           roomId),
            new SqlParameter("@PeriodMonth",      periodMonth),
            new SqlParameter("@ElectricOld",      electricOld),
            new SqlParameter("@ElectricNew",      electricNew),
            new SqlParameter("@WaterOld",         waterOld),
            new SqlParameter("@WaterNew",         waterNew),
            new SqlParameter("@RecordedByUserId", recordedByUserId)
        );
    }
}
