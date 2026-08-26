using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PharmaCare.Controllers;

public class MarketplaceController : Controller
{
    private readonly DataDbContext _db;
    public MarketplaceController(DataDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? city, CancellationToken ct)
    {
        city = string.IsNullOrWhiteSpace(city) ? "Amman" : city.Trim();
        q = string.IsNullOrWhiteSpace(q) ? null : q.Trim();

        var pharmacyQuery = _db.Pharmacies.AsNoTracking().Where(p => p.IsActive && p.IsVerified && p.City == city);
        var pharmacies = await pharmacyQuery
            .OrderByDescending(p => p.IsOpen).ThenByDescending(p => p.Rating).ThenBy(p => p.EstimatedDeliveryMinutes)
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
                StartingPrice = p.Products.Where(x => x.IsAvailable && x.Stock > 0).Select(x => (decimal?)x.Price).Min()
            }).ToListAsync(ct);

        var offersQuery = _db.PharmacyProducts.AsNoTracking()
            .Where(x => x.Pharmacy.IsActive && x.Pharmacy.IsVerified && x.Pharmacy.City == city && x.IsAvailable && x.Stock > 0 && x.Product.IsActive)
            .Where(x => q == null || x.Product.ProductName.Contains(q) || x.Product.Description.Contains(q) || x.Product.Category.CategoryName.Contains(q));

        var offers = await offersQuery
            .OrderByDescending(x => q != null && x.Product.ProductName.Contains(q))
            .ThenByDescending(x => x.IsFeatured)
            .ThenBy(x => x.Price)
            .Take(q == null ? 12 : 40)
            .Select(x => new MarketplaceOfferViewModel
            {
                PharmacyId=x.PharmacyId, PharmacyProductId=x.PharmacyProductId, ProductId=x.ProductId,
                PharmacyName=x.Pharmacy.Name, ProductName=x.Product.ProductName, CategoryName=x.Product.Category.CategoryName,
                ImageUrl=x.Product.ImageUrl, Price=x.Price, CompareAtPrice=x.CompareAtPrice, Stock=x.Stock,
                RequiresPrescription=x.Product.RequiresPrescription, DeliveryMinutes=x.Pharmacy.EstimatedDeliveryMinutes,
                DeliveryFee=x.Pharmacy.DeliveryFee
            }).ToListAsync(ct);

        var model = new MarketplaceHomeViewModel
        {
            City = city,
            Query = q,
            Pharmacies = pharmacies,
            PopularOffers = offers,
            Categories = await _db.Category.AsNoTracking().OrderBy(c => c.CategoryName).ToListAsync(ct)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Pharmacy(int id, string? q, int? categoryId, CancellationToken ct)
    {
        var pharmacy = await _db.Pharmacies.AsNoTracking().Include(p => p.Hours).Include(p => p.DeliveryZones)
            .FirstOrDefaultAsync(p => p.PharmacyId == id && p.IsActive && p.IsVerified, ct);
        if (pharmacy == null) return NotFound();

        var offersQuery = _db.PharmacyProducts.AsNoTracking()
            .Where(x => x.PharmacyId == id && x.IsAvailable && x.Stock > 0 && x.Product.IsActive)
            .Where(x => string.IsNullOrWhiteSpace(q) || x.Product.ProductName.Contains(q) || x.Product.Description.Contains(q))
            .Where(x => !categoryId.HasValue || x.Product.CategoryID == categoryId.Value);

        var offers = await offersQuery.OrderByDescending(x => x.IsFeatured).ThenBy(x => x.Product.ProductName)
            .Select(x => new MarketplaceOfferViewModel
            {
                PharmacyId=x.PharmacyId, PharmacyProductId=x.PharmacyProductId, ProductId=x.ProductId,
                PharmacyName=x.Pharmacy.Name, ProductName=x.Product.ProductName, CategoryName=x.Product.Category.CategoryName,
                ImageUrl=x.Product.ImageUrl, Price=x.Price, CompareAtPrice=x.CompareAtPrice, Stock=x.Stock,
                RequiresPrescription=x.Product.RequiresPrescription, DeliveryMinutes=x.Pharmacy.EstimatedDeliveryMinutes,
                DeliveryFee=x.Pharmacy.DeliveryFee
            }).ToListAsync(ct);

        return View(new PharmacyStoreViewModel
        {
            Pharmacy=pharmacy, Offers=offers, Query=q, CategoryId=categoryId,
            Categories=await _db.Category.AsNoTracking().OrderBy(c => c.CategoryName).ToListAsync(ct)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Compare(int id, CancellationToken ct)
    {
        var product = await _db.Product.AsNoTracking().Include(p => p.Category).Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive, ct);
        if (product == null) return NotFound();

        var offers = await _db.PharmacyProducts.AsNoTracking()
            .Where(x => x.ProductId == id && x.IsAvailable && x.Stock > 0 && x.Pharmacy.IsActive && x.Pharmacy.IsVerified)
            .OrderBy(x => x.Price).ThenBy(x => x.Pharmacy.EstimatedDeliveryMinutes)
            .Select(x => new MarketplaceOfferViewModel
            {
                PharmacyId=x.PharmacyId, PharmacyProductId=x.PharmacyProductId, ProductId=x.ProductId,
                PharmacyName=x.Pharmacy.Name, ProductName=x.Product.ProductName, CategoryName=x.Product.Category.CategoryName,
                ImageUrl=x.Product.ImageUrl, Price=x.Price, CompareAtPrice=x.CompareAtPrice, Stock=x.Stock,
                RequiresPrescription=x.Product.RequiresPrescription, DeliveryMinutes=x.Pharmacy.EstimatedDeliveryMinutes,
                DeliveryFee=x.Pharmacy.DeliveryFee
            }).ToListAsync(ct);

        return View(new ProductCompareViewModel { Product=product, Offers=offers });
    }
}
