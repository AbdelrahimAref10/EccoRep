using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _logger = logger;
            _smtpServer = configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            _smtpPort = int.TryParse(configuration["EmailSettings:SmtpPort"], out var port) ? port : 587;
            _smtpUsername = (configuration["EmailSettings:Username"] ?? string.Empty).Trim();
            // Gmail app passwords are often copied with spaces — strip them
            _smtpPassword = (configuration["EmailSettings:Password"] ?? string.Empty).Replace(" ", string.Empty);
            var fromEmail = (configuration["EmailSettings:FromEmail"] ?? string.Empty).Trim();
            // For Gmail, From must match the authenticated account when FromEmail is empty
            _fromEmail = !string.IsNullOrWhiteSpace(fromEmail) ? fromEmail : _smtpUsername;
            _fromName = configuration["EmailSettings:FromName"] ?? "ECCO";
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("Skip email send: recipient is empty. Subject: {Subject}", subject);
                return;
            }

            if (string.IsNullOrWhiteSpace(_smtpUsername) || string.IsNullOrWhiteSpace(_smtpPassword))
            {
                _logger.LogWarning(
                    "EMAIL (Mock) — EmailSettings Username/Password are empty. To: {To}, Subject: {Subject}. Code body logged for local testing.",
                    toEmail,
                    subject);
                _logger.LogInformation("EMAIL (Mock) body: {Body}", body);
                return;
            }

            if (string.IsNullOrWhiteSpace(_fromEmail))
            {
                _logger.LogError("Cannot send email: EmailSettings:FromEmail and Username are both empty.");
                return;
            }

            try
            {
                using var client = new SmtpClient(_smtpServer, _smtpPort)
                {
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                    Timeout = 30000
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };
                message.To.Add(new MailAddress(toEmail));

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent to {To}. Subject: {Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                // Do not fail registration/activation flow if SMTP fails
                _logger.LogError(
                    ex,
                    "Error sending email to {To}. Subject: {Subject}. Check EmailSettings (Gmail needs App Password + 2FA).",
                    toEmail,
                    subject);
            }
        }

        public async Task SendActivationCodeAsync(string toEmail, string activationCode)
        {
            var subject = "Your Activation Code - ECCO";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif; padding: 20px;'>
                    <h2 style='color: #0d9488;'>Welcome to ECCO!</h2>
                    <p>Thank you for registering. Please use the following activation code to activate your account:</p>
                    <div style='background-color: #F3F4F6; padding: 15px; border-radius: 5px; text-align: center; margin: 20px 0;'>
                        <h1 style='color: #0d9488; margin: 0; letter-spacing: 5px;'>{activationCode}</h1>
                    </div>
                    <p>This code will expire in 24 hours.</p>
                    <p>If you did not request this code, please ignore this email.</p>
                    <hr style='border: none; border-top: 1px solid #E5E7EB; margin: 20px 0;'/>
                    <p style='color: #6B7280; font-size: 12px;'>This is an automated message, please do not reply.</p>
                </body>
                </html>";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}
