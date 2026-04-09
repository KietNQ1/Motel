using Motel.Models;
using Motel.ViewModels.Room;

namespace Motel.Repositories.Interface
{
    public interface IRoomRepository
    {
        Task<Room?> GetRoomByIdAsync(int roomId, CancellationToken ct = default);
        Task<RoomDetailViewModel?> GetRoomDetailAsync(int roomId, CancellationToken ct = default);
        Task<bool> UpdateRoomAsync(Room room, CancellationToken ct = default);
        Task<bool> DeleteRoomAsync(int roomId, CancellationToken ct = default);
        Task<bool> RestoreRoomAsync(int roomId);

        Task<bool> RentRoomAsync(
            int roomId,
            int landlordId,
            int recordedByUserId,
            List<TenantInputViewModel> occupants,
            int primaryIndex,
            decimal depositAmount,
            DateOnly startDate,
            DateOnly endDate,
            int? initialElectric,
            int? initialWater,
            CancellationToken ct = default);
    }
}