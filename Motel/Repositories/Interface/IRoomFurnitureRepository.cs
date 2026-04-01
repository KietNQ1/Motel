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
    Task<List<RoomFurniture>> GetRoomFurnituresByLandlordAsync(int landlordId);
    Task DeleteImagesAsync(List<int> imageIds);
    Task AddStoredFileAsync(StoredFile storedFile);
    Task AddStoredFileReferenceAsync(StoredFileReference reference);
    Task<int> GetRoomFurnitureCountByLandlordAsync(int landlordId);
    Task<int> GetStoredFileCountByLandlordAsync(int landlordId);
    Task<List<StoredFile>> GetStoredFilesByLandlordAsync(int landlordId);
    Task<int> GetStoredFileReferenceCountByLandlordAsync(int landlordId);
    Task<List<StoredFileReference>> GetStoredFileReferencesByLandlordAsync(int landlordId);
    Task<int> GetFurnitureCatalogCountAsync();
    Task<List<FurnitureCatalog>> GetFurnitureCatalogsAsync();
    Task<int> GetFurnitureStatusCountAsync();
    Task<List<FurnitureStatus>> GetFurnitureStatusesAsync();
    Task SaveChangesAsync();
}