using Motel.ViewModels.Room;  // Giả sử tạo ViewModel riêng

namespace Motel.Services.Interface;

public interface IRoomFurnitureService
{
    Task<IEnumerable<RoomFurnitureViewModel>> GetFurnituresForRoomAsync(int roomId);
    Task<bool> AddFurnitureAsync(AddFurnitureViewModel model, int landlordId);
    Task<bool> UpdateFurnitureAsync(UpdateFurnitureViewModel model, int landlordId);
    Task<bool> DeleteFurnitureAsync(int furnitureId, int landlordId);
}