using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Motel.Models;
using Motel.ViewModels.Chat;

namespace Motel.Repositories.Interface
{
    public interface IChatRepository
{
     Task SaveMessageAsync(ChatMessage message);
    Task<List<ChatMessage>> GetRecentMessagesAsync(int userId, int limit = 10);
}

}