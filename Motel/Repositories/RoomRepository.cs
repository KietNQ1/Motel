using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Repositories.Interface;
using Motel.ViewModels.Room;

namespace Motel.Repositories
{
    public class RoomRepository : IRoomRepository
    {
        private readonly MotelDbContext _context;

        public RoomRepository(MotelDbContext context)
        {
            _context = context;
        }

        public async Task<RoomDetailViewModel?> GetRoomDetailAsync(int roomId)
        {
            var room = await _context.Rooms
                .Include(r => r.Property)
                .Include(r => r.Contract)
                    .ThenInclude(c => c.Tenant)
                .Include(r => r.RoomUtilitySettings)
                .Include(r => r.MeterReadings.OrderByDescending(m => m.PeriodMonth).Take(2))
                .FirstOrDefaultAsync(r => r.RoomId == roomId && !r.IsDeleted);

            if (room == null)
                return null;

            var viewModel = new RoomDetailViewModel
            {
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                RentPrice = room.RentPrice,
                Status = room.Status,
                MaxOccupants = room.MaxOccupants,
                PropertyName = room.Property.Name,
                PropertyId = room.Property.PropertyId
            };

            // Get active contract and tenant info
            var activeContract = room.Contract;
            if (activeContract != null && !activeContract.IsDeleted && activeContract.Status == "active")
            {
                viewModel.HasTenant = true;
                viewModel.ContractId = activeContract.ContractId;
                viewModel.TenantId = activeContract.TenantId;
                viewModel.TenantName = activeContract.Tenant?.FullName;
                viewModel.TenantPhone = activeContract.Tenant?.Phone;
                viewModel.TenantEmail = activeContract.Tenant?.Email;
                viewModel.ContractStartDate = activeContract.StartDate;
                viewModel.ContractEndDate = activeContract.EndDate;
                viewModel.DepositAmount = activeContract.DepositAmount;
            }

            // Get current utility settings
            var currentUtility = room.RoomUtilitySettings
                .Where(u => u.EffectiveTo == null || u.EffectiveTo >= DateOnly.FromDateTime(DateTime.Now))
                .OrderByDescending(u => u.EffectiveFrom)
                .FirstOrDefault();

            if (currentUtility != null)
            {
                viewModel.ElectricUnitPrice = currentUtility.ElectricUnitPrice;
                viewModel.WaterUnitPrice = currentUtility.WaterUnitPrice;
                viewModel.InternetFee = currentUtility.InternetFee;
                viewModel.TrashFee = currentUtility.TrashFee;
            }

            // Get meter readings (current and previous month)
            var meterReadings = room.MeterReadings
                .OrderByDescending(m => m.PeriodMonth)
                .Take(2)
                .ToList();

            if (meterReadings.Any())
            {
                var currentReading = meterReadings[0];
                viewModel.CurrentPeriodMonth = currentReading.PeriodMonth;
                viewModel.CurrentElectricOld = currentReading.ElectricOld;
                viewModel.CurrentElectricNew = currentReading.ElectricNew;
                viewModel.CurrentWaterOld = currentReading.WaterOld;
                viewModel.CurrentWaterNew = currentReading.WaterNew;

                if (meterReadings.Count > 1)
                {
                    var previousReading = meterReadings[1];
                    viewModel.PreviousPeriodMonth = previousReading.PeriodMonth;
                    viewModel.PreviousElectricOld = previousReading.ElectricOld;
                    viewModel.PreviousElectricNew = previousReading.ElectricNew;
                    viewModel.PreviousWaterOld = previousReading.WaterOld;
                    viewModel.PreviousWaterNew = previousReading.WaterNew;
                }
            }

            return viewModel;
        }

        public async Task<Room?> GetRoomByIdAsync(int roomId)
        {
            return await _context.Rooms
                .FirstOrDefaultAsync(r => r.RoomId == roomId && !r.IsDeleted);
        }

        public async Task<bool> UpdateRoomAsync(Room room)
        {
            try
            {
                _context.Rooms.Update(room);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

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
            int? initialWater)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Get room and verify it's available
                var room = await _context.Rooms
                    .FirstOrDefaultAsync(r => r.RoomId == roomId && !r.IsDeleted);
                
                if (room == null || room.Status != "available")
                    return false;

                // 2. Create Tenant
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
                _context.Tenants.Add(tenant);
                await _context.SaveChangesAsync(); // Save to get TenantId

                // 3. Create Contract
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
                _context.Contracts.Add(contract);

                // 4. Update Room status
                room.Status = "occupied";
                _context.Rooms.Update(room);

                // 5. Create initial meter reading if provided
                if (initialElectric.HasValue || initialWater.HasValue)
                {
                    var currentMonth = int.Parse(DateTime.Now.ToString("yyyyMM"));
                    var meterReading = new MeterReading
                    {
                        RoomId = roomId,
                        PeriodMonth = currentMonth,
                        ElectricOld = initialElectric ?? 0,
                        ElectricNew = initialElectric ?? 0,
                        WaterOld = initialWater ?? 0,
                        WaterNew = initialWater ?? 0,
                        RecordedAt = DateTime.Now,
                        RecordedByUserId = landlordId
                    };
                    _context.MeterReadings.Add(meterReading);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }
    }
}
