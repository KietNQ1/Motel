using Microsoft.AspNetCore.Http;
using Motel.Models;
using System.ComponentModel.DataAnnotations;

namespace Motel.ViewModels.Room;

public class AddFurnitureViewModel
{
    public int RoomId { get; set; }

    public List<FurnitureCatalog> Catalogs { get; set; } = new();
    public List<FurnitureStatus> Statuses { get; set; } = new();

    public List<FurnitureRowVM> Furnitures { get; set; } = new();

    // Fields for adding new furniture manually
    public string? NewFurnitureName { get; set; }
    public int? NewFurnitureQuantity { get; set; }
    public int? NewFurnitureStatusId { get; set; }
    public List<IFormFile> NewFurnitureImages { get; set; } = new();
}

public class FurnitureRowVM
{
    public int FurnitureCatalogId { get; set; }

    public bool Selected { get; set; }

    public int Quantity { get; set; }

    public int FurnitureStatusId { get; set; }

    public List<IFormFile> Images { get; set; } = new();
}