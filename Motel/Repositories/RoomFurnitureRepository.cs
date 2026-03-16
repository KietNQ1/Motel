using Motel.Data;
using Motel.Models;
using Motel.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace Motel.Repositories;

public class RoomFurnitureRepository : IRoomFurnitureRepository
{
    private readonly MotelDbContext _db;

    public RoomFurnitureRepository(MotelDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<RoomFurniture>> GetFurnituresByRoomIdAsync(int roomId)
    {
        return await _db.RoomFurnitures
            .Include(f => f.FurnitureCatalog)
            .Include(f => f.FurnitureStatus)
            .Where(f => f.RoomId == roomId && !f.IsDeleted)
            .ToListAsync();
    }

    public async Task<RoomFurniture?> GetFurnitureByIdAsync(int furnitureId)
    {
        return await _db.RoomFurnitures
            .FirstOrDefaultAsync(f => f.FurnitureId == furnitureId && !f.IsDeleted);
    }

    //public async Task<RoomFurniture?> GetFurnitureByNameAsync(int roomId, string name)
    //{
    //    return await _db.RoomFurnitures
    //        .FirstOrDefaultAsync(f =>
    //            f.RoomId == roomId &&
    //            f.FurnitureCatalog.Name == name &&
    //            !f.IsDeleted);
    //}

    public Task AddFurnitureAsync(RoomFurniture furniture)
    {
        _db.RoomFurnitures.Add(furniture);
        return Task.CompletedTask;
    }

    public Task UpdateFurnitureAsync(RoomFurniture furniture)
    {
        _db.RoomFurnitures.Update(furniture);
        return Task.CompletedTask;
    }

    public Task DeleteFurnitureAsync(RoomFurniture furniture)
    {
        furniture.IsDeleted = true;
        return Task.CompletedTask;
    }

    public async Task<bool> IsRoomOwnedByLandlordAsync(int roomId, int landlordId)
    {
        return await _db.Rooms
            .Include(r => r.Property)
            .AnyAsync(r => r.RoomId == roomId && r.Property.LandlordId == landlordId);
    }
    public async Task<RoomFurniture?> GetFurnitureByCatalogAsync(int roomId, int catalogId)
    {
        return await _db.RoomFurnitures
            .FirstOrDefaultAsync(f =>
                f.RoomId == roomId &&
                f.FurnitureCatalogId == catalogId &&
                !f.IsDeleted);
    }
    public async Task DeleteImagesAsync(List<int> imageIds)
    {
        var images = await _db.StoredFileReferences
            .Where(x => imageIds.Contains(x.StoredFileId))
            .ToListAsync();

        _db.StoredFileReferences.RemoveRange(images);
    }
    public Task AddStoredFileAsync(StoredFile storedFile)
    {
        _db.StoredFiles.Add(storedFile);
        return Task.CompletedTask;
    }

    public Task AddStoredFileReferenceAsync(StoredFileReference reference)
    {
        _db.StoredFileReferences.Add(reference);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}