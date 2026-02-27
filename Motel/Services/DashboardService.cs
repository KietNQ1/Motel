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
                // Load data tuần tự để tránh DbContext concurrency issues
                var properties = await _repository.GetPropertiesAsync(landlordId);
                var roomStats = await _repository.GetRoomStatisticsAsync(landlordId);
                var financialStats = await _repository.GetFinancialStatisticsAsync(landlordId);
                var recentInvoices = await _repository.GetRecentInvoicesAsync(landlordId, 5);
                var expiringContracts = await _repository.GetExpiringContractsAsync(landlordId, 30);
                var revenueChart = await _repository.GetMonthlyRevenueDataAsync(landlordId, 12);

                return new DashboardIndexViewModel
                {
                    Properties = properties,
                    RoomStats = roomStats,
                    FinancialStats = financialStats,
                    RecentInvoices = recentInvoices,
                    ExpiringContracts = expiringContracts,
                    RevenueChart = revenueChart
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
