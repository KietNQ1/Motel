using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Motel.Data;
using Motel.Models;
using Motel.Repositories.Interface;
using Microsoft.EntityFrameworkCore;


namespace Motel.Repositories;

  public class ChatRepository : IChatRepository {
    private readonly MotelDbContext _context;
    public ChatRepository(MotelDbContext context) => _context = context;

    public async Task SaveMessageAsync(ChatMessage message) {
        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();
    }

    public async Task<List<ChatMessage>> GetRecentMessagesAsync(int userId, int limit = 10) {
        return await _context.ChatMessages
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .OrderBy(m => m.CreatedAt).ToListAsync();
    }
}