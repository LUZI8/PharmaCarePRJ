using Microsoft.AspNetCore.Mvc;

namespace PharmaCare.Controllers;

public sealed class LocaleController : Controller
{
    [HttpGet]
    public IActionResult Set(string culture = "en", string? returnUrl = null)
    {
        var value = string.Equals(culture, "ar", StringComparison.OrdinalIgnoreCase) ? "ar" : "en";
        Response.Cookies.Append("pc-culture", value, new CookieOptions
        {
            HttpOnly = false,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.AddYears(1)
        });

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "Marketplace");
    }
}
