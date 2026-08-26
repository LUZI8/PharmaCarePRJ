namespace PharmaCare.ViewModels;

public sealed class MarketplaceOrderDetailsViewModel
{
    public MarketplaceOrder Order { get; set; } = null!;
    public List<MarketplaceOrderStatusHistory> History { get; set; } = new();
}
