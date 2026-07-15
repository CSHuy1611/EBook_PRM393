using System.Threading.Tasks;

namespace MathIBook.Application.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}
