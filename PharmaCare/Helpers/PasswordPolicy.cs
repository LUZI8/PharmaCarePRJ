namespace PharmaCare.Helpers;

public static class PasswordPolicy
{
    public const int MinimumLength = 10;
    public const string Message = "Password must be at least 10 characters and include uppercase, lowercase, number, and special character.";

    public static bool IsValid(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinimumLength) return false;
        return password.Any(char.IsUpper)
            && password.Any(char.IsLower)
            && password.Any(char.IsDigit)
            && password.Any(c => !char.IsLetterOrDigit(c));
    }
}
