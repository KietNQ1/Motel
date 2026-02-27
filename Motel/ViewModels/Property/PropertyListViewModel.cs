namespace Motel.ViewModels.Property
{
    public class PropertyListViewModel
    {
        public int PropertyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int TotalRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public int AvailableRooms { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool HasRooms { get; set; }
    }
}
