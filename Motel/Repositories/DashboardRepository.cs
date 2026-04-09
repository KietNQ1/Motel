using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.ViewModels.Contract;
using Motel.ViewModels.Dashboard;
using Motel.ViewModels.Invoice;

namespace Motel.Repositories
{
    /// <summary>
    /// Repository implementation cho Dashboard
    /// Xử lý tất cả database queries liên quan đến Dashboard
    /// </summary>
    public class DashboardRepository : IDashboardRepository
    {
        private readonly MotelDbContext _context;

        public DashboardRepository(MotelDbContext context)
        {
            _context = context;
        }

        public async Task<RoomStatisticsViewModel> GetRoomStatisticsAsync(int landlordId, int? propertyId = null)
        {
            var query = _context.Rooms
                .Include(r => r.Property)
                .Where(r => r.Property.LandlordId == landlordId && !r.IsDeleted);

            // Lọc theo property nếu có
            if (propertyId.HasValue)
            {
                query = query.Where(r => r.PropertyId == propertyId.Value);
            }

            var rooms = await query.ToListAsync();

            var totalRooms = rooms.Count;
            var occupiedRooms = rooms.Count(r => r.Status == "occupied");
            var availableRooms = rooms.Count(r => r.Status == "available");
            var maintenanceRooms = rooms.Count(r => r.Status == "maintenance");

            var occupancyRate = totalRooms > 0 ? (decimal)occupiedRooms / totalRooms * 100 : 0;

            return new RoomStatisticsViewModel
            {
                TotalRooms = totalRooms,
                OccupiedRooms = occupiedRooms,
                AvailableRooms = availableRooms,
                MaintenanceRooms = maintenanceRooms,
                OccupancyRate = Math.Round(occupancyRate, 1)
            };
        }

        public async Task<FinancialStatisticsViewModel> GetFinancialStatisticsAsync(int landlordId, int? propertyId = null, int? year = null, int? month = null)
        {
            var y = year ?? DateTime.Now.Year;
            var m = month ?? DateTime.Now.Month;
            var currentMonth = y * 100 + m;

            var query = _context.Invoices
                .Include(i => i.Room)
                    .ThenInclude(r => r.Property)
                .Where(i => i.Room.Property.LandlordId == landlordId && i.PeriodMonth == currentMonth);

            // Lọc theo property nếu có
            if (propertyId.HasValue)
            {
                query = query.Where(i => i.Room.PropertyId == propertyId.Value);
            }

            var invoices = await query.ToListAsync();

            var monthlyRevenue = invoices.Sum(i => i.TotalAmount);
            var collectedAmount = invoices.Where(i => i.Status == "paid").Sum(i => i.TotalAmount);
            var unpaidAmount = invoices.Where(i => i.Status == "unpaid").Sum(i => i.TotalAmount);
            var unpaidInvoiceCount = invoices.Count(i => i.Status == "unpaid");

            var collectionRate = monthlyRevenue > 0 ? collectedAmount / monthlyRevenue * 100 : 0;

            return new FinancialStatisticsViewModel
            {
                MonthlyRevenue = monthlyRevenue,
                CollectedAmount = collectedAmount,
                UnpaidAmount = unpaidAmount,
                UnpaidInvoiceCount = unpaidInvoiceCount,
                CollectionRate = Math.Round(collectionRate, 1)
            };
        }

        public async Task<List<RecentInvoiceViewModel>> GetRecentInvoicesAsync(int landlordId, int count, int? propertyId = null)
        {
            var query = _context.Invoices
                .Include(i => i.Room)
                    .ThenInclude(r => r.Property)
                .Where(i => i.Room.Property.LandlordId == landlordId);

            // Lọc theo property nếu có
            if (propertyId.HasValue)
            {
                query = query.Where(i => i.Room.PropertyId == propertyId.Value);
            }

            var invoices = await query
                .OrderByDescending(i => i.CreatedAt)
                .Take(count)
                .Select(i => new RecentInvoiceViewModel
                {
                    InvoiceId = i.InvoiceId,
                    RoomName = i.Room.RoomName,
                    PropertyName = i.Room.Property.Name,
                    PeriodMonth = i.PeriodMonth,
                    TotalAmount = i.TotalAmount,
                    Status = i.Status,
                    DueDate = i.DueDate.ToDateTime(TimeOnly.MinValue)
                })
                .ToListAsync();

            return invoices;
        }

        public async Task<List<ExpiringContractViewModel>> GetExpiringContractsAsync(int landlordId, int daysAhead, int? propertyId = null)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var futureDate = today.AddDays(daysAhead);

            var query = _context.Contracts
                .Include(c => c.Room)
                    .ThenInclude(r => r.Property)
                .Include(c => c.Tenant)
                .Where(c => c.Room.Property.LandlordId == landlordId
                    && c.Status == "active"
                    && !c.IsDeleted
                    && c.EndDate >= today
                    && c.EndDate <= futureDate);

            // Lọc theo property nếu có
            if (propertyId.HasValue)
            {
                query = query.Where(c => c.Room.PropertyId == propertyId.Value);
            }

            var contracts = await query
                .OrderBy(c => c.EndDate)
                .Select(c => new ExpiringContractViewModel
                {
                    ContractId = c.ContractId,
                    RoomName = c.Room.RoomName,
                    PropertyName = c.Room.Property.Name,
                    TenantName = c.Tenant.FullName,
                    EndDate = c.EndDate.ToDateTime(TimeOnly.MinValue),
                    DaysRemaining = c.EndDate.DayNumber - today.DayNumber
                })
                .ToListAsync();

            return contracts;
        }

        public async Task<MonthlyRevenueChartViewModel> GetMonthlyRevenueDataAsync(int landlordId, int monthCount, int? propertyId = null, int? year = null)
        {
            var months = new List<string>();
            var revenues = new List<decimal>();

            var refYear = year ?? DateTime.Now.Year;
            var refMonth = DateTime.Now.Month;
            var currentDate = new DateTime(refYear, refMonth, 1);

            for (int i = monthCount - 1; i >= 0; i--)
            {
                var targetMonth = currentDate.AddMonths(-i);
                var periodMonth = targetMonth.Year * 100 + targetMonth.Month;

                var query = _context.Invoices
                    .Include(inv => inv.Room)
                        .ThenInclude(r => r.Property)
                    .Where(inv => inv.Room.Property.LandlordId == landlordId
                        && inv.PeriodMonth == periodMonth
                        && inv.Status == "paid");

                // Lọc theo property nếu có
                if (propertyId.HasValue)
                {
                    query = query.Where(inv => inv.Room.PropertyId == propertyId.Value);
                }

                var monthRevenue = await query.SumAsync(inv => inv.TotalAmount);

                months.Add($"T{targetMonth.Month}");
                revenues.Add(monthRevenue);
            }

            return new MonthlyRevenueChartViewModel
            {
                Months = months,
                Revenues = revenues
            };
        }

        public async Task<List<PropertyOptionViewModel>> GetPropertiesAsync(int landlordId)
        {
            var properties = await _context.Properties
                .Where(p => p.LandlordId == landlordId && !p.IsDeleted)
                .OrderBy(p => p.Name)
                .Select(p => new PropertyOptionViewModel
                {
                    PropertyId = p.PropertyId,
                    Name = p.Name,
                    Address = p.Address
                })
                .ToListAsync();

            return properties;
        }

        public async Task<List<InvoiceListViewModel>> GetInvoiceListAsync(int landlordId, int? propertyId = null, string? status = null, int? periodMonth = null)
        {
            var query = _context.Invoices
                .Include(i => i.Room).ThenInclude(r => r.Property)
                .Include(i => i.Contract).ThenInclude(c => c.Tenant)
                .Where(i => i.Room.Property.LandlordId == landlordId);

            if (propertyId.HasValue)
                query = query.Where(i => i.Room.PropertyId == propertyId.Value);
            if (!string.IsNullOrEmpty(status))
                query = query.Where(i => i.Status == status);
            if (periodMonth.HasValue)
                query = query.Where(i => i.PeriodMonth == periodMonth.Value);

            var list = await query
                .OrderByDescending(i => i.PeriodMonth)
                .ThenByDescending(i => i.CreatedAt)
                .Select(i => new InvoiceListViewModel
                {
                    InvoiceId = i.InvoiceId,
                    RoomName = i.Room.RoomName,
                    PropertyName = i.Room.Property.Name,
                    TenantEmail = i.Contract.Tenant.Email,
                    TenantName = i.Contract.Tenant.FullName,
                    PeriodMonth = i.PeriodMonth,
                    TotalAmount = i.TotalAmount,
                    Status = i.Status,
                    DueDate = i.DueDate,
                    CreatedAt = i.CreatedAt
                })
                .ToListAsync();

            return list;
        }

        public async Task<List<ContractListViewModel>> GetContractListAsync(int landlordId, int? propertyId = null, bool expiringOnly = false)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var future = today.AddDays(30);

            var query = _context.Contracts
                .Include(c => c.Room).ThenInclude(r => r.Property)
                .Include(c => c.Tenant)
                .Where(c => c.Room.Property.LandlordId == landlordId && !c.IsDeleted);

            if (propertyId.HasValue)
                query = query.Where(c => c.Room.PropertyId == propertyId.Value);
            if (expiringOnly)
                query = query.Where(c => c.Status == "active" && c.EndDate >= today && c.EndDate <= future);
            else
                query = query.Where(c => c.Status == "active");

            var list = await query
                .OrderBy(c => c.EndDate)
                .Select(c => new ContractListViewModel
                {
                    ContractId = c.ContractId,
                    RoomName = c.Room.RoomName,
                    PropertyName = c.Room.Property.Name,
                    TenantName = c.Tenant.FullName,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    Status = c.Status,
                    DaysRemaining = c.EndDate.DayNumber - today.DayNumber
                })
                .ToListAsync();

            return list;
        }
    }
}
