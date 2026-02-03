using System;
using System.Collections.Generic;

namespace Motel.Models;

public partial class RoomOccupancy
{
    public int OccupancyId { get; set; }

    public int RoomId { get; set; }

    public int TenantId { get; set; }

    public DateOnly MoveInDate { get; set; }

    public DateOnly? MoveOutDate { get; set; }

    public bool IsPrimary { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Room Room { get; set; } = null!;

    public virtual Tenant Tenant { get; set; } = null!;
}
