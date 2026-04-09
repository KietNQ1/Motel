namespace Motel.Services.Interface
{
    public interface INotificationService
    {
        Task SendToPropertyAsync(
            int landlordId,
            int propertyId,
            string title,
            string content);

        Task SendToRoomAsync(
            int landlordId,
            int roomId,
            string title,
            string content);

        Task SendToTenantAsync(
            int landlordId,
            int tenantId,
            string title,
            string content);
    }
}
