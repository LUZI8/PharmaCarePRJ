using Microsoft.EntityFrameworkCore;

namespace PharmaCare.Services;

public interface IMarketplaceOperationsService
{
    Task RecordOrderCreatedAsync(MarketplaceOrder order, CancellationToken ct = default);
    Task<bool> ChangeOrderStatusAsync(int orderId, string newStatus, int? changedByUserId, string? notes = null, CancellationToken ct = default);
}

public sealed class MarketplaceOperationsService : IMarketplaceOperationsService
{
    private static readonly Dictionary<string, string[]> AllowedTransitions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Pending"] = new[] { "Accepted", "Cancelled" },
        ["Accepted"] = new[] { "Preparing", "Cancelled" },
        ["Preparing"] = new[] { "Ready for Pickup", "Out for Delivery", "Cancelled" },
        ["Ready for Pickup"] = new[] { "Driver Assigned", "Out for Delivery", "Delivered", "Cancelled" },
        ["Driver Assigned"] = new[] { "Picked Up", "Cancelled" },
        ["Picked Up"] = new[] { "On the Way", "Failed Delivery" },
        ["On the Way"] = new[] { "Delivered", "Failed Delivery" },
        ["Out for Delivery"] = new[] { "Delivered", "Failed Delivery" },
        ["Failed Delivery"] = new[] { "On the Way", "Out for Delivery", "Cancelled" },
        ["Delivered"] = Array.Empty<string>(),
        ["Cancelled"] = Array.Empty<string>()
    };

    private readonly DataDbContext _db;
    public MarketplaceOperationsService(DataDbContext db) => _db = db;

    public async Task RecordOrderCreatedAsync(MarketplaceOrder order, CancellationToken ct = default)
    {
        _db.MarketplaceOrderStatusHistory.Add(new MarketplaceOrderStatusHistory
        {
            MarketplaceOrderId = order.MarketplaceOrderId,
            Status = order.Status,
            ChangedByUserId = order.UserId,
            Notes = "Order submitted by customer.",
            ChangedAt = order.OrderDate
        });

        _db.MarketplaceNotifications.Add(new MarketplaceNotification
        {
            UserId = order.UserId,
            Type = "Order",
            Title = $"Order {order.OrderNumber} received",
            Message = "Your order was sent to the pharmacy and is waiting for confirmation.",
            ActionUrl = $"/MarketplaceOrders/Details/{order.MarketplaceOrderId}",
            CreatedAt = DateTime.Now
        });

        _db.MarketplaceAuditLogs.Add(new MarketplaceAuditLog
        {
            UserId = order.UserId,
            Action = "CreateOrder",
            EntityName = "MarketplaceOrder",
            EntityId = order.MarketplaceOrderId.ToString(),
            Details = $"Order {order.OrderNumber} created for pharmacy {order.PharmacyId}. Total {order.TotalAmount:0.00}.",
            CreatedAt = DateTime.Now
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> ChangeOrderStatusAsync(int orderId, string newStatus, int? changedByUserId, string? notes = null, CancellationToken ct = default)
    {
        var order = await _db.MarketplaceOrders.FirstOrDefaultAsync(x => x.MarketplaceOrderId == orderId, ct);
        if (order == null) return false;
        if (string.Equals(order.Status, newStatus, StringComparison.OrdinalIgnoreCase)) return true;

        if (!AllowedTransitions.TryGetValue(order.Status, out var allowed) || !allowed.Contains(newStatus, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Order cannot move from {order.Status} to {newStatus}.");

        var previous = order.Status;
        order.Status = newStatus;
        if (newStatus == "Accepted") order.AcceptedAt ??= DateTime.Now;
        if (newStatus is "On the Way" or "Out for Delivery") order.OutForDeliveryAt ??= DateTime.Now;
        if (newStatus == "Delivered") order.DeliveredAt ??= DateTime.Now;

        _db.MarketplaceOrderStatusHistory.Add(new MarketplaceOrderStatusHistory
        {
            MarketplaceOrderId = order.MarketplaceOrderId,
            Status = newStatus,
            ChangedByUserId = changedByUserId,
            Notes = notes,
            ChangedAt = DateTime.Now
        });

        _db.MarketplaceNotifications.Add(new MarketplaceNotification
        {
            UserId = order.UserId,
            Type = "OrderStatus",
            Title = $"Order {order.OrderNumber}: {newStatus}",
            Message = StatusMessage(newStatus),
            ActionUrl = $"/MarketplaceOrders/Details/{order.MarketplaceOrderId}",
            CreatedAt = DateTime.Now
        });

        _db.MarketplaceAuditLogs.Add(new MarketplaceAuditLog
        {
            UserId = changedByUserId,
            Action = "ChangeOrderStatus",
            EntityName = "MarketplaceOrder",
            EntityId = order.MarketplaceOrderId.ToString(),
            Details = $"{previous} -> {newStatus}. {notes}",
            CreatedAt = DateTime.Now
        });

        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static string StatusMessage(string status) => status switch
    {
        "Accepted" => "The pharmacy confirmed your order.",
        "Preparing" => "The pharmacy is preparing your items.",
        "Ready for Pickup" => "Your order is ready for pickup or driver collection.",
        "Driver Assigned" => "A driver has been assigned to your order.",
        "Picked Up" => "Your order was picked up from the pharmacy.",
        "On the Way" => "Your order is on the way.",
        "Out for Delivery" => "Your order is on the way.",
        "Delivered" => "Your order has been delivered. Thank you for using PharmaCare.",
        "Cancelled" => "Your order was cancelled.",
        "Failed Delivery" => "The delivery attempt was not completed. The pharmacy will review the next step.",
        _ => $"Your order status changed to {status}."
    };
}
