using Motel.Models;
using Motel.Repositories.Interface;
using Motel.Services.Interfaces;
using Motel.ViewModels.Tenant;
using Motel.Data;
using Microsoft.EntityFrameworkCore;

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

        public async Task<TenantEditViewModel?> BuildEditViewModelAsync(int landlordId, int tenantId, int? occupancyId = null)
        {
            var tenant = await _repo.GetTenantByIdAsync(tenantId, landlordId);
            if (tenant == null) return null;

            var resolvedOccupancyId = await ResolveOccupancyIdAsync(landlordId, tenantId, occupancyId);
            var propertyId = await ResolvePropertyIdAsync(landlordId, tenantId, resolvedOccupancyId);
            var isRegistered = propertyId.HasValue && await HasResidenceProofAsync(tenantId, propertyId.Value);

            return new TenantEditViewModel
            {
                TenantId = tenant.TenantId,
                OccupancyId = resolvedOccupancyId,
                FullName = tenant.FullName,
                Phone = tenant.Phone,
                Email = tenant.Email,
                IdentityNo = tenant.IdentityNo,
                DateOfBirth = tenant.DateOfBirth,
                PermanentAddress = tenant.PermanentAddress,
                IsTemporaryResidenceRegistered = isRegistered
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

            if (vm.CccdFrontImageId.HasValue && vm.CccdFrontImageId.Value > 0)
            {
                var refFront = new StoredFileReference { StoredFileId = vm.CccdFrontImageId.Value, RefType = "tenant", RefId = tenant.TenantId, CreatedAt = DateTime.Now };
                _context.StoredFileReferences.Add(refFront);
            }
            if (vm.CccdBackImageId.HasValue && vm.CccdBackImageId.Value > 0)
            {
                var refBack = new StoredFileReference { StoredFileId = vm.CccdBackImageId.Value, RefType = "tenant", RefId = tenant.TenantId, CreatedAt = DateTime.Now };
                _context.StoredFileReferences.Add(refBack);
            }
            if (vm.ResidenceProofImageId.HasValue && vm.ResidenceProofImageId.Value > 0)
            {
                var propertyId = await ResolvePropertyIdAsync(landlordId, tenant.TenantId, vm.OccupancyId);
                if (propertyId.HasValue)
                {
                    var tenantProofRef = new StoredFileReference
                    {
                        StoredFileId = vm.ResidenceProofImageId.Value,
                        RefType = "tenant",
                        RefId = tenant.TenantId,
                        CreatedAt = DateTime.Now
                    };
                    var propertyProofRef = new StoredFileReference
                    {
                        StoredFileId = vm.ResidenceProofImageId.Value,
                        RefType = "property",
                        RefId = propertyId.Value,
                        CreatedAt = DateTime.Now
                    };
                    _context.StoredFileReferences.Add(tenantProofRef);
                    _context.StoredFileReferences.Add(propertyProofRef);
                }
            }

            if (vm.CccdFrontImageId.HasValue || vm.CccdBackImageId.HasValue || vm.ResidenceProofImageId.HasValue)
            {
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<(string? frontImage, string? backImage)> GetTenantCccdImagesAsync(int tenantId)
        {
            var refs = await _context.StoredFileReferences
                .Include(r => r.StoredFile)
                .Where(r => r.RefType == "tenant" && r.RefId == tenantId)
                .ToListAsync();

            var front = refs.FirstOrDefault(r => r.StoredFile.StoragePath.Contains("_front"))?.StoredFile.StoragePath;
            var back = refs.FirstOrDefault(r => r.StoredFile.StoragePath.Contains("_back"))?.StoredFile.StoragePath;

            return (front, back);
        }

        public async Task<string?> GetTenantResidenceProofImageAsync(int tenantId, int? occupancyId = null)
        {
            var propertyId = await ResolvePropertyIdAsync(null, tenantId, occupancyId);
            if (!propertyId.HasValue)
            {
                return null;
            }

            return await GetResidenceProofPathAsync(tenantId, propertyId.Value);
        }

        public async Task<CT01ViewModel?> GetCT01DataAsync(int landlordId, int tenantId, int? occupancyId = null)
        {
            var tenant = await _repo.GetTenantByIdAsync(tenantId, landlordId);
            if (tenant == null) return null;

            // Extract gender from IdentityNo
            string gender = "Nam"; // Default
            if (!string.IsNullOrEmpty(tenant.IdentityNo) && tenant.IdentityNo.Length == 12)
            {
                char genderChar = tenant.IdentityNo[3];
                if (genderChar == '1' || genderChar == '3' || genderChar == '5' || genderChar == '7' || genderChar == '9')
                {
                    gender = "Nữ";
                }
            }

            // Get Current Room Address
            var resolvedOccupancyId = await ResolveOccupancyIdAsync(landlordId, tenantId, occupancyId);
            var activeOccupancy = resolvedOccupancyId.HasValue
                ? await _context.RoomOccupancies
                    .Include(o => o.Room)
                    .ThenInclude(r => r.Property)
                    .FirstOrDefaultAsync(o => o.OccupancyId == resolvedOccupancyId.Value)
                : null;

            string currentAddress = activeOccupancy?.Room != null 
                ? $"Phòng {activeOccupancy.Room.RoomName}, {(activeOccupancy.Room.Property?.Address ?? "")}"
                : "";

            return new CT01ViewModel
            {
                TenantId = tenant.TenantId,
                FullName = tenant.FullName,
                DateOfBirth = tenant.DateOfBirth,
                Gender = gender,
                IdentityNo = tenant.IdentityNo ?? "",
                Phone = tenant.Phone ?? "",
                Email = tenant.Email ?? "",
                PermanentAddress = tenant.PermanentAddress ?? "",
                LandlordFullName = tenant.Landlord?.DisplayName ?? "",
                LandlordIdentityNo = tenant.Landlord?.IdentityNo ?? "",
                CurrentAddress = currentAddress
            };
        }

        private async Task<int?> ResolveOccupancyIdAsync(int? landlordId, int tenantId, int? occupancyId)
        {
            if (occupancyId.HasValue)
            {
                return await _context.RoomOccupancies
                    .Where(o =>
                        o.OccupancyId == occupancyId.Value &&
                        o.TenantId == tenantId &&
                        o.Status == "active" &&
                        (!landlordId.HasValue || o.Room.Property.LandlordId == landlordId.Value))
                    .Select(o => (int?)o.OccupancyId)
                    .FirstOrDefaultAsync();
            }

            return await _context.RoomOccupancies
                .Where(o =>
                    o.TenantId == tenantId &&
                    o.Status == "active" &&
                    (!landlordId.HasValue || o.Room.Property.LandlordId == landlordId.Value))
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => (int?)o.OccupancyId)
                .FirstOrDefaultAsync();
        }

        private async Task<int?> ResolvePropertyIdAsync(int? landlordId, int tenantId, int? occupancyId)
        {
            if (occupancyId.HasValue)
            {
                return await _context.RoomOccupancies
                    .Where(o =>
                        o.OccupancyId == occupancyId.Value &&
                        o.TenantId == tenantId &&
                        o.Status == "active" &&
                        (!landlordId.HasValue || o.Room.Property.LandlordId == landlordId.Value))
                    .Select(o => (int?)o.Room.PropertyId)
                    .FirstOrDefaultAsync();
            }

            return await _context.RoomOccupancies
                .Where(o =>
                    o.TenantId == tenantId &&
                    o.Status == "active" &&
                    (!landlordId.HasValue || o.Room.Property.LandlordId == landlordId.Value))
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => (int?)o.Room.PropertyId)
                .FirstOrDefaultAsync();
        }

        private async Task<bool> HasResidenceProofAsync(int tenantId, int propertyId)
        {
            return await _context.StoredFiles
                .Where(f => f.StoragePath.Contains("residence_proofs"))
                .AnyAsync(f =>
                    f.StoredFileReferences.Any(r => r.RefType == "tenant" && r.RefId == tenantId) &&
                    f.StoredFileReferences.Any(r => r.RefType == "property" && r.RefId == propertyId));
        }

        private async Task<string?> GetResidenceProofPathAsync(int tenantId, int propertyId)
        {
            return await _context.StoredFiles
                .Where(f => f.StoragePath.Contains("residence_proofs"))
                .Where(f =>
                    f.StoredFileReferences.Any(r => r.RefType == "tenant" && r.RefId == tenantId) &&
                    f.StoredFileReferences.Any(r => r.RefType == "property" && r.RefId == propertyId))
                .OrderByDescending(f => f.UploadedAt)
                .Select(f => f.StoragePath)
                .FirstOrDefaultAsync();
        }
    }
}
