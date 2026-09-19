using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PharmaCare.Controllers;

public class MarketplaceAccountController : Controller
{
    private readonly DataDbContext _db;
    public MarketplaceAccountController(DataDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return RedirectToAction("Login", "Account");

        var notifications = await _db.MarketplaceNotifications.AsNoTracking()
            .Where(x => x.UserId == userId.Value)
            .OrderByDescending(x => x.CreatedAt)
            .Take(30)
            .ToListAsync(ct);

        return View(new MarketplaceAccountViewModel
        {
            Addresses = await _db.CustomerAddresses.AsNoTracking().Where(x => x.UserId == userId.Value)
                .OrderByDescending(x => x.IsDefault).ThenBy(x => x.Label).ToListAsync(ct),
            Notifications = notifications,
            UnreadNotifications = notifications.Count(x => !x.IsRead)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAddress(CustomerAddressInputModel input, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return RedirectToAction("Login", "Account");
        if (!ModelState.IsValid)
        {
            TempData["AccountError"] = "Please complete the required address fields.";
            return RedirectToAction(nameof(Index));
        }

        CustomerAddress address;
        if (input.CustomerAddressId.HasValue)
        {
            address = await _db.CustomerAddresses.FirstOrDefaultAsync(x => x.CustomerAddressId == input.CustomerAddressId.Value && x.UserId == userId.Value, ct)
                ?? throw new InvalidOperationException("Address not found.");
        }
        else
        {
            address = new CustomerAddress { UserId = userId.Value, CreatedAt = DateTime.Now };
            _db.CustomerAddresses.Add(address);
        }

        if (input.IsDefault)
        {
            var others = await _db.CustomerAddresses.Where(x => x.UserId == userId.Value && (!input.CustomerAddressId.HasValue || x.CustomerAddressId != input.CustomerAddressId.Value)).ToListAsync(ct);
            foreach (var other in others) other.IsDefault = false;
        }

        address.Label = input.Label.Trim();
        address.City = input.City.Trim();
        address.Area = input.Area?.Trim();
        address.Street = input.Street.Trim();
        address.Building = input.Building?.Trim();
        address.Floor = input.Floor?.Trim();
        address.Apartment = input.Apartment?.Trim();
        address.Landmark = input.Landmark?.Trim();
        address.DeliveryInstructions = input.DeliveryInstructions?.Trim();
        address.Latitude = input.Latitude;
        address.Longitude = input.Longitude;
        address.IsDefault = input.IsDefault || !await _db.CustomerAddresses.AnyAsync(x => x.UserId == userId.Value && x.CustomerAddressId != address.CustomerAddressId, ct);

        await _db.SaveChangesAsync(ct);
        TempData["AccountMessage"] = "Address saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(int id, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return RedirectToAction("Login", "Account");
        var address = await _db.CustomerAddresses.FirstOrDefaultAsync(x => x.CustomerAddressId == id && x.UserId == userId.Value, ct);
        if (address == null) return NotFound();
        _db.CustomerAddresses.Remove(address);
        await _db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return RedirectToAction("Login", "Account");
        var unread = await _db.MarketplaceNotifications.Where(x => x.UserId == userId.Value && !x.IsRead).ToListAsync(ct);
        foreach (var item in unread) item.IsRead = true;
        await _db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Index));
    }
}
