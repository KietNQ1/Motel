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
            .Where(f => f.RoomId == roomId && !f.IsDeleted)
            .ToListAsync();
    }

    public async Task<RoomFurniture?> GetFurnitureByIdAsync(int furnitureId)
    {
        return await _db.RoomFurnitures
            .FirstOrDefaultAsync(f => f.FurnitureId == furnitureId && !f.IsDeleted);
    }

    public async Task AddFurnitureAsync(RoomFurniture furniture)
    {
        _db.RoomFurnitures.Add(furniture);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateFurnitureAsync(RoomFurniture furniture)
    {
        _db.RoomFurnitures.Update(furniture);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteFurnitureAsync(int furnitureId)
    {
        var furniture = await GetFurnitureByIdAsync(furnitureId);
        if (furniture != null)
        {
            furniture.IsDeleted = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> IsRoomOwnedByLandlordAsync(int roomId, int landlordId)
    {
        return await _db.Rooms
            .Include(r => r.Property)
            .AnyAsync(r => r.RoomId == roomId && r.Property.LandlordId == landlordId);
    }

    public async Task AddStoredFileAsync(StoredFile storedFile)
    {
        _db.StoredFiles.Add(storedFile);
        await Task.CompletedTask;
    }

    public async Task AddStoredFileReferenceAsync(StoredFileReference reference)
    {
        _db.StoredFileReferences.Add(reference);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}