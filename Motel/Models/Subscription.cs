using System;
using System.Collections.Generic;

namespace Motel.Models;

public partial class Subscription
{
    public int SubscriptionId { get; set; }

    public int LandlordId { get; set; }

    public string PlanName { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Landlord Landlord { get; set; } = null!;
}
