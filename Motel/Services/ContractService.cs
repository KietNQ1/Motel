using Microsoft.AspNetCore.Mvc.Rendering;
using Motel.Models;
using Motel.Repositories.Interface;
using Motel.Services.Interfaces;
using Motel.ViewModels.Contract;

namespace Motel.Services
{
    public class ContractService : IContractService
    {
        private readonly IContractRepository _repo;

        public ContractService(IContractRepository repo) => _repo = repo;

        public async Task<ContractCreateViewModel?> BuildCreateViewModelAsync(int landlordId, int roomId)
        {
            var room = await _repo.GetRoomWithPropertyAsync(roomId);
            if (room == null || room.Property.LandlordId != landlordId) return null;

            if (await _repo.RoomHasActiveContractAsync(roomId)) return null;

            var tenants = await _repo.GetTenantsByLandlordAsync(landlordId);

            return new ContractCreateViewModel
            {
                RoomId = room.RoomId,
                PropertyId = room.PropertyId,
                RoomName = room.RoomName,
                MaxOccupants = room.MaxOccupants,
                TenantOptions = tenants.Select(t => new SelectListItem
                {
                    Value = t.TenantId.ToString(),
                    Text = $"{t.FullName}{(string.IsNullOrWhiteSpace(t.Phone) ? "" : $" - {t.Phone}")}"
                }).ToList()
            };
        }

        public async Task<int> CreateContractAsync(int landlordId, ContractCreateViewModel vm)
        {
            var room = await _repo.GetRoomWithPropertyAsync(vm.RoomId);
            if (room == null || room.Property.LandlordId != landlordId)
                throw new InvalidOperationException("Room not found.");

            if (await _repo.RoomHasActiveContractAsync(vm.RoomId))
                throw new InvalidOperationException("Room already has active contract.");

            if (vm.EndDate < vm.StartDate)
                throw new InvalidOperationException("Invalid date range.");

            // ensure occupant list contains primary tenant
            vm.OccupantTenantIds ??= new List<int>();
            if (!vm.OccupantTenantIds.Contains(vm.TenantId))
                vm.OccupantTenantIds.Insert(0, vm.TenantId);

            vm.OccupantTenantIds = vm.OccupantTenantIds.Distinct().ToList();

            if (vm.OccupantTenantIds.Count > room.MaxOccupants)
                throw new InvalidOperationException("Over capacity.");

            var contract = new Contract
            {
                RoomId = room.RoomId,
                TenantId = vm.TenantId,
                DepositAmount = vm.DepositAmount,
                StartDate = vm.StartDate,
                EndDate = vm.EndDate,
                Status = "active",
                IsDeleted = false,
                CreatedAt = DateTime.Now
            };

            var occupancies = vm.OccupantTenantIds.Select(tid => new RoomOccupancy
            {
                RoomId = room.RoomId,
                TenantId = tid,
                MoveInDate = vm.StartDate,
                MoveOutDate = null,
                IsPrimary = tid == vm.TenantId,
                Status = "active",
                CreatedAt = DateTime.Now
            }).ToList();

            return await _repo.CreateContractWithOccupanciesAsync(contract, occupancies, setRoomOccupied: true);
        }

        public async Task<(Contract? Contract, List<RoomOccupancy> Occupants)> GetContractDetailsAsync(int landlordId, int contractId)
        {
            var contract = await _repo.GetContractDetailsAsync(contractId, landlordId);
            if (contract == null) return (null, new());

            var occ = await _repo.GetActiveOccupanciesAsync(contract.RoomId);
            return (contract, occ);
        }

        public Task<bool> EndContractAsync(int landlordId, int contractId)
            => _repo.EndContractAsync(contractId, landlordId);
    }
}