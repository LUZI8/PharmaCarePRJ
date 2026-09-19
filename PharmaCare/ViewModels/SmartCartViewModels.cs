namespace PharmaCare.ViewModels;

public sealed class SmartCartPageViewModel
{
    public string City { get; set; } = "Amman";
    public List<Product> Products { get; set; } = new();
    public List<SmartCartSelectionViewModel> Selections { get; set; } = new();
    public SmartCartResult? Result { get; set; }
}

public sealed class SmartCartSelectionViewModel
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
}
