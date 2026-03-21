using Motel.Models;
using Motel.Repositories.Interface;
using Motel.Services.Interfaces;
using Motel.ViewModels.Tenant;
using Motel.Data;

namespace Motel.Services
{
    public class TenantService : ITenantService
    {
        private readonly ITenantRepository _repo;
        private readonly IPropertyRepository _propertyRepo;
        private readonly MotelDbContext _context;

        public TenantService(ITenantRepository repo, IPropertyRepository propertyRepo, MotelDbContext context) 
        {
            _repo = repo;
            _propertyRepo = propertyRepo;
            _context = context;
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
                DateOfBirth = vm.DateOfBirth,
                PermanentAddress = vm.PermanentAddress?.Trim(),
                IsDeleted = false,
                CreatedAt = DateTime.Now
            };

            var tenantId = await _repo.CreateTenantAsync(tenant);

            if (vm.CccdImageId.HasValue && vm.CccdImageId.Value > 0)
            {
                var storedFileRef = new StoredFileReference
                {
                    StoredFileId = vm.CccdImageId.Value,
                    RefType = "tenant",
                    RefId = tenantId,
                    CreatedAt = DateTime.Now
                };
                _context.StoredFileReferences.Add(storedFileRef);
                await _context.SaveChangesAsync();
            }

            return tenantId;
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

        public async Task<TenantEditViewModel?> BuildEditViewModelAsync(int landlordId, int tenantId)
        {
            var tenant = await _repo.GetTenantByIdAsync(tenantId, landlordId);
            if (tenant == null) return null;

            return new TenantEditViewModel
            {
                TenantId = tenant.TenantId,
                FullName = tenant.FullName,
                Phone = tenant.Phone,
                Email = tenant.Email,
                IdentityNo = tenant.IdentityNo,
                DateOfBirth = tenant.DateOfBirth,
                PermanentAddress = tenant.PermanentAddress
            };
        }

        public async Task<bool> UpdateTenantAsync(int landlordId, TenantEditViewModel vm)
        {
            var tenant = await _repo.GetTenantByIdAsync(vm.TenantId, landlordId);
            if (tenant == null) return false;

            tenant.FullName           = vm.FullName.Trim();
            tenant.Phone              = vm.Phone?.Trim();
            tenant.Email              = vm.Email?.Trim();
            tenant.IdentityNo         = vm.IdentityNo?.Trim();
            tenant.DateOfBirth        = vm.DateOfBirth;
            tenant.PermanentAddress   = vm.PermanentAddress?.Trim();

            await _repo.UpdateTenantAsync(tenant);
            return true;
        }
    }
}