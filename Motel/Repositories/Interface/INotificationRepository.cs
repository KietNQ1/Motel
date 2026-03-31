using System.Collections.Generic;
using System.Threading.Tasks;
using Motel.Models;

namespace Motel.Repositories.Interface
{
    public interface INotificationRepository
    {
        Task<List<Notification>> GetNotificationsByUserIdAsync(int userId);
    }
}
