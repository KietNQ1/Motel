using Motel.ViewModels.Contract;

namespace Motel.Services.Interfaces
{
    public interface IContractService
    {
        Task<ContractCreateViewModel?> BuildCreateViewModelAsync(int landlordId, int roomId);
        Task<int> CreateContractAsync(int landlordId, ContractCreateViewModel vm);
        Task<(Motel.Models.Contract? Contract, List<Motel.Models.RoomOccupancy> Occupants, List<Motel.Models.FeeSetting> FeeSettings)> GetContractDetailsAsync(int landlordId, int contractId);
        Task<bool> EndContractAsync(int landlordId, int contractId);
    }
}