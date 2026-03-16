using System;
using System.Collections.Generic;

namespace Motel.Models;

public partial class FeeSetting
{
    public int FeeSettingId { get; set; }

    public int? PropertyId { get; set; }

    public int? RoomId { get; set; }

    public int FeeTypeId { get; set; }

    public string CalculationMethod { get; set; } = null!;

    public decimal UnitPrice { get; set; }

    public decimal BaseAmount { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual FeeType FeeType { get; set; } = null!;

    public virtual Property? Property { get; set; }

    public virtual Room? Room { get; set; }
}
