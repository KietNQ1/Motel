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

        public async Task<DashboardIndexViewModel> GetDashboardDataAsync(int landlordId, int? propertyId = null, int? year = null, int? month = null)
        {
            try
            {
                var properties = await _repository.GetPropertiesAsync(landlordId);
                var roomStats = await _repository.GetRoomStatisticsAsync(landlordId, propertyId);
                var financialStats = await _repository.GetFinancialStatisticsAsync(landlordId, propertyId, year, month);
                var recentInvoices = await _repository.GetRecentInvoicesAsync(landlordId, 5, propertyId);
                var expiringContracts = await _repository.GetExpiringContractsAsync(landlordId, 30, propertyId);
                var chartYear = year ?? DateTime.Now.Year;
                var revenueChart = await _repository.GetMonthlyRevenueDataAsync(landlordId, 12, propertyId, chartYear);

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
