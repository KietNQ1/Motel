using Microsoft.AspNetCore.Mvc;
using Motel.Services.Interfaces;
using Microsoft.AspNetCore.Identity.UI.Services;
using Motel.Services.Interface;
namespace Motel.Controllers
{
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationController(
            INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public IActionResult Send()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendToProperty(
            int propertyId,
            string title,
            string content)
        {
            int landlordId = 1;

            await _notificationService.SendToPropertyAsync(
                landlordId,
                propertyId,
                title,
                content);

            return Ok("Sent to property");
        }

        [HttpPost]
        public async Task<IActionResult> SendToRoom(
            int roomId,
            string title,
            string content)
        {
            int landlordId = 1;

            await _notificationService.SendToRoomAsync(
                landlordId,
                roomId,
                title,
                content);

            return Ok("Sent to room");
        }

        [HttpPost]
        public async Task<IActionResult> SendToTenant(
            int tenantId,
            string title,
            string content)
        {
            int landlordId = 1;

            await _notificationService.SendToTenantAsync(
                landlordId,
                tenantId,
                title,
                content);

            return Ok("Sent to tenant");
        }
    }
}