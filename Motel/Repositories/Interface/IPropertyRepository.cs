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
    }
}
