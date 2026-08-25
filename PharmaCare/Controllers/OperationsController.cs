using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PharmaCare.Controllers;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class OperationsController : Controller
{
    private readonly DataDbContext _db;

    public OperationsController(DataDbContext db) => _db = db;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        var role = HttpContext.Session.GetString("UserRole");
        if (role is not ("Admin" or "Pharmacist"))
            context.Result = RedirectToAction("Login", "Account");
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var now = DateTime.Now;
        var today = now.Date;
        var tomorrow = today.AddDays(1);
        var expiry90 = today.AddDays(90);

        var products = await _db.Product.AsNoTracking().Where(p => p.IsActive).ToListAsync(ct);
        var recentOrders = await _db.Orders.AsNoTracking().OrderByDescending(o => o.OrderDate).Take(12).ToListAsync(ct);
        var reservations = await _db.PrescriptionReservations.AsNoTracking()
            .Include(r => r.Product)
            .OrderByDescending(r => r.ReservationDate).Take(30).ToListAsync(ct);
        var feedback = await _db.ContactMessages.AsNoTracking().OrderByDescending(m => m.DateSubmitted).Take(20).ToListAsync(ct);

        var ordersToday = await _db.Orders.AsNoTracking().CountAsync(o => o.OrderDate >= today && o.OrderDate < tomorrow, ct);
        var revenueToday = await _db.Orders.AsNoTracking()
            .Where(o => o.Status != "Cancelled" && o.OrderDate >= today && o.OrderDate < tomorrow)
            .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0m;
        var pendingOrders = await _db.Orders.AsNoTracking()
            .CountAsync(o => o.Status == "Pending" || o.Status == "Processing", ct);
        var pendingPrescriptions = await _db.PrescriptionReservations.AsNoTracking()
            .CountAsync(r => r.Status == "Reserved", ct);

        var model = new OperationsViewModel
        {
            GeneratedAt = now,
            OrdersToday = ordersToday,
            RevenueToday = revenueToday,
            PendingOrders = pendingOrders,
            PendingPrescriptions = pendingPrescriptions,
            LowStock = products.Where(p => p.Stock > 0 && (p.Stock <= 10 || p.Stock <= p.ReorderLevel)).OrderBy(p => p.Stock).Take(12).ToList(),
            OutOfStock = products.Where(p => p.Stock <= 0).OrderBy(p => p.ProductName).Take(12).ToList(),
            ExpiringSoon = products.Where(p => p.ExpiryDate >= today && p.ExpiryDate <= expiry90).OrderBy(p => p.ExpiryDate).Take(12).ToList(),
            Reservations = reservations,
            RecentFeedback = feedback,
            RecentOrders = recentOrders
        };

        return View(model);
    }
}

public sealed class OperationsViewModel
{
    public DateTime GeneratedAt { get; set; }
    public int OrdersToday { get; set; }
    public decimal RevenueToday { get; set; }
    public int PendingOrders { get; set; }
    public int PendingPrescriptions { get; set; }
    public List<Product> LowStock { get; set; } = new();
    public List<Product> OutOfStock { get; set; } = new();
    public List<Product> ExpiringSoon { get; set; } = new();
    public List<PrescriptionReservation> Reservations { get; set; } = new();
    public List<ContactMessage> RecentFeedback { get; set; } = new();
    public List<Order> RecentOrders { get; set; } = new();
}
