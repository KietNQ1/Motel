using Motel.Models;

namespace Motel.Repositories.Interface;

public interface IRoomFurnitureRepository
{
    Task<IEnumerable<RoomFurniture>> GetFurnituresByRoomIdAsync(int roomId);
    Task<RoomFurniture?> GetFurnitureByIdAsync(int furnitureId);
    //Task<RoomFurniture?> GetFurnitureByNameAsync(int roomId, string name);
    Task AddFurnitureAsync(RoomFurniture furniture);
    Task UpdateFurnitureAsync(RoomFurniture furniture);
    Task DeleteFurnitureAsync(RoomFurniture furniture);
    Task<bool> IsRoomOwnedByLandlordAsync(int roomId, int landlordId);
    Task<RoomFurniture?> GetFurnitureByCatalogAsync(int roomId, int catalogId);
    Task DeleteImagesAsync(List<int> imageIds);
    Task AddStoredFileAsync(StoredFile storedFile);
    Task AddStoredFileReferenceAsync(StoredFileReference reference);
    Task SaveChangesAsync();
}