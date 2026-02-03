using System;
using System.Collections.Generic;

namespace Motel.Models;

public partial class RoomUtilitySetting
{
    public int UtilitySettingId { get; set; }

    public int RoomId { get; set; }

    public decimal ElectricUnitPrice { get; set; }

    public decimal WaterUnitPrice { get; set; }

    public decimal InternetFee { get; set; }

    public decimal TrashFee { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Room Room { get; set; } = null!;
}
