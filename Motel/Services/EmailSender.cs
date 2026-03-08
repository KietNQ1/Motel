using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Motel.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        public EmailSender(IConfiguration config)
        {
            _config = config;
        }
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var from = _config["Email:From"]!;
            var host = _config["Email:SmtpHost"]!;
            var port = int.Parse(_config["Email:SmtpPort"]!);
            var username = _config["Email:Username"]!;
            var password = _config["Email:Password"]!;
            var useSsl = bool.Parse(_config["Email:UseSsl"]!);
            using var message = new MailMessage(from, email, subject, htmlMessage)
            {
                IsBodyHtml = true,
            };
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = useSsl,
                Credentials = new NetworkCredential(username, password)
            };
            await client.SendMailAsync(message);
        }
    }
}
