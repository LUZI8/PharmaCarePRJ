namespace PharmaCare.ViewModels;

public sealed class MarketplacePrescriptionConfirmViewModel
{
    public PharmacyProduct Offer { get; set; } = null!;
    [Required, MaxLength(30)] public string ContactPhone { get; set; } = string.Empty;
    [Range(1, 5)] public int Quantity { get; set; } = 1;
    [MaxLength(500)] public string? CustomerNote { get; set; }
    [Required] public IFormFile? PrescriptionFile { get; set; }
}

public sealed class MarketplacePrescriptionCompleteViewModel
{
    public MarketplacePrescriptionRequest Request { get; set; } = null!;
}
