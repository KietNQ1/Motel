using Motel.Models;
using Motel.Repositories.Interface;
using Motel.Services.Interface;

namespace Motel.Services;

/// <summary>
/// Service implementation for tax estimation operations
/// Handles business logic for rental income tax calculation
/// </summary>
public class TaxService : ITaxService
{
    private readonly ITaxRepository _repo;

    public TaxService(ITaxRepository repo) => _repo = repo;

    /// <summary>
    /// Calculate and save tax estimation for a landlord for a specific year
    /// 
    /// Business Logic Points (Addressing Common Pitfalls):
    /// 
    /// 1. Revenue Calculation:
    ///    - Based on CONTRACT AMOUNT (Room.RentPrice), NOT actual payments
    ///    - Only includes RENT, excludes utilities (electricity, water) collected on behalf
    ///    - Prorated based on overlap with calendar year, not full contract period
    /// 
    /// 2. Tax Threshold (100 Million VND):
    ///    - Calculated per CALENDAR YEAR (Jan 1 - Dec 31), not contract period
    ///    - If total annual revenue <= threshold, tax is EXEMPT
    /// 
    /// 3. Tax Rates (Vietnam Circular 40/2021/TT-BTC):
    ///    - VAT: 5% of taxable revenue
    ///    - PIT: 5% of taxable revenue
    ///    - Total: 10% effective tax rate
    /// 
    /// 4. Multiple Properties:
    ///    - Tax is calculated on TOTAL revenue from all properties owned by landlord
    ///    - Not calculated per property
    /// 
    /// 5. This is ESTIMATION only:
    ///    - Not official tax declaration
    ///    - Official filing must be done through Vietnam Tax Authority system
    /// </summary>
    public async Task<TaxEstimation> CalculateTaxEstimationAsync(int landlordId, int year, CancellationToken ct = default)
    {
        // Validate landlord exists
        var landlord = await _repo.GetLandlordAsync(landlordId, ct);
        if (landlord == null)
            throw new InvalidOperationException($"Landlord with ID {landlordId} not found.");

        // Get active tax rule
        var taxRule = await _repo.GetActiveTaxRuleAsync(new DateOnly(year, 1, 1), ct);
        if (taxRule == null)
            throw new InvalidOperationException($"No active tax rule found for year {year}.");

        // Get all contracts for this landlord that overlap with the target year
        var contracts = await _repo.GetLandlordContractsForYearAsync(landlordId, year, ct);

        // Calculate total revenue for the year
        var totalRevenue = CalculateYearlyRevenue(contracts, year);

        // Determine if exempt from tax (below threshold)
        var isExempt = totalRevenue <= taxRule.RevenueThreshold;

        // Calculate tax amounts
        decimal taxableRevenue = isExempt ? 0 : totalRevenue;
        decimal vatAmount = taxableRevenue * taxRule.VatRate;
        decimal pitAmount = taxableRevenue * taxRule.PitRate;
        decimal totalTaxAmount = vatAmount + pitAmount;

        // Create tax estimation entity
        var estimation = new TaxEstimation
        {
            LandlordId = landlordId,
            Year = year,
            TotalRevenue = totalRevenue,
            TaxableRevenue = taxableRevenue,
            VatAmount = vatAmount,
            PitAmount = pitAmount,
            TotalTaxAmount = totalTaxAmount,
            TaxRuleId = taxRule.TaxRuleId,
            IsExempt = isExempt,
            Notes = BuildEstimationNotes(totalRevenue, taxRule.RevenueThreshold, isExempt, contracts.Count),
            CalculatedAt = DateTime.Now
        };

        // Save estimation
        await _repo.SaveTaxEstimationAsync(estimation, ct);

        // Reload with tax rule info for return
        var saved = await _repo.GetTaxEstimationAsync(landlordId, year, ct);
        return saved ?? estimation;
    }

    /// <summary>
    /// Get tax estimation history for a landlord
    /// </summary>
    public async Task<List<TaxEstimation>> GetTaxEstimationHistoryAsync(int landlordId, CancellationToken ct = default)
    {
        return await _repo.GetLandlordTaxEstimationsAsync(landlordId, ct);
    }

    /// <summary>
    /// Get tax estimation for a specific year (if exists)
    /// </summary>
    public async Task<TaxEstimation?> GetTaxEstimationForYearAsync(int landlordId, int year, CancellationToken ct = default)
    {
        return await _repo.GetTaxEstimationAsync(landlordId, year, ct);
    }

    // ============================================
    // Private Helper Methods
    // ============================================

    /// <summary>
    /// Calculate total rental revenue for a specific calendar year
    /// 
    /// Key Points:
    /// - Only counts rent (Room.RentPrice), not utilities
    /// - Prorates revenue based on overlap with calendar year
    /// - Handles contracts that start or end mid-year
    /// - Handles contracts that span multiple years
    /// </summary>
    private decimal CalculateYearlyRevenue(List<Contract> contracts, int year)
    {
        var yearStart = new DateOnly(year, 1, 1);
        var yearEnd = new DateOnly(year, 12, 31);

        decimal totalRevenue = 0;

        foreach (var contract in contracts)
        {
            // Determine the overlap period between contract and calendar year
            var overlapStart = contract.StartDate > yearStart ? contract.StartDate : yearStart;
            var overlapEnd = contract.EndDate < yearEnd ? contract.EndDate : yearEnd;

            // Calculate number of days in overlap
            var daysInOverlap = overlapEnd.DayNumber - overlapStart.DayNumber + 1;

            if (daysInOverlap <= 0)
                continue; // No overlap (shouldn't happen due to query filter, but safety check)

            // Get monthly rent from Room
            var monthlyRent = contract.Room?.RentPrice ?? 0;

            // Prorate rent based on overlap days
            // Formula: (monthlyRent / 30) * daysInOverlap
            // Note: Using 30 days per month as standard practice in rental calculation
            var proratedRevenue = (monthlyRent / 30m) * daysInOverlap;

            totalRevenue += proratedRevenue;
        }

        return Math.Round(totalRevenue, 2);
    }

    /// <summary>
    /// Build human-readable notes for tax estimation
    /// </summary>
    private string BuildEstimationNotes(decimal totalRevenue, decimal threshold, bool isExempt, int contractCount)
    {
        var notes = $"Tính toán dựa trên {contractCount} hợp đồng. ";

        if (isExempt)
        {
            notes += $"Tổng doanh thu {totalRevenue:N0} VND không vượt ngưỡng {threshold:N0} VND nên được MIỄN THUẾ. ";
        }
        else
        {
            notes += $"Tổng doanh thu {totalRevenue:N0} VND vượt ngưỡng {threshold:N0} VND nên phải nộp thuế. ";
        }

        notes += "Đây chỉ là ước tính, kê khai chính thức thực hiện qua hệ thống của Tổng cục Thuế.";

        return notes;
    }
}
