using System;
using System.Collections.Generic;

namespace Motel.Models;

public partial class Tenant
{
    public int TenantId { get; set; }

    public int LandlordId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? IdentityNo { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? PermanentAddress { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public virtual Landlord Landlord { get; set; } = null!;

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<RoomOccupancy> RoomOccupancies { get; set; } = new List<RoomOccupancy>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
