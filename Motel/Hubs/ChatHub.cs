using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Motel.Hubs
{
   public class ChatHub : Hub
    {
        // Hàm này giúp lấy ConnectionId ở phía Client (JavaScript)
        public string GetConnectionId() => Context.ConnectionId;
    }
}