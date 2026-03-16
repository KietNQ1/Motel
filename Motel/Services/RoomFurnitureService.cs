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
                .Where(sfr => sfr.StoredFile != null)
                .Select(sfr => sfr.StoredFile!.StoragePath)
                .ToListAsync();

            result.Add(new RoomFurnitureViewModel
            {
                FurnitureId = f.FurnitureId,
                Name = f.FurnitureCatalog.Name,
                Quantity = f.Quantity,
                Status = f.FurnitureStatus?.Name ?? string.Empty,
                Description = f.Description,
                ImageUrls = imageUrls
            });
        }

        return result;
    }

    public async Task<bool> AddFurnitureAsync(FurnitureRowVM model, int roomId, int landlordId)
    {
        //if (!await _repo.IsRoomOwnedByLandlordAsync(model.RoomId, landlordId))
        //    return false;

        // tìm nội thất trùng
        var existingFurniture = await _repo.GetFurnitureByCatalogAsync(roomId, model.FurnitureCatalogId);

        RoomFurniture furniture;

        if (existingFurniture != null)
        {
            // cộng số lượng nếu trùng
            existingFurniture.Quantity += model.Quantity;

            await _repo.UpdateFurnitureAsync(existingFurniture);

            furniture = existingFurniture;
        }
        else
        {
            furniture = new RoomFurniture
            {
                RoomId = roomId,
                FurnitureCatalogId = model.FurnitureCatalogId,
                Quantity = model.Quantity,
                FurnitureStatusId = model.FurnitureStatusId,
                IsDeleted = false,
                CreatedAt = DateTime.Now
            };

            await _repo.AddFurnitureAsync(furniture);
            await _repo.SaveChangesAsync();
        }

        // upload ảnh
        if (model.Images != null && model.Images.Any())
        {
            await UploadImagesAsync(model.Images, furniture.FurnitureId, landlordId);
        }

        return true;
   
    }

    public async Task<bool> UpdateFurnitureAsync(UpdateFurnitureViewModel model, int landlordId)
    {
        var furniture = await _repo.GetFurnitureByIdAsync(model.FurnitureId);

        if (furniture == null)
            return false;

        furniture.FurnitureCatalogId = model.FurnitureCatalogId;
        furniture.Quantity = model.Quantity;
        furniture.Description = model.Description;
        furniture.FurnitureStatusId = model.FurnitureStatusId;

        await _repo.UpdateFurnitureAsync(furniture);
        await _repo.SaveChangesAsync();

        // xóa ảnh
        if (model.DeleteImageIds != null && model.DeleteImageIds.Any())
        {
            await _repo.DeleteImagesAsync(model.DeleteImageIds);
            await _repo.SaveChangesAsync();
        }

        // upload ảnh mới
        if (model.Images != null && model.Images.Any())
        {
            await UploadImagesAsync(model.Images, furniture.FurnitureId, landlordId);
        }

        return true;
    }

    public async Task<bool> DeleteFurnitureAsync(int furnitureId, int landlordId)
    {
        var furniture = await _repo.GetFurnitureByIdAsync(furnitureId);

        if (furniture == null)
            return false;

        //if (!await _repo.IsRoomOwnedByLandlordAsync(furniture.RoomId, landlordId))
        //    return false;

        await _repo.DeleteFurnitureAsync(furniture);
        await _repo.SaveChangesAsync();

        return true;
    }

    private async Task UploadImagesAsync(List<IFormFile> images, int furnitureId, int landlordId)
    {
        var uploadFolder = Path.Combine(_env.WebRootPath, "uploads");

        if (!Directory.Exists(uploadFolder))
            Directory.CreateDirectory(uploadFolder);

        foreach (var image in images)
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
                StoragePath = "/uploads/" + fileName,
                MimeType = image.ContentType,
                LandlordId = landlordId,
                UploadedByUserId = 1
            };

            await _repo.AddStoredFileAsync(storedFile);

            var fileRef = new StoredFileReference
            {
                StoredFile = storedFile,
                RefType = "roomfurniture",
                RefId = furnitureId,
                CreatedAt = DateTime.Now
            };

            await _repo.AddStoredFileReferenceAsync(fileRef);
        }

        await _repo.SaveChangesAsync();
    }
}