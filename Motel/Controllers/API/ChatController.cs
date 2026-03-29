using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Motel.Models;
using Motel.Services.Interfaces;
using Motel.ViewModels.Chat;

namespace Motel.Controllers.API;

[ApiController]
[Route("api/[controller]")] // Đường dẫn sẽ là /api/Chat
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly UserManager<ApplicationUser> _userManager;

    public ChatController(IChatService chatService, UserManager<ApplicationUser> userManager)
    {
        _chatService = chatService;
        _userManager = userManager;
    }

    [HttpPost("send")] 
    public async Task<IActionResult> SendMessage([FromBody] SendMessageViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        // Chạy ngầm việc xử lý AI để không làm treo giao diện
        await _chatService.SendMessageAsync(user.Id, model.Message, model.ConnectionId);

        return Ok();
    }
}
