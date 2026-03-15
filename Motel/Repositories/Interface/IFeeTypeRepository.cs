using Motel.Models;

namespace Motel.Repositories.Interface;

public interface IFeeTypeRepository
{
    Task<List<FeeType>> GetAllAsync(CancellationToken ct = default);
    Task<FeeType?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<FeeType> AddAsync(FeeType feeType, CancellationToken ct = default);
}
