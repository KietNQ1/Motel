using System;
using System.Collections.Generic;

namespace Motel.Models;

public partial class Transaction
{
    public int TransactionId { get; set; }

    public int LandlordId { get; set; }

    public int? RoomId { get; set; }

    public int? TenantId { get; set; }

    public int? ContractId { get; set; }

    public int? InvoiceId { get; set; }

    public string Type { get; set; } = null!;

    public decimal Amount { get; set; }

    public string Direction { get; set; } = null!;

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public int CreatedByUserId { get; set; }

    public virtual Contract? Contract { get; set; }

    public virtual AspNetUser CreatedByUser { get; set; } = null!;

    public virtual Invoice? Invoice { get; set; }

    public virtual Landlord Landlord { get; set; } = null!;

    public virtual Room? Room { get; set; }

    public virtual Tenant? Tenant { get; set; }
}
