using System.Threading.Tasks;

namespace Bikontrol.Infrastructure.Email
{
    public interface IEmailSender
    {
        Task SendAsync(string toEmail, string subject, string body);
    }
}
