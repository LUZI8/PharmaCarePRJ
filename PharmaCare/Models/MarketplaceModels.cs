namespace PharmaCare.Models;

public class Pharmacy
{
    public int PharmacyId { get; set; }
    [Required, MaxLength(150)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? LogoUrl { get; set; }
    [MaxLength(220)] public string Address { get; set; } = string.Empty;
    [MaxLength(100)] public string City { get; set; } = "Amman";
    [MaxLength(30)] public string? Phone { get; set; }
    [MaxLength(180)] public string? Email { get; set; }
    [Column(TypeName = "decimal(9,6)")] public decimal? Latitude { get; set; }
    [Column(TypeName = "decimal(9,6)")] public decimal? Longitude { get; set; }
    [Column(TypeName = "decimal(4,2)")] public decimal Rating { get; set; } = 4.5m;
    public int RatingCount { get; set; }
    public int EstimatedDeliveryMinutes { get; set; } = 30;
    [Column(TypeName = "decimal(18,2)")] public decimal DeliveryFee { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal MinimumOrder { get; set; }
    public bool IsOpen { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public bool IsVerified { get; set; } = true;
    [MaxLength(1000)] public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<PharmacyProduct> Products { get; set; } = new List<PharmacyProduct>();
    public ICollection<PharmacyHour> Hours { get; set; } = new List<PharmacyHour>();
    public ICollection<PharmacyDeliveryZone> DeliveryZones { get; set; } = new List<PharmacyDeliveryZone>();
    public ICollection<PharmacyStaff> Staff { get; set; } = new List<PharmacyStaff>();
    public ICollection<MarketplaceOrder> MarketplaceOrders { get; set; } = new List<MarketplaceOrder>();
}

public class PharmacyProduct
{
    public int PharmacyProductId { get; set; }
    public int PharmacyId { get; set; }
    public int ProductId { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Price { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal? CompareAtPrice { get; set; }
    public int Stock { get; set; }
    public int ReorderLevel { get; set; } = 10;
    public bool IsAvailable { get; set; } = true;
    public bool IsFeatured { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public Pharmacy Pharmacy { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public class PharmacyHour
{
    public int PharmacyHourId { get; set; }
    public int PharmacyId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan OpensAt { get; set; }
    public TimeSpan ClosesAt { get; set; }
    public bool IsClosed { get; set; }
    public Pharmacy Pharmacy { get; set; } = null!;
}

public class PharmacyDeliveryZone
{
    public int PharmacyDeliveryZoneId { get; set; }
    public int PharmacyId { get; set; }
    [Required, MaxLength(120)] public string ZoneName { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18,2)")] public decimal DeliveryFee { get; set; }
    public int EstimatedMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;
    public Pharmacy Pharmacy { get; set; } = null!;
}

public class PharmacyStaff
{
    public int PharmacyStaffId { get; set; }
    public int PharmacyId { get; set; }
    public int UserId { get; set; }
    [Required, MaxLength(40)] public string Role { get; set; } = "Pharmacist";
    public bool IsActive { get; set; } = true;
    public Pharmacy Pharmacy { get; set; } = null!;
    public User User { get; set; } = null!;
}

public class MarketplaceOrder
{
    public int MarketplaceOrderId { get; set; }
    [Required, MaxLength(40)] public string OrderNumber { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int PharmacyId { get; set; }
    [Required, MaxLength(220)] public string ShippingAddress { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string City { get; set; } = string.Empty;
    [Required, MaxLength(30)] public string PhoneNumber { get; set; } = string.Empty;
    [MaxLength(500)] public string? DeliveryNotes { get; set; }
    [MaxLength(30)] public string PaymentMethod { get; set; } = "Cash on Delivery";
    [MaxLength(30)] public string Status { get; set; } = "Pending";
    [Column(TypeName = "decimal(18,2)")] public decimal Subtotal { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal DeliveryFee { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public DateTime? AcceptedAt { get; set; }
    public DateTime? OutForDeliveryAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public User User { get; set; } = null!;
    public Pharmacy Pharmacy { get; set; } = null!;
    public ICollection<MarketplaceOrderItem> Items { get; set; } = new List<MarketplaceOrderItem>();
}

public class MarketplaceOrderItem
{
    public int MarketplaceOrderItemId { get; set; }
    public int MarketplaceOrderId { get; set; }
    public int PharmacyProductId { get; set; }
    public int ProductId { get; set; }
    [Required, MaxLength(180)] public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal LineTotal { get; set; }
    public bool RequiresPrescription { get; set; }

    public MarketplaceOrder MarketplaceOrder { get; set; } = null!;
    public PharmacyProduct PharmacyProduct { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
