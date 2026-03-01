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
                .Include(r => r.Contract)
                    .ThenInclude(c => c.Tenant)
                .Include(r => r.RoomUtilitySettings)
                .Include(r => r.MeterReadings.OrderByDescending(m => m.PeriodMonth).Take(2))
                .FirstOrDefaultAsync(r => r.RoomId == roomId && !r.IsDeleted, ct);

            if (room == null) return null;

            var contract = room.Contract;
            var hasTenant = contract != null && !contract.IsDeleted && contract.Status == "active";
            var tenant = contract?.Tenant;

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
                TenantId = tenant?.TenantId,
                TenantName = tenant?.FullName,
                TenantPhone = tenant?.Phone,
                TenantEmail = tenant?.Email,
                ContractStartDate = contract?.StartDate,
                ContractEndDate = contract?.EndDate,
                DepositAmount = contract?.DepositAmount
            };

            var today = DateOnly.FromDateTime(DateTime.Today);
            var currentSetting = room.RoomUtilitySettings
                .Where(s => s.EffectiveFrom <= today && (s.EffectiveTo == null || s.EffectiveTo >= today))
                .OrderByDescending(s => s.EffectiveFrom)
                .FirstOrDefault();
            if (currentSetting != null)
            {
                viewModel.ElectricUnitPrice = currentSetting.ElectricUnitPrice;
                viewModel.WaterUnitPrice = currentSetting.WaterUnitPrice;
                viewModel.InternetFee = currentSetting.InternetFee;
                viewModel.TrashFee = currentSetting.TrashFee;
            }

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
        // RENT ROOM
        // =========================
        public async Task<bool> RentRoomAsync(
            int roomId,
            int landlordId,
            string tenantName,
            string? phone,
            string? email,
            string? identityNo,
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

                if (room == null || room.Status != "available")
                    return false;

                var tenant = new Tenant
                {
                    LandlordId = landlordId,
                    FullName = tenantName,
                    Phone = phone,
                    Email = email,
                    IdentityNo = identityNo,
                    IsDeleted = false,
                    CreatedAt = DateTime.Now
                };

                _db.Tenants.Add(tenant);
                await _db.SaveChangesAsync(ct);

                var contract = new Contract
                {
                    RoomId = roomId,
                    TenantId = tenant.TenantId,
                    DepositAmount = depositAmount,
                    StartDate = startDate,
                    EndDate = endDate,
                    Status = "active",
                    IsDeleted = false,
                    CreatedAt = DateTime.Now
                };

                _db.Contracts.Add(contract);

                room.Status = "occupied";

                if (initialElectric.HasValue || initialWater.HasValue)
                {
                    var currentMonth = int.Parse(DateTime.Now.ToString("yyyyMM"));

                    _db.MeterReadings.Add(new MeterReading
                    {
                        RoomId = roomId,
                        PeriodMonth = currentMonth,
                        ElectricOld = initialElectric ?? 0,
                        ElectricNew = initialElectric ?? 0,
                        WaterOld = initialWater ?? 0,
                        WaterNew = initialWater ?? 0,
                        RecordedAt = DateTime.Now,
                        RecordedByUserId = landlordId
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