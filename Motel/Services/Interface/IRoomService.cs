using Motel.ViewModels.Room;

namespace Motel.Services.Interface
{
    public interface IRoomService
    {
        Task<bool> RentRoomAsync(RentRoomViewModel model, int landlordId, int userId, CancellationToken ct = default);
    }
}