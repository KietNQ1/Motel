using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Repositories.Interface;

namespace Motel.Repositories
{
    public sealed class TaxRepository : ITaxRepository
    {
        private readonly MotelDbContext _db;
        public TaxRepository(MotelDbContext db) => _db = db;

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
                    c.StartDate <= yearEnd &&
                    c.EndDate >= yearStart)
                .ToListAsync(ct);
        }

        public async Task<TaxEstimation?> GetTaxEstimationAsync(int landlordId, int year, CancellationToken ct = default)
        {
            return await _db.TaxEstimations
                .AsNoTracking()
                .Include(e => e.TaxRule)
                .FirstOrDefaultAsync(e =>
                    e.LandlordId == landlordId &&
                    e.Year == year, ct);
        }

        public async Task<List<TaxEstimation>> GetLandlordTaxEstimationsAsync(int landlordId, CancellationToken ct = default)
        {
            return await _db.TaxEstimations
                .AsNoTracking()
                .Include(e => e.TaxRule)
                .Where(e => e.LandlordId == landlordId)
                .OrderByDescending(e => e.Year)
                .ToListAsync(ct);
        }

        public async Task<int> SaveTaxEstimationAsync(TaxEstimation estimation, CancellationToken ct = default)
        {
            var existing = await _db.TaxEstimations
                .FirstOrDefaultAsync(e =>
                    e.LandlordId == estimation.LandlordId &&
                    e.Year == estimation.Year, ct);

            if (existing != null)
            {
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
                estimation.CalculatedAt = DateTime.Now;
                _db.TaxEstimations.Add(estimation);
                await _db.SaveChangesAsync(ct);
                return estimation.TaxEstimationId;
            }
        }

        public async Task<Landlord?> GetLandlordAsync(int landlordId, CancellationToken ct = default)
        {
            return await _db.Landlords
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.LandlordId == landlordId && !l.IsDeleted, ct);
        }

        public async Task<List<TaxEstimation>> GetTaxEstimationsByUserIdAsync(int userId)
        {
            return await _db.TaxEstimations.Where(t => t.LandlordId == userId).ToListAsync();
        }
    }
}
