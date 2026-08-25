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
        var expiry90 = today.AddDays(90);

        var products = await _db.Product.AsNoTracking().Where(p => p.IsActive).ToListAsync(ct);
        var orders = await _db.Orders.AsNoTracking().OrderByDescending(o => o.OrderDate).Take(100).ToListAsync(ct);
        var reservations = await _db.PrescriptionReservations.AsNoTracking()
            .Include(r => r.Product).Include(r => r.User)
            .OrderByDescending(r => r.ReservationDate).Take(30).ToListAsync(ct);
        var feedback = await _db.ContactMessages.AsNoTracking().OrderByDescending(m => m.DateSubmitted).Take(20).ToListAsync(ct);

        var model = new OperationsViewModel
        {
            GeneratedAt = now,
            OrdersToday = orders.Count(o => o.OrderDate.Date == today),
            RevenueToday = orders.Where(o => o.Status != "Cancelled" && o.OrderDate.Date == today).Sum(o => o.TotalAmount),
            PendingOrders = orders.Count(o => o.Status is "Pending" or "Processing"),
            PendingPrescriptions = reservations.Count(r => r.Status == "Reserved"),
            LowStock = products.Where(p => p.Stock > 0 && p.Stock <= Math.Max(10, p.ReorderLevel)).OrderBy(p => p.Stock).Take(12).ToList(),
            OutOfStock = products.Where(p => p.Stock <= 0).OrderBy(p => p.ProductName).Take(12).ToList(),
            ExpiringSoon = products.Where(p => p.ExpiryDate.Date >= today && p.ExpiryDate.Date <= expiry90).OrderBy(p => p.ExpiryDate).Take(12).ToList(),
            Reservations = reservations,
            RecentFeedback = feedback,
            RecentOrders = orders.Take(12).ToList()
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
