namespace PharmaCare.ViewModels;

public sealed class MarketplaceAccountViewModel
{
    public List<CustomerAddress> Addresses { get; set; } = new();
    public List<MarketplaceNotification> Notifications { get; set; } = new();
    public int UnreadNotifications { get; set; }
}

public sealed class CustomerAddressInputModel
{
    public int? CustomerAddressId { get; set; }
    [Required, MaxLength(40)] public string Label { get; set; } = "Home";
    [Required, MaxLength(100)] public string City { get; set; } = "Amman";
    [MaxLength(100)] public string? Area { get; set; }
    [Required, MaxLength(180)] public string Street { get; set; } = string.Empty;
    [MaxLength(60)] public string? Building { get; set; }
    [MaxLength(30)] public string? Floor { get; set; }
    [MaxLength(30)] public string? Apartment { get; set; }
    [MaxLength(180)] public string? Landmark { get; set; }
    [MaxLength(500)] public string? DeliveryInstructions { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool IsDefault { get; set; }
}
