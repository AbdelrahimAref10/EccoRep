using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services
{
    public class SmsService : ISmsService
    {
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly string _fromNumber;

        public SmsService(IConfiguration configuration)
        {
            _configuration = configuration;
            _apiKey = _configuration["SmsSettings:ApiKey"] ?? "";
            _apiSecret = _configuration["SmsSettings:ApiSecret"] ?? "";
            _fromNumber = _configuration["SmsSettings:FromNumber"] ?? "";
        }

        public async Task SendSmsAsync(string mobileNumber, string message)
        {
            try
            {
                // For development/testing: If no API credentials, just log
                if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_apiSecret) || string.IsNullOrWhiteSpace(_fromNumber))
                {
                    Console.WriteLine($"SMS (Mock) to {mobileNumber}:");
                    Console.WriteLine($"Message: {message}");
                    await Task.CompletedTask;
                    return;
                }

                // TODO: Integrate with SMS provider
                // Example providers:
                // - Twilio: https://www.twilio.com/docs/sms
                // - AWS SNS: https://docs.aws.amazon.com/sns/
                // - Azure Communication Services: https://docs.microsoft.com/azure/communication-services/
                
                // Example Twilio integration (uncomment and configure):
                /*
                var client = new TwilioRestClient(_apiKey, _apiSecret);
                var messageResource = await MessageResource.CreateAsync(
                    body: message,
                    from: new PhoneNumber(_fromNumber),
                    to: new PhoneNumber(mobileNumber)
                );
                */

                // For now, just log
                Console.WriteLine($"SMS to {mobileNumber}: {message}");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                // Log error but don't throw - we don't want registration to fail if SMS fails
                Console.WriteLine($"Error sending SMS to {mobileNumber}: {ex.Message}");
                // In production, use proper logging service
            }
        }

        public async Task SendActivationCodeAsync(string mobileNumber, string activationCode)
        {
            var message = $"Your Volt activation code is: {activationCode}. This code will expire in 24 hours.";
            await SendSmsAsync(mobileNumber, message);
        }
    }
}

