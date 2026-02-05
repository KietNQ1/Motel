using Motel.Repositories;
using Motel.ViewModels.Dashboard;

namespace Motel.Services
{
    /// <summary>
    /// Service implementation cho Dashboard
    /// Orchestrates data từ repository và business logic
    /// </summary>
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _repository;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(
            IDashboardRepository repository, 
            ILogger<DashboardService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<DashboardIndexViewModel> GetDashboardDataAsync(int landlordId)
        {
            try
            {
                // Load tất cả data song song để tối ưu performance
                var roomStatsTask = _repository.GetRoomStatisticsAsync(landlordId);
                var financialStatsTask = _repository.GetFinancialStatisticsAsync(landlordId);
                var recentInvoicesTask = _repository.GetRecentInvoicesAsync(landlordId, 5);
                var expiringContractsTask = _repository.GetExpiringContractsAsync(landlordId, 30);
                var revenueChartTask = _repository.GetMonthlyRevenueDataAsync(landlordId, 12);

                await Task.WhenAll(
                    roomStatsTask, 
                    financialStatsTask, 
                    recentInvoicesTask, 
                    expiringContractsTask, 
                    revenueChartTask
                );

                return new DashboardIndexViewModel
                {
                    RoomStats = await roomStatsTask,
                    FinancialStats = await financialStatsTask,
                    RecentInvoices = await recentInvoicesTask,
                    ExpiringContracts = await expiringContractsTask,
                    RevenueChart = await revenueChartTask
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data for landlord {LandlordId}", landlordId);
                
                // Return empty data thay vì throw exception
                return new DashboardIndexViewModel();
            }
        }
    }
}
