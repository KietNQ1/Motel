using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Repositories.Interface;

namespace Motel.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly MotelDbContext _db;
        public TransactionRepository(MotelDbContext db) => _db = db;
        public async Task<List<Transaction>> GetTransactionsByUserIdAsync(int userId)
        {
            // Lọc theo LandlordId (userId)
            return await _db.Transactions.Where(t => t.LandlordId == userId).ToListAsync();
        }
    }
}
