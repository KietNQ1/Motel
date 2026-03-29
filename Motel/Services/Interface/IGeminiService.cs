using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Motel.Models;
using Motel.ViewModels.Chat;

namespace Motel.Services.Interface
{
    public interface IGeminiService
    {
       Task<string> GenerateReplyAsync(List<ChatMessage> history, string userMessage);
    }
}