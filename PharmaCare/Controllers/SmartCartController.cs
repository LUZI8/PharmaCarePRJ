using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PharmaCare.Controllers;

public class SmartCartController : Controller
{
    private readonly DataDbContext _db;
    private readonly ISmartCartService _smartCart;

    public SmartCartController(DataDbContext db, ISmartCartService smartCart)
    {
        _db = db;
        _smartCart = smartCart;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        return View(new SmartCartPageViewModel
        {
            Products = await GetProductsAsync(ct),
            Selections = new List<SmartCartSelectionViewModel>
            {
                new(), new(), new()
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SmartCartPageViewModel model, CancellationToken ct)
    {
        var items = model.Selections
            .Where(x => x.ProductId > 0 && x.Quantity > 0)
            .Select(x => new SmartCartItemRequest { ProductId = x.ProductId, Quantity = x.Quantity })
            .ToList();

        if (items.Count == 0)
            ModelState.AddModelError(string.Empty, "Choose at least one product.");
        else
            model.Result = await _smartCart.RecommendAsync(items, model.City, ct);

        model.Products = await GetProductsAsync(ct);
        while (model.Selections.Count < 5) model.Selections.Add(new SmartCartSelectionViewModel());
        return View(model);
    }

    private Task<List<Product>> GetProductsAsync(CancellationToken ct) => _db.Product.AsNoTracking()
        .Where(x => x.IsActive)
        .OrderBy(x => x.ProductName)
        .Take(250)
        .ToListAsync(ct);
}
