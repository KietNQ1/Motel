using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Motel.Data;
using Motel.Models;
using Motel.Services.Interface;


namespace Motel.Services
{
    public class NotificationService : INotificationService
    {
        private readonly MotelDbContext _context;
        private readonly Microsoft.AspNetCore.Identity.UI.Services.IEmailSender _emailSender;

        public NotificationService(
            MotelDbContext context,
          Microsoft.AspNetCore.Identity.UI.Services.IEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        public async Task SendToPropertyAsync(
            int landlordId,
            int propertyId,
            string title,
            string content)
        {
            var tenants = await _context.RoomOccupancies
                .Where(ro => ro.Room.PropertyId == propertyId
                    && ro.MoveOutDate == null
                    && ro.Status == "Active")
                .Select(ro => ro.Tenant)
                .Where(t => !t.IsDeleted && t.Email != null)
                .Distinct()
                .ToListAsync();

            foreach (var tenant in tenants)
            {
                await SendToTenantAsync(
                    landlordId,
                    tenant.TenantId,
                    title,
                    content);
            }
        }

        public async Task SendToRoomAsync(
            int landlordId,
            int roomId,
            string title,
            string content)
        {
            var tenants = await _context.RoomOccupancies
                .Where(ro => ro.RoomId == roomId
                    && ro.MoveOutDate == null
                    && ro.Status == "Active")
                .Select(ro => ro.Tenant)
                .Where(t => !t.IsDeleted && t.Email != null)
                .ToListAsync();

            foreach (var tenant in tenants)
            {
                await SendToTenantAsync(
                    landlordId,
                    tenant.TenantId,
                    title,
                    content);
            }
        }

        public async Task SendToTenantAsync(
            int landlordId,
            int tenantId,
            string title,
            string content)
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.TenantId == tenantId);

            if (tenant == null || string.IsNullOrEmpty(tenant.Email))
                return;

            var notification = new Notification
            {
                LandlordId = landlordId,
                TenantId = tenantId,
                Channel = "Email",
                Type = "Announcement",
                Title = title,
                Content = content,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            try
            {
                await _emailSender.SendEmailAsync(
                    tenant.Email,
                    title,
                    content);

                notification.Status = "Sent";
                notification.SentAt = DateTime.Now;
            }
            catch
            {
                notification.Status = "Failed";
            }

            await _context.SaveChangesAsync();
        }
    }
}