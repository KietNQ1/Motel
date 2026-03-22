using System;
using System.Collections.Generic;

namespace Motel.Models;

public partial class Landlord
{
    public int LandlordId { get; set; }

    public int UserId { get; set; }

    public string DisplayName { get; set; } = null!;

    public string? Address { get; set; }

    public string? IdentityNo { get; set; }

    public string? PermanentAddress { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Property> Properties { get; set; } = new List<Property>();

    public virtual ICollection<StoredFile> StoredFiles { get; set; } = new List<StoredFile>();

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

    public virtual ICollection<TaxEstimation> TaxEstimations { get; set; } = new List<TaxEstimation>();

    public virtual ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public virtual ApplicationUser User { get; set; } = null!;

    public virtual ICollection<LandlordBankAccount> LandlordBankAccounts { get; set; } = new List<LandlordBankAccount>();
}
