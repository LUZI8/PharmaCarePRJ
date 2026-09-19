namespace PharmaCare.Helpers;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class SessionAuthorizeAttribute : ActionFilterAttribute
{
    private readonly HashSet<string> _roles;

    public SessionAuthorizeAttribute(params string[] roles)
    {
        _roles = new HashSet<string>(roles ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32("UserId");
        var role = session.GetString("UserRole");

        if (!userId.HasValue || string.IsNullOrWhiteSpace(role) || (_roles.Count > 0 && !_roles.Contains(role)))
        {
            context.HttpContext.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.Result = new RedirectToActionResult("Login", "Account", new
            {
                returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString
            });
        }
    }
}
