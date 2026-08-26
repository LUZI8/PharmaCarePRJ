using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PharmaCare.Controllers;

[ApiController]
[Route("api/marketplace")]
public class MarketplaceApiController : ControllerBase
{
    private readonly DataDbContext _db;
    private readonly ISmartCartService _smartCart;

    public MarketplaceApiController(DataDbContext db, ISmartCartService smartCart)
    {
        _db = db;
        _smartCart = smartCart;
    }

    [HttpGet("pharmacies")]
    public async Task<IActionResult> Pharmacies(string city = "Amman", string sort = "recommended", decimal? lat = null, decimal? lng = null, CancellationToken ct = default)
    {
        var rows = await _db.Pharmacies.AsNoTracking()
            .Where(x => x.IsActive && x.IsVerified && x.City == city)
            .Select(x => new PharmacyApiItem
            {
                PharmacyId = x.PharmacyId, Name = x.Name, Address = x.Address, Rating = x.Rating,
                RatingCount = x.RatingCount, DeliveryFee = x.DeliveryFee, EstimatedDeliveryMinutes = x.EstimatedDeliveryMinutes,
                IsOpen = x.IsOpen, Latitude = x.Latitude, Longitude = x.Longitude,
                AvailableProducts = x.Products.Count(p => p.IsAvailable && p.Stock > 0)
            }).ToListAsync(ct);

        if (lat.HasValue && lng.HasValue)
            foreach (var x in rows)
                if (x.Latitude.HasValue && x.Longitude.HasValue)
                    x.DistanceKm = DistanceKm((double)lat.Value, (double)lng.Value, (double)x.Latitude.Value, (double)x.Longitude.Value);

        rows = sort.ToLowerInvariant() switch
        {
            "nearest" => rows.OrderBy(x => x.DistanceKm ?? double.MaxValue).ThenByDescending(x => x.Rating).ToList(),
            "fastest" => rows.OrderBy(x => x.EstimatedDeliveryMinutes).ThenBy(x => x.DeliveryFee).ToList(),
            "rating" => rows.OrderByDescending(x => x.Rating).ThenBy(x => x.EstimatedDeliveryMinutes).ToList(),
            "deliveryfee" => rows.OrderBy(x => x.DeliveryFee).ThenBy(x => x.EstimatedDeliveryMinutes).ToList(),
            _ => rows.OrderByDescending(x => x.IsOpen).ThenByDescending(x => x.Rating).ThenBy(x => x.EstimatedDeliveryMinutes).ToList()
        };

        return Ok(new { success = true, count = rows.Count, items = rows });
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(string q, string city = "Amman", string sort = "cheapest", int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q)) return BadRequest(new { success = false, message = "Search text is required." });
        q = q.Trim();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _db.PharmacyProducts.AsNoTracking()
            .Where(x => x.IsAvailable && x.Stock > 0 && x.Product.IsActive && x.Pharmacy.IsActive && x.Pharmacy.IsVerified && x.Pharmacy.City == city)
            .Where(x => x.Product.ProductName.Contains(q) ||
                        (x.Product.Manufacturer != null && x.Product.Manufacturer.Contains(q)) ||
                        (x.Product.SKU != null && x.Product.SKU.Contains(q)) ||
                        (x.Product.Barcode != null && x.Product.Barcode.Contains(q)) ||
                        x.Product.Category.CategoryName.Contains(q) ||
                        x.Pharmacy.Name.Contains(q));

        query = sort.ToLowerInvariant() switch
        {
            "fastest" => query.OrderBy(x => x.Pharmacy.EstimatedDeliveryMinutes).ThenBy(x => x.Price),
            "rating" => query.OrderByDescending(x => x.Pharmacy.Rating).ThenBy(x => x.Price),
            _ => query.OrderBy(x => x.Price).ThenBy(x => x.Pharmacy.EstimatedDeliveryMinutes)
        };

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new
            {
                x.PharmacyProductId, x.ProductId, x.PharmacyId,
                product = x.Product.ProductName,
                brand = x.Product.Manufacturer,
                category = x.Product.Category.CategoryName,
                image = x.Product.ImageUrl,
                prescription = x.Product.RequiresPrescription,
                pharmacy = x.Pharmacy.Name,
                x.Price, x.CompareAtPrice, x.Stock,
                deliveryMinutes = x.Pharmacy.EstimatedDeliveryMinutes,
                deliveryFee = x.Pharmacy.DeliveryFee,
                rating = x.Pharmacy.Rating
            }).ToListAsync(ct);

        return Ok(new { success = true, query = q, page, pageSize, total, items });
    }

    [HttpGet("suggest")]
    public async Task<IActionResult> Suggest(string q, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2) return Ok(Array.Empty<object>());
        q = q.Trim();
        var suggestions = await _db.Product.AsNoTracking()
            .Where(x => x.IsActive && (x.ProductName.Contains(q) || (x.Manufacturer != null && x.Manufacturer.Contains(q))))
            .OrderBy(x => x.ProductName).Take(8)
            .Select(x => new { x.ProductId, x.ProductName, x.Manufacturer, x.ImageUrl, x.RequiresPrescription })
            .ToListAsync(ct);
        return Ok(suggestions);
    }

    [HttpPost("smart-cart")]
    public async Task<IActionResult> SmartCart([FromBody] SmartCartApiRequest request, CancellationToken ct)
    {
        if (request.Items == null || request.Items.Count == 0)
            return BadRequest(new { success = false, message = "Add at least one product to analyze." });

        var result = await _smartCart.RecommendAsync(request.Items, request.City ?? "Amman", ct);
        return Ok(new
        {
            success = true,
            result.RequestedProducts,
            result.AllProductsCanBeCovered,
            bestSinglePharmacy = result.BestSinglePharmacy,
            suggestedCombination = result.SuggestedCombination,
            alternatives = result.Matches.Take(8)
        });
    }

    private static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double radius = 6371d;
        static double Rad(double v) => v * Math.PI / 180d;
        var dLat = Rad(lat2 - lat1);
        var dLon = Rad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return Math.Round(radius * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)), 2);
    }
}

public sealed class SmartCartApiRequest
{
    public string? City { get; set; } = "Amman";
    public List<SmartCartItemRequest> Items { get; set; } = new();
}

public sealed class PharmacyApiItem
{
    public int PharmacyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public int RatingCount { get; set; }
    public decimal DeliveryFee { get; set; }
    public int EstimatedDeliveryMinutes { get; set; }
    public bool IsOpen { get; set; }
    public int AvailableProducts { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public double? DistanceKm { get; set; }
}
