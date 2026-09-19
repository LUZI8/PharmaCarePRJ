namespace PharmaCare.Models;

public class MarketplacePrescriptionRequest
{
    public int MarketplacePrescriptionRequestId { get; set; }
    [Required, MaxLength(40)] public string RequestNumber { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int PharmacyId { get; set; }
    public int PharmacyProductId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    [Required, MaxLength(30)] public string ContactPhone { get; set; } = string.Empty;
    [MaxLength(500)] public string? CustomerNote { get; set; }
    [MaxLength(30)] public string Status { get; set; } = "Requested";
    public DateTime RequestedAt { get; set; } = DateTime.Now;
    public DateTime ExpiresAt { get; set; } = DateTime.Now.AddDays(3);
    public DateTime? ReviewedAt { get; set; }
    [MaxLength(500)] public string? StaffNote { get; set; }

    public User User { get; set; } = null!;
    public Pharmacy Pharmacy { get; set; } = null!;
    public PharmacyProduct PharmacyProduct { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
