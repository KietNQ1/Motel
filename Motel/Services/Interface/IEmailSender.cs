namespace Motel.Services.Interface
{
    public interface IEmailSender
    {
        Task SendAsync(string email, string subject, string htmlBody);
    }
}
