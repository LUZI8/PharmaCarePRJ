namespace PharmaCare.Data;

public static class MarketplacePrescriptionBootstrapper
{
    public static Task EnsureAsync(DataDbContext db) => db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[MarketplacePrescriptionRequests]', N'U') IS NULL
BEGIN
 CREATE TABLE [MarketplacePrescriptionRequests](
  [MarketplacePrescriptionRequestId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [RequestNumber] nvarchar(40) NOT NULL,
  [UserId] int NOT NULL,
  [PharmacyId] int NOT NULL,
  [PharmacyProductId] int NOT NULL,
  [ProductId] int NOT NULL,
  [Quantity] int NOT NULL DEFAULT 1,
  [ContactPhone] nvarchar(30) NOT NULL,
  [CustomerNote] nvarchar(500) NULL,
  [Status] nvarchar(30) NOT NULL DEFAULT N'Requested',
  [RequestedAt] datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
  [ExpiresAt] datetime2 NOT NULL,
  [ReviewedAt] datetime2 NULL,
  [StaffNote] nvarchar(500) NULL,
  CONSTRAINT [FK_MarketplacePrescriptionRequests_User_UserId] FOREIGN KEY([UserId]) REFERENCES [User]([UserId]),
  CONSTRAINT [FK_MarketplacePrescriptionRequests_Pharmacies_PharmacyId] FOREIGN KEY([PharmacyId]) REFERENCES [Pharmacies]([PharmacyId]),
  CONSTRAINT [FK_MarketplacePrescriptionRequests_PharmacyProducts_PharmacyProductId] FOREIGN KEY([PharmacyProductId]) REFERENCES [PharmacyProducts]([PharmacyProductId]),
  CONSTRAINT [FK_MarketplacePrescriptionRequests_Product_ProductId] FOREIGN KEY([ProductId]) REFERENCES [Product]([ProductId])
 );
 CREATE UNIQUE INDEX [IX_MarketplacePrescriptionRequests_RequestNumber] ON [MarketplacePrescriptionRequests]([RequestNumber]);
 CREATE INDEX [IX_MarketplacePrescriptionRequests_PharmacyId_Status] ON [MarketplacePrescriptionRequests]([PharmacyId],[Status]);
 CREATE INDEX [IX_MarketplacePrescriptionRequests_UserId] ON [MarketplacePrescriptionRequests]([UserId]);
END;");
}
