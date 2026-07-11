using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace PharmaCare.Services
{
    /* SMTP-backed email sender with a development fallback.
       When no SMTP Host is configured, emails are written to the application log so the whole
       verification/reset flow is testable locally without real SMTP credentials. */
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        /* True when real SMTP is configured; otherwise we run in log-only fallback mode. */
        private bool IsConfigured => !string.IsNullOrWhiteSpace(_settings.Host);

        public async Task<bool> SendEmailAsync(string toAddress, string subject, string htmlBody)
        {
            /* Development fallback: no SMTP configured, so log the message instead of sending. */
            if (!IsConfigured)
            {
                _logger.LogWarning(
                    "[EMAIL FALLBACK] SMTP not configured. Would send to {To}\nSubject: {Subject}\nBody:\n{Body}",
                    toAddress, subject, htmlBody);
                return true;
            }

            try
            {
                var fromAddress = string.IsNullOrWhiteSpace(_settings.FromAddress)
                    ? _settings.Username
                    : _settings.FromAddress;

                using var message = new MailMessage
                {
                    From = new MailAddress(fromAddress, _settings.FromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                message.To.Add(toAddress);

                using var client = new SmtpClient(_settings.Host, _settings.Port)
                {
                    EnableSsl = _settings.EnableSsl,
                    Credentials = new NetworkCredential(_settings.Username, _settings.Password)
                };

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent to {To} (subject: {Subject})", toAddress, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}", toAddress);
                return false;
            }
        }

        public Task SendVerificationCodeAsync(string toAddress, string firstName, string code)
        {
            var body = BuildCodeEmail(
                greeting: $"Hi {firstName},",
                intro: "Thanks for creating a PharmaCare account. Use the code below to verify your email address:",
                code: code,
                note: "This code expires in 15 minutes. If you didn't sign up, you can safely ignore this email.");
            return SendEmailAsync(toAddress, "Verify your PharmaCare email", body);
        }

        public Task SendPasswordResetCodeAsync(string toAddress, string firstName, string code)
        {
            var body = BuildCodeEmail(
                greeting: $"Hi {firstName},",
                intro: "We received a request to reset your PharmaCare password. Use the code below to continue:",
                code: code,
                note: "This code expires in 15 minutes. If you didn't request this, please ignore this email and your password will stay unchanged.");
            return SendEmailAsync(toAddress, "Reset your PharmaCare password", body);
        }

        /* Simple branded HTML template shared by both code emails. */
        private static string BuildCodeEmail(string greeting, string intro, string code, string note)
        {
            return $@"
<div style='font-family:Arial,Helvetica,sans-serif;max-width:480px;margin:0 auto;color:#222'>
  <h2 style='color:#dc3545;margin-bottom:4px'>PharmaCare</h2>
  <p>{greeting}</p>
  <p>{intro}</p>
  <div style='font-size:32px;font-weight:bold;letter-spacing:8px;text-align:center;
              background:#f4f4f4;border-radius:8px;padding:16px;margin:20px 0'>{code}</div>
  <p style='color:#666;font-size:13px'>{note}</p>
  <hr style='border:none;border-top:1px solid #eee;margin:24px 0'>
  <p style='color:#999;font-size:12px'>PharmaCare, Amman, Jordan</p>
</div>";
        }
    }
}
