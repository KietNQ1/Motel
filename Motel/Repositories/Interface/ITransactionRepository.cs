using System.Collections.Generic;
using System.Threading.Tasks;
using Motel.Models;

namespace Motel.Repositories.Interface
{
    public interface ITransactionRepository
    {
        Task<List<Transaction>> GetTransactionsByUserIdAsync(int userId);
    }
}
