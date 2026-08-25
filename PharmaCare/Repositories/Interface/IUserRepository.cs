namespace PharmaCare.Repositories.Interface
{
    /* Repository interface for user management and authentication operations */
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User> GetByIdAsync(int id);
        Task<User> GetByEmailAsync(string email);
        Task<User> CreateAsync(User user);
        Task<User> UpdateAsync(User user);
        Task<bool> DeleteAsync(int id);
        Task<bool> UserExistsAsync(string email);
        Task<bool> ValidateCredentialsAsync(string email, string password);

        /* Explicit password update avoids overloading UpdateAsync with password reset markers. */
        Task<bool> SetPasswordAsync(int userId, string newPassword);

        Task SetEmailVerificationCodeAsync(int userId, string code, DateTime expiry);
        Task<bool> ConfirmEmailAsync(int userId, string code);
        Task SetPasswordResetCodeAsync(int userId, string code, DateTime expiry);
        Task<bool> ResetPasswordWithCodeAsync(int userId, string code, string newPassword);
    }
}
