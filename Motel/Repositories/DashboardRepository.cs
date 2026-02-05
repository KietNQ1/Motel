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
            // HARD CODE - TODO: Replace with real database query
            await Task.CompletedTask;
            return new RoomStatisticsViewModel
            {
                TotalRooms = 50,
                OccupiedRooms = 38,
                AvailableRooms = 10,
                MaintenanceRooms = 2,
                OccupancyRate = 76.0m
            };
        }

        public async Task<FinancialStatisticsViewModel> GetFinancialStatisticsAsync(int landlordId)
        {
            // HARD CODE - TODO: Replace with real database query
            await Task.CompletedTask;
            return new FinancialStatisticsViewModel
            {
                MonthlyRevenue = 125000000,
                CollectedAmount = 98000000,
                UnpaidAmount = 27000000,
                UnpaidInvoiceCount = 8,
                CollectionRate = 78.4m
            };
        }

        public async Task<List<RecentInvoiceViewModel>> GetRecentInvoicesAsync(int landlordId, int count)
        {
            // HARD CODE - TODO: Replace with real database query
            await Task.CompletedTask;
            return new List<RecentInvoiceViewModel>
            {
                new RecentInvoiceViewModel { InvoiceId = 1, RoomName = "Phòng 101", PropertyName = "Nhà Trọ ABC", PeriodMonth = 202602, TotalAmount = 3500000, Status = "paid", DueDate = DateTime.Now.AddDays(-5) },
                new RecentInvoiceViewModel { InvoiceId = 2, RoomName = "Phòng 205", PropertyName = "Nhà Trọ XYZ", PeriodMonth = 202602, TotalAmount = 4200000, Status = "unpaid", DueDate = DateTime.Now.AddDays(5) },
                new RecentInvoiceViewModel { InvoiceId = 3, RoomName = "Phòng 302", PropertyName = "Nhà Trọ ABC", PeriodMonth = 202602, TotalAmount = 3800000, Status = "paid", DueDate = DateTime.Now.AddDays(-2) },
                new RecentInvoiceViewModel { InvoiceId = 4, RoomName = "Phòng 410", PropertyName = "Nhà Trọ XYZ", PeriodMonth = 202602, TotalAmount = 5000000, Status = "unpaid", DueDate = DateTime.Now.AddDays(10) },
                new RecentInvoiceViewModel { InvoiceId = 5, RoomName = "Phòng 108", PropertyName = "Nhà Trọ ABC", PeriodMonth = 202602, TotalAmount = 3200000, Status = "paid", DueDate = DateTime.Now.AddDays(-8) }
            };
        }

        public async Task<List<ExpiringContractViewModel>> GetExpiringContractsAsync(int landlordId, int daysAhead)
        {
            // HARD CODE - TODO: Replace with real database query
            await Task.CompletedTask;
            return new List<ExpiringContractViewModel>
            {
                new ExpiringContractViewModel { ContractId = 1, RoomName = "Phòng 201", PropertyName = "Nhà Trọ ABC", TenantName = "Nguyễn Văn A", EndDate = DateTime.Now.AddDays(5), DaysRemaining = 5 },
                new ExpiringContractViewModel { ContractId = 2, RoomName = "Phòng 305", PropertyName = "Nhà Trọ XYZ", TenantName = "Trần Thị B", EndDate = DateTime.Now.AddDays(12), DaysRemaining = 12 },
                new ExpiringContractViewModel { ContractId = 3, RoomName = "Phòng 402", PropertyName = "Nhà Trọ ABC", TenantName = "Lê Văn C", EndDate = DateTime.Now.AddDays(20), DaysRemaining = 20 },
                new ExpiringContractViewModel { ContractId = 4, RoomName = "Phòng 105", PropertyName = "Nhà Trọ XYZ", TenantName = "Phạm Thị D", EndDate = DateTime.Now.AddDays(28), DaysRemaining = 28 }
            };
        }

        public async Task<MonthlyRevenueChartViewModel> GetMonthlyRevenueDataAsync(int landlordId, int monthCount)
        {
            // HARD CODE - TODO: Replace with real database query
            await Task.CompletedTask;
            return new MonthlyRevenueChartViewModel
            {
                Months = new List<string> { "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12", "T1", "T2" },
                Revenues = new List<decimal> { 95000000, 102000000, 98000000, 110000000, 115000000, 108000000, 120000000, 125000000, 118000000, 122000000, 130000000, 125000000 }
            };
        }
    }
}
