namespace PharmaCare.Models;

public class MarketplaceOrderStatusHistory
{
    public int MarketplaceOrderStatusHistoryId { get; set; }
    public int MarketplaceOrderId { get; set; }
    [Required, MaxLength(40)] public string Status { get; set; } = string.Empty;
    public int? ChangedByUserId { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.Now;
    public MarketplaceOrder MarketplaceOrder { get; set; } = null!;
    public User? ChangedByUser { get; set; }
}

public class CustomerAddress
{
    public int CustomerAddressId { get; set; }
    public int UserId { get; set; }
    [Required, MaxLength(40)] public string Label { get; set; } = "Home";
    [Required, MaxLength(100)] public string City { get; set; } = "Amman";
    [MaxLength(100)] public string? Area { get; set; }
    [Required, MaxLength(180)] public string Street { get; set; } = string.Empty;
    [MaxLength(60)] public string? Building { get; set; }
    [MaxLength(30)] public string? Floor { get; set; }
    [MaxLength(30)] public string? Apartment { get; set; }
    [MaxLength(180)] public string? Landmark { get; set; }
    [MaxLength(500)] public string? DeliveryInstructions { get; set; }
    [Column(TypeName = "decimal(9,6)")] public decimal? Latitude { get; set; }
    [Column(TypeName = "decimal(9,6)")] public decimal? Longitude { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public User User { get; set; } = null!;
}

public class MarketplaceNotification
{
    public int MarketplaceNotificationId { get; set; }
    public int UserId { get; set; }
    [Required, MaxLength(80)] public string Type { get; set; } = "General";
    [Required, MaxLength(160)] public string Title { get; set; } = string.Empty;
    [Required, MaxLength(1000)] public string Message { get; set; } = string.Empty;
    [MaxLength(500)] public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public User User { get; set; } = null!;
}

public class MarketplaceAuditLog
{
    public long MarketplaceAuditLogId { get; set; }
    public int? UserId { get; set; }
    [Required, MaxLength(80)] public string Action { get; set; } = string.Empty;
    [Required, MaxLength(80)] public string EntityName { get; set; } = string.Empty;
    [MaxLength(80)] public string? EntityId { get; set; }
    [MaxLength(1000)] public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public User? User { get; set; }
}
