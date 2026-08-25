using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PharmaCare.Controllers;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public class AdminAIController : Controller
{
    private readonly DataDbContext _db;
    private readonly IAIService _ai;

    public AdminAIController(DataDbContext db, IAIService ai)
    {
        _db = db;
        _ai = ai;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OperationsBrief(CancellationToken ct)
    {
        var role = HttpContext.Session.GetString("UserRole");
        if (role is not ("Admin" or "Pharmacist"))
            return Unauthorized(new { success = false, message = "Staff access required." });

        if (!_ai.IsConfigured)
            return StatusCode(503, new { success = false, message = "AI is not configured." });

        var today = DateTime.Now.Date;
        var expiry90 = today.AddDays(90);

        var lowStock = await _db.Product.AsNoTracking()
            .Where(p => p.IsActive && p.Stock > 0 && p.Stock <= Math.Max(10, p.ReorderLevel))
            .OrderBy(p => p.Stock)
            .Take(12)
            .Select(p => new { p.ProductName, p.Stock, p.ReorderLevel })
            .ToListAsync(ct);

        var outOfStock = await _db.Product.AsNoTracking()
            .Where(p => p.IsActive && p.Stock <= 0)
            .OrderBy(p => p.ProductName)
            .Take(12)
            .Select(p => p.ProductName)
            .ToListAsync(ct);

        var expiring = await _db.Product.AsNoTracking()
            .Where(p => p.IsActive && p.ExpiryDate >= today && p.ExpiryDate <= expiry90)
            .OrderBy(p => p.ExpiryDate)
            .Take(12)
            .Select(p => new { p.ProductName, p.Stock, p.ExpiryDate })
            .ToListAsync(ct);

        var pendingOrders = await _db.Orders.AsNoTracking()
            .CountAsync(o => o.Status == "Pending" || o.Status == "Processing", ct);

        var ordersToday = await _db.Orders.AsNoTracking().CountAsync(o => o.OrderDate.Date == today, ct);
        var revenueToday = await _db.Orders.AsNoTracking()
            .Where(o => o.Status != "Cancelled" && o.OrderDate.Date == today)
            .SumAsync(o => (decimal?)o.TotalAmount, ct) ?? 0m;

        var pendingRx = await _db.PrescriptionReservations.AsNoTracking()
            .CountAsync(r => r.Status == "Reserved", ct);

        var recentSupport = await _db.ContactMessages.AsNoTracking()
            .OrderByDescending(m => m.DateSubmitted)
            .Take(6)
            .Select(m => new { m.Subject, m.DateSubmitted })
            .ToListAsync(ct);

        var context = new StringBuilder();
        context.AppendLine("You are the internal PharmaCare pharmacy operations copilot.");
        context.AppendLine("Give a concise staff briefing with: 1) urgent actions, 2) today overview, 3) inventory risks, 4) prescription/support follow-up.");
        context.AppendLine("Do not diagnose, prescribe, recommend dose changes, or infer clinical decisions. Never expose private customer information.");
        context.AppendLine($"Orders today: {ordersToday}; revenue today: ${revenueToday:0.00}; pending/processing orders: {pendingOrders}; reserved prescriptions: {pendingRx}.");
        context.AppendLine("Low stock:");
        foreach (var p in lowStock) context.AppendLine($"- {p.ProductName}: {p.Stock} units, reorder level {p.ReorderLevel}");
        context.AppendLine("Out of stock:");
        foreach (var p in outOfStock) context.AppendLine($"- {p}");
        context.AppendLine("Expiring within 90 days:");
        foreach (var p in expiring) context.AppendLine($"- {p.ProductName}: {p.Stock} units, expires {p.ExpiryDate:yyyy-MM-dd}");
        context.AppendLine("Recent support subjects:");
        foreach (var m in recentSupport) context.AppendLine($"- {m.Subject ?? "Customer support message"} ({m.DateSubmitted:yyyy-MM-dd})");

        var result = await _ai.AskAsync(new AIRequest
        {
            Message = "Create my current operations briefing. Keep it practical and prioritized.",
            SiteContext = context.ToString(),
            UserContext = $"Staff role: {role}",
            History = Array.Empty<AIChatMessage>()
        }, ct);

        if (!result.Success)
            return StatusCode(502, new { success = false, message = result.Message });

        return Json(new { success = true, message = result.Message });
    }
}
