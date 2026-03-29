using System;
using System.Collections.Generic;

namespace Motel.Models;

public partial class LandlordBankAccount
{
    public int LandlordBankAccountId { get; set; }

    public int LandlordId { get; set; }

    public string BankName { get; set; } = null!;

    public string BankAccountNumber { get; set; } = null!;

    public string BankAccountName { get; set; } = null!;

    public string BankCode { get; set; } = null!;

    public bool IsPrimary { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Landlord Landlord { get; set; } = null!;
}

