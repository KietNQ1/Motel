using Motel.Models;

namespace Motel.Services.Interface;

/// <summary>
/// Service interface for tax estimation operations
/// </summary>
public interface ITaxService
{
    /// <summary>
    /// Calculate and save tax estimation for a landlord for a specific year
    /// </summary>
    /// <param name="landlordId">Landlord ID</param>
    /// <param name="year">Calendar year (e.g., 2026)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Calculated tax estimation</returns>
    Task<TaxEstimation> CalculateTaxEstimationAsync(int landlordId, int year, CancellationToken ct = default);

    /// <summary>
    /// Get tax estimation history for a landlord
    /// </summary>
    /// <param name="landlordId">Landlord ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of tax estimations ordered by year descending</returns>
    Task<List<TaxEstimation>> GetTaxEstimationHistoryAsync(int landlordId, CancellationToken ct = default);

    /// <summary>
    /// Get tax estimation for a specific year (if exists)
    /// </summary>
    /// <param name="landlordId">Landlord ID</param>
    /// <param name="year">Calendar year</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tax estimation or null</returns>
    Task<TaxEstimation?> GetTaxEstimationForYearAsync(int landlordId, int year, CancellationToken ct = default);
}
