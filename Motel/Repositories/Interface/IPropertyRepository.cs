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
        Task<bool> CreateRoomsWithUtilitiesAsync(int propertyId, List<(string RoomName, decimal RentPrice, int MaxOccupants)> rooms, decimal electricPrice, decimal waterPrice, decimal internetFee, decimal trashFee);
        Task<bool> PropertyExistsAsync(int propertyId, int landlordId);
    }
}
