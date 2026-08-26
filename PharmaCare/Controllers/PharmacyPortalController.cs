using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PharmaCare.Controllers;

public class PharmacyPortalController : Controller
{
    private readonly DataDbContext _db;
    public PharmacyPortalController(DataDbContext db) => _db = db;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        var role = HttpContext.Session.GetString("UserRole");
        if (role is not ("Admin" or "Pharmacist")) context.Result = RedirectToAction("Login", "Account");
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? pharmacyId, CancellationToken ct)
    {
        var role = HttpContext.Session.GetString("UserRole");
        var userId = HttpContext.Session.GetInt32("UserId");
        Pharmacy? pharmacy;

        if (role == "Admin")
        {
            pharmacy = pharmacyId.HasValue
                ? await _db.Pharmacies.AsNoTracking().FirstOrDefaultAsync(x => x.PharmacyId == pharmacyId.Value && x.IsActive, ct)
                : await _db.Pharmacies.AsNoTracking().OrderBy(x => x.PharmacyId).FirstOrDefaultAsync(x => x.IsActive, ct);
        }
        else
        {
            pharmacy = await _db.PharmacyStaff.AsNoTracking().Where(x => x.UserId == userId && x.IsActive)
                .Select(x => x.Pharmacy).FirstOrDefaultAsync(ct);
        }

        if (pharmacy == null) return View("NoPharmacy");
        var today = DateTime.Now.Date;
        var tomorrow = today.AddDays(1);

        var orders = await _db.MarketplaceOrders.AsNoTracking().Include(x => x.User).Include(x => x.Items)
            .Where(x => x.PharmacyId == pharmacy.PharmacyId).OrderByDescending(x => x.OrderDate).Take(30).ToListAsync(ct);
        var prescriptionRequests = await _db.MarketplacePrescriptionRequests.AsNoTracking()
            .Include(x => x.User).Include(x => x.Product)
            .Where(x => x.PharmacyId == pharmacy.PharmacyId)
            .OrderByDescending(x => x.RequestedAt).Take(30).ToListAsync(ct);
        var low = await _db.PharmacyProducts.AsNoTracking().Include(x => x.Product)
            .Where(x => x.PharmacyId == pharmacy.PharmacyId && x.IsAvailable && x.Stock <= x.ReorderLevel)
            .OrderBy(x => x.Stock).Take(12).ToListAsync(ct);

        var model = new PharmacyPortalViewModel
        {
            Pharmacy = pharmacy,
            Orders = orders,
            PrescriptionRequests = prescriptionRequests,
            LowStock = low,
            PendingOrders = await _db.MarketplaceOrders.CountAsync(x => x.PharmacyId == pharmacy.PharmacyId && x.Status == "Pending", ct),
            PreparingOrders = await _db.MarketplaceOrders.CountAsync(x => x.PharmacyId == pharmacy.PharmacyId && (x.Status == "Accepted" || x.Status == "Preparing"), ct),
            PendingPrescriptionRequests = await _db.MarketplacePrescriptionRequests.CountAsync(x => x.PharmacyId == pharmacy.PharmacyId && (x.Status == "Requested" || x.Status == "Approved" || x.Status == "Ready for Pickup"), ct),
            RevenueToday = await _db.MarketplaceOrders.Where(x => x.PharmacyId == pharmacy.PharmacyId && x.Status != "Cancelled" && x.OrderDate >= today && x.OrderDate < tomorrow).SumAsync(x => (decimal?)x.TotalAmount, ct) ?? 0m,
            ActiveProducts = await _db.PharmacyProducts.CountAsync(x => x.PharmacyId == pharmacy.PharmacyId && x.IsAvailable, ct)
        };
        ViewBag.Pharmacies = role == "Admin" ? await _db.Pharmacies.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync(ct) : new List<Pharmacy>();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int orderId, string status, CancellationToken ct)
    {
        var allowed = new[] { "Pending", "Accepted", "Preparing", "Out for Delivery", "Delivered", "Cancelled" };
        if (!allowed.Contains(status)) return BadRequest();
        var order = await _db.MarketplaceOrders.FirstOrDefaultAsync(x => x.MarketplaceOrderId == orderId, ct);
        if (order == null) return NotFound();
        if (!await CanManagePharmacyAsync(order.PharmacyId, ct)) return Forbid();

        order.Status = status;
        if (status == "Accepted") order.AcceptedAt ??= DateTime.Now;
        if (status == "Out for Delivery") order.OutForDeliveryAt ??= DateTime.Now;
        if (status == "Delivered") order.DeliveredAt ??= DateTime.Now;
        await _db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Index), new { pharmacyId = order.PharmacyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePrescriptionStatus(int requestId, string status, string? staffNote, CancellationToken ct)
    {
        var allowed = new[] { "Requested", "Approved", "Ready for Pickup", "Completed", "Rejected", "Cancelled" };
        if (!allowed.Contains(status)) return BadRequest();
        var request = await _db.MarketplacePrescriptionRequests.FirstOrDefaultAsync(x => x.MarketplacePrescriptionRequestId == requestId, ct);
        if (request == null) return NotFound();
        if (!await CanManagePharmacyAsync(request.PharmacyId, ct)) return Forbid();

        request.Status = status;
        request.StaffNote = string.IsNullOrWhiteSpace(staffNote) ? request.StaffNote : staffNote.Trim();
        if (status is "Approved" or "Ready for Pickup" or "Completed" or "Rejected") request.ReviewedAt ??= DateTime.Now;
        await _db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Index), new { pharmacyId = request.PharmacyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStock(int pharmacyProductId, int stock, decimal price, CancellationToken ct)
    {
        var offer = await _db.PharmacyProducts.FirstOrDefaultAsync(x => x.PharmacyProductId == pharmacyProductId, ct);
        if (offer == null) return NotFound();
        if (!await CanManagePharmacyAsync(offer.PharmacyId, ct)) return Forbid();

        offer.Stock = Math.Max(0, stock);
        offer.Price = Math.Max(.01m, price);
        offer.IsAvailable = offer.Stock > 0;
        offer.UpdatedAt = DateTime.Now;
        await _db.SaveChangesAsync(ct);
        return RedirectToAction(nameof(Index), new { pharmacyId = offer.PharmacyId });
    }

    private async Task<bool> CanManagePharmacyAsync(int pharmacyId, CancellationToken ct)
    {
        var role = HttpContext.Session.GetString("UserRole");
        if (role == "Admin") return true;
        var userId = HttpContext.Session.GetInt32("UserId");
        return userId.HasValue && await _db.PharmacyStaff.AnyAsync(x => x.UserId == userId.Value && x.PharmacyId == pharmacyId && x.IsActive, ct);
    }
}
