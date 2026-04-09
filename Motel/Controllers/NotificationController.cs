using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Motel.Helpers;
using Motel.Services.Interface;
using Motel.Services.Interfaces;
namespace Motel.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly LandlordHelper _landlordHelper;

        public NotificationController(
            INotificationService notificationService,
            LandlordHelper landlordHelper)
        {
            _notificationService = notificationService;
            _landlordHelper = landlordHelper;
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
            var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
            if (landlordId == 0)
                return Unauthorized("Bạn không có quyền thực hiện thao tác này.");

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
            var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
            if (landlordId == 0)
                return Unauthorized("Bạn không có quyền thực hiện thao tác này.");

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
            var landlordId = await _landlordHelper.GetCurrentLandlordIdAsync(User);
            if (landlordId == 0)
                return Unauthorized("Bạn không có quyền thực hiện thao tác này.");

            await _notificationService.SendToTenantAsync(
                landlordId,
                tenantId,
                title,
                content);

            return Ok("Sent to tenant");
        }
    }
}