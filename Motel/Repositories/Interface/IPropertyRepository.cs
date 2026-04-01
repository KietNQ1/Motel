using Motel.Models;
using Motel.ViewModels.Property;

namespace Motel.Repositories.Interface
{
    public interface IPropertyRepository
    {
        Task<List<PropertyListViewModel>> GetPropertiesByLandlordIdAsync(int landlordId);
        Task<Property?> GetPropertyByIdAsync(int propertyId);
        Task<PropertyDetailsViewModel?> GetPropertyDetailsAsync(int propertyId);
        Task<int> CreatePropertyAsync(Property property);
        Task<bool> CreateRoomsAsync(int propertyId, List<(string RoomName, decimal RentPrice, int MaxOccupants)> rooms);
        Task<bool> PropertyExistsAsync(int propertyId, int landlordId);
        Task<bool> UpdatePropertyAsync(Property property);
        Task<int> GetRoomOccupancyCountByLandlordAsync(int landlordId);
        Task<List<RoomOccupancy>> GetRoomOccupanciesByLandlordAsync(int landlordId);
        Task<int> GetSubscriptionCountByLandlordAsync(int landlordId);
        Task<int> GetActiveSubscriptionCountByLandlordAsync(int landlordId);
        Task<List<Subscription>> GetSubscriptionsByLandlordAsync(int landlordId);
        Task<(int TotalAccounts, int PrimaryAccounts)> GetBankAccountSummaryByLandlordAsync(int landlordId);
        Task<List<LandlordBankAccount>> GetBankAccountsByLandlordAsync(int landlordId);
        Task<Landlord?> GetLandlordProfileAsync(int landlordId);
    }
}
