using System;
using System.Collections.Generic;

namespace Motel.Models;

public partial class FeeType
{
    public int FeeTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsSystem { get; set; }

    public virtual ICollection<FeeSetting> FeeSettings { get; set; } = new List<FeeSetting>();
    public virtual ICollection<InvoiceLine> InvoiceLines { get; set; } = new List<InvoiceLine>();
}
