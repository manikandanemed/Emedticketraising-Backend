using System.Threading.Tasks;

namespace TeamTrack.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
