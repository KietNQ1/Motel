using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Repositories.Interface;
using Motel.ViewModels.Property;

namespace Motel.Repositories
{
    public class PropertyRepository : IPropertyRepository
    {
        private readonly MotelDbContext _context;

        public PropertyRepository(MotelDbContext context)
        {
            _context = context;
        }

        public async Task<List<PropertyListViewModel>> GetPropertiesByLandlordIdAsync(int landlordId)
        {
            return await _context.Properties
                .Where(p => p.LandlordId == landlordId && !p.IsDeleted)
                .Select(p => new PropertyListViewModel
                {
                    PropertyId = p.PropertyId,
                    Name = p.Name,
                    Address = p.Address,
                    TotalRooms = p.Rooms.Count(r => !r.IsDeleted),
                    OccupiedRooms = p.Rooms.Count(r => !r.IsDeleted && r.Status == "occupied"),
                    AvailableRooms = p.Rooms.Count(r => !r.IsDeleted && r.Status == "available"),
                    CreatedAt = p.CreatedAt,
                    HasRooms = p.Rooms.Any(r => !r.IsDeleted)
                })
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Property?> GetPropertyByIdAsync(int propertyId)
        {
            return await _context.Properties
                .FirstOrDefaultAsync(p => p.PropertyId == propertyId && !p.IsDeleted);
        }

        public async Task<PropertyDetailsViewModel?> GetPropertyDetailsAsync(int propertyId)
        {
            var property = await _context.Properties
                .Where(p => p.PropertyId == propertyId && !p.IsDeleted)
                .Include(p => p.Rooms) // Include all rooms including deleted
                    .ThenInclude(r => r.Contracts) 
                .Include(p => p.Rooms)
                    .ThenInclude(r => r.RoomOccupancies)
                        .ThenInclude(o => o.Tenant)
                .FirstOrDefaultAsync();

            if (property == null) return null;

            var vm = new PropertyDetailsViewModel
            {
                PropertyId = property.PropertyId,
                Name = property.Name,
                Address = property.Address,
                Description = property.Description,
                CreatedAt = property.CreatedAt,
                RoomsByFloor = new Dictionary<int, List<RoomGridItemViewModel>>()
            };

            var roomsByFloor = property.Rooms
                .GroupBy(r => ExtractFloorNumber(r.RoomName))
                .OrderBy(g => g.Key);

            foreach (var floorGroup in roomsByFloor)
            {
                var roomList = floorGroup.Select(r =>
                {
                    var activeContract = r.Contracts
                        .FirstOrDefault(c => !c.IsDeleted && c.Status == "active");

                    var activeOccupants = r.RoomOccupancies
                        .Where(o => o.Status == "active")
                        .ToList();

                    var primary = activeOccupants.FirstOrDefault(o => o.IsPrimary)
                                  ?? activeOccupants.FirstOrDefault(); // fallback

                    return new RoomGridItemViewModel
                    {
                        RoomId = r.RoomId,
                        RoomName = r.RoomName,
                        RentPrice = r.RentPrice,
                        Status = r.Status,
                        MaxOccupants = r.MaxOccupants,

                        ActiveContractId = activeContract?.ContractId,
                        HasTenant = activeContract != null,
                        OccupantsCount = activeOccupants.Count,
                        PrimaryTenantId = primary?.TenantId,
                        TenantName = primary?.Tenant?.FullName,
                        IsDeleted = r.IsDeleted
                    };
                })
                .OrderBy(x => x.RoomName)
                .ToList();

                vm.RoomsByFloor[floorGroup.Key] = roomList;
            }

            return vm;
        }

        public async Task<int> CreatePropertyAsync(Property property)
        {
            _context.Properties.Add(property);
            await _context.SaveChangesAsync();
            return property.PropertyId;
        }

        public async Task<bool> CreateRoomsWithUtilitiesAsync(
            int propertyId, 
            List<(string RoomName, decimal RentPrice, int MaxOccupants)> rooms, 
            decimal electricPrice, 
            decimal waterPrice, 
            decimal internetFee, 
            decimal trashFee)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var today = DateOnly.FromDateTime(DateTime.Now);

                foreach (var roomData in rooms)
                {
                    // Create room
                    var room = new Room
                    {
                        PropertyId = propertyId,
                        RoomName = roomData.RoomName,
                        RentPrice = roomData.RentPrice,
                        Status = "available",
                        MaxOccupants = roomData.MaxOccupants,
                        IsDeleted = false
                    };

                    _context.Rooms.Add(room);
                    await _context.SaveChangesAsync(); // Save to get RoomId

                    // Create utility settings for this room
                    var utilitySetting = new RoomUtilitySetting
                    {
                        RoomId = room.RoomId,
                        ElectricUnitPrice = electricPrice,
                        WaterUnitPrice = waterPrice,
                        InternetFee = internetFee,
                        TrashFee = trashFee,
                        EffectiveFrom = today,
                        EffectiveTo = null // Active indefinitely
                    };

                    _context.RoomUtilitySettings.Add(utilitySetting);
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

        public async Task<bool> PropertyExistsAsync(int propertyId, int landlordId)
        {
            return await _context.Properties
                .AnyAsync(p => p.PropertyId == propertyId && p.LandlordId == landlordId && !p.IsDeleted);
        }

        private int ExtractFloorNumber(string roomName)
        {
            // Extract floor number from room name (e.g., "301" -> 3, "A201" -> 2)
            if (string.IsNullOrEmpty(roomName))
                return 0;

            // Remove non-digit characters
            var digits = new string(roomName.Where(char.IsDigit).ToArray());
            
            if (digits.Length >= 3)
            {
                // First digit is floor number (e.g., "301" -> 3)
                if (int.TryParse(digits[0].ToString(), out int floor))
                    return floor;
            }

            return 0;
        }
    }
}
