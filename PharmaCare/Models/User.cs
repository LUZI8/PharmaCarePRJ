namespace PharmaCare.Models
{
    /* Entity model representing system users including customers and administrators */
    public class User
    {
        /* Primary key for user identification */
        public int UserId { get; set; }

        /* User's first name for personalization and identification */
        public string FirstName { get; set; }

        /* User's last name for complete identification */
        public string LastName { get; set; }

        /* Email address used for login and communication */
        public string Email { get; set; }

        /* Password stored as SHA256 hash for security protection */
        public string Password { get; set; }

        /* User role for authorization (Admin, Customer, Pharmacist) */
        public string Role { get; set; }

        /* Physical address for delivery and billing purposes */
        public string Address { get; set; }

        /* City for shipping calculations and delivery coordination */
        public string City { get; set; }

        /* Contact phone number for order communication */
        public string PhoneNumber { get; set; }

        /* Timestamp when user account was initially created */
        public DateTime DateCreated { get; set; }

        /* Boolean flag indicating if user account is currently active */
        public bool IsActive { get; set; }

        /* True once the user has confirmed ownership of their email via a code */
        public bool IsEmailVerified { get; set; }

        /* Short-lived code emailed to confirm the account at sign-up */
        public string? EmailVerificationCode { get; set; }

        /* Expiry timestamp for the email verification code */
        public DateTime? EmailVerificationCodeExpiry { get; set; }

        /* Short-lived code emailed to authorize a password reset */
        public string? PasswordResetCode { get; set; }

        /* Expiry timestamp for the password reset code */
        public DateTime? PasswordResetCodeExpiry { get; set; }
    }
}