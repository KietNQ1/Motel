using Motel.Models;

namespace Motel.Repositories.Interface
{
    public interface ITenantRepository
    {
        Task<List<Tenant>> GetTenantsByLandlordAsync(int landlordId);
        Task<Tenant?> GetTenantByIdAsync(int tenantId, int landlordId);
        Task<int> CreateTenantAsync(Tenant tenant);
    }
}