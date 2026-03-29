using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Motel.Hubs;
using Motel.Models;
using Motel.Repositories.Interface;
using Motel.Services.Interface;
using Motel.Services.Interfaces;
using Motel.ViewModels.Chat;

namespace Motel.Services;

public class ChatService : IChatService {
    private readonly IChatRepository _repo;
    private readonly IGeminiService _gemini;
    private readonly IHubContext<ChatHub> _hubContext;

    public ChatService(IChatRepository repo, IGeminiService gemini, IHubContext<ChatHub> hubContext) {
        _repo = repo; _gemini = gemini; _hubContext = hubContext;
    }

    public async Task SendMessageAsync(int userId, string message, string connectionId) {
        await _repo.SaveMessageAsync(new ChatMessage { UserId = userId, Role = "user", Content = message });
        var history = await _repo.GetRecentMessagesAsync(userId);
        var reply = await _gemini.GenerateReplyAsync(history, message);
        await _repo.SaveMessageAsync(new ChatMessage { UserId = userId, Role = "model", Content = reply });

        // Gửi về Client qua SignalR
        await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveBotMessage", reply);
    }
}


   