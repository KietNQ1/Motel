using Motel.Models;
using Motel.ViewModels.Tenant;

namespace Motel.Services.Interfaces
{
    public interface ITenantService
    {
        Task<int> CreateTenantAsync(int landlordId, TenantCreateViewModel vm);
        Task<Tenant?> GetTenantDetailsAsync(int landlordId, int tenantId);
        Task<PropertyTenantsViewModel?> GetTenantsByPropertyIdAsync(int landlordId, int propertyId);
    }
}