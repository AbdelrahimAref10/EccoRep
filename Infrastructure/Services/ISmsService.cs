namespace Infrastructure.Services
{
    public interface ISmsService
    {
        Task SendSmsAsync(string mobileNumber, string message);
        Task SendActivationCodeAsync(string mobileNumber, string activationCode);
    }
}










