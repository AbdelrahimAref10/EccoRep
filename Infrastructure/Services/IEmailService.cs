namespace Infrastructure.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
        Task SendActivationCodeAsync(string toEmail, string activationCode);
    }
}










