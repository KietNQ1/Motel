using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Repositories.Interface;

namespace Motel.Repositories;

public sealed class RoomRepository : IRoomRepository
{
    private readonly MotelDbContext _db;

    public RoomRepository(MotelDbContext db) => _db = db;

    public Task<Room?> GetByIdAsync(int roomId, CancellationToken ct = default)
        => _db.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.RoomId == roomId && x.IsDeleted == false, ct);
}
