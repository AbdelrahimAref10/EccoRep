using Domain.Enums;
using System;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public interface IInvitationCodeService
    {
        string GenerateInvitationCode();
        Task SendInvitationCodeAsync(string mobileNumber, string code);
        Task SendInvitationCodeAsync(string mobileNumber, string? email, int verificationBy, string code);
    }

    public class InvitationCodeService : IInvitationCodeService
    {
        private readonly Random _random = new Random();
        private readonly ISmsService _smsService;
        private readonly IEmailService _emailService;

        public InvitationCodeService(ISmsService smsService, IEmailService emailService)
        {
            _smsService = smsService;
            _emailService = emailService;
        }

        public string GenerateInvitationCode()
        {
            // Generate 6-digit code
            return _random.Next(100000, 999999).ToString();
        }

        public async Task SendInvitationCodeAsync(string mobileNumber, string code)
        {
            // Legacy method - send via SMS
            await _smsService.SendActivationCodeAsync(mobileNumber, code);
        }

        public async Task SendInvitationCodeAsync(string mobileNumber, string? email, int verificationBy, string code)
        {
            // verificationBy: 0 = Phone, 1 = Email
            if (verificationBy == (int)VerificationBy.Email && !string.IsNullOrWhiteSpace(email))
            {
                // Send via Email
                await _emailService.SendActivationCodeAsync(email, code);
            }
            else
            {
                // Send via SMS
                await _smsService.SendActivationCodeAsync(mobileNumber, code);
            }
        }
    }
}

