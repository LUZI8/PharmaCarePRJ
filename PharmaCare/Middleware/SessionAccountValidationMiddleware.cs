using Microsoft.EntityFrameworkCore;

namespace PharmaCare.Middleware;

public sealed class SessionAccountValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SessionAccountValidationMiddleware> _logger;

    public SessionAccountValidationMiddleware(RequestDelegate next, ILogger<SessionAccountValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, DataDbContext db)
    {
        var userId = context.Session.GetInt32("UserId");
        if (userId.HasValue)
        {
            var user = await db.User.AsNoTracking()
                .Where(x => x.UserId == userId.Value)
                .Select(x => new { x.IsActive, x.Role, x.FirstName, x.LastName })
                .FirstOrDefaultAsync(context.RequestAborted);

            if (user == null || !user.IsActive)
            {
                context.Session.Clear();
                _logger.LogInformation("Cleared stale session for user {UserId}", userId.Value);
            }
            else
            {
                var sessionRole = context.Session.GetString("UserRole");
                if (!string.Equals(sessionRole, user.Role, StringComparison.Ordinal))
                    context.Session.SetString("UserRole", user.Role);

                var expectedName = $"{user.FirstName} {user.LastName}".Trim();
                if (!string.Equals(context.Session.GetString("UserName"), expectedName, StringComparison.Ordinal))
                    context.Session.SetString("UserName", expectedName);
            }
        }

        await _next(context);
    }
}
