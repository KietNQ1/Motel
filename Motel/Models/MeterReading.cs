using System;
using System.Collections.Generic;

namespace Motel.Models;

public partial class MeterReading
{
    public int MeterReadingId { get; set; }

    public int RoomId { get; set; }

    public int PeriodMonth { get; set; }

    public int ElectricOld { get; set; }

    public int ElectricNew { get; set; }

    public int WaterOld { get; set; }

    public int WaterNew { get; set; }

    public DateTime RecordedAt { get; set; }

    public int RecordedByUserId { get; set; }

    public virtual AspNetUser RecordedByUser { get; set; } = null!;

    public virtual Room Room { get; set; } = null!;
}
