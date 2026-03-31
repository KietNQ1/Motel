using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Motel.Models;
using Motel.Services.Interfaces;
using Motel.ViewModels.Chat;
using Motel.Helpers;

namespace Motel.Controllers.API;

[ApiController]
[Route("api/[controller]")] // Đường dẫn sẽ là /api/Chat

public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly LandlordHelper _landlordHelper;

    public ChatController(IChatService chatService, UserManager<ApplicationUser> userManager, LandlordHelper landlordHelper)
    {
        _chatService = chatService;
        _userManager = userManager;
        _landlordHelper = landlordHelper;
    }


    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        var userIdFromIdentity = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
        Console.WriteLine($"[ChatController] User.Identity userId: {userIdFromIdentity}, user.Id: {user?.Id}, landlordId: {landlordId}");

        // Chạy ngầm việc xử lý AI để không làm treo giao diện
        await _chatService.SendMessageAsync(landlordId, user.Id, model.Message, model.ConnectionId);

        return Ok();
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        Console.WriteLine("[ChatController] GetHistory called");
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var history = await _chatService.GetHistoryAsync(user.Id);
            // Chỉ trả về role và content
            var result = history.Select(m => new { role = m.Role, content = m.Content }).ToList();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.ToString() });
        }
    }
}
