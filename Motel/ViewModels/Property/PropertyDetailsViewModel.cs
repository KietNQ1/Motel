namespace Motel.ViewModels.Property
{
    public class PropertyDetailsViewModel
    {
        public int PropertyId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }

        // Rooms grouped by floor
        public Dictionary<int, List<RoomGridItemViewModel>> RoomsByFloor { get; set; } = new();
    }

    public class RoomGridItemViewModel
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public decimal RentPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public int MaxOccupants { get; set; }

        public bool HasTenant { get; set; }

        // NEW
        public int? ActiveContractId { get; set; }
        public int OccupantsCount { get; set; }
        public int? PrimaryTenantId { get; set; }
        public string? TenantName { get; set; } // Primary tenant name (nguoi dai dien)
    }
}
