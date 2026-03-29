namespace Motel.Services.Interfaces;
public interface IChatService
{
    Task SendMessageAsync(int userId, string message, string connectionId);
}
