using Motel.Models;

namespace Motel.Repositories.Interface;

/// <summary>
/// Repository interface for tax-related data operations
/// </summary>
public interface ITaxRepository
{
    /// <summary>
    /// Get the active tax rule for a specific date
    /// </summary>
    /// <param name="effectiveDate">The date to check (default: today)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Active tax rule or null if not found</returns>
    Task<TaxRule?> GetActiveTaxRuleAsync(DateOnly? effectiveDate = null, CancellationToken ct = default);

    /// <summary>
    /// Get all active contracts for a landlord in a specific calendar year
    /// Used to calculate total rental revenue
    /// </summary>
    /// <param name="landlordId">Landlord ID</param>
    /// <param name="year">Calendar year (e.g., 2026)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of contracts with room and property information</returns>
    Task<List<Contract>> GetLandlordContractsForYearAsync(int landlordId, int year, CancellationToken ct = default);

    /// <summary>
    /// Get existing tax estimation for a landlord and year
    /// </summary>
    /// <param name="landlordId">Landlord ID</param>
    /// <param name="year">Calendar year</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tax estimation or null if not exists</returns>
    Task<TaxEstimation?> GetTaxEstimationAsync(int landlordId, int year, CancellationToken ct = default);

    /// <summary>
    /// Get all tax estimations for a landlord (ordered by year descending)
    /// </summary>
    /// <param name="landlordId">Landlord ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of tax estimations with tax rule information</returns>
    Task<List<TaxEstimation>> GetLandlordTaxEstimationsAsync(int landlordId, CancellationToken ct = default);

    /// <summary>
    /// Create or update tax estimation for a landlord and year
    /// </summary>
    /// <param name="estimation">Tax estimation entity</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created or updated tax estimation ID</returns>
    Task<int> SaveTaxEstimationAsync(TaxEstimation estimation, CancellationToken ct = default);

    /// <summary>
    /// Get landlord by ID (with basic info)
    /// </summary>
    /// <param name="landlordId">Landlord ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Landlord or null</returns>
    Task<Landlord?> GetLandlordAsync(int landlordId, CancellationToken ct = default);
}
