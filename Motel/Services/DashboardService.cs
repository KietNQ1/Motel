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

        public async Task<DashboardIndexViewModel> GetDashboardDataAsync(int landlordId, int? propertyId = null)
        {
            try
            {
                // Load data tuần tự để tránh DbContext concurrency issues
                // Lấy tất cả properties để hiển thị trong dropdown
                var properties = await _repository.GetPropertiesAsync(landlordId);
                
                // Nếu có propertyId, filter data theo property đó
                var roomStats = await _repository.GetRoomStatisticsAsync(landlordId, propertyId);
                var financialStats = await _repository.GetFinancialStatisticsAsync(landlordId, propertyId);
                var recentInvoices = await _repository.GetRecentInvoicesAsync(landlordId, 5, propertyId);
                var expiringContracts = await _repository.GetExpiringContractsAsync(landlordId, 30, propertyId);
                var revenueChart = await _repository.GetMonthlyRevenueDataAsync(landlordId, 12, propertyId);

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
