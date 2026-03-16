using System;
using System.Collections.Generic;

namespace Motel.Models;

public partial class Property
{
    public int PropertyId { get; set; }

    public int LandlordId { get; set; }

    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Landlord Landlord { get; set; } = null!;

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();

    public virtual ICollection<FeeSetting> FeeSettings { get; set; } = new List<FeeSetting>();
}
