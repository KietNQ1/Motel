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

        public async Task<TenantEditViewModel?> BuildEditViewModelAsync(int landlordId, int tenantId)
        {
            var tenant = await _repo.GetTenantByIdAsync(tenantId, landlordId);
            if (tenant == null) return null;

            var isRegistered = await _context.StoredFileReferences
                .Include(r => r.StoredFile)
                .AnyAsync(r => r.RefType == "tenant" && r.RefId == tenantId 
                               && r.StoredFile.StoragePath.Contains("residence_proofs"));

            return new TenantEditViewModel
            {
                TenantId = tenant.TenantId,
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
                var refProof = new StoredFileReference { StoredFileId = vm.ResidenceProofImageId.Value, RefType = "residence_proof", RefId = tenant.TenantId, CreatedAt = DateTime.Now };
                _context.StoredFileReferences.Add(refProof);
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

        public async Task<string?> GetTenantResidenceProofImageAsync(int tenantId)
        {
            var rRef = await _context.StoredFileReferences
                .Include(r => r.StoredFile)
                .Where(r => r.RefType == "tenant" && r.RefId == tenantId 
                            && r.StoredFile.StoragePath.Contains("residence_proofs"))
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

            return rRef?.StoredFile.StoragePath;
        }

        public async Task<CT01ViewModel?> GetCT01DataAsync(int landlordId, int tenantId)
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
            var activeOccupancy = await _context.RoomOccupancies
                .Include(o => o.Room)
                .ThenInclude(r => r.Property)
                .Where(o => o.TenantId == tenantId && o.Status == "active")
                .FirstOrDefaultAsync();

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
    }
}