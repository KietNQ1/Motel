namespace Motel.Models
{
    public class FurnitureCatalog
    {
        public int FurnitureCatalogId { get; set; }

        public string Name { get; set; } = "";

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<RoomFurniture> RoomFurnitures { get; set; }
    }
}
