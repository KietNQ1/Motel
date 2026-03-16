namespace Motel.ViewModels.Room
{
    public class RoomFurnitureViewModel
    {
        public int FurnitureId { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; } = "";
        public string? Description { get; set; }
        public List<string> ImageUrls { get; set; } = new();
    }
}
