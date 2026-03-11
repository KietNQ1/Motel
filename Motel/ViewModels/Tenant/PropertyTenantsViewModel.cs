using System;
using System.Collections.Generic;

namespace Motel.ViewModels.Tenant
{
    public class TenantListItemViewModel
    {
        public int TenantId { get; set; }
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string? IdentityNo { get; set; }
        
        public int RoomId { get; set; }
        public string RoomName { get; set; } = null!;
        public string OccupancyStatus { get; set; } = null!;
        
        public int? ContractId { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
    }

    public class PropertyTenantsViewModel
    {
        public int PropertyId { get; set; }
        public string PropertyName { get; set; } = null!;
        public List<TenantListItemViewModel> Tenants { get; set; } = new();
    }
}
