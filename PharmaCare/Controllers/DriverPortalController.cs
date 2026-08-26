using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PharmaCare.Controllers;

public class DriverPortalController : Controller
{
    private readonly DataDbContext _db;
    private readonly IMarketplaceOperationsService _operations;

    public DriverPortalController(DataDbContext db, IMarketplaceOperationsService operations)
    {
        _db = db;
        _operations = operations;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        var role = HttpContext.Session.GetString("UserRole");
        if (role is not ("Driver" or "Admin")) context.Result = RedirectToAction("Login", "Account");
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return RedirectToAction("Login", "Account");

        var available = await _db.MarketplaceOrders.AsNoTracking().Include(x => x.Pharmacy)
            .Where(x => x.Status == "Ready for Pickup" && !_db.MarketplaceDeliveryAssignments.Any(a => a.MarketplaceOrderId == x.MarketplaceOrderId))
            .OrderBy(x => x.OrderDate).Take(20).ToListAsync(ct);

        var assigned = await _db.MarketplaceDeliveryAssignments.AsNoTracking()
            .Include(x => x.MarketplaceOrder).ThenInclude(x => x.Pharmacy)
            .Where(x => x.DriverUserId == userId.Value && x.Status != "Delivered" && x.Status != "Cancelled")
            .OrderByDescending(x => x.AssignedAt).ToListAsync(ct);

        return View(new DriverPortalViewModel { AvailableOrders = available, Assignments = assigned });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Claim(int orderId, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return RedirectToAction("Login", "Account");
        var order = await _db.MarketplaceOrders.FirstOrDefaultAsync(x => x.MarketplaceOrderId == orderId && x.Status == "Ready for Pickup", ct);
        if (order == null) return NotFound();
        if (await _db.MarketplaceDeliveryAssignments.AnyAsync(x => x.MarketplaceOrderId == orderId, ct))
        {
            TempData["DriverError"] = "This delivery has already been assigned.";
            return RedirectToAction(nameof(Index));
        }

        _db.MarketplaceDeliveryAssignments.Add(new MarketplaceDeliveryAssignment
        {
            MarketplaceOrderId = orderId,
            DriverUserId = userId.Value,
            Status = "Assigned",
            AssignedAt = DateTime.Now
        });
        await _db.SaveChangesAsync(ct);
        await _operations.ChangeOrderStatusAsync(orderId, "Driver Assigned", userId.Value, "Driver accepted the delivery.", ct);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int assignmentId, string action, string? problemNote, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return RedirectToAction("Login", "Account");
        var role = HttpContext.Session.GetString("UserRole");
        var assignment = await _db.MarketplaceDeliveryAssignments.Include(x => x.MarketplaceOrder)
            .FirstOrDefaultAsync(x => x.MarketplaceDeliveryAssignmentId == assignmentId && (x.DriverUserId == userId.Value || role == "Admin"), ct);
        if (assignment == null) return NotFound();

        try
        {
            switch (action)
            {
                case "Arrived":
                    assignment.Status = "Arrived";
                    assignment.ArrivedAtPharmacy ??= DateTime.Now;
                    break;
                case "PickedUp":
                    assignment.Status = "Picked Up";
                    assignment.PickedUpAt ??= DateTime.Now;
                    await _operations.ChangeOrderStatusAsync(assignment.MarketplaceOrderId, "Picked Up", userId.Value, "Driver collected the order from the pharmacy.", ct);
                    break;
                case "StartDelivery":
                    assignment.Status = "On the Way";
                    assignment.StartedDeliveryAt ??= DateTime.Now;
                    await _operations.ChangeOrderStatusAsync(assignment.MarketplaceOrderId, "On the Way", userId.Value, "Driver started delivery.", ct);
                    break;
                case "Delivered":
                    assignment.Status = "Delivered";
                    assignment.DeliveredAt ??= DateTime.Now;
                    await _operations.ChangeOrderStatusAsync(assignment.MarketplaceOrderId, "Delivered", userId.Value, "Driver confirmed delivery.", ct);
                    break;
                case "Problem":
                    assignment.Status = "Problem";
                    assignment.ProblemNote = string.IsNullOrWhiteSpace(problemNote) ? "Delivery issue reported." : problemNote.Trim();
                    if (assignment.MarketplaceOrder.Status is "Picked Up" or "On the Way")
                        await _operations.ChangeOrderStatusAsync(assignment.MarketplaceOrderId, "Failed Delivery", userId.Value, assignment.ProblemNote, ct);
                    break;
                default:
                    return BadRequest();
            }

            await _db.SaveChangesAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            TempData["DriverError"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}

public sealed class DriverPortalViewModel
{
    public List<MarketplaceOrder> AvailableOrders { get; set; } = new();
    public List<MarketplaceDeliveryAssignment> Assignments { get; set; } = new();
}
