using Motel.Models;

namespace Motel.Services.Interfaces;
public interface IChatService
{
    Task SendMessageAsync(int landlordId, int userId, string message, string connectionId);
    Task<List<ChatMessage>> GetHistoryAsync(int userId);
}
