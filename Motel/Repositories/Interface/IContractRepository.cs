using Motel.Models;

namespace Motel.Repositories.Interface;

public interface IContractRepository
{
    // ===== Existing (giữ nguyên) =====
    Task<Contract?> GetByIdAsync(int contractId, CancellationToken ct = default);
    Task<Contract?> GetActiveByIdAsync(int contractId, CancellationToken ct = default);
    Task<Contract?> GetActiveContractByRoomIdAsync(int roomId, CancellationToken ct = default);


    // Room + Property để check landlord sở hữu phòng
    Task<Room?> GetRoomWithPropertyAsync(int roomId, CancellationToken ct = default);

    // check 1 phòng đã có contract active chưa
    Task<bool> RoomHasActiveContractAsync(int roomId, CancellationToken ct = default);

    // list tenants của landlord (để build dropdown/checkbox)
    Task<List<Tenant>> GetTenantsByLandlordAsync(int landlordId, CancellationToken ct = default);

    // tạo contract + occupancies + update room status (transaction)
    Task<int> CreateContractWithOccupanciesAsync(
        Contract contract,
        List<RoomOccupancy> occupancies,
        bool setRoomOccupied,
        CancellationToken ct = default);

    // details hợp đồng (include Room.Property + Tenant)
    Task<Contract?> GetContractDetailsAsync(int contractId, int landlordId, CancellationToken ct = default);

    // occupants active của phòng (include Tenant)
    Task<List<RoomOccupancy>> GetActiveOccupanciesAsync(int roomId, CancellationToken ct = default);

    // kết thúc hợp đồng (end contract + room available + occupancies inactive)
    Task<bool> EndContractAsync(int contractId, int landlordId, CancellationToken ct = default);
}