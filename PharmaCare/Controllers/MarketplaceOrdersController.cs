using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PharmaCare.Controllers;

public class MarketplaceOrdersController : Controller
{
    private readonly DataDbContext _db;
    public MarketplaceOrdersController(DataDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return RedirectToAction("Login", "Account");
        var orders = await _db.MarketplaceOrders.AsNoTracking().Include(x => x.Pharmacy).Include(x => x.Items)
            .Where(x => x.UserId == userId.Value).OrderByDescending(x => x.OrderDate).ToListAsync(ct);
        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return RedirectToAction("Login", "Account");
        var order = await _db.MarketplaceOrders.AsNoTracking().Include(x => x.Pharmacy).Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.MarketplaceOrderId == id && x.UserId == userId.Value, ct);
        if (order == null) return NotFound();

        var history = await _db.MarketplaceOrderStatusHistory.AsNoTracking()
            .Where(x => x.MarketplaceOrderId == id)
            .OrderBy(x => x.ChangedAt)
            .ToListAsync(ct);

        if (history.Count == 0)
        {
            history.Add(new MarketplaceOrderStatusHistory
            {
                MarketplaceOrderId = order.MarketplaceOrderId,
                Status = "Pending",
                ChangedAt = order.OrderDate,
                Notes = "Order submitted."
            });
        }

        return View(new MarketplaceOrderDetailsViewModel { Order = order, History = history });
    }
}
