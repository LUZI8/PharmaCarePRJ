using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PharmaCare.Controllers;

public class MarketplacePrescriptionController : Controller
{
    private readonly DataDbContext _db;
    private readonly IEmailService _email;

    public MarketplacePrescriptionController(DataDbContext db, IEmailService email)
    {
        _db = db;
        _email = email;
    }

    [HttpGet]
    public async Task<IActionResult> Confirm(int offerId, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return RedirectToAction("Login", "Account", new { returnUrl = $"/MarketplacePrescription/Confirm?offerId={offerId}" });

        var offer = await _db.PharmacyProducts.AsNoTracking()
            .Include(x => x.Pharmacy).Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.PharmacyProductId == offerId && x.IsAvailable && x.Stock > 0 && x.Product.RequiresPrescription && x.Pharmacy.IsActive, ct);
        if (offer == null) return NotFound();

        var user = await _db.User.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId.Value, ct);
        return View(new MarketplacePrescriptionConfirmViewModel
        {
            Offer = offer,
            ContactPhone = user?.PhoneNumber ?? string.Empty,
            Quantity = 1
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int offerId, MarketplacePrescriptionConfirmViewModel input, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return RedirectToAction("Login", "Account");

        var offer = await _db.PharmacyProducts
            .Include(x => x.Pharmacy).Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.PharmacyProductId == offerId && x.IsAvailable && x.Stock > 0 && x.Product.RequiresPrescription && x.Pharmacy.IsActive, ct);
        if (offer == null) return NotFound();
        input.Offer = offer;
        if (input.Quantity > offer.Stock) ModelState.AddModelError(nameof(input.Quantity), $"Only {offer.Stock} units are available at this pharmacy.");
        if (!ModelState.IsValid) return View("Confirm", input);

        var request = new MarketplacePrescriptionRequest
        {
            RequestNumber = $"RXM-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            UserId = userId.Value,
            PharmacyId = offer.PharmacyId,
            PharmacyProductId = offer.PharmacyProductId,
            ProductId = offer.ProductId,
            Quantity = input.Quantity,
            ContactPhone = input.ContactPhone.Trim(),
            CustomerNote = input.CustomerNote,
            Status = "Requested",
            RequestedAt = DateTime.Now,
            ExpiresAt = DateTime.Now.AddDays(3)
        };
        _db.MarketplacePrescriptionRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        try
        {
            var user = await _db.User.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId.Value, ct);
            if (user != null && !string.IsNullOrWhiteSpace(user.Email))
            {
                await _email.SendEmailAsync(user.Email,
                    $"Prescription request received - {request.RequestNumber}",
                    $"<h2>We received your prescription request</h2><p><strong>{offer.Product.ProductName}</strong> at <strong>{offer.Pharmacy.Name}</strong></p><p>Request: {request.RequestNumber}<br/>Quantity: {request.Quantity}<br/>Expires: {request.ExpiresAt:MMM d, yyyy}</p><p>Please bring a valid prescription. The pharmacy must verify it before fulfillment or payment.</p>");
            }

            var staffEmails = await _db.PharmacyStaff.AsNoTracking().Where(x => x.PharmacyId == offer.PharmacyId && x.IsActive)
                .Select(x => x.User.Email).Where(x => x != null && x != "").Distinct().ToListAsync(ct);
            var adminEmails = await _db.User.AsNoTracking().Where(x => x.IsActive && x.Role == "Admin").Select(x => x.Email).ToListAsync(ct);
            foreach (var email in staffEmails.Concat(adminEmails).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
                await _email.SendEmailAsync(email, $"New marketplace prescription request - {request.RequestNumber}",
                    $"<h2>New prescription request</h2><p>{offer.Product.ProductName} × {request.Quantity}</p><p>Pharmacy: {offer.Pharmacy.Name}<br/>Request: {request.RequestNumber}<br/>Contact: {request.ContactPhone}</p><p>Review this request in the Pharmacy Portal.</p>");
        }
        catch { /* Email must never roll back a valid reservation request. */ }

        return RedirectToAction(nameof(Complete), new { id = request.MarketplacePrescriptionRequestId });
    }

    [HttpGet]
    public async Task<IActionResult> Complete(int id, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return RedirectToAction("Login", "Account");
        var request = await _db.MarketplacePrescriptionRequests.AsNoTracking()
            .Include(x => x.Pharmacy).Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.MarketplacePrescriptionRequestId == id && x.UserId == userId.Value, ct);
        if (request == null) return NotFound();
        return View(new MarketplacePrescriptionCompleteViewModel { Request = request });
    }

    [HttpGet]
    public async Task<IActionResult> MyRequests(CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return RedirectToAction("Login", "Account");
        var requests = await _db.MarketplacePrescriptionRequests.AsNoTracking().Include(x => x.Pharmacy).Include(x => x.Product)
            .Where(x => x.UserId == userId.Value).OrderByDescending(x => x.RequestedAt).ToListAsync(ct);
        return View(requests);
    }
}
