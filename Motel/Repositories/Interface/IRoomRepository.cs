using Motel.Models;

namespace Motel.Repositories.Interface;

public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(int roomId, CancellationToken ct = default);
}
