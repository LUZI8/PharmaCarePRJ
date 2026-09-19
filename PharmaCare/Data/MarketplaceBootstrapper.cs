namespace PharmaCare.Data;

public static class MarketplaceBootstrapper
{
    public static async Task EnsureAsync(DataDbContext db, ILogger logger)
    {
        // Schema is managed by EF migrations. This class seeds development marketplace data only.\n\n        if (await db.Pharmacies.AnyAsync()) return;

        var pharmacies = new[]
        {
            new Pharmacy { Name="PharmaCare Main Pharmacy", Address="Almadina Almonoara St, Amman", City="Amman", Phone="+962 7 9999 8888", Email="pharmacare@info.com", Latitude=31.986800m, Longitude=35.889200m, Rating=4.9m, RatingCount=1320, EstimatedDeliveryMinutes=22, DeliveryFee=0m, MinimumOrder=8m, Description="Fast local pharmacy fulfillment with prescription pickup and live inventory." },
            new Pharmacy { Name="Shifa Pharmacy", Address="Gardens St, Amman", City="Amman", Phone="+962 7 9000 1122", Email="orders@shifa.demo", Latitude=31.982400m, Longitude=35.899100m, Rating=4.8m, RatingCount=860, EstimatedDeliveryMinutes=28, DeliveryFee=1.50m, MinimumOrder=6m, Description="Community pharmacy with strong everyday medicine availability and fast delivery." },
            new Pharmacy { Name="LifeCare Pharmacy", Address="Mecca St, Amman", City="Amman", Phone="+962 7 9111 2233", Email="hello@lifecare.demo", Latitude=31.977300m, Longitude=35.863700m, Rating=4.7m, RatingCount=615, EstimatedDeliveryMinutes=34, DeliveryFee=2m, MinimumOrder=7m, Description="Wellness, OTC and prescription reservation service across west Amman." },
            new Pharmacy { Name="Al Hayat Pharmacy", Address="University St, Amman", City="Amman", Phone="+962 7 9222 3344", Email="care@alhayat.demo", Latitude=32.014200m, Longitude=35.873100m, Rating=4.6m, RatingCount=440, EstimatedDeliveryMinutes=31, DeliveryFee=1.25m, MinimumOrder=5m, Description="Trusted neighborhood pharmacy with extended opening hours." },
            new Pharmacy { Name="CarePlus Pharmacy", Address="Abdoun, Amman", City="Amman", Phone="+962 7 9333 4455", Email="support@careplus.demo", Latitude=31.948000m, Longitude=35.886000m, Rating=4.9m, RatingCount=998, EstimatedDeliveryMinutes=19, DeliveryFee=2.25m, MinimumOrder=10m, Description="Premium pharmacy delivery with curated health and wellness products." },
            new Pharmacy { Name="Irbid Health Pharmacy", Address="University St, Irbid", City="Irbid", Phone="+962 7 9444 5566", Email="orders@irbidhealth.demo", Latitude=32.556800m, Longitude=35.846900m, Rating=4.8m, RatingCount=524, EstimatedDeliveryMinutes=24, DeliveryFee=1.25m, MinimumOrder=6m, Description="Verified pharmacy fulfillment across central Irbid." },
            new Pharmacy { Name="Zarqa Care Pharmacy", Address="King Hussein St, Zarqa", City="Zarqa", Phone="+962 7 9555 6677", Email="orders@zarqacare.demo", Latitude=32.072800m, Longitude=36.088000m, Rating=4.7m, RatingCount=408, EstimatedDeliveryMinutes=27, DeliveryFee=1.00m, MinimumOrder=5m, Description="Everyday pharmacy essentials and prescription verification in Zarqa." },
            new Pharmacy { Name="Al Salt Community Pharmacy", Address="Al Hamman St, Salt", City="Salt", Phone="+962 7 9666 7788", Email="orders@saltcommunity.demo", Latitude=32.039200m, Longitude=35.727200m, Rating=4.7m, RatingCount=287, EstimatedDeliveryMinutes=29, DeliveryFee=1.50m, MinimumOrder=5m, Description="Neighborhood pharmacy serving Salt with live marketplace stock." }
        };

        db.Pharmacies.AddRange(pharmacies);
        await db.SaveChangesAsync();

        foreach (var pharmacy in pharmacies)
        {
            for (var d = 0; d < 7; d++)
                db.PharmacyHours.Add(new PharmacyHour { PharmacyId=pharmacy.PharmacyId, DayOfWeek=(DayOfWeek)d, OpensAt=new TimeSpan(8,0,0), ClosesAt=new TimeSpan(23,30,0), IsClosed=false });

            var zones = pharmacy.City == "Amman"
                ? new[] { "Amman", "Abdoun", "Gardens", "Khalda", "Sweifieh" }
                : new[] { pharmacy.City, "Central " + pharmacy.City };
            foreach (var zone in zones)
                db.PharmacyDeliveryZones.Add(new PharmacyDeliveryZone { PharmacyId=pharmacy.PharmacyId, ZoneName=zone, DeliveryFee=pharmacy.DeliveryFee, EstimatedMinutes=pharmacy.EstimatedDeliveryMinutes, IsActive=true });
        }

        var products = await db.Product.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.ProductId).ToListAsync();
        var random = new Random(5154);
        for (var pi = 0; pi < pharmacies.Length; pi++)
        {
            var pharmacy = pharmacies[pi];
            foreach (var product in products)
            {
                if (random.NextDouble() < .18 && pi != 0) continue;
                var variance = 1m + ((pi - 2) * .025m) + (decimal)(random.NextDouble() * .05 - .025);
                var price = Math.Max(.50m, Math.Round(product.Price * variance, 2));
                var stock = pi == 0 ? product.Stock : random.Next(4, Math.Max(12, Math.Min(90, product.Stock + 1)));
                db.PharmacyProducts.Add(new PharmacyProduct
                {
                    PharmacyId=pharmacy.PharmacyId, ProductId=product.ProductId, Price=price,
                    CompareAtPrice=random.NextDouble()<.22 ? Math.Round(price*1.12m,2) : null,
                    Stock=stock, ReorderLevel=Math.Max(5, product.ReorderLevel), IsAvailable=stock>0,
                    IsFeatured=random.NextDouble()<.18, ExpiryDate=product.ExpiryDate, UpdatedAt=DateTime.Now
                });
            }
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Marketplace bootstrapped with {PharmacyCount} pharmacies and {OfferCount} pharmacy offers.", pharmacies.Length, await db.PharmacyProducts.CountAsync());
    }
}
