using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PharmaCare.Controllers;

public class MarketplaceController : Controller
{
    private readonly DataDbContext _db;
    public MarketplaceController(DataDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? city, string? sort, decimal? lat, decimal? lng, CancellationToken ct)
    {
        q = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        sort = string.IsNullOrWhiteSpace(sort) ? "recommended" : sort.Trim().ToLowerInvariant();

        var cities = await _db.Pharmacies.AsNoTracking()
            .Where(p => p.IsActive && p.IsVerified)
            .Select(p => p.City)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(ct);

        city = string.IsNullOrWhiteSpace(city)
            ? (cities.Contains("Amman") ? "Amman" : cities.FirstOrDefault() ?? "Amman")
            : city.Trim();

        var pharmacies = await _db.Pharmacies.AsNoTracking()
            .Where(p => p.IsActive && p.IsVerified && p.City == city)
            .Select(p => new PharmacyCardViewModel
            {
                PharmacyId = p.PharmacyId,
                Name = p.Name,
                Address = p.Address,
                LogoUrl = p.LogoUrl,
                Rating = p.Rating,
                RatingCount = p.RatingCount,
                DeliveryMinutes = p.EstimatedDeliveryMinutes,
                DeliveryFee = p.DeliveryFee,
                IsOpen = p.IsOpen,
                AvailableProducts = p.Products.Count(x => x.IsAvailable && x.Stock > 0),
                StartingPrice = p.Products.Where(x => x.IsAvailable && x.Stock > 0).Select(x => (decimal?)x.Price).Min(),
                Latitude = p.Latitude,
                Longitude = p.Longitude
            }).ToListAsync(ct);

        if (lat.HasValue && lng.HasValue)
        {
            foreach (var p in pharmacies)
                if (p.Latitude.HasValue && p.Longitude.HasValue)
                    p.DistanceKm = DistanceKm((double)lat.Value, (double)lng.Value, (double)p.Latitude.Value, (double)p.Longitude.Value);
        }

        pharmacies = sort switch
        {
            "nearest" => pharmacies.OrderBy(x => x.DistanceKm ?? double.MaxValue).ThenByDescending(x => x.Rating).ToList(),
            "fastest" => pharmacies.OrderBy(x => x.DeliveryMinutes).ThenByDescending(x => x.Rating).ToList(),
            "rating" => pharmacies.OrderByDescending(x => x.Rating).ThenBy(x => x.DeliveryMinutes).ToList(),
            "deliveryfee" => pharmacies.OrderBy(x => x.DeliveryFee).ThenBy(x => x.DeliveryMinutes).ToList(),
            _ => pharmacies.OrderByDescending(x => x.IsOpen).ThenByDescending(x => x.Rating).ThenBy(x => x.DeliveryMinutes).ToList()
        };

        var offersQuery = _db.PharmacyProducts.AsNoTracking()
            .Where(x => x.Pharmacy.IsActive && x.Pharmacy.IsVerified && x.Pharmacy.City == city && x.IsAvailable && x.Stock > 0 && x.Product.IsActive)
            .Where(x => q == null ||
                        x.Product.ProductName.Contains(q) ||
                        x.Product.Description.Contains(q) ||
                        x.Product.Category.CategoryName.Contains(q) ||
                        (x.Product.Manufacturer != null && x.Product.Manufacturer.Contains(q)) ||
                        (x.Product.SKU != null && x.Product.SKU.Contains(q)) ||
                        (x.Product.Barcode != null && x.Product.Barcode.Contains(q)) ||
                        x.Pharmacy.Name.Contains(q));

        offersQuery = sort switch
        {
            "fastest" => offersQuery.OrderBy(x => x.Pharmacy.EstimatedDeliveryMinutes).ThenBy(x => x.Price),
            "rating" => offersQuery.OrderByDescending(x => x.Pharmacy.Rating).ThenBy(x => x.Price),
            "deliveryfee" => offersQuery.OrderBy(x => x.Pharmacy.DeliveryFee).ThenBy(x => x.Price),
            "cheapest" => offersQuery.OrderBy(x => x.Price).ThenBy(x => x.Pharmacy.EstimatedDeliveryMinutes),
            _ => offersQuery.OrderByDescending(x => q != null && x.Product.ProductName.Contains(q)).ThenByDescending(x => x.IsFeatured).ThenBy(x => x.Price)
        };

        var rawOffers = await offersQuery.Take(q == null ? 80 : 60)
            .Select(x => new MarketplaceOfferViewModel
            {
                PharmacyId=x.PharmacyId, PharmacyProductId=x.PharmacyProductId, ProductId=x.ProductId,
                PharmacyName=x.Pharmacy.Name, ProductName=x.Product.ProductName, CategoryName=x.Product.Category.CategoryName,
                ImageUrl=x.Product.ImageUrl, Price=x.Price, CompareAtPrice=x.CompareAtPrice, Stock=x.Stock,
                RequiresPrescription=x.Product.RequiresPrescription, DeliveryMinutes=x.Pharmacy.EstimatedDeliveryMinutes,
                DeliveryFee=x.Pharmacy.DeliveryFee, Rating=x.Pharmacy.Rating
            }).ToListAsync(ct);

        var offers = q == null
            ? rawOffers.GroupBy(x => x.ProductId).Select(g => g.OrderBy(x => x.Price).ThenBy(x => x.DeliveryMinutes).First()).Take(16).ToList()
            : rawOffers;

        return View(new MarketplaceHomeViewModel
        {
            City = city,
            Query = q,
            Sort = sort,
            Cities = cities,
            Latitude = lat,
            Longitude = lng,
            Pharmacies = pharmacies,
            PopularOffers = offers,
            Categories = await _db.Category.AsNoTracking().OrderBy(c => c.CategoryName).ToListAsync(ct)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Pharmacy(int id, string? q, int? categoryId, CancellationToken ct)
    {
        var pharmacy = await _db.Pharmacies.AsNoTracking().Include(p => p.Hours).Include(p => p.DeliveryZones)
            .FirstOrDefaultAsync(p => p.PharmacyId == id && p.IsActive && p.IsVerified, ct);
        if (pharmacy == null) return NotFound();

        var offersQuery = _db.PharmacyProducts.AsNoTracking()
            .Where(x => x.PharmacyId == id && x.IsAvailable && x.Stock > 0 && x.Product.IsActive)
            .Where(x => string.IsNullOrWhiteSpace(q) ||
                        x.Product.ProductName.Contains(q) ||
                        x.Product.Description.Contains(q) ||
                        (x.Product.Manufacturer != null && x.Product.Manufacturer.Contains(q)))
            .Where(x => !categoryId.HasValue || x.Product.CategoryID == categoryId.Value);

        var offers = await offersQuery.OrderByDescending(x => x.IsFeatured).ThenBy(x => x.Product.ProductName)
            .Select(x => new MarketplaceOfferViewModel
            {
                PharmacyId=x.PharmacyId, PharmacyProductId=x.PharmacyProductId, ProductId=x.ProductId,
                PharmacyName=x.Pharmacy.Name, ProductName=x.Product.ProductName, CategoryName=x.Product.Category.CategoryName,
                ImageUrl=x.Product.ImageUrl, Price=x.Price, CompareAtPrice=x.CompareAtPrice, Stock=x.Stock,
                RequiresPrescription=x.Product.RequiresPrescription, DeliveryMinutes=x.Pharmacy.EstimatedDeliveryMinutes,
                DeliveryFee=x.Pharmacy.DeliveryFee, Rating=x.Pharmacy.Rating
            }).ToListAsync(ct);

        return View(new PharmacyStoreViewModel
        {
            Pharmacy=pharmacy, Offers=offers, Query=q, CategoryId=categoryId,
            Categories=await _db.Category.AsNoTracking().OrderBy(c => c.CategoryName).ToListAsync(ct)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Compare(int id, string sort = "cheapest", CancellationToken ct = default)
    {
        var product = await _db.Product.AsNoTracking().Include(p => p.Category).Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive, ct);
        if (product == null) return NotFound();

        var query = _db.PharmacyProducts.AsNoTracking()
            .Where(x => x.ProductId == id && x.IsAvailable && x.Stock > 0 && x.Pharmacy.IsActive && x.Pharmacy.IsVerified);

        query = sort.ToLowerInvariant() switch
        {
            "fastest" => query.OrderBy(x => x.Pharmacy.EstimatedDeliveryMinutes).ThenBy(x => x.Price),
            "rating" => query.OrderByDescending(x => x.Pharmacy.Rating).ThenBy(x => x.Price),
            "deliveryfee" => query.OrderBy(x => x.Pharmacy.DeliveryFee).ThenBy(x => x.Price),
            _ => query.OrderBy(x => x.Price).ThenBy(x => x.Pharmacy.EstimatedDeliveryMinutes)
        };

        var offers = await query.Select(x => new MarketplaceOfferViewModel
        {
            PharmacyId=x.PharmacyId, PharmacyProductId=x.PharmacyProductId, ProductId=x.ProductId,
            PharmacyName=x.Pharmacy.Name, ProductName=x.Product.ProductName, CategoryName=x.Product.Category.CategoryName,
            ImageUrl=x.Product.ImageUrl, Price=x.Price, CompareAtPrice=x.CompareAtPrice, Stock=x.Stock,
            RequiresPrescription=x.Product.RequiresPrescription, DeliveryMinutes=x.Pharmacy.EstimatedDeliveryMinutes,
            DeliveryFee=x.Pharmacy.DeliveryFee, Rating=x.Pharmacy.Rating
        }).ToListAsync(ct);

        return View(new ProductCompareViewModel { Product=product, Offers=offers });
    }
    private static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double radius = 6371d;
        static double Rad(double value) => value * Math.PI / 180d;
        var dLat = Rad(lat2 - lat1);
        var dLon = Rad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(Rad(lat1)) * Math.Cos(Rad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return Math.Round(radius * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)), 2);
    }
}
