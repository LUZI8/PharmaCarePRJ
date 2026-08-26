namespace PharmaCare.ViewModels;

public sealed class PharmacyPortalViewModel
{
    public Pharmacy Pharmacy { get; set; } = null!;
    public List<MarketplaceOrder> Orders { get; set; } = new();
    public List<MarketplacePrescriptionRequest> PrescriptionRequests { get; set; } = new();
    public List<PharmacyProduct> LowStock { get; set; } = new();
    public int PendingOrders { get; set; }
    public int PreparingOrders { get; set; }
    public int PendingPrescriptionRequests { get; set; }
    public decimal RevenueToday { get; set; }
    public int ActiveProducts { get; set; }
}
