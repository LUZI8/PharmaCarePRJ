using System.Data;

namespace Repositories.Repository
{
    /* Repository implementation for user management with secure password handling */
    public class UserRepository : IUserRepository
    {
        private readonly DataDbContext _context;

        public UserRepository(DataDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllAsync() => await _context.User.ToListAsync();

        public async Task<User> GetByIdAsync(int id) => await _context.User.FindAsync(id);

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _context.User.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<User> CreateAsync(User user)
        {
            user.Password = HashPassword(user.Password);
            user.DateCreated = DateTime.UtcNow;
            user.IsActive = true;

            if (string.IsNullOrEmpty(user.Role))
                user.Role = "Customer";

            string[] validRoles = { "Admin", "Customer", "Pharmacist" };
            if (!validRoles.Contains(user.Role))
                user.Role = "Customer";

            _context.User.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> UpdateAsync(User user)
        {
            var existingUser = await _context.User.FindAsync(user.UserId);
            if (existingUser == null)
                return null;

            existingUser.FirstName = user.FirstName;
            existingUser.LastName = user.LastName;
            existingUser.Email = user.Email;
            existingUser.Role = user.Role;
            existingUser.Address = user.Address;
            existingUser.City = user.City;
            existingUser.PhoneNumber = user.PhoneNumber;
            existingUser.IsActive = user.IsActive;

            /* Password changes are deliberately handled only by SetPasswordAsync or reset-code flow. */
            _context.User.Update(existingUser);
            await _context.SaveChangesAsync();
            return existingUser;
        }

        public async Task<bool> SetPasswordAsync(int userId, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                return false;

            var user = await _context.User.FindAsync(userId);
            if (user == null)
                return false;

            user.Password = HashPassword(newPassword);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _context.User.FindAsync(id);
            if (user == null)
                return false;

            _context.User.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            return await _context.User.AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<bool> ValidateCredentialsAsync(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                return false;

            var user = await _context.User
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive);

            if (user == null)
                return false;

            return VerifyPassword(password, user.Password);
        }

        public async Task SetEmailVerificationCodeAsync(int userId, string code, DateTime expiry)
        {
            var user = await _context.User.FindAsync(userId);
            if (user == null)
                return;

            user.EmailVerificationCode = code;
            user.EmailVerificationCodeExpiry = expiry;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ConfirmEmailAsync(int userId, string code)
        {
            var user = await _context.User.FindAsync(userId);
            if (user == null)
                return false;

            if (string.IsNullOrEmpty(user.EmailVerificationCode) ||
                user.EmailVerificationCode != code ||
                user.EmailVerificationCodeExpiry == null ||
                user.EmailVerificationCodeExpiry < DateTime.UtcNow)
                return false;

            user.IsEmailVerified = true;
            user.EmailVerificationCode = null;
            user.EmailVerificationCodeExpiry = null;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task SetPasswordResetCodeAsync(int userId, string code, DateTime expiry)
        {
            var user = await _context.User.FindAsync(userId);
            if (user == null)
                return;

            user.PasswordResetCode = code;
            user.PasswordResetCodeExpiry = expiry;
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ResetPasswordWithCodeAsync(int userId, string code, string newPassword)
        {
            var user = await _context.User.FindAsync(userId);
            if (user == null)
                return false;

            if (string.IsNullOrEmpty(user.PasswordResetCode) ||
                user.PasswordResetCode != code ||
                user.PasswordResetCodeExpiry == null ||
                user.PasswordResetCodeExpiry < DateTime.UtcNow)
                return false;

            user.Password = HashPassword(newPassword);
            user.PasswordResetCode = null;
            user.PasswordResetCodeExpiry = null;
            await _context.SaveChangesAsync();
            return true;
        }

        private static string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return null;

            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        private static bool VerifyPassword(string inputPassword, string storedHashedPassword)
        {
            if (string.IsNullOrEmpty(inputPassword) || string.IsNullOrEmpty(storedHashedPassword))
                return false;

            try
            {
                return BCrypt.Net.BCrypt.Verify(inputPassword, storedHashedPassword);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                /* Legacy SHA256 hashes are intentionally not accepted as BCrypt hashes. */
                return false;
            }
        }
    }
}
