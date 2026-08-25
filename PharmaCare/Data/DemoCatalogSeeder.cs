using Microsoft.EntityFrameworkCore;
using PharmaCare.Models;

namespace PharmaCare.Data;

/// <summary>
/// Replaces the visible development catalog with a larger, polished demo catalog.
/// Existing products that are referenced by historical orders/reservations are archived
/// instead of being physically deleted so order history remains valid.
/// </summary>
public static class DemoCatalogSeeder
{
    private const string SeedMarkerSku = "PC-DEMO-001";

    public static async Task SeedAsync(DataDbContext db, ILogger logger)
    {
        // Idempotent: once this catalog exists, do not recreate it on every startup.
        if (await db.Product.AnyAsync(p => p.SKU == SeedMarkerSku))
            return;

        await using var transaction = await db.Database.BeginTransactionAsync();

        var existingIds = await db.Product.Select(p => p.ProductId).ToListAsync();
        if (existingIds.Count > 0)
        {
            // Old cart lines should not keep stale catalog items around.
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
                    // Keep relational history intact, but remove the old medicine from storefront/admin active catalog.
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
            New("Panadol Advance 500mg", "Pain Relief", 3.25m, 160, "Haleon", false, "Paracetamol tablets for everyday pain and fever relief.", 1),
            New("Panadol Extra 500mg", "Pain Relief", 4.10m, 130, "Haleon", false, "Paracetamol with caffeine for short-term relief of common aches and headaches.", 2),
            New("Panadol ActiFast 500mg", "Pain Relief", 4.35m, 105, "Haleon", false, "Fast-release paracetamol caplets for temporary pain and fever relief.", 3),
            New("Nurofen 200mg Tablets", "Pain Relief", 4.75m, 88, "Reckitt", false, "Ibuprofen tablets for short-term relief of pain and inflammation.", 4),
            New("Voltaren Emulgel 1% 50g", "Pain Relief", 6.90m, 62, "Haleon", false, "Topical diclofenac gel for localized muscle and joint pain relief.", 5),

            New("Panadol Cold & Flu Day", "Cold & Flu", 5.40m, 94, "Haleon", false, "Daytime cold and flu symptom relief tablets.", 6),
            New("Panadol Cold & Flu All in One", "Cold & Flu", 6.15m, 76, "Haleon", false, "Multi-symptom cold and flu relief product for short-term use.", 7),
            New("Strepsils Honey & Lemon", "Cold & Flu", 3.60m, 115, "Reckitt", false, "Soothing lozenges for temporary relief of sore throat discomfort.", 8),
            New("Otrivin Adult Nasal Spray 0.1%", "Cold & Flu", 4.90m, 58, "Haleon", false, "Short-term nasal decongestant spray for blocked nose symptoms.", 9),

            New("Zyrtec 10mg Tablets", "Allergy", 5.25m, 98, "UCB", false, "Cetirizine antihistamine tablets for common allergy symptoms.", 10),
            New("Clarityn 10mg Tablets", "Allergy", 5.80m, 72, "Bayer", false, "Loratadine antihistamine tablets for seasonal allergy symptoms.", 11),
            New("Telfast 120mg Tablets", "Allergy", 7.30m, 55, "Sanofi", false, "Fexofenadine antihistamine tablets for allergy symptom relief.", 12),

            New("Gaviscon Double Action 300ml", "Digestive Health", 7.25m, 66, "Reckitt", false, "Liquid antacid and alginate formula for heartburn and indigestion relief.", 13),
            New("Rennie Peppermint Tablets", "Digestive Health", 4.20m, 81, "Bayer", false, "Chewable antacid tablets for occasional heartburn and indigestion.", 14),
            New("Dulcolax 5mg Tablets", "Digestive Health", 4.55m, 64, "Sanofi", false, "Short-term stimulant laxative tablets for occasional constipation.", 15),

            New("Centrum Adults Multivitamin", "Vitamins & Supplements", 11.90m, 70, "Haleon", false, "Daily multivitamin and mineral supplement for adults.", 16),
            New("Vitamin D3 1000 IU", "Vitamins & Supplements", 7.50m, 92, "PharmaCare Select", false, "Vitamin D3 dietary supplement in convenient daily tablets.", 17),
            New("Vitamin C 1000mg Effervescent", "Vitamins & Supplements", 6.20m, 84, "PharmaCare Select", false, "Effervescent vitamin C supplement with a citrus flavor.", 18),

            New("Bepanthen Cream 30g", "Skin Care", 5.60m, 75, "Bayer", false, "Moisturizing dexpanthenol cream for dry or irritated skin care.", 19),
            New("Sudocrem Antiseptic Cream 125g", "Skin Care", 7.95m, 49, "Teva", false, "Protective skin-care cream for minor skin irritation.", 20),
            New("Savlon Antiseptic Cream 30g", "First Aid", 4.15m, 69, "Haleon", false, "Antiseptic cream for minor cuts, grazes and superficial skin injuries.", 21),

            New("Amoxicillin 500mg Capsules", "Antibiotics", 8.50m, 44, "PharmaCare Rx", true, "Prescription antibiotic. Dispensed only after a valid prescription is verified.", 22, "Valid prescription required. Complete payment at the pharmacy after verification."),
            New("Augmentin 625mg Tablets", "Antibiotics", 14.80m, 36, "GSK", true, "Prescription antibiotic containing amoxicillin and clavulanic acid.", 23, "Valid prescription required. Complete payment at the pharmacy after verification."),
            New("Azithromycin 500mg Tablets", "Antibiotics", 12.40m, 31, "PharmaCare Rx", true, "Prescription macrolide antibiotic for clinician-directed treatment.", 24, "Valid prescription required. Complete payment at the pharmacy after verification."),
            New("Montelukast 10mg Tablets", "Respiratory", 18.20m, 42, "PharmaCare Rx", true, "Prescription leukotriene receptor antagonist for clinician-directed respiratory/allergy management.", 25, "Valid prescription required. Complete payment at the pharmacy after verification.")
        };

        for (var index = 0; index < products.Length; index++)
        {
            var seed = products[index];
            var sku = $"PC-DEMO-{index + 1:000}";
            var imageUrl = BuildImageUrl(seed.Name);

            var product = new Product
            {
                ProductName = seed.Name,
                CategoryID = categoryMap[seed.Category].CategoryID,
                Description = seed.Description,
                Price = seed.Price,
                Stock = seed.Stock,
                SKU = sku,
                Barcode = $"625100{index + 1:000000}",
                Manufacturer = seed.Manufacturer,
                ReorderLevel = Math.Max(8, seed.Stock / 6),
                ImageUrl = imageUrl,
                IsActive = true,
                RequiresPrescription = seed.RequiresPrescription,
                PrescriptionNote = seed.PrescriptionNote,
                ExpiryDate = now.AddMonths(18 + (index % 15)),
                CreatedAt = now.AddMinutes(index)
            };

            product.Images.Add(new ProductImage
            {
                ImageUrl = imageUrl,
                DisplayOrder = 0,
                IsPrimary = true,
                CreatedAt = now
            });

            db.Product.Add(product);
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        logger.LogInformation("Demo pharmacy catalog refreshed with {Count} active products.", products.Length);
    }

    private static SeedProduct New(
        string name,
        string category,
        decimal price,
        int stock,
        string manufacturer,
        bool requiresPrescription,
        string description,
        int imageNumber,
        string? prescriptionNote = null)
        => new(name, category, price, stock, manufacturer, requiresPrescription, description, prescriptionNote);

    private static string BuildImageUrl(string productName)
    {
        var text = Uri.EscapeDataString(productName);
        // Reliable temporary storefront artwork; admins can replace any item with real gallery photos later.
        return $"https://placehold.co/900x700/F7FBFA/0D9488?text={text}";
    }

    private sealed record SeedProduct(
        string Name,
        string Category,
        decimal Price,
        int Stock,
        string Manufacturer,
        bool RequiresPrescription,
        string Description,
        string? PrescriptionNote);
}
