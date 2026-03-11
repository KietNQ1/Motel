using System;
using System.Collections.Generic;

namespace Motel.Models;

public partial class TaxRule
{
    public int TaxRuleId { get; set; }

    public string RuleName { get; set; } = null!;

    public decimal VatRate { get; set; }

    public decimal PitRate { get; set; }

    public decimal RevenueThreshold { get; set; }

    public DateOnly EffectiveDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public bool IsActive { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<TaxEstimation> TaxEstimations { get; set; } = new List<TaxEstimation>();
}
