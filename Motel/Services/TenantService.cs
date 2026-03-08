using Motel.Models;
using Motel.Repositories.Interface;
using Motel.Services.Interfaces;
using Motel.ViewModels.Tenant;

namespace Motel.Services
{
    public class TenantService : ITenantService
    {
        private readonly ITenantRepository _repo;
        public TenantService(ITenantRepository repo) => _repo = repo;

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
    }
}