using Motel.Repositories.Interface;
using Motel.Services.Interface;
using Motel.ViewModels.Room;

namespace Motel.Services
{
    public sealed class RoomService : IRoomService
    {
        private readonly IRoomRepository _roomRepo;

        public RoomService(IRoomRepository roomRepo)
        {
            _roomRepo = roomRepo;
        }

        public async Task<bool> RentRoomAsync(RentRoomViewModel model, int landlordId, int userId, CancellationToken ct = default)
        {
            // validate nghiệp vụ nhanh ở service
            if (model.Occupants == null || model.Occupants.Count == 0) return false;
            if (model.PrimaryIndex < 0 || model.PrimaryIndex >= model.Occupants.Count) return false;
            if (model.MaxOccupants > 0 && model.Occupants.Count > model.MaxOccupants) return false;
            if (model.EndDate < model.StartDate) return false;

            return await _roomRepo.RentRoomAsync(
                model.RoomId,
                landlordId,
                userId,
                model.Occupants,
                model.PrimaryIndex,
                model.DepositAmount,
                model.StartDate,
                model.EndDate,
                model.InitialElectricReading,
                model.InitialWaterReading,
                ct
            );
        }
    }
}