using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Motel.Hubs
{
    // Lấy userId từ ClaimsPrincipal để SignalR gửi tới tất cả tab của user
    public class UserIdProviderById : IUserIdProvider
    {
        private readonly ILogger<UserIdProviderById> _logger;

        public UserIdProviderById(ILogger<UserIdProviderById> logger)
        {
            _logger = logger;
        }

        public string? GetUserId(HubConnectionContext connection)
        {
            var userId = connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("[UserIdProviderById] GetUserId: {UserId}", userId);
            return userId;
        }
    }
}
