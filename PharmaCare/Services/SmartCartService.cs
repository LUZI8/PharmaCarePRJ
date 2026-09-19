using Microsoft.EntityFrameworkCore;

namespace PharmaCare.Services;

public interface ISmartCartService
{
    Task<SmartCartResult> RecommendAsync(IReadOnlyCollection<SmartCartItemRequest> items, string city, CancellationToken ct = default);
}

public sealed class SmartCartService : ISmartCartService
{
    private readonly DataDbContext _db;
    public SmartCartService(DataDbContext db) => _db = db;

    public async Task<SmartCartResult> RecommendAsync(IReadOnlyCollection<SmartCartItemRequest> items, string city, CancellationToken ct = default)
    {
        var normalized = items.Where(x => x.ProductId > 0 && x.Quantity > 0)
            .GroupBy(x => x.ProductId)
            .Select(g => new SmartCartItemRequest { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToList();

        if (normalized.Count == 0) return new SmartCartResult();
        city = string.IsNullOrWhiteSpace(city) ? "Amman" : city.Trim();
        var ids = normalized.Select(x => x.ProductId).ToList();

        var offers = await _db.PharmacyProducts.AsNoTracking()
            .Where(x => ids.Contains(x.ProductId) && x.Pharmacy.IsActive && x.Pharmacy.IsVerified && x.Pharmacy.City == city && x.IsAvailable && x.Stock > 0)
            .Select(x => new
            {
                x.PharmacyId, x.ProductId, x.Price, x.Stock,
                PharmacyName = x.Pharmacy.Name,
                x.Pharmacy.DeliveryFee,
                x.Pharmacy.EstimatedDeliveryMinutes,
                x.Pharmacy.Rating
            }).ToListAsync(ct);

        var pharmacies = offers.GroupBy(x => new { x.PharmacyId, x.PharmacyName, x.DeliveryFee, x.EstimatedDeliveryMinutes, x.Rating });
        var matches = new List<SmartCartPharmacyMatch>();

        foreach (var pharmacy in pharmacies)
        {
            var lines = new List<SmartCartMatchedLine>();
            foreach (var requested in normalized)
            {
                var offer = pharmacy.FirstOrDefault(x => x.ProductId == requested.ProductId && x.Stock >= requested.Quantity);
                if (offer == null) continue;
                lines.Add(new SmartCartMatchedLine
                {
                    ProductId = requested.ProductId,
                    Quantity = requested.Quantity,
                    UnitPrice = offer.Price,
                    LineTotal = offer.Price * requested.Quantity
                });
            }

            if (lines.Count == 0) continue;
            var subtotal = lines.Sum(x => x.LineTotal);
            matches.Add(new SmartCartPharmacyMatch
            {
                PharmacyId = pharmacy.Key.PharmacyId,
                PharmacyName = pharmacy.Key.PharmacyName,
                MatchedProducts = lines.Count,
                RequestedProducts = normalized.Count,
                CoversAllProducts = lines.Count == normalized.Count,
                Subtotal = subtotal,
                DeliveryFee = pharmacy.Key.DeliveryFee,
                EstimatedTotal = subtotal + pharmacy.Key.DeliveryFee,
                EstimatedDeliveryMinutes = pharmacy.Key.EstimatedDeliveryMinutes,
                Rating = pharmacy.Key.Rating,
                Lines = lines
            });
        }

        matches = matches
            .OrderByDescending(x => x.CoversAllProducts)
            .ThenByDescending(x => x.MatchedProducts)
            .ThenBy(x => x.EstimatedTotal)
            .ThenBy(x => x.EstimatedDeliveryMinutes)
            .ToList();

        var result = new SmartCartResult { Matches = matches, RequestedProducts = normalized.Count };
        result.BestSinglePharmacy = matches.FirstOrDefault(x => x.CoversAllProducts);
        if (result.BestSinglePharmacy == null)
        {
            var uncovered = normalized.Select(x => x.ProductId).ToHashSet();
            var combination = new List<SmartCartPharmacyMatch>();
            foreach (var match in matches.OrderByDescending(x => x.MatchedProducts).ThenBy(x => x.EstimatedTotal))
            {
                if (!match.Lines.Any(x => uncovered.Contains(x.ProductId))) continue;
                combination.Add(match);
                foreach (var line in match.Lines) uncovered.Remove(line.ProductId);
                if (uncovered.Count == 0) break;
            }
            result.SuggestedCombination = combination;
            result.AllProductsCanBeCovered = uncovered.Count == 0;
        }
        else
        {
            result.AllProductsCanBeCovered = true;
        }

        return result;
    }
}

public sealed class SmartCartItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
}

public sealed class SmartCartResult
{
    public int RequestedProducts { get; set; }
    public bool AllProductsCanBeCovered { get; set; }
    public SmartCartPharmacyMatch? BestSinglePharmacy { get; set; }
    public List<SmartCartPharmacyMatch> SuggestedCombination { get; set; } = new();
    public List<SmartCartPharmacyMatch> Matches { get; set; } = new();
}

public sealed class SmartCartPharmacyMatch
{
    public int PharmacyId { get; set; }
    public string PharmacyName { get; set; } = string.Empty;
    public int MatchedProducts { get; set; }
    public int RequestedProducts { get; set; }
    public bool CoversAllProducts { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal EstimatedTotal { get; set; }
    public int EstimatedDeliveryMinutes { get; set; }
    public decimal Rating { get; set; }
    public List<SmartCartMatchedLine> Lines { get; set; } = new();
}

public sealed class SmartCartMatchedLine
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
