using System;
using System.Collections.Generic;

namespace Motel.Models;

public partial class RoomFurniture
{
    public int FurnitureId { get; set; }  // Primary Key
    public int RoomId { get; set; }       // Foreign Key to Room
    public int FurnitureCatalogId { get; set; } // Foreign Key to FurnitureCatalog
    public int Quantity { get; set; }     // Số lượng (e.g., 1)
    public string? Description { get; set; }  // Mô tả (e.g., "Panasonic, 1HP")
    public bool IsDeleted { get; set; } = false;  // Soft delete
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int FurnitureStatusId { get; set; }
    public FurnitureStatus FurnitureStatus { get; set; }
    public FurnitureCatalog FurnitureCatalog { get; set; } = null!;

    // Navigation properties
    public virtual Room Room { get; set; } = null!;
  
}