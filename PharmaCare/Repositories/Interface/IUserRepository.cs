namespace PharmaCare.Repositories.Interface
{
    /* Repository interface for user management and authentication operations */
    public interface IUserRepository
    {
        /* Retrieve all users in the system for admin management */
        Task<IEnumerable<User>> GetAllAsync();

        /* Get single user by unique user ID */
        Task<User> GetByIdAsync(int id);

        /* Find user by email address for login and account operations */
        Task<User> GetByEmailAsync(string email);

        /* Create new user account and return created user entity */
        Task<User> CreateAsync(User user);

        /* Update existing user information and return updated entity */
        Task<User> UpdateAsync(User user);

        /* Delete user account by ID - returns success status */
        Task<bool> DeleteAsync(int id);

        /* Check if user account exists with given email - returns boolean for validation */
        Task<bool> UserExistsAsync(string email);

        /* Authenticate user credentials during login process - validates email/password combination */
        Task<bool> ValidateCredentialsAsync(string email, string password);

        /* Store a freshly generated email-verification code and its expiry for the given user */
        Task SetEmailVerificationCodeAsync(int userId, string code, DateTime expiry);

        /* Confirm the verification code for a user; on success marks the email verified and clears the code */
        Task<bool> ConfirmEmailAsync(int userId, string code);

        /* Store a freshly generated password-reset code and its expiry for the given user */
        Task SetPasswordResetCodeAsync(int userId, string code, DateTime expiry);

        /* Validate the reset code and, if valid and unexpired, set a new (hashed) password and clear the code */
        Task<bool> ResetPasswordWithCodeAsync(int userId, string code, string newPassword);
    }
}