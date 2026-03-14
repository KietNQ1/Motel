using System;
using System.Collections.Generic;

namespace Motel.Models;

public partial class Room
{
    public int RoomId { get; set; }

    public int PropertyId { get; set; }

    public string RoomName { get; set; } = null!;

    public decimal RentPrice { get; set; }

    public string Status { get; set; } = null!;

    public int MaxOccupants { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Contract> Contracts { get; set; } = new List<Contract>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();

    public virtual Property Property { get; set; } = null!;

    public virtual ICollection<RoomOccupancy> RoomOccupancies { get; set; } = new List<RoomOccupancy>();

    public virtual ICollection<FeeSetting> FeeSettings { get; set; } = new List<FeeSetting>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public virtual ICollection<RoomFurniture> RoomFurnitures { get; set; } = new List<RoomFurniture>();
}
