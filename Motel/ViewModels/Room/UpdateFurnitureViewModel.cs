using Microsoft.AspNetCore.Http;
using Motel.ViewModels.Common;
using Motel.Models;
namespace Motel.ViewModels.Room;

public class UpdateFurnitureViewModel
{
    public int FurnitureId { get; set; }
    public int FurnitureCatalogId { get; set; }

    public int RoomId { get; set; }

    public int Quantity { get; set; }
    public int FurnitureStatusId { get; set; }
    public List<FurnitureStatus> Statuses { get; set; } = new();
    public string? Description { get; set; }
    public List<IFormFile> Images { get; set; } = new();
    public List<FurnitureCatalog> Catalogs { get; set; } = new();

    // ảnh hiện tại
    public List<StoredFile> ExistingImages { get; set; } = new();
    public List<int> DeleteImageIds { get; set; } = new();
    public bool IsDeleted { get; set; }
}