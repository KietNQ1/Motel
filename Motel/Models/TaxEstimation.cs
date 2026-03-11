using System;
using System.Collections.Generic;

namespace Motel.Models;

public partial class TaxEstimation
{
    public int TaxEstimationId { get; set; }

    public int LandlordId { get; set; }

    public int Year { get; set; }

    public decimal TotalRevenue { get; set; }

    public decimal TaxableRevenue { get; set; }

    public decimal VatAmount { get; set; }

    public decimal PitAmount { get; set; }

    public decimal TotalTaxAmount { get; set; }

    public int TaxRuleId { get; set; }

    public bool IsExempt { get; set; }

    public string? Notes { get; set; }

    public DateTime CalculatedAt { get; set; }

    public virtual Landlord Landlord { get; set; } = null!;

    public virtual TaxRule TaxRule { get; set; } = null!;
}
