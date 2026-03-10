using Motel.Models;
using Motel.Repositories.Interface;
using Motel.Services.Interfaces;
using Motel.ViewModels.Tenant;

namespace Motel.Services
{
    public class TenantService : ITenantService
    {
        private readonly ITenantRepository _repo;
        private readonly IPropertyRepository _propertyRepo;
        public TenantService(ITenantRepository repo, IPropertyRepository propertyRepo) 
        {
            _repo = repo;
            _propertyRepo = propertyRepo;
        }

        public async Task<int> CreateTenantAsync(int landlordId, TenantCreateViewModel vm)
        {
            var tenant = new Tenant
            {
                LandlordId = landlordId,
                FullName = vm.FullName.Trim(),
                Phone = vm.Phone?.Trim(),
                Email = vm.Email?.Trim(),
                IdentityNo = vm.IdentityNo?.Trim(),
                IsDeleted = false,
                CreatedAt = DateTime.Now
            };

            return await _repo.CreateTenantAsync(tenant);
        }

        public Task<Tenant?> GetTenantDetailsAsync(int landlordId, int tenantId)
            => _repo.GetTenantByIdAsync(tenantId, landlordId);

        public async Task<PropertyTenantsViewModel?> GetTenantsByPropertyIdAsync(int landlordId, int propertyId)
        {
            var property = await _propertyRepo.GetPropertyByIdAsync(propertyId);
            if (property == null || property.LandlordId != landlordId) return null;

            var tenants = await _repo.GetTenantsByPropertyIdAsync(landlordId, propertyId);
            
            return new PropertyTenantsViewModel
            {
                PropertyId = propertyId,
                PropertyName = property.Name,
                Tenants = tenants
            };
        }
    }
}