using System;
using System.Collections.Generic;

namespace Motel.Models;

public partial class AspNetUser
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Landlord? Landlord { get; set; }

    public virtual ICollection<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();

    public virtual ICollection<StoredFile> StoredFiles { get; set; } = new List<StoredFile>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
