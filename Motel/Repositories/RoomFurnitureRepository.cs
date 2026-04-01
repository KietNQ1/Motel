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

    public Task<List<RoomFurniture>> GetRoomFurnituresByLandlordAsync(int landlordId)
    {
        return _db.RoomFurnitures
            .AsNoTracking()
            .Include(f => f.FurnitureCatalog)
            .Include(f => f.FurnitureStatus)
            .Where(f => !f.IsDeleted && f.Room.Property.LandlordId == landlordId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
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

    public Task<int> GetRoomFurnitureCountByLandlordAsync(int landlordId)
    {
        return _db.RoomFurnitures
            .AsNoTracking()
            .CountAsync(rf => !rf.IsDeleted && rf.Room.Property.LandlordId == landlordId);
    }

    public Task<int> GetStoredFileCountByLandlordAsync(int landlordId)
    {
        return _db.StoredFiles
            .AsNoTracking()
            .CountAsync(sf => sf.LandlordId == landlordId);
    }

    public Task<List<StoredFile>> GetStoredFilesByLandlordAsync(int landlordId)
    {
        return _db.StoredFiles
            .AsNoTracking()
            .Where(sf => sf.LandlordId == landlordId)
            .OrderByDescending(sf => sf.UploadedAt)
            .ToListAsync();
    }

    public Task<int> GetStoredFileReferenceCountByLandlordAsync(int landlordId)
    {
        return _db.StoredFileReferences
            .AsNoTracking()
            .CountAsync(sfr => sfr.StoredFile.LandlordId == landlordId);
    }

    public Task<List<StoredFileReference>> GetStoredFileReferencesByLandlordAsync(int landlordId)
    {
        return _db.StoredFileReferences
            .AsNoTracking()
            .Where(sfr => sfr.StoredFile.LandlordId == landlordId)
            .OrderByDescending(sfr => sfr.CreatedAt)
            .ToListAsync();
    }

    public Task<int> GetFurnitureCatalogCountAsync()
    {
        return _db.FurnitureCatalogs
            .AsNoTracking()
            .CountAsync();
    }

    public Task<List<FurnitureCatalog>> GetFurnitureCatalogsAsync()
    {
        return _db.FurnitureCatalogs
            .AsNoTracking()
            .OrderBy(fc => fc.Name)
            .ToListAsync();
    }

    public Task<int> GetFurnitureStatusCountAsync()
    {
        return _db.FurnitureStatuses
            .AsNoTracking()
            .CountAsync();
    }

    public Task<List<FurnitureStatus>> GetFurnitureStatusesAsync()
    {
        return _db.FurnitureStatuses
            .AsNoTracking()
            .OrderBy(fs => fs.Name)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}