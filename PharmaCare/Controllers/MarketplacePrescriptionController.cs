using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PharmaCare.Controllers;

public class MarketplacePrescriptionController : Controller
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".pdf" };
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "application/pdf" };
    private const long MaxPrescriptionBytes = 8 * 1024 * 1024;

    private readonly DataDbContext _db;
    private readonly IEmailService _email;
    private readonly IWebHostEnvironment _environment;

    public MarketplacePrescriptionController(DataDbContext db, IEmailService email, IWebHostEnvironment environment)
    {
        _db = db;
        _email = email;
        _environment = environment;
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
        ValidatePrescriptionFile(input.PrescriptionFile);
        if (!ModelState.IsValid) return View("Confirm", input);

        MarketplacePrescriptionRequest request;
        string? savedPath = null;
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            request = new MarketplacePrescriptionRequest
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

            var file = input.PrescriptionFile!;
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var relativeDirectory = Path.Combine("App_Data", "PrescriptionUploads", request.MarketplacePrescriptionRequestId.ToString());
            var directory = Path.Combine(_environment.ContentRootPath, relativeDirectory);
            Directory.CreateDirectory(directory);
            var storedName = Guid.NewGuid().ToString("N") + extension;
            savedPath = Path.Combine(directory, storedName);
            await using (var stream = System.IO.File.Create(savedPath))
                await file.CopyToAsync(stream, ct);

            _db.Set<MarketplacePrescriptionFile>().Add(new MarketplacePrescriptionFile
            {
                MarketplacePrescriptionRequestId = request.MarketplacePrescriptionRequestId,
                FileUrl = Path.Combine(relativeDirectory, storedName).Replace('\\', '/'),
                OriginalFileName = Path.GetFileName(file.FileName),
                ContentType = file.ContentType,
                FileSizeBytes = file.Length,
                UploadedAt = DateTime.Now
            });
            _db.MarketplaceAuditLogs.Add(new MarketplaceAuditLog
            {
                UserId = userId.Value,
                Action = "UploadPrescription",
                EntityName = "MarketplacePrescriptionRequest",
                EntityId = request.MarketplacePrescriptionRequestId.ToString(),
                Details = $"Prescription file uploaded for request {request.RequestNumber}.",
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            if (!string.IsNullOrWhiteSpace(savedPath) && System.IO.File.Exists(savedPath)) System.IO.File.Delete(savedPath);
            ModelState.AddModelError(string.Empty, "The prescription could not be stored safely. Please try again.");
            return View("Confirm", input);
        }

        try
        {
            var user = await _db.User.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId.Value, ct);
            if (user != null && !string.IsNullOrWhiteSpace(user.Email))
            {
                await _email.SendEmailAsync(user.Email,
                    $"Prescription request received - {request.RequestNumber}",
                    $"<h2>We received your prescription request</h2><p><strong>{offer.Product.ProductName}</strong> at <strong>{offer.Pharmacy.Name}</strong></p><p>Request: {request.RequestNumber}<br/>Quantity: {request.Quantity}<br/>Expires: {request.ExpiresAt:MMM d, yyyy}</p><p>Your uploaded prescription will be reviewed by an authorized pharmacy professional before fulfillment.</p>");
            }

            var staffEmails = await _db.PharmacyStaff.AsNoTracking().Where(x => x.PharmacyId == offer.PharmacyId && x.IsActive)
                .Select(x => x.User.Email).Where(x => x != null && x != "").Distinct().ToListAsync(ct);
            var adminEmails = await _db.User.AsNoTracking().Where(x => x.IsActive && x.Role == "Admin").Select(x => x.Email).ToListAsync(ct);
            foreach (var email in staffEmails.Concat(adminEmails).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct())
                await _email.SendEmailAsync(email, $"New marketplace prescription request - {request.RequestNumber}",
                    $"<h2>New prescription request</h2><p>{offer.Product.ProductName} × {request.Quantity}</p><p>Pharmacy: {offer.Pharmacy.Name}<br/>Request: {request.RequestNumber}<br/>Contact: {request.ContactPhone}</p><p>Review the uploaded prescription securely in the Pharmacy Portal.</p>");
        }
        catch { }

        return RedirectToAction(nameof(Complete), new { id = request.MarketplacePrescriptionRequestId });
    }

    [HttpGet]
    public async Task<IActionResult> PrescriptionFile(int id, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return Unauthorized();
        var file = await _db.Set<MarketplacePrescriptionFile>().AsNoTracking()
            .Include(x => x.Request)
            .FirstOrDefaultAsync(x => x.MarketplacePrescriptionFileId == id, ct);
        if (file == null) return NotFound();

        var role = HttpContext.Session.GetString("UserRole");
        var allowed = file.Request.UserId == userId.Value || role == "Admin" ||
            (role == "Pharmacist" && await _db.PharmacyStaff.AnyAsync(x => x.UserId == userId.Value && x.PharmacyId == file.Request.PharmacyId && x.IsActive, ct));
        if (!allowed) return Forbid();

        var physical = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, file.FileUrl.Replace('/', Path.DirectorySeparatorChar)));
        var root = Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "App_Data", "PrescriptionUploads"));
        if (!physical.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(physical)) return NotFound();
        return PhysicalFile(physical, file.ContentType, file.OriginalFileName);
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

    private void ValidatePrescriptionFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError(nameof(MarketplacePrescriptionConfirmViewModel.PrescriptionFile), "Upload a prescription image or PDF.");
            return;
        }
        if (file.Length > MaxPrescriptionBytes)
            ModelState.AddModelError(nameof(MarketplacePrescriptionConfirmViewModel.PrescriptionFile), "Prescription file must be 8 MB or smaller.");
        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension) || !AllowedContentTypes.Contains(file.ContentType))
            ModelState.AddModelError(nameof(MarketplacePrescriptionConfirmViewModel.PrescriptionFile), "Allowed prescription formats are JPG, PNG and PDF.");
    }
}
