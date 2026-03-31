using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Motel.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        // Hàm này giúp lấy ConnectionId ở phía Client (JavaScript)
        public override Task OnConnectedAsync()
        {
            _logger.LogInformation("=== SIGNALR CONNECT ===");
            _logger.LogInformation("UserIdentifier: {UserIdentifier}", Context.UserIdentifier);

            foreach (var claim in Context.User.Claims)
            {
                _logger.LogInformation("Claim: {Type} = {Value}", claim.Type, claim.Value);
            }

            return base.OnConnectedAsync();
        }

        // Gửi tin nhắn tới tất cả tab của user
        public async Task SendBotMessageToUser(string message)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Clients.User(userId).SendAsync("ReceiveBotMessage", message);
            }
        }
    }
}