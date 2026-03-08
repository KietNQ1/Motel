using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Repositories.Interface;

namespace Motel.Repositories
{
    public class TenantRepository : ITenantRepository
    {
        private readonly MotelDbContext _db;
        public TenantRepository(MotelDbContext db) => _db = db;

        public async Task<List<Tenant>> GetTenantsByLandlordAsync(int landlordId)
        {
            return await _db.Tenants
                .Where(t => t.LandlordId == landlordId && !t.IsDeleted)
                .OrderBy(t => t.FullName)
                .ToListAsync();
        }

        public async Task<Tenant?> GetTenantByIdAsync(int tenantId, int landlordId)
        {
            return await _db.Tenants
                .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.LandlordId == landlordId && !t.IsDeleted);
        }

        public async Task<int> CreateTenantAsync(Tenant tenant)
        {
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();
            return tenant.TenantId;
        }
    }
}