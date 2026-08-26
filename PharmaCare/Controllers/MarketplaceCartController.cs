using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PharmaCare.Controllers;

public class MarketplaceCartController : Controller
{
    private const string SessionKey = "MarketplaceBasket";
    private readonly DataDbContext _db;

    public MarketplaceCartController(DataDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var model = await BuildBasketAsync(ct);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int pharmacyProductId, int quantity = 1, CancellationToken ct = default)
    {
        quantity = Math.Max(1, quantity);
        var offer = await _db.PharmacyProducts.AsNoTracking()
            .Include(x => x.Pharmacy).Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.PharmacyProductId == pharmacyProductId && x.IsAvailable && x.Stock > 0 && x.Pharmacy.IsActive && x.Product.IsActive, ct);
        if (offer == null) return NotFound();

        if (offer.Product.RequiresPrescription)
        {
            TempData["MarketplaceMessage"] = "Prescription medicines use the reservation flow. Choose this pharmacy, then complete prescription verification.";
            return Redirect($"/FrontEnd/ShopSingle/{offer.ProductId}");
        }

        var basket = ReadBasket();
        if (basket.Items.Count > 0 && basket.PharmacyId != offer.PharmacyId)
        {
            TempData["MarketplaceError"] = "Your basket already belongs to another pharmacy. Complete or clear that basket before ordering from a different pharmacy.";
            return RedirectToAction(nameof(Index));
        }

        basket.PharmacyId = offer.PharmacyId;
        var item = basket.Items.FirstOrDefault(x => x.PharmacyProductId == pharmacyProductId);
        var desired = (item?.Quantity ?? 0) + quantity;
        if (desired > offer.Stock) desired = offer.Stock;
        if (item == null) basket.Items.Add(new MarketplaceBasketItemState { PharmacyProductId = pharmacyProductId, Quantity = desired });
        else item.Quantity = desired;
        SaveBasket(basket);

        TempData["MarketplaceMessage"] = $"{offer.Product.ProductName} added from {offer.Pharmacy.Name}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int pharmacyProductId, int quantity, CancellationToken ct)
    {
        var basket = ReadBasket();
        var item = basket.Items.FirstOrDefault(x => x.PharmacyProductId == pharmacyProductId);
        if (item == null) return RedirectToAction(nameof(Index));
        if (quantity <= 0) basket.Items.Remove(item);
        else
        {
            var stock = await _db.PharmacyProducts.AsNoTracking().Where(x => x.PharmacyProductId == pharmacyProductId).Select(x => x.Stock).FirstOrDefaultAsync(ct);
            item.Quantity = Math.Min(Math.Max(1, quantity), stock);
        }
        if (basket.Items.Count == 0) basket.PharmacyId = 0;
        SaveBasket(basket);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear()
    {
        HttpContext.Session.Remove(SessionKey);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Checkout(CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return RedirectToAction("Login", "Account", new { returnUrl = "/MarketplaceCart/Checkout" });
        var basket = await BuildBasketAsync(ct);
        if (!basket.Items.Any()) return RedirectToAction(nameof(Index));
        var user = await _db.User.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId.Value, ct);
        return View(new MarketplaceCheckoutViewModel
        {
            Basket = basket,
            ShippingAddress = user?.Address ?? string.Empty,
            City = string.IsNullOrWhiteSpace(user?.City) ? "Amman" : user!.City,
            PhoneNumber = user?.PhoneNumber ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(MarketplaceCheckoutViewModel input, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return RedirectToAction("Login", "Account");
        var basketState = ReadBasket();
        var basket = await BuildBasketAsync(ct);
        input.Basket = basket;
        if (!basket.Items.Any()) ModelState.AddModelError(string.Empty, "Your marketplace basket is empty.");
        if (basket.HasPrescriptionItems) ModelState.AddModelError(string.Empty, "Prescription medicines must be reserved through the prescription verification flow.");
        if (!ModelState.IsValid) return View("Checkout", input);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            var ids = basketState.Items.Select(x => x.PharmacyProductId).ToList();
            var offers = await _db.PharmacyProducts.Include(x => x.Product).Where(x => ids.Contains(x.PharmacyProductId)).ToDictionaryAsync(x => x.PharmacyProductId, ct);
            var order = new MarketplaceOrder
            {
                OrderNumber = $"MKT-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                UserId = userId.Value,
                PharmacyId = basketState.PharmacyId,
                ShippingAddress = input.ShippingAddress.Trim(), City = input.City.Trim(), PhoneNumber = input.PhoneNumber.Trim(),
                DeliveryNotes = input.DeliveryNotes, PaymentMethod = "Cash on Delivery", Status = "Pending",
                DeliveryFee = basket.DeliveryFee, OrderDate = DateTime.Now
            };

            foreach (var state in basketState.Items)
            {
                if (!offers.TryGetValue(state.PharmacyProductId, out var offer) || !offer.IsAvailable || offer.Stock < state.Quantity)
                    throw new InvalidOperationException("One or more pharmacy items are no longer available in the requested quantity.");
                if (offer.Product.RequiresPrescription)
                    throw new InvalidOperationException("Prescription medicines cannot be checked out as standard marketplace items.");
                offer.Stock -= state.Quantity;
                offer.IsAvailable = offer.Stock > 0;
                offer.UpdatedAt = DateTime.Now;
                order.Items.Add(new MarketplaceOrderItem
                {
                    PharmacyProductId = offer.PharmacyProductId, ProductId = offer.ProductId, ProductName = offer.Product.ProductName,
                    Quantity = state.Quantity, UnitPrice = offer.Price, LineTotal = offer.Price * state.Quantity,
                    RequiresPrescription = offer.Product.RequiresPrescription
                });
            }

            order.Subtotal = order.Items.Sum(x => x.LineTotal);
            order.TotalAmount = order.Subtotal + order.DeliveryFee;
            _db.MarketplaceOrders.Add(order);
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            HttpContext.Session.Remove(SessionKey);
            return RedirectToAction(nameof(ThankYou), new { id = order.MarketplaceOrderId });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            ModelState.AddModelError(string.Empty, ex.Message);
            input.Basket = await BuildBasketAsync(ct);
            return View("Checkout", input);
        }
    }

    [HttpGet]
    public async Task<IActionResult> ThankYou(int id, CancellationToken ct)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue) return RedirectToAction("Login", "Account");
        var order = await _db.MarketplaceOrders.AsNoTracking().Include(x => x.Pharmacy).Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.MarketplaceOrderId == id && x.UserId == userId.Value, ct);
        if (order == null) return NotFound();
        return View(new MarketplaceThankYouViewModel { Order = order });
    }

    [HttpGet]
    public IActionResult Count()
    {
        var basket = ReadBasket();
        return Json(new { success = true, count = basket.Items.Sum(x => x.Quantity), pharmacyId = basket.PharmacyId });
    }

    private MarketplaceBasketState ReadBasket()
    {
        var json = HttpContext.Session.GetString(SessionKey);
        if (string.IsNullOrWhiteSpace(json)) return new MarketplaceBasketState();
        try { return JsonSerializer.Deserialize<MarketplaceBasketState>(json) ?? new MarketplaceBasketState(); }
        catch { return new MarketplaceBasketState(); }
    }

    private void SaveBasket(MarketplaceBasketState basket) => HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(basket));

    private async Task<MarketplaceBasketViewModel> BuildBasketAsync(CancellationToken ct)
    {
        var state = ReadBasket();
        var model = new MarketplaceBasketViewModel();
        if (state.PharmacyId <= 0 || state.Items.Count == 0) return model;

        model.Pharmacy = await _db.Pharmacies.AsNoTracking().FirstOrDefaultAsync(x => x.PharmacyId == state.PharmacyId && x.IsActive, ct);
        if (model.Pharmacy == null) return new MarketplaceBasketViewModel();

        var ids = state.Items.Select(x => x.PharmacyProductId).Distinct().ToList();
        var offers = await _db.PharmacyProducts.AsNoTracking().Include(x => x.Product)
            .Where(x => ids.Contains(x.PharmacyProductId) && x.PharmacyId == state.PharmacyId).ToDictionaryAsync(x => x.PharmacyProductId, ct);
        foreach (var item in state.Items)
        {
            if (!offers.TryGetValue(item.PharmacyProductId, out var offer)) continue;
            var qty = Math.Min(item.Quantity, offer.Stock);
            if (qty <= 0) continue;
            model.Items.Add(new MarketplaceBasketLineViewModel
            {
                PharmacyProductId=offer.PharmacyProductId, ProductId=offer.ProductId, ProductName=offer.Product.ProductName,
                ImageUrl=offer.Product.ImageUrl, UnitPrice=offer.Price, Quantity=qty, AvailableStock=offer.Stock,
                RequiresPrescription=offer.Product.RequiresPrescription
            });
        }
        model.Subtotal = model.Items.Sum(x => x.LineTotal);
        model.DeliveryFee = model.Pharmacy.DeliveryFee;
        model.Total = model.Subtotal + model.DeliveryFee;
        return model;
    }
}
