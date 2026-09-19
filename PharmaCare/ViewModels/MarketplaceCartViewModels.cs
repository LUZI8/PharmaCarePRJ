namespace PharmaCare.ViewModels;

public sealed class MarketplaceBasketState
{
    public int PharmacyId { get; set; }
    public List<MarketplaceBasketItemState> Items { get; set; } = new();
}

public sealed class MarketplaceBasketItemState
{
    public int PharmacyProductId { get; set; }
    public int Quantity { get; set; }
}

public sealed class MarketplaceBasketViewModel
{
    public Pharmacy? Pharmacy { get; set; }
    public List<MarketplaceBasketLineViewModel> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Total { get; set; }
    public bool HasPrescriptionItems => Items.Any(i => i.RequiresPrescription);
}

public sealed class MarketplaceBasketLineViewModel
{
    public int PharmacyProductId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public int AvailableStock { get; set; }
    public bool RequiresPrescription { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed class MarketplaceCheckoutViewModel
{
    public MarketplaceBasketViewModel Basket { get; set; } = new();
    [Required, MaxLength(220)] public string ShippingAddress { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string City { get; set; } = "Amman";
    [Required, MaxLength(30)] public string PhoneNumber { get; set; } = string.Empty;
    [MaxLength(500)] public string? DeliveryNotes { get; set; }
}

public sealed class MarketplaceThankYouViewModel
{
    public MarketplaceOrder Order { get; set; } = null!;
}
