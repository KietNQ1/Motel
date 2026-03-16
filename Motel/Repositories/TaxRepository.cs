using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Repositories.Interface;

namespace Motel.Repositories;

/// <summary>
/// Repository implementation for tax-related data operations
/// </summary>
public sealed class TaxRepository : ITaxRepository
{
    private readonly MotelDbContext _db;

    public TaxRepository(MotelDbContext db) => _db = db;

    /// <summary>
    /// Lấy quy định thuế đang áp dụng cho một ngày cụ thể
    /// Get the active tax rule for a specific date
    /// 
    /// Business Logic: 
    /// - Chỉ lấy rule có IsActive = true
    /// - effectiveDate <= targetDate (đã bắt đầu áp dụng)
    /// - endDate = null HOẶC endDate >= targetDate (chưa hết hạn)
    /// - Nếu có nhiều rule, lấy rule mới nhất (OrderBy EffectiveDate DESC)
    /// </summary>
    public async Task<TaxRule?> GetActiveTaxRuleAsync(DateOnly? effectiveDate = null, CancellationToken ct = default)
    {
        var targetDate = effectiveDate ?? DateOnly.FromDateTime(DateTime.Today);

        return await _db.TaxRules
            .AsNoTracking()
            .Where(r =>
                r.IsActive &&
                r.EffectiveDate <= targetDate &&
                (r.EndDate == null || r.EndDate >= targetDate))
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Lấy tất cả hợp đồng của chủ nhà có overlap với một năm cụ thể
    /// Get all active contracts for a landlord in a specific calendar year
    /// 
    /// Business Logic:
    /// - Chỉ lấy hợp đồng KHÔNG bị xóa (IsDeleted = false)
    /// - Thuộc property của landlord này
    /// - Property không bị xóa
    /// - Hợp đồng có overlap với năm: StartDate <= yearEnd AND EndDate >= yearStart
    /// - Include Room và Property để lấy RentPrice và verify ownership
    /// </summary>
        public async Task<List<Contract>> GetLandlordContractsForYearAsync(int landlordId, int year, CancellationToken ct = default)
    {
        var yearStart = new DateOnly(year, 1, 1);
        var yearEnd = new DateOnly(year, 12, 31);

        return await _db.Contracts
            .AsNoTracking()
            .Include(c => c.Room)
            .ThenInclude(r => r.Property)
            .Include(c => c.Room)
            .ThenInclude(r => r.FeeSettings)
                .ThenInclude(fs => fs.FeeType)
            .Where(c =>
                !c.IsDeleted &&
                c.Room.Property.LandlordId == landlordId &&
                !c.Room.Property.IsDeleted &&
                // Hợp đồng overlap với năm: Contract overlaps with the year
                c.StartDate <= yearEnd &&
                c.EndDate >= yearStart)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Get existing tax estimation for a landlord and year
    /// </summary>
    public async Task<TaxEstimation?> GetTaxEstimationAsync(int landlordId, int year, CancellationToken ct = default)
    {
        return await _db.TaxEstimations
            .AsNoTracking()
            .Include(e => e.TaxRule)
            .FirstOrDefaultAsync(e =>
                e.LandlordId == landlordId &&
                e.Year == year, ct);
    }

    /// <summary>
    /// Get all tax estimations for a landlord (ordered by year descending)
    /// </summary>
    public async Task<List<TaxEstimation>> GetLandlordTaxEstimationsAsync(int landlordId, CancellationToken ct = default)
    {
        return await _db.TaxEstimations
            .AsNoTracking()
            .Include(e => e.TaxRule)
            .Where(e => e.LandlordId == landlordId)
            .OrderByDescending(e => e.Year)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Create or update tax estimation for a landlord and year
    /// Business Logic: If estimation exists for the same landlord and year, update it; otherwise create new
    /// </summary>
    public async Task<int> SaveTaxEstimationAsync(TaxEstimation estimation, CancellationToken ct = default)
    {
        var existing = await _db.TaxEstimations
            .FirstOrDefaultAsync(e =>
                e.LandlordId == estimation.LandlordId &&
                e.Year == estimation.Year, ct);

        if (existing != null)
        {
            // Update existing
            existing.TotalRevenue = estimation.TotalRevenue;
            existing.TaxableRevenue = estimation.TaxableRevenue;
            existing.VatAmount = estimation.VatAmount;
            existing.PitAmount = estimation.PitAmount;
            existing.TotalTaxAmount = estimation.TotalTaxAmount;
            existing.TaxRuleId = estimation.TaxRuleId;
            existing.IsExempt = estimation.IsExempt;
            existing.Notes = estimation.Notes;
            existing.CalculatedAt = DateTime.Now;

            await _db.SaveChangesAsync(ct);
            return existing.TaxEstimationId;
        }
        else
        {
            // Create new
            estimation.CalculatedAt = DateTime.Now;
            _db.TaxEstimations.Add(estimation);
            await _db.SaveChangesAsync(ct);
            return estimation.TaxEstimationId;
        }
    }

    /// <summary>
    /// Get landlord by ID (with basic info)
    /// </summary>
    public async Task<Landlord?> GetLandlordAsync(int landlordId, CancellationToken ct = default)
    {
        return await _db.Landlords
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.LandlordId == landlordId && !l.IsDeleted, ct);
    }
}
