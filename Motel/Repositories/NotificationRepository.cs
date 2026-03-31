using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Repositories.Interface;

namespace Motel.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly MotelDbContext _db;
        public NotificationRepository(MotelDbContext db) => _db = db;
        public async Task<List<Notification>> GetNotificationsByUserIdAsync(int userId)
        {
            // Lọc theo LandlordId (userId)
            return await _db.Notifications.Where(n => n.LandlordId == userId).ToListAsync();
        }
    }
}
