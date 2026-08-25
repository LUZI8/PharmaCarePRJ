using Microsoft.EntityFrameworkCore;
using PharmaCare.Models;

namespace PharmaCare.Data;

/// <summary>
/// Refreshes the visible development catalog with a larger demo catalog.
/// Historical products are archived instead of deleted so order/reservation history stays valid.
/// If a historical product has the same name/category as a demo item, it is reused and updated
/// instead of inserting a duplicate row that violates IX_Product_ProductName_CategoryID.
/// </summary>
public static class DemoCatalogSeeder
{
    private const string SeedMarkerSku = "PC-DEMO-001";

    public static async Task SeedAsync(DataDbContext db, ILogger logger)
    {
        if (await db.Product.AnyAsync(p => p.SKU == SeedMarkerSku))
            return;

        await using var transaction = await db.Database.BeginTransactionAsync();

        var existingIds = await db.Product.Select(p => p.ProductId).ToListAsync();
        if (existingIds.Count > 0)
        {
            var oldCartItems = await db.CartItems.Where(i => existingIds.Contains(i.ProductId)).ToListAsync();
            db.CartItems.RemoveRange(oldCartItems);

            var orderProductIds = await db.OrderItems
                .Where(i => existingIds.Contains(i.ProductId))
                .Select(i => i.ProductId)
                .Distinct()
                .ToListAsync();

            var reservationProductIds = await db.PrescriptionReservations
                .Where(r => existingIds.Contains(r.ProductId))
                .Select(r => r.ProductId)
                .Distinct()
                .ToListAsync();

            var protectedIds = orderProductIds.Concat(reservationProductIds).ToHashSet();
            var existingProducts = await db.Product.Include(p => p.Images).ToListAsync();

            foreach (var product in existingProducts)
            {
                if (protectedIds.Contains(product.ProductId))
                {
                    product.IsActive = false;
                    product.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    db.Product.Remove(product);
                }
            }

            await db.SaveChangesAsync();
        }

        var categories = new[]
        {
            "Pain Relief", "Cold & Flu", "Allergy", "Digestive Health",
            "Vitamins & Supplements", "Skin Care", "First Aid", "Antibiotics", "Respiratory"
        };

        var categoryMap = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in categories)
        {
            var category = await db.Category.FirstOrDefaultAsync(c => c.CategoryName == name);
            if (category == null)
            {
                category = new Category { CategoryName = name };
                db.Category.Add(category);
                await db.SaveChangesAsync();
            }
            categoryMap[name] = category;
        }

        var now = DateTime.UtcNow;
        var products = new[]
        {
            New("Panadol Advance 500mg", "Pain Relief", 3.25m, 160, "Haleon", false, "Paracetamol tablets for everyday pain and fever relief."),
            New("Panadol Extra 500mg", "Pain Relief", 4.10m, 130, "Haleon", false, "Paracetamol with caffeine for short-term relief of common aches and headaches."),
            New("Panadol ActiFast 500mg", "Pain Relief", 4.35m, 105, "Haleon", false, "Fast-release paracetamol caplets for temporary pain and fever relief."),
            New("Nurofen 200mg Tablets", "Pain Relief", 4.75m, 88, "Reckitt", false, "Ibuprofen tablets for short-term relief of pain and inflammation."),
            New("Voltaren Emulgel 1% 50g", "Pain Relief", 6.90m, 62, "Haleon", false, "Topical diclofenac gel for localized muscle and joint pain relief."),
            New("Panadol Cold & Flu Day", "Cold & Flu", 5.40m, 94, "Haleon", false, "Daytime cold and flu symptom relief tablets."),
            New("Panadol Cold & Flu All in One", "Cold & Flu", 6.15m, 76, "Haleon", false, "Multi-symptom cold and flu relief product for short-term use."),
            New("Strepsils Honey & Lemon", "Cold & Flu", 3.60m, 115, "Reckitt", false, "Soothing lozenges for temporary relief of sore throat discomfort."),
            New("Otrivin Adult Nasal Spray 0.1%", "Cold & Flu", 4.90m, 58, "Haleon", false, "Short-term nasal decongestant spray for blocked nose symptoms."),
            New("Zyrtec 10mg Tablets", "Allergy", 5.25m, 98, "UCB", false, "Cetirizine antihistamine tablets for common allergy symptoms."),
            New("Clarityn 10mg Tablets", "Allergy", 5.80m, 72, "Bayer", false, "Loratadine antihistamine tablets for seasonal allergy symptoms."),
            New("Telfast 120mg Tablets", "Allergy", 7.30m, 55, "Sanofi", false, "Fexofenadine antihistamine tablets for allergy symptom relief."),
            New("Gaviscon Double Action 300ml", "Digestive Health", 7.25m, 66, "Reckitt", false, "Liquid antacid and alginate formula for heartburn and indigestion relief."),
            New("Rennie Peppermint Tablets", "Digestive Health", 4.20m, 81, "Bayer", false, "Chewable antacid tablets for occasional heartburn and indigestion."),
            New("Dulcolax 5mg Tablets", "Digestive Health", 4.55m, 64, "Sanofi", false, "Short-term stimulant laxative tablets for occasional constipation."),
            New("Centrum Adults Multivitamin", "Vitamins & Supplements", 11.90m, 70, "Haleon", false, "Daily multivitamin and mineral supplement for adults."),
            New("Vitamin D3 1000 IU", "Vitamins & Supplements", 7.50m, 92, "PharmaCare Select", false, "Vitamin D3 dietary supplement in convenient daily tablets."),
            New("Vitamin C 1000mg Effervescent", "Vitamins & Supplements", 6.20m, 84, "PharmaCare Select", false, "Effervescent vitamin C supplement with a citrus flavor."),
            New("Bepanthen Cream 30g", "Skin Care", 5.60m, 75, "Bayer", false, "Moisturizing dexpanthenol cream for dry or irritated skin care."),
            New("Sudocrem Antiseptic Cream 125g", "Skin Care", 7.95m, 49, "Teva", false, "Protective skin-care cream for minor skin irritation."),
            New("Savlon Antiseptic Cream 30g", "First Aid", 4.15m, 69, "Haleon", false, "Antiseptic cream for minor cuts, grazes and superficial skin injuries."),
            New("Amoxicillin 500mg Capsules", "Antibiotics", 8.50m, 44, "PharmaCare Rx", true, "Prescription antibiotic. Dispensed only after a valid prescription is verified.", "Valid prescription required. Complete payment at the pharmacy after verification."),
            New("Augmentin 625mg Tablets", "Antibiotics", 14.80m, 36, "GSK", true, "Prescription antibiotic containing amoxicillin and clavulanic acid.", "Valid prescription required. Complete payment at the pharmacy after verification."),
            New("Azithromycin 500mg Tablets", "Antibiotics", 12.40m, 31, "PharmaCare Rx", true, "Prescription macrolide antibiotic for clinician-directed treatment.", "Valid prescription required. Complete payment at the pharmacy after verification."),
            New("Montelukast 10mg Tablets", "Respiratory", 18.20m, 42, "PharmaCare Rx", true, "Prescription leukotriene receptor antagonist for clinician-directed respiratory/allergy management.", "Valid prescription required. Complete payment at the pharmacy after verification.")
        };

        for (var index = 0; index < products.Length; index++)
        {
            var seed = products[index];
            var categoryId = categoryMap[seed.Category].CategoryID;
            var sku = $"PC-DEMO-{index + 1:000}";
            var imageUrl = BuildImageUrl(seed.Name);

            // A protected historical row may still exist with this exact name/category.
            // Reuse it so the unique index is respected and its old order/reservation links remain valid.
            var product = await db.Product
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.ProductName == seed.Name && p.CategoryID == categoryId);

            if (product == null)
            {
                product = new Product
                {
                    ProductName = seed.Name,
                    CategoryID = categoryId,
                    CreatedAt = now.AddMinutes(index)
                };
                db.Product.Add(product);
            }

            product.Description = seed.Description;
            product.Price = seed.Price;
            product.Stock = seed.Stock;
            product.SKU = sku;
            product.Barcode = $"625100{index + 1:000000}";
            product.Manufacturer = seed.Manufacturer;
            product.ReorderLevel = Math.Max(8, seed.Stock / 6);
            product.ImageUrl = imageUrl;
            product.IsActive = true;
            product.RequiresPrescription = seed.RequiresPrescription;
            product.PrescriptionNote = seed.PrescriptionNote;
            product.ExpiryDate = now.AddMonths(18 + (index % 15));
            product.UpdatedAt = now;

            if (!product.Images.Any())
            {
                product.Images.Add(new ProductImage
                {
                    ImageUrl = imageUrl,
                    DisplayOrder = 0,
                    IsPrimary = true,
                    CreatedAt = now
                });
            }
            else
            {
                var primary = product.Images.OrderBy(i => i.DisplayOrder).First();
                primary.ImageUrl = imageUrl;
                primary.DisplayOrder = 0;
                primary.IsPrimary = true;
            }
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        logger.LogInformation("Demo pharmacy catalog refreshed with {Count} active products.", products.Length);
    }

    private static SeedProduct New(string name, string category, decimal price, int stock, string manufacturer,
        bool requiresPrescription, string description, string? prescriptionNote = null)
        => new(name, category, price, stock, manufacturer, requiresPrescription, description, prescriptionNote);

    private static string BuildImageUrl(string productName)
    {
        var text = Uri.EscapeDataString(productName);
        return $"https://placehold.co/900x700/F7FBFA/0D9488?text={text}";
    }

    private sealed record SeedProduct(string Name, string Category, decimal Price, int Stock,
        string Manufacturer, bool RequiresPrescription, string Description, string? PrescriptionNote);
}
