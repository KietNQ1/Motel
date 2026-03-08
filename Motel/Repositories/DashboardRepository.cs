using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.ViewModels.Dashboard;

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

        public async Task<RoomStatisticsViewModel> GetRoomStatisticsAsync(int landlordId)
        {
            var rooms = await _context.Rooms
                .Include(r => r.Property)
                .Where(r => r.Property.LandlordId == landlordId && !r.IsDeleted)
                .ToListAsync();

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

        public async Task<FinancialStatisticsViewModel> GetFinancialStatisticsAsync(int landlordId)
        {
            var currentMonth = DateTime.Now.Year * 100 + DateTime.Now.Month;

            var invoices = await _context.Invoices
                .Include(i => i.Room)
                    .ThenInclude(r => r.Property)
                .Where(i => i.Room.Property.LandlordId == landlordId && i.PeriodMonth == currentMonth)
                .ToListAsync();

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

        public async Task<List<RecentInvoiceViewModel>> GetRecentInvoicesAsync(int landlordId, int count)
        {
            var invoices = await _context.Invoices
                .Include(i => i.Room)
                    .ThenInclude(r => r.Property)
                .Where(i => i.Room.Property.LandlordId == landlordId)
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

        public async Task<List<ExpiringContractViewModel>> GetExpiringContractsAsync(int landlordId, int daysAhead)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var futureDate = today.AddDays(daysAhead);

            var contracts = await _context.Contracts
                .Include(c => c.Room)
                    .ThenInclude(r => r.Property)
                .Include(c => c.Tenant)
                .Where(c => c.Room.Property.LandlordId == landlordId
                    && c.Status == "active"
                    && !c.IsDeleted
                    && c.EndDate >= today
                    && c.EndDate <= futureDate)
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

        public async Task<MonthlyRevenueChartViewModel> GetMonthlyRevenueDataAsync(int landlordId, int monthCount)
        {
            var months = new List<string>();
            var revenues = new List<decimal>();

            var currentDate = DateTime.Now;

            for (int i = monthCount - 1; i >= 0; i--)
            {
                var targetMonth = currentDate.AddMonths(-i);
                var periodMonth = targetMonth.Year * 100 + targetMonth.Month;

                var monthRevenue = await _context.Invoices
                    .Include(inv => inv.Room)
                        .ThenInclude(r => r.Property)
                    .Where(inv => inv.Room.Property.LandlordId == landlordId
                        && inv.PeriodMonth == periodMonth
                        && inv.Status == "paid")
                    .SumAsync(inv => inv.TotalAmount);

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
    }
}
