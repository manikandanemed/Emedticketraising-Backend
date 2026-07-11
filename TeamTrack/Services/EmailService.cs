using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace TeamTrack.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var server = _config["SmtpConfig:Server"] ?? "smtp.office365.com";
            var username = _config["SmtpConfig:Username"] ?? "";
            var password = _config["SmtpConfig:Password"] ?? "";
            var portStr = _config["SmtpConfig:Port"] ?? "587";
            var sslStr = _config["SmtpConfig:Ssl"] ?? "true";
            var senderName = _config["SmtpConfig:SenderName"] ?? "Emedlogix solutions";
            var senderEmail = _config["SmtpConfig:SenderEmail"] ?? username;

            int port = int.TryParse(portStr, out int p) ? p : 587;
            bool enableSsl = !bool.TryParse(sslStr, out bool s) || s; // Default to true if parse fails

            using (var client = new SmtpClient(server, port))
            {
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(username, password);
                client.EnableSsl = enableSsl;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
            }
        }
    }
}
