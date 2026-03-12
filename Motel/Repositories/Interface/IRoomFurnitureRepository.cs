using Motel.Models;

namespace Motel.Repositories.Interface;

public interface IRoomFurnitureRepository
{
    Task<IEnumerable<RoomFurniture>> GetFurnituresByRoomIdAsync(int roomId);
    Task<RoomFurniture?> GetFurnitureByIdAsync(int furnitureId);
    Task AddFurnitureAsync(RoomFurniture furniture);
    Task UpdateFurnitureAsync(RoomFurniture furniture);
    Task DeleteFurnitureAsync(int furnitureId);
    Task<bool> IsRoomOwnedByLandlordAsync(int roomId, int landlordId);

    Task AddStoredFileAsync(StoredFile storedFile);
    Task AddStoredFileReferenceAsync(StoredFileReference reference);
    Task SaveChangesAsync();
}