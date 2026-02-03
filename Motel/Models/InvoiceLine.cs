using System;
using System.Collections.Generic;

namespace Motel.Models;

public partial class InvoiceLine
{
    public int InvoiceLineId { get; set; }

    public int InvoiceId { get; set; }

    public string ItemType { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? LineTotal { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;
}
