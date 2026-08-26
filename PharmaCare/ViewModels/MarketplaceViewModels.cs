namespace PharmaCare.ViewModels;

public sealed class MarketplaceHomeViewModel
{
    public string City { get; set; } = "Amman";
    public string? Query { get; set; }
    public string Sort { get; set; } = "recommended";
    public List<PharmacyCardViewModel> Pharmacies { get; set; } = new();
    public List<MarketplaceOfferViewModel> PopularOffers { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
}

public sealed class PharmacyCardViewModel
{
    public int PharmacyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public decimal Rating { get; set; }
    public int RatingCount { get; set; }
    public int DeliveryMinutes { get; set; }
    public decimal DeliveryFee { get; set; }
    public bool IsOpen { get; set; }
    public int AvailableProducts { get; set; }
    public decimal? StartingPrice { get; set; }
}

public sealed class MarketplaceOfferViewModel
{
    public int PharmacyId { get; set; }
    public int PharmacyProductId { get; set; }
    public int ProductId { get; set; }
    public string PharmacyName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public int Stock { get; set; }
    public bool RequiresPrescription { get; set; }
    public int DeliveryMinutes { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Rating { get; set; }
}

public sealed class PharmacyStoreViewModel
{
    public Pharmacy Pharmacy { get; set; } = null!;
    public List<MarketplaceOfferViewModel> Offers { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public string? Query { get; set; }
    public int? CategoryId { get; set; }
}

public sealed class ProductCompareViewModel
{
    public Product Product { get; set; } = null!;
    public List<MarketplaceOfferViewModel> Offers { get; set; } = new();
}
