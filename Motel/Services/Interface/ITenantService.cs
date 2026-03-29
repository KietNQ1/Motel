using Motel.Models;
using Motel.ViewModels.Tenant;

namespace Motel.Services.Interfaces
{
    public interface ITenantService
    {
        Task<int> CreateTenantAsync(int landlordId, TenantCreateViewModel vm);
        Task<Tenant?> GetTenantDetailsAsync(int landlordId, int tenantId);
        Task<PropertyTenantsViewModel?> GetTenantsByPropertyIdAsync(int landlordId, int propertyId);
        Task<TenantEditViewModel?> BuildEditViewModelAsync(int landlordId, int tenantId);
        Task<bool> UpdateTenantAsync(int landlordId, TenantEditViewModel vm);
        Task<(string? frontImage, string? backImage)> GetTenantCccdImagesAsync(int tenantId);
        Task<string?> GetTenantResidenceProofImageAsync(int tenantId);
        Task<CT01ViewModel?> GetCT01DataAsync(int landlordId, int tenantId);
    }
}