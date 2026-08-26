using Microsoft.EntityFrameworkCore;
using PharmaCare.Models;

namespace PharmaCare.Data;

/// <summary>
/// Refreshes the visible development catalog with a larger demo pharmacy catalog.
/// Historical products are archived instead of deleted so order/reservation history stays valid.
/// Each seeded product receives a four-image storefront gallery (one primary + three alternates).
/// </summary>
public static class DemoCatalogSeeder
{
    // Bump this marker whenever the demo catalog shape changes so an existing dev database refreshes once.
    private const string SeedMarkerSku = "PC-DEMO-037";

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
            "Vitamins & Supplements", "Skin Care", "First Aid", "Antibiotics", "Respiratory",
            "Cardiovascular", "Diabetes", "Cholesterol"
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

        var products = new[]
        {
            New("Panadol Advance 500mg", "Pain Relief", 3.25m, 160, "Haleon", false,
                "Paracetamol tablets for everyday short-term relief of common mild to moderate pain and fever. This storefront description is intended to help customers identify the product and package; always follow the package instructions and pharmacist guidance."),
            New("Panadol Extra 500mg", "Pain Relief", 4.10m, 130, "Haleon", false,
                "Paracetamol with caffeine presented as an everyday pharmacy option for short-term relief of common aches and headaches. Customers should review the package information before use and ask the pharmacy team if they have questions."),
            New("Panadol ActiFast 500mg", "Pain Relief", 4.35m, 105, "Haleon", false,
                "Fast-release paracetamol caplets designed for temporary pain and fever relief. Product information, stock and expiry are managed by the pharmacy and shown here for convenient online ordering."),
            New("Nurofen 200mg Tablets", "Pain Relief", 4.75m, 88, "Reckitt", false,
                "Ibuprofen tablets for short-term relief of pain and inflammation. Use only as directed on the product packaging and consult a pharmacist if you are unsure whether this medicine is suitable for you."),
            New("Voltaren Emulgel 1% 50g", "Pain Relief", 6.90m, 62, "Haleon", false,
                "Topical diclofenac gel supplied for localized muscle and joint discomfort. The product page is for identification, stock and ordering purposes and does not replace professional medical advice."),

            New("Panadol Cold & Flu Day", "Cold & Flu", 5.40m, 94, "Haleon", false,
                "Daytime cold and flu symptom relief tablets. The package and pharmacist instructions should be followed carefully, especially when using other products that may contain overlapping active ingredients."),
            New("Panadol Cold & Flu All in One", "Cold & Flu", 6.15m, 76, "Haleon", false,
                "Multi-symptom cold and flu relief product for short-term use. Check the full package information before use and contact the pharmacy team if you have questions about ingredients or suitability."),
            New("Strepsils Honey & Lemon", "Cold & Flu", 3.60m, 115, "Reckitt", false,
                "Honey and lemon lozenges for temporary relief of sore-throat discomfort. Conveniently listed with live stock, price and expiry information for in-store pharmacy fulfillment."),
            New("Otrivin Adult Nasal Spray 0.1%", "Cold & Flu", 4.90m, 58, "Haleon", false,
                "Adult nasal decongestant spray for short-term relief of blocked-nose symptoms. Use according to the package directions and avoid prolonged use unless advised by a healthcare professional."),

            New("Zyrtec 10mg Tablets", "Allergy", 5.25m, 98, "UCB", false,
                "Cetirizine antihistamine tablets commonly used for allergy symptom relief. This page provides product identification and pharmacy inventory details; follow the label and pharmacist advice."),
            New("Clarityn 10mg Tablets", "Allergy", 5.80m, 72, "Bayer", false,
                "Loratadine antihistamine tablets for common seasonal allergy symptoms. Review the official pack information before use and speak with the pharmacy team for product-specific questions."),
            New("Telfast 120mg Tablets", "Allergy", 7.30m, 55, "Sanofi", false,
                "Fexofenadine antihistamine tablets presented as part of the allergy-care range. Stock, price and expiry data are maintained by PharmaCare for convenient ordering."),

            New("Gaviscon Double Action 300ml", "Digestive Health", 7.25m, 66, "Reckitt", false,
                "Liquid antacid and alginate formulation for occasional heartburn and indigestion relief. Follow the package instructions and consult a pharmacist if symptoms persist or frequently return."),
            New("Rennie Peppermint Tablets", "Digestive Health", 4.20m, 81, "Bayer", false,
                "Chewable peppermint antacid tablets for occasional heartburn and indigestion. The listing includes live pharmacy stock and expiry visibility for easy shopping."),
            New("Dulcolax 5mg Tablets", "Digestive Health", 4.55m, 64, "Sanofi", false,
                "Short-term stimulant laxative tablets for occasional constipation. Use only as directed on the packaging and ask a pharmacist if you need help choosing an appropriate product."),

            New("Centrum Adults Multivitamin", "Vitamins & Supplements", 11.90m, 70, "Haleon", false,
                "Daily multivitamin and mineral supplement for adults. The storefront shows current stock and product details to support convenient pharmacy purchasing."),
            New("Vitamin D3 1000 IU", "Vitamins & Supplements", 7.50m, 92, "PharmaCare Select", false,
                "Vitamin D3 dietary supplement in convenient daily tablets. Follow the package directions and professional advice, particularly if you already take other supplements."),
            New("Vitamin C 1000mg Effervescent", "Vitamins & Supplements", 6.20m, 84, "PharmaCare Select", false,
                "Effervescent vitamin C supplement with a citrus flavor. Product price, stock and expiry information are displayed for convenient pharmacy ordering."),

            New("Bepanthen Cream 30g", "Skin Care", 5.60m, 75, "Bayer", false,
                "Dexpanthenol moisturizing cream for dry or irritated skin care. Review the package information before use and ask the pharmacist if irritation is persistent or severe."),
            New("Sudocrem Antiseptic Cream 125g", "Skin Care", 7.95m, 49, "Teva", false,
                "Protective skin-care cream for minor skin irritation. The product page provides customer-facing identification, stock and pricing information."),
            New("Savlon Antiseptic Cream 30g", "First Aid", 4.15m, 69, "Haleon", false,
                "Antiseptic cream intended for minor cuts, grazes and superficial skin injuries. Follow the package directions and seek professional advice for deeper or serious wounds."),

            // Prescription catalog. These listings intentionally avoid dosage/treatment instructions;
            // PharmaCare's flow requires a valid prescription and pharmacy verification before pickup.
            New("Amoxicillin 500mg Capsules", "Antibiotics", 8.50m, 44, "PharmaCare Rx", true,
                "Prescription-only amoxicillin capsule listing for customers who already have a valid clinician-issued prescription. The medicine is reserved online, then verified by the pharmacy before pickup and payment. The storefront does not recommend antibiotics or provide treatment/dose decisions.", RxNote()),
            New("Augmentin 625mg Tablets", "Antibiotics", 14.80m, 36, "GSK", true,
                "Prescription-only amoxicillin/clavulanic acid product listing. Reservation does not complete a medicine sale: the customer must bring a valid prescription and the pharmacy team verifies it before dispensing and payment.", RxNote()),
            New("Azithromycin 500mg Tablets", "Antibiotics", 12.40m, 31, "PharmaCare Rx", true,
                "Prescription-only azithromycin tablet listing for clinician-directed treatment. Customers may reserve available stock online, but dispensing occurs only after prescription verification at the pharmacy.", RxNote()),
            New("Montelukast 10mg Tablets", "Respiratory", 18.20m, 42, "PharmaCare Rx", true,
                "Prescription-only montelukast product listing for customers with an existing prescription. PharmaCare displays inventory and reservation information while all clinical decisions remain with the prescriber and pharmacist.", RxNote()),
            New("Salbutamol Inhaler 100mcg", "Respiratory", 9.75m, 47, "PharmaCare Rx", true,
                "Prescription-only reliever inhaler listing for customers with a valid prescription. The page supports product identification and reservation only; inhaler technique, suitability and treatment decisions should be confirmed with a pharmacist or clinician.", RxNote()),
            New("Budesonide/Formoterol Inhaler", "Respiratory", 24.90m, 29, "PharmaCare Rx", true,
                "Prescription-only combination inhaler listing. Customers can reserve pharmacy stock for pickup after prescription verification. The online storefront does not provide dosing, switching or treatment recommendations.", RxNote()),

            New("Metformin 500mg Tablets", "Diabetes", 6.80m, 83, "PharmaCare Rx", true,
                "Prescription-only metformin tablet listing for customers with an established prescription. Online reservation is provided for convenience; dispensing and any medicine changes require professional verification.", RxNote()),
            New("Gliclazide MR 30mg Tablets", "Diabetes", 9.60m, 54, "PharmaCare Rx", true,
                "Prescription-only gliclazide modified-release tablet listing. The page provides product, price, stock and reservation details without offering individual treatment or dose guidance.", RxNote()),
            New("Sitagliptin 100mg Tablets", "Diabetes", 21.50m, 37, "PharmaCare Rx", true,
                "Prescription-only sitagliptin tablet listing. A valid prescription is required and the pharmacy verifies the medicine before pickup and payment.", RxNote()),

            New("Amlodipine 5mg Tablets", "Cardiovascular", 5.90m, 88, "PharmaCare Rx", true,
                "Prescription-only amlodipine tablet listing for customers with a valid prescription. Product availability can be reserved online while clinical monitoring and treatment decisions remain with the prescriber.", RxNote()),
            New("Losartan 50mg Tablets", "Cardiovascular", 7.80m, 61, "PharmaCare Rx", true,
                "Prescription-only losartan tablet listing. PharmaCare supports stock reservation and pickup workflow after prescription verification; no online dose or treatment recommendations are provided.", RxNote()),
            New("Bisoprolol 5mg Tablets", "Cardiovascular", 8.40m, 46, "PharmaCare Rx", true,
                "Prescription-only bisoprolol tablet listing for pharmacy reservation. Customers should follow the directions provided by their prescriber and pharmacist and should not alter treatment based on storefront information.", RxNote()),
            New("Atorvastatin 20mg Tablets", "Cholesterol", 9.25m, 74, "PharmaCare Rx", true,
                "Prescription-only atorvastatin tablet listing for customers with an existing prescription. Live inventory and pricing are shown for reservation; treatment monitoring remains with healthcare professionals.", RxNote()),
            New("Rosuvastatin 10mg Tablets", "Cholesterol", 11.60m, 52, "PharmaCare Rx", true,
                "Prescription-only rosuvastatin tablet listing. The product can be reserved when in stock and is dispensed only after prescription verification at the pharmacy.", RxNote())
        };

        var now = DateTime.UtcNow;

        for (var index = 0; index < products.Length; index++)
        {
            var seed = products[index];
            var categoryId = categoryMap[seed.Category].CategoryID;
            var sku = $"PC-DEMO-{index + 1:000}";
            var galleryUrls = BuildGalleryUrls(seed.Name, seed.RequiresPrescription);

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
            product.ImageUrl = galleryUrls[0];
            product.IsActive = true;
            product.RequiresPrescription = seed.RequiresPrescription;
            product.PrescriptionNote = seed.PrescriptionNote;
            product.ExpiryDate = now.AddMonths(18 + (index % 15));
            product.UpdatedAt = now;

            // Replace any old demo gallery with a consistent four-image gallery.
            if (product.Images.Count > 0)
            {
                db.ProductImages.RemoveRange(product.Images);
                product.Images.Clear();
            }

            for (var imageIndex = 0; imageIndex < galleryUrls.Length; imageIndex++)
            {
                product.Images.Add(new ProductImage
                {
                    ImageUrl = galleryUrls[imageIndex],
                    DisplayOrder = imageIndex,
                    IsPrimary = imageIndex == 0,
                    CreatedAt = now.AddSeconds(imageIndex)
                });
            }
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        logger.LogInformation("Demo pharmacy catalog refreshed with {Count} active products and four-image galleries.", products.Length);
    }

    private static SeedProduct New(string name, string category, decimal price, int stock, string manufacturer,
        bool requiresPrescription, string description, string? prescriptionNote = null)
        => new(name, category, price, stock, manufacturer, requiresPrescription, description, prescriptionNote);

    private static string RxNote()
        => "Valid prescription required. Reserve online, then bring the original valid prescription for pharmacy verification before pickup and payment.";

    private static string[] BuildGalleryUrls(string productName, bool prescription)
    {
        var encodedName = Uri.EscapeDataString(productName);
        var accent = prescription ? "1F9D8A" : "0D9488";
        var badge = prescription ? "Prescription" : "Pharmacy";

        // Demo storefront artwork: clearly product-specific, four separate views, and safe to hot-link in dev.
        // Admins can replace these with real pack photography through the existing product gallery editor.
        return new[]
        {
            $"https://placehold.co/1000x800/F7FBFA/{accent}?text={encodedName}%0A{badge}%20%E2%80%A2%20Front%20Pack",
            $"https://placehold.co/1000x800/EEF8F6/{accent}?text={encodedName}%0ASide%20View",
            $"https://placehold.co/1000x800/FFFFFF/{accent}?text={encodedName}%0APackaging%20Details",
            $"https://placehold.co/1000x800/E8F5F2/{accent}?text={encodedName}%0APharmacy%20Gallery"
        };
    }

    private sealed record SeedProduct(string Name, string Category, decimal Price, int Stock,
        string Manufacturer, bool RequiresPrescription, string Description, string? PrescriptionNote);
}
