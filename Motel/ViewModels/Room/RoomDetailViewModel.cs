namespace Motel.ViewModels.Room
{
    public class RoomDetailViewModel
    {
        // Room Info
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public decimal RentPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public int MaxOccupants { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public int PropertyId { get; set; }

        // Tenant Info (if occupied)
        public bool HasTenant { get; set; }
        public List<TenantViewModel> Tenants { get; set; } = new();

        // Contract Info
        public int? ContractId { get; set; }
        public DateOnly? ContractStartDate { get; set; }
        public DateOnly? ContractEndDate { get; set; }
        public decimal? DepositAmount { get; set; }

        // Utility Settings
        public decimal ElectricUnitPrice { get; set; }
        public decimal WaterUnitPrice { get; set; }
        public decimal InternetFee { get; set; }
        public decimal TrashFee { get; set; }

        // Meter Readings (current month)
        public int? CurrentElectricOld { get; set; }
        public int? CurrentElectricNew { get; set; }
        public int? CurrentWaterOld { get; set; }
        public int? CurrentWaterNew { get; set; }
        public int? CurrentPeriodMonth { get; set; }

        // Previous month readings
        public int? PreviousElectricOld { get; set; }
        public int? PreviousElectricNew { get; set; }
        public int? PreviousWaterOld { get; set; }
        public int? PreviousWaterNew { get; set; }
        public int? PreviousPeriodMonth { get; set; }

        // Calculated values
        public int ElectricUsage => (CurrentElectricNew ?? 0) - (CurrentElectricOld ?? 0);
        public int WaterUsage => (CurrentWaterNew ?? 0) - (CurrentWaterOld ?? 0);
        public decimal EstimatedTotal => RentPrice + 
            (ElectricUsage * ElectricUnitPrice) + 
            (WaterUsage * WaterUnitPrice) + 
            InternetFee + 
            TrashFee;

        public List<RoomFurnitureViewModel> Furnitures { get; set; } = new();
    }

    public class TenantViewModel
    {
        public int TenantId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public bool IsPrimary { get; set; }
        public int? ContractId { get; set; }  // contract for this specific tenant
        public bool IsTemporaryResidenceRegistered { get; set; }
    }
}
