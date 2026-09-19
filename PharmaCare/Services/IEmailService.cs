namespace PharmaCare.Services
{
    /* Abstraction for transactional email used by account, order and prescription flows. */
    public interface IEmailService
    {
        /* Send an arbitrary email. Returns true if handed off to SMTP (or logged in dev fallback). */
        Task<bool> SendEmailAsync(string toAddress, string subject, string htmlBody);

        /* Send the sign-up email verification code. */
        Task SendVerificationCodeAsync(string toAddress, string firstName, string code);

        /* Send the password-reset code. */
        Task SendPasswordResetCodeAsync(string toAddress, string firstName, string code);

        /* Send a branded order receipt to the customer and a detailed new-order alert to staff. */
        Task SendOrderPlacedNotificationsAsync(Order order, User customer, IEnumerable<User> staffRecipients);

        /* Send a reservation receipt to the customer and a pickup alert to Admin/Pharmacist users. */
        Task SendPrescriptionReservationNotificationsAsync(PrescriptionReservation reservation, User customer, IEnumerable<User> staffRecipients);
    }
}
