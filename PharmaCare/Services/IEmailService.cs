namespace PharmaCare.Services
{
    /* Abstraction for sending transactional emails (verification and password-reset codes). */
    public interface IEmailService
    {
        /* Send an arbitrary email. Returns true if handed off to SMTP (or logged in dev fallback). */
        Task<bool> SendEmailAsync(string toAddress, string subject, string htmlBody);

        /* Send the sign-up email verification code. */
        Task SendVerificationCodeAsync(string toAddress, string firstName, string code);

        /* Send the password-reset code. */
        Task SendPasswordResetCodeAsync(string toAddress, string firstName, string code);
    }
}
