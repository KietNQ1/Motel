using System;
using System.Collections.Generic;

namespace Motel.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int LandlordId { get; set; }

    public int? TenantId { get; set; }

    public string Channel { get; set; } = null!;

    public string Type { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Content { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public virtual Landlord Landlord { get; set; } = null!;

    public virtual Tenant? Tenant { get; set; }
}
