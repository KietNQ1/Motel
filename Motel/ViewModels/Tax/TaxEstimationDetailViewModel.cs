using Motel.Models;

namespace Motel.ViewModels.Tax;

/// <summary>
/// ViewModel for displaying tax estimation details
/// </summary>
public class TaxEstimationDetailViewModel
{
    public int Year { get; set; }

    public decimal TotalRevenue { get; set; }

    public decimal RevenueThreshold { get; set; }

    public bool IsExempt { get; set; }

    public decimal TaxableRevenue { get; set; }

    public decimal VatRate { get; set; }

    public decimal VatAmount { get; set; }

    public decimal PitRate { get; set; }

    public decimal PitAmount { get; set; }

    public decimal TotalTaxAmount { get; set; }

    public string? Notes { get; set; }

    public DateTime CalculatedAt { get; set; }

    public string TaxRuleName { get; set; } = string.Empty;

    /// <summary>
    /// Map from TaxEstimation model to ViewModel
    /// </summary>
    public static TaxEstimationDetailViewModel FromModel(TaxEstimation model)
    {
        return new TaxEstimationDetailViewModel
        {
            Year = model.Year,
            TotalRevenue = model.TotalRevenue,
            RevenueThreshold = model.TaxRule.RevenueThreshold,
            IsExempt = model.IsExempt,
            TaxableRevenue = model.TaxableRevenue,
            VatRate = model.TaxRule.VatRate,
            VatAmount = model.VatAmount,
            PitRate = model.TaxRule.PitRate,
            PitAmount = model.PitAmount,
            TotalTaxAmount = model.TotalTaxAmount,
            Notes = model.Notes,
            CalculatedAt = model.CalculatedAt,
            TaxRuleName = model.TaxRule.RuleName
        };
    }
}
