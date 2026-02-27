using Motel.Models;
using Motel.ViewModels.Room;

namespace Motel.Repositories.Interface
{
    public interface IRoomRepository
    {
        Task<RoomDetailViewModel?> GetRoomDetailAsync(int roomId);
        Task<Room?> GetRoomByIdAsync(int roomId);
        Task<bool> UpdateRoomAsync(Room room);
        Task<bool> RentRoomAsync(int roomId, int landlordId, string tenantName, string? phone, string? email, string? identityNo, decimal depositAmount, DateOnly startDate, DateOnly endDate, int? initialElectric, int? initialWater);
    }
}
