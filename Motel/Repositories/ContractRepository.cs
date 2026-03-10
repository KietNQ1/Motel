using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Repositories.Interface;

namespace Motel.Repositories;

public sealed class ContractRepository : IContractRepository
{
    private readonly MotelDbContext _db;

    public ContractRepository(MotelDbContext db) => _db = db;

    // ===== Existing (giữ nguyên) =====
    public Task<Contract?> GetByIdAsync(int contractId, CancellationToken ct = default)
        => _db.Contracts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ContractId == contractId, ct);

    public Task<Contract?> GetActiveByIdAsync(int contractId, CancellationToken ct = default)
        => _db.Contracts
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.ContractId == contractId &&
                x.Status == "active" &&
                x.IsDeleted == false, ct);

    // ===== New =====

    public Task<Room?> GetRoomWithPropertyAsync(int roomId, CancellationToken ct = default)
        => _db.Rooms
            .Include(r => r.Property)
            .FirstOrDefaultAsync(r => r.RoomId == roomId && !r.IsDeleted && !r.Property.IsDeleted, ct);

    public Task<bool> RoomHasActiveContractAsync(int roomId, CancellationToken ct = default)
        => _db.Contracts.AnyAsync(c => c.RoomId == roomId && !c.IsDeleted && c.Status == "active", ct);

    public Task<List<Tenant>> GetTenantsByLandlordAsync(int landlordId, CancellationToken ct = default)
        => _db.Tenants
            .Where(t => t.LandlordId == landlordId && !t.IsDeleted)
            .OrderBy(t => t.FullName)
            .ToListAsync(ct);

    public async Task<int> CreateContractWithOccupanciesAsync(
        Contract contract,
        List<RoomOccupancy> occupancies,
        bool setRoomOccupied,
        CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            _db.Contracts.Add(contract);

            if (setRoomOccupied)
            {
                var room = await _db.Rooms.FirstAsync(r => r.RoomId == contract.RoomId, ct);
                room.Status = "occupied";
            }

            // Deactivate old active occupancies (nếu có dữ liệu cũ)
            var oldOcc = await _db.RoomOccupancies
                .Where(o => o.RoomId == contract.RoomId && o.Status == "active")
                .ToListAsync(ct);

            var today = DateOnly.FromDateTime(DateTime.Today);
            foreach (var o in oldOcc)
            {
                o.Status = "inactive";
                o.MoveOutDate = today;
            }

            _db.RoomOccupancies.AddRange(occupancies);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return contract.ContractId;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public Task<Contract?> GetContractDetailsAsync(int contractId, int landlordId, CancellationToken ct = default)
        => _db.Contracts
            .Include(c => c.Room).ThenInclude(r => r.Property).ThenInclude(p => p.Landlord).ThenInclude(l => l.User)
            .Include(c => c.Room).ThenInclude(r => r.RoomUtilitySettings)
            .Include(c => c.Tenant)
            .FirstOrDefaultAsync(c =>
                c.ContractId == contractId &&
                !c.IsDeleted &&
                c.Room.Property.LandlordId == landlordId, ct);

    public Task<List<RoomOccupancy>> GetActiveOccupanciesAsync(int roomId, CancellationToken ct = default)
        => _db.RoomOccupancies
            .Where(o => o.RoomId == roomId && o.Status == "active")
            .Include(o => o.Tenant)
            .OrderByDescending(o => o.IsPrimary)
            .ThenBy(o => o.Tenant.FullName)
            .ToListAsync(ct);

    public async Task<bool> EndContractAsync(int contractId, int landlordId, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var contract = await _db.Contracts
            .Include(c => c.Room).ThenInclude(r => r.Property)
            .FirstOrDefaultAsync(c =>
                c.ContractId == contractId &&
                !c.IsDeleted &&
                c.Room.Property.LandlordId == landlordId, ct);

        if (contract == null) return false;
        if (contract.Status != "active") return false;

        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            contract.Status = "ended";
            contract.EndDate = today;

            contract.Room.Status = "available";

            var occ = await _db.RoomOccupancies
                .Where(o => o.RoomId == contract.RoomId && o.Status == "active")
                .ToListAsync(ct);

            foreach (var o in occ)
            {
                o.Status = "inactive";
                o.MoveOutDate = today;
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return true;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}