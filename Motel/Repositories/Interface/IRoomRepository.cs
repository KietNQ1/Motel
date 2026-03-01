using Motel.Models;
using Motel.ViewModels.Room;

namespace Motel.Repositories.Interface
{
    public interface IRoomRepository
    {
        Task<Room?> GetRoomByIdAsync(int roomId, CancellationToken ct = default);

        Task<RoomDetailViewModel?> GetRoomDetailAsync(int roomId, CancellationToken ct = default);

        Task<bool> UpdateRoomAsync(Room room, CancellationToken ct = default);

        Task<bool> RentRoomAsync(
            int roomId,
            int landlordId,
            string tenantName,
            string? phone,
            string? email,
            string? identityNo,
            decimal depositAmount,
            DateOnly startDate,
            DateOnly endDate,
            int? initialElectric,
            int? initialWater,
            CancellationToken ct = default);
    }
}