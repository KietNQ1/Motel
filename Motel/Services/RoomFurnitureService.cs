using Motel.Repositories.Interface;
using Motel.Services.Interface;
using Motel.ViewModels.Room;
using Motel.Models;
using Microsoft.EntityFrameworkCore;

namespace Motel.Services;

public class RoomFurnitureService : IRoomFurnitureService
{
    private readonly IRoomFurnitureRepository _repo;
    private readonly IWebHostEnvironment _env;
    private readonly Motel.Data.MotelDbContext _context;

    public RoomFurnitureService(
        IRoomFurnitureRepository repo,
        IWebHostEnvironment env,
        Motel.Data.MotelDbContext context)
    {
        _repo = repo;
        _env = env;
        _context = context;
    }

    public async Task<IEnumerable<RoomFurnitureViewModel>> GetFurnituresForRoomAsync(int roomId)
    {
        var furnitures = await _repo.GetFurnituresByRoomIdAsync(roomId);

        var result = new List<RoomFurnitureViewModel>();

        foreach (var f in furnitures)
        {
            var imageUrls = await _context.StoredFileReferences
                .Where(sfr => sfr.RefType == "roomfurniture" && sfr.RefId == f.FurnitureId)
                .Include(sfr => sfr.StoredFile)
                .Select(sfr => sfr.StoredFile.StoragePath)
                .ToListAsync();

            result.Add(new RoomFurnitureViewModel
            {
                FurnitureId = f.FurnitureId,
                Name = f.Name,
                Quantity = f.Quantity,
                Description = f.Description,
                ImageUrls = imageUrls
            });
        }

        return result;
    }

    public async Task<bool> AddFurnitureAsync(AddFurnitureViewModel model, int landlordId)
    {
        if (!await _repo.IsRoomOwnedByLandlordAsync(model.RoomId, landlordId))
            return false;

        var furniture = new RoomFurniture
        {
            RoomId = model.RoomId,
            Name = model.Name,
            Quantity = model.Quantity,
            Description = model.Description,
            IsDeleted = false,
            CreatedAt = DateTime.Now
        };

        await _repo.AddFurnitureAsync(furniture);

        if (model.Images != null && model.Images.Any())
        {
            var uploadFolder = Path.Combine(_env.WebRootPath, "uploads");

            if (!Directory.Exists(uploadFolder))
                Directory.CreateDirectory(uploadFolder);

            foreach (var image in model.Images)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                var physicalPath = Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(physicalPath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                var storedFile = new StoredFile
                {
                    FileName = image.FileName,
                    StoragePath = $"/uploads/{fileName}",
                    MimeType = image.ContentType,
                    LandlordId = landlordId,
                    UploadedByUserId = 1
                };

                await _repo.AddStoredFileAsync(storedFile);
                await _repo.SaveChangesAsync();

                var fileRef = new StoredFileReference
                {
                    StoredFileId = storedFile.StoredFileId,
                    RefType = "roomfurniture",
                    RefId = furniture.FurnitureId,
                    CreatedAt = DateTime.Now
                };

                await _repo.AddStoredFileReferenceAsync(fileRef);
            }

            await _repo.SaveChangesAsync();
        }

        return true;
    }

    public async Task<bool> UpdateFurnitureAsync(UpdateFurnitureViewModel model, int landlordId)
    {
        var furniture = await _repo.GetFurnitureByIdAsync(model.FurnitureId);

        if (furniture == null)
            return false;

        if (!await _repo.IsRoomOwnedByLandlordAsync(furniture.RoomId, landlordId))
            return false;

        furniture.Name = model.Name;
        furniture.Quantity = model.Quantity;
        furniture.Description = model.Description;

        await _repo.UpdateFurnitureAsync(furniture);

        return true;
    }

    public async Task<bool> DeleteFurnitureAsync(int furnitureId, int landlordId)
    {
        var furniture = await _repo.GetFurnitureByIdAsync(furnitureId);

        if (furniture == null)
            return false;

        if (!await _repo.IsRoomOwnedByLandlordAsync(furniture.RoomId, landlordId))
            return false;

        await _repo.DeleteFurnitureAsync(furnitureId);

        return true;
    }
}