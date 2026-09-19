using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PharmaCare.Controllers;

public class FrontEndController : Controller
{
    private readonly DataDbContext _db;
    private readonly ILogger<FrontEndController> _logger;

    public FrontEndController(DataDbContext db, ILogger<FrontEndController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private async Task LoadShellAsync(CancellationToken ct = default)
    {
        ViewBag.Categories = await _db.Category.AsNoTracking().OrderBy(x => x.CategoryName).ToListAsync(ct);
        ViewBag.IsLoggedIn = HttpContext.Session.GetInt32("UserId") != null;
        ViewBag.UserName = HttpContext.Session.GetString("UserName");
        ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
    }

    // Legacy storefront URLs are kept only as compatibility redirects.
    public IActionResult Index() => RedirectToAction("Index", "Marketplace");

    [HttpGet]
    public IActionResult Shop(int? category, string? search, decimal? minPrice, decimal? maxPrice,
        string sort = "relevance", int page = 1, bool prescriptionOnly = false, bool includeExpired = false)
        => RedirectToAction("Index", "Marketplace", new
        {
            q = search,
            sort = sort == "price-asc" ? "cheapest" : "recommended"
        });

    public IActionResult ShopSingle(int id) => RedirectToAction("Compare", "Marketplace", new { id });
    public IActionResult Cart() => RedirectToAction("Index", "MarketplaceCart");
    public IActionResult Checkout() => RedirectToAction("Checkout", "MarketplaceCart");
    public IActionResult ThankYou() => RedirectToAction("Index", "MarketplaceOrders");

    public async Task<IActionResult> About(CancellationToken ct)
    {
        await LoadShellAsync(ct);
        return View();
    }

    public async Task<IActionResult> Contact(string? subject = null, CancellationToken ct = default)
    {
        await LoadShellAsync(ct);
        ViewBag.PrefilledSubject = subject;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitContact(
        string c_fname,
        string c_lname,
        string c_email,
        string c_subject,
        string c_message,
        CancellationToken ct)
    {
        try
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var contactMessage = new ContactMessage
            {
                FirstName = (c_fname ?? string.Empty).Trim(),
                LastName = (c_lname ?? string.Empty).Trim(),
                Email = (c_email ?? string.Empty).Trim(),
                Subject = (c_subject ?? string.Empty).Trim(),
                Message = (c_message ?? string.Empty).Trim(),
                DateSubmitted = DateTime.UtcNow,
                UserId = userId,
                UserType = userId.HasValue ? "User" : "non-user"
            };

            if (string.IsNullOrWhiteSpace(contactMessage.Email) ||
                string.IsNullOrWhiteSpace(contactMessage.Message))
            {
                TempData["ErrorMessage"] = "Email and message are required.";
                return RedirectToAction(nameof(Contact));
            }

            _db.ContactMessages.Add(contactMessage);
            await _db.SaveChangesAsync(ct);

            TempData["SuccessMessage"] = "Thank you for your message. We will get back to you shortly.";
            return RedirectToAction(nameof(Contact));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting contact message");
            TempData["ErrorMessage"] = "An error occurred while submitting your message. Please try again.";
            return RedirectToAction(nameof(Contact));
        }
    }

    // Compatibility JSON endpoint used by older navbar/search scripts.
    // Results now come from the canonical marketplace catalog, not the retired single-store price/stock fields.
    [HttpGet]
    public async Task<JsonResult> SearchProducts(
        string? query,
        decimal? minPrice,
        decimal? maxPrice,
        int? categoryId,
        string sort = "relevance",
        CancellationToken ct = default)
    {
        query = string.IsNullOrWhiteSpace(query) ? null : query.Trim();

        var offers = _db.PharmacyProducts.AsNoTracking()
            .Where(x => x.IsAvailable && x.Stock > 0 &&
                        x.Product.IsActive &&
                        x.Pharmacy.IsActive && x.Pharmacy.IsVerified)
            .Where(x => !categoryId.HasValue || x.Product.CategoryID == categoryId.Value)
            .Where(x => query == null ||
                        x.Product.ProductName.Contains(query) ||
                        x.Product.Description.Contains(query) ||
                        x.Product.Category.CategoryName.Contains(query) ||
                        (x.Product.Manufacturer != null && x.Product.Manufacturer.Contains(query)) ||
                        x.Pharmacy.Name.Contains(query))
            .Where(x => !minPrice.HasValue || x.Price >= minPrice.Value)
            .Where(x => !maxPrice.HasValue || x.Price <= maxPrice.Value);

        var grouped = await offers
            .GroupBy(x => new
            {
                x.ProductId,
                x.Product.ProductName,
                x.Product.ImageUrl,
                x.Product.CategoryID,
                CategoryName = x.Product.Category.CategoryName,
                x.Product.Description,
                x.Product.RequiresPrescription
            })
            .Select(g => new
            {
                id = g.Key.ProductId,
                name = g.Key.ProductName,
                price = g.Min(x => x.Price),
                image = g.Key.ImageUrl,
                stock = g.Sum(x => x.Stock),
                category = g.Key.CategoryName,
                categoryId = g.Key.CategoryID,
                description = g.Key.Description,
                requiresPrescription = g.Key.RequiresPrescription,
                pharmacyCount = g.Select(x => x.PharmacyId).Distinct().Count()
            })
            .Take(50)
            .ToListAsync(ct);

        var results = sort switch
        {
            "price-desc" => grouped.OrderByDescending(x => x.price).ToList(),
            "price-asc" => grouped.OrderBy(x => x.price).ToList(),
            "name-desc" => grouped.OrderByDescending(x => x.name).ToList(),
            _ => grouped.OrderBy(x => x.name).ToList()
        };

        return Json(results);
    }

    [HttpGet]
    public async Task<JsonResult> GetCategories(CancellationToken ct)
        => Json(await _db.Category.AsNoTracking().OrderBy(x => x.CategoryName).ToListAsync(ct));
}
