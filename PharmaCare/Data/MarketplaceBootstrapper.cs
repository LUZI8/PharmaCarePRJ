using Microsoft.Data.SqlClient;

namespace PharmaCare.Data;

public static class MarketplaceBootstrapper
{
    public static async Task EnsureAsync(DataDbContext db, ILogger logger)
    {
        var sql = @"
IF OBJECT_ID(N'[Pharmacies]', N'U') IS NULL
BEGIN
 CREATE TABLE [Pharmacies](
  [PharmacyId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [Name] nvarchar(150) NOT NULL,
  [LogoUrl] nvarchar(500) NULL,
  [Address] nvarchar(220) NOT NULL,
  [City] nvarchar(100) NOT NULL,
  [Phone] nvarchar(30) NULL,
  [Email] nvarchar(180) NULL,
  [Latitude] decimal(9,6) NULL,
  [Longitude] decimal(9,6) NULL,
  [Rating] decimal(4,2) NOT NULL DEFAULT 4.5,
  [RatingCount] int NOT NULL DEFAULT 0,
  [EstimatedDeliveryMinutes] int NOT NULL DEFAULT 30,
  [DeliveryFee] decimal(18,2) NOT NULL DEFAULT 0,
  [MinimumOrder] decimal(18,2) NOT NULL DEFAULT 0,
  [IsOpen] bit NOT NULL DEFAULT 1,
  [IsActive] bit NOT NULL DEFAULT 1,
  [IsVerified] bit NOT NULL DEFAULT 1,
  [Description] nvarchar(1000) NULL,
  [CreatedAt] datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAt] datetime2 NULL
 );
 CREATE INDEX [IX_Pharmacies_Name] ON [Pharmacies]([Name]);
END;

IF OBJECT_ID(N'[PharmacyProducts]', N'U') IS NULL
BEGIN
 CREATE TABLE [PharmacyProducts](
  [PharmacyProductId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [PharmacyId] int NOT NULL,
  [ProductId] int NOT NULL,
  [Price] decimal(18,2) NOT NULL,
  [CompareAtPrice] decimal(18,2) NULL,
  [Stock] int NOT NULL,
  [ReorderLevel] int NOT NULL DEFAULT 10,
  [IsAvailable] bit NOT NULL DEFAULT 1,
  [IsFeatured] bit NOT NULL DEFAULT 0,
  [ExpiryDate] datetime2 NULL,
  [UpdatedAt] datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT [FK_PharmacyProducts_Pharmacies_PharmacyId] FOREIGN KEY([PharmacyId]) REFERENCES [Pharmacies]([PharmacyId]) ON DELETE CASCADE,
  CONSTRAINT [FK_PharmacyProducts_Product_ProductId] FOREIGN KEY([ProductId]) REFERENCES [Product]([ProductId]) ON DELETE CASCADE
 );
 CREATE UNIQUE INDEX [IX_PharmacyProducts_PharmacyId_ProductId] ON [PharmacyProducts]([PharmacyId],[ProductId]);
 CREATE INDEX [IX_PharmacyProducts_ProductId] ON [PharmacyProducts]([ProductId]);
END;

IF OBJECT_ID(N'[PharmacyHours]', N'U') IS NULL
BEGIN
 CREATE TABLE [PharmacyHours](
  [PharmacyHourId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [PharmacyId] int NOT NULL,
  [DayOfWeek] int NOT NULL,
  [OpensAt] time NOT NULL,
  [ClosesAt] time NOT NULL,
  [IsClosed] bit NOT NULL DEFAULT 0,
  CONSTRAINT [FK_PharmacyHours_Pharmacies_PharmacyId] FOREIGN KEY([PharmacyId]) REFERENCES [Pharmacies]([PharmacyId]) ON DELETE CASCADE
 );
 CREATE UNIQUE INDEX [IX_PharmacyHours_PharmacyId_DayOfWeek] ON [PharmacyHours]([PharmacyId],[DayOfWeek]);
END;

IF OBJECT_ID(N'[PharmacyDeliveryZones]', N'U') IS NULL
BEGIN
 CREATE TABLE [PharmacyDeliveryZones](
  [PharmacyDeliveryZoneId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [PharmacyId] int NOT NULL,
  [ZoneName] nvarchar(120) NOT NULL,
  [DeliveryFee] decimal(18,2) NOT NULL,
  [EstimatedMinutes] int NOT NULL DEFAULT 30,
  [IsActive] bit NOT NULL DEFAULT 1,
  CONSTRAINT [FK_PharmacyDeliveryZones_Pharmacies_PharmacyId] FOREIGN KEY([PharmacyId]) REFERENCES [Pharmacies]([PharmacyId]) ON DELETE CASCADE
 );
 CREATE INDEX [IX_PharmacyDeliveryZones_PharmacyId] ON [PharmacyDeliveryZones]([PharmacyId]);
END;

IF OBJECT_ID(N'[PharmacyStaff]', N'U') IS NULL
BEGIN
 CREATE TABLE [PharmacyStaff](
  [PharmacyStaffId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [PharmacyId] int NOT NULL,
  [UserId] int NOT NULL,
  [Role] nvarchar(40) NOT NULL,
  [IsActive] bit NOT NULL DEFAULT 1,
  CONSTRAINT [FK_PharmacyStaff_Pharmacies_PharmacyId] FOREIGN KEY([PharmacyId]) REFERENCES [Pharmacies]([PharmacyId]) ON DELETE CASCADE,
  CONSTRAINT [FK_PharmacyStaff_User_UserId] FOREIGN KEY([UserId]) REFERENCES [User]([UserId]) ON DELETE CASCADE
 );
 CREATE UNIQUE INDEX [IX_PharmacyStaff_PharmacyId_UserId] ON [PharmacyStaff]([PharmacyId],[UserId]);
 CREATE INDEX [IX_PharmacyStaff_UserId] ON [PharmacyStaff]([UserId]);
END;";

        await db.Database.ExecuteSqlRawAsync(sql);

        if (await db.Pharmacies.AnyAsync()) return;

        var pharmacies = new[]
        {
            new Pharmacy { Name="PharmaCare Main Pharmacy", Address="Almadina Almonoara St, Amman", City="Amman", Phone="+962 7 9999 8888", Email="pharmacare@info.com", Latitude=31.986800m, Longitude=35.889200m, Rating=4.9m, RatingCount=1320, EstimatedDeliveryMinutes=22, DeliveryFee=0m, MinimumOrder=8m, Description="Fast local pharmacy fulfillment with prescription pickup and live inventory." },
            new Pharmacy { Name="Shifa Pharmacy", Address="Gardens St, Amman", City="Amman", Phone="+962 7 9000 1122", Email="orders@shifa.demo", Latitude=31.982400m, Longitude=35.899100m, Rating=4.8m, RatingCount=860, EstimatedDeliveryMinutes=28, DeliveryFee=1.50m, MinimumOrder=6m, Description="Community pharmacy with strong everyday medicine availability and fast delivery." },
            new Pharmacy { Name="LifeCare Pharmacy", Address="Mecca St, Amman", City="Amman", Phone="+962 7 9111 2233", Email="hello@lifecare.demo", Latitude=31.977300m, Longitude=35.863700m, Rating=4.7m, RatingCount=615, EstimatedDeliveryMinutes=34, DeliveryFee=2m, MinimumOrder=7m, Description="Wellness, OTC and prescription reservation service across west Amman." },
            new Pharmacy { Name="Al Hayat Pharmacy", Address="University St, Amman", City="Amman", Phone="+962 7 9222 3344", Email="care@alhayat.demo", Latitude=32.014200m, Longitude=35.873100m, Rating=4.6m, RatingCount=440, EstimatedDeliveryMinutes=31, DeliveryFee=1.25m, MinimumOrder=5m, Description="Trusted neighborhood pharmacy with extended opening hours." },
            new Pharmacy { Name="CarePlus Pharmacy", Address="Abdoun, Amman", City="Amman", Phone="+962 7 9333 4455", Email="support@careplus.demo", Latitude=31.948000m, Longitude=35.886000m, Rating=4.9m, RatingCount=998, EstimatedDeliveryMinutes=19, DeliveryFee=2.25m, MinimumOrder=10m, Description="Premium pharmacy delivery with curated health and wellness products." }
        };

        db.Pharmacies.AddRange(pharmacies);
        await db.SaveChangesAsync();

        foreach (var pharmacy in pharmacies)
        {
            for (var d = 0; d < 7; d++)
                db.PharmacyHours.Add(new PharmacyHour { PharmacyId=pharmacy.PharmacyId, DayOfWeek=(DayOfWeek)d, OpensAt=new TimeSpan(8,0,0), ClosesAt=new TimeSpan(23,30,0), IsClosed=false });

            foreach (var zone in new[] { "Amman", "Abdoun", "Gardens", "Khalda", "Sweifieh" })
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
