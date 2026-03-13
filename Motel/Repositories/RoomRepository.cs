using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Repositories.Interface;
using Motel.ViewModels.Room;

namespace Motel.Repositories
{
    public sealed class RoomRepository : IRoomRepository
    {
        private readonly MotelDbContext _db;
        public RoomRepository(MotelDbContext db) => _db = db;
        public Task<Room?> GetRoomByIdAsync(int roomId, CancellationToken ct = default)
            => _db.Rooms
                .FirstOrDefaultAsync(r => r.RoomId == roomId && !r.IsDeleted, ct);

        // =========================
        // ROOM DETAIL VIEW
        // =========================
        public async Task<RoomDetailViewModel?> GetRoomDetailAsync(int roomId, CancellationToken ct = default)
        {
            var room = await _db.Rooms
                .Include(r => r.Property)
                // lấy contract active (nếu có) + tenant
                .Include(r => r.Contracts.Where(c => !c.IsDeleted && c.Status == "active"))
                    .ThenInclude(c => c.Tenant)
                // lấy danh sách người đang ở
                .Include(r => r.RoomOccupancies.Where(ro => ro.Status == "active"))
                    .ThenInclude(ro => ro.Tenant)
                .Include(r => r.MeterReadings.OrderByDescending(m => m.PeriodMonth).Take(2))
                .FirstOrDefaultAsync(r => r.RoomId == roomId && !r.IsDeleted, ct);

            if (room == null) return null;

            // vì unique index đảm bảo mỗi room chỉ 1 contract active, nên FirstOrDefault là đủ
            var contract = room.Contracts.FirstOrDefault();
            var hasTenant = contract != null; // đã filter active ở Include

            var viewModel = new RoomDetailViewModel
            {
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                RentPrice = room.RentPrice,
                Status = room.Status,
                MaxOccupants = room.MaxOccupants,
                PropertyName = room.Property.Name,
                PropertyId = room.Property.PropertyId,

                HasTenant = hasTenant,
                ContractId = contract?.ContractId,
                ContractStartDate = contract?.StartDate,
                ContractEndDate = contract?.EndDate,
                DepositAmount = contract?.DepositAmount,

                Tenants = room.RoomOccupancies.Select(ro => new TenantViewModel
                {
                    TenantId = ro.Tenant.TenantId,
                    FullName = ro.Tenant.FullName,
                    Phone = ro.Tenant.Phone,
                    Email = ro.Tenant.Email,
                    IsPrimary = ro.IsPrimary
                }).OrderByDescending(t => t.IsPrimary).ThenBy(t => t.FullName).ToList()
            };

            var today = DateOnly.FromDateTime(DateTime.Today);
            var applicableSettings = await _db.FeeSettings
                .Include(s => s.FeeType)
                .AsNoTracking()
                .Where(s => (s.RoomId == roomId) || (s.PropertyId == room.Property.PropertyId && s.RoomId == null))
                .Where(s => s.EffectiveFrom <= today)
                .Where(s => s.EffectiveTo == null || s.EffectiveTo >= today)
                .ToListAsync(ct);

            var effectiveSettings = applicableSettings
                .GroupBy(s => s.FeeTypeId)
                .Select(g => g.OrderByDescending(s => s.RoomId.HasValue).ThenByDescending(s => s.EffectiveFrom).First())
                .ToList();

            viewModel.ElectricUnitPrice = effectiveSettings.FirstOrDefault(s => s.FeeType?.Name == "Electricity")?.UnitPrice ?? 0;
            viewModel.WaterUnitPrice = effectiveSettings.FirstOrDefault(s => s.FeeType?.Name == "Water")?.UnitPrice ?? 0;
            viewModel.InternetFee = effectiveSettings.FirstOrDefault(s => s.FeeType?.Name == "Internet")?.BaseAmount ?? 0;
            viewModel.TrashFee = effectiveSettings.FirstOrDefault(s => s.FeeType?.Name == "Trash")?.BaseAmount ?? 0;

            var readings = room.MeterReadings.OrderByDescending(m => m.PeriodMonth).Take(2).ToList();

            if (readings.Count > 0)
            {
                var current = readings[0];
                viewModel.CurrentPeriodMonth = current.PeriodMonth;
                viewModel.CurrentElectricOld = current.ElectricOld;
                viewModel.CurrentElectricNew = current.ElectricNew;
                viewModel.CurrentWaterOld = current.WaterOld;
                viewModel.CurrentWaterNew = current.WaterNew;
            }

            if (readings.Count > 1)
            {
                var prev = readings[1];
                viewModel.PreviousPeriodMonth = prev.PeriodMonth;
                viewModel.PreviousElectricOld = prev.ElectricOld;
                viewModel.PreviousElectricNew = prev.ElectricNew;
                viewModel.PreviousWaterOld = prev.WaterOld;
                viewModel.PreviousWaterNew = prev.WaterNew;
            }

            return viewModel;
        }

        // =========================
        // UPDATE ROOM
        // =========================
        public async Task<bool> UpdateRoomAsync(Room room, CancellationToken ct = default)
        {
            try
            {
                _db.Rooms.Update(room);
                await _db.SaveChangesAsync(ct);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // =========================
        // DELETE ROOM (SOFT)
        // =========================
        public async Task<bool> DeleteRoomAsync(int roomId, CancellationToken ct = default)
        {
            try
            {
                var room = await _db.Rooms.FindAsync(new object[] { roomId }, ct);
                if (room == null || room.IsDeleted) return false;

                room.IsDeleted = true;
                _db.Rooms.Update(room);
                await _db.SaveChangesAsync(ct);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RestoreRoomAsync(int roomId)
        {
            try
            {
                var room = await _db.Rooms.FirstOrDefaultAsync(r => r.RoomId == roomId);
                if (room == null || !room.IsDeleted) return false;

                room.IsDeleted = false;
                _db.Rooms.Update(room);
                await _db.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // =========================
        // RENT ROOM
        // =========================
        public async Task<bool> RentRoomAsync(
    int roomId,
    int landlordId,
    int recordedByUserId,
    List<TenantInputViewModel> occupants,
    int primaryIndex,
    decimal depositAmount,
    DateOnly startDate,
    DateOnly endDate,
    int? initialElectric,
    int? initialWater,
    CancellationToken ct = default)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(ct);

            try
            {
                var room = await _db.Rooms
                    .FirstOrDefaultAsync(r => r.RoomId == roomId && !r.IsDeleted, ct);

                if (room == null) return false;
                if (room.Status != "available") return false;

                // chặn nếu đã có contract active (phòng có thể bị lệch status)
                var hasActiveContract = await _db.Contracts
                    .AnyAsync(c => c.RoomId == roomId && !c.IsDeleted && c.Status == "active", ct);

                if (hasActiveContract) return false;

                // validate số người
                if (occupants == null || occupants.Count == 0) return false;
                if (primaryIndex < 0 || primaryIndex >= occupants.Count) return false;
                if (room.MaxOccupants > 0 && occupants.Count > room.MaxOccupants) return false;

                // 1) tạo hoặc lấy lại tenants theo CCCD
                var tenantEntities = new List<Tenant>();
                foreach (var o in occupants)
                {
                    Tenant t = null;
                    if (!string.IsNullOrWhiteSpace(o.IdentityNo))
                    {
                        t = await _db.Tenants.FirstOrDefaultAsync(x => x.LandlordId == landlordId && x.IdentityNo == o.IdentityNo && !x.IsDeleted, ct);
                    }
                    
                    if (t == null)
                    {
                        t = new Tenant
                        {
                            LandlordId = landlordId,
                            FullName = o.FullName.Trim(),
                            Phone = o.Phone,
                            Email = o.Email,
                            IdentityNo = o.IdentityNo,
                            IsDeleted = false,
                            CreatedAt = DateTime.Now
                        };
                        _db.Tenants.Add(t);
                    }
                    else
                    {
                        t.FullName = o.FullName.Trim();
                        t.Phone = o.Phone;
                        t.Email = o.Email;
                    }
                    tenantEntities.Add(t);
                }

                await _db.SaveChangesAsync(ct);

                // 2) tạo contract với tenant chính
                var primaryTenant = tenantEntities[primaryIndex];

                var contract = new Contract
                {
                    RoomId = roomId,
                    TenantId = primaryTenant.TenantId,
                    DepositAmount = depositAmount,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = "active",
                    IsDeleted = false,
                    CreatedAt = DateTime.Now
                };
                _db.Contracts.Add(contract);

                // 3) tạo occupancies (tenant chính bắt buộc nằm trong danh sách)
                var occupancies = tenantEntities.Select((t, idx) => new RoomOccupancy
                {
                    RoomId = roomId,
                    TenantId = t.TenantId,
                    MoveInDate = startDate,
                    MoveOutDate = null,
                    IsPrimary = (idx == primaryIndex),
                    Status = "active",
                    CreatedAt = DateTime.Now
                }).ToList();

                _db.RoomOccupancies.AddRange(occupancies);

                // 4) update room status
                room.Status = "occupied";

                // 5) meter reading ban đầu (PeriodMonth theo StartDate)
                if (initialElectric.HasValue || initialWater.HasValue)
                {
                    var periodMonth = startDate.Year * 100 + startDate.Month;

                    _db.MeterReadings.Add(new MeterReading
                    {
                        RoomId = roomId,
                        PeriodMonth = periodMonth,
                        ElectricOld = initialElectric ?? 0,
                        ElectricNew = initialElectric ?? 0,
                        WaterOld = initialWater ?? 0,
                        WaterNew = initialWater ?? 0,
                        RecordedAt = DateTime.Now,
                        RecordedByUserId = recordedByUserId
                    });
                }

                await _db.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return true;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                return false;
            }
        }
    }
}