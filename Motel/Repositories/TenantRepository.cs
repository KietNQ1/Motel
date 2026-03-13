using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Repositories.Interface;
using Motel.ViewModels.Tenant;

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

        public async Task UpdateTenantAsync(Tenant tenant)
        {
            _db.Tenants.Update(tenant);
            await _db.SaveChangesAsync();
        }

        public async Task<List<TenantListItemViewModel>> GetTenantsByPropertyIdAsync(int landlordId, int propertyId)
        {
            return await _db.RoomOccupancies
                .Where(ro => ro.Room.PropertyId == propertyId && 
                             ro.Room.Property.LandlordId == landlordId)
                .Select(ro => new TenantListItemViewModel
                {
                    TenantId = ro.TenantId,
                    FullName = ro.Tenant.FullName,
                    Phone = ro.Tenant.Phone,
                    IdentityNo = ro.Tenant.IdentityNo,
                    RoomId = ro.RoomId,
                    RoomName = ro.Room.RoomName,
                    OccupancyStatus = ro.Status,
                    ContractId = ro.Tenant.Contracts
                        .Where(c => c.RoomId == ro.RoomId && c.Status == "active" && !c.IsDeleted)
                        .Select(c => (int?)c.ContractId)
                        .FirstOrDefault(),
                    StartDate = ro.Tenant.Contracts
                        .Where(c => c.RoomId == ro.RoomId && c.Status == "active" && !c.IsDeleted)
                        .Select(c => (DateOnly?)c.StartDate)
                        .FirstOrDefault(),
                    EndDate = ro.Tenant.Contracts
                        .Where(c => c.RoomId == ro.RoomId && c.Status == "active" && !c.IsDeleted)
                        .Select(c => (DateOnly?)c.EndDate)
                        .FirstOrDefault()
                })
                .OrderByDescending(t => t.OccupancyStatus == "active")
                .ThenBy(t => t.RoomName)
                .ThenBy(t => t.FullName)
                .ToListAsync();
        }
    }
}