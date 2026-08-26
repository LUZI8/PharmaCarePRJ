namespace PharmaCare.Data;

public static class MarketplaceOrderBootstrapper
{
    public static Task EnsureAsync(DataDbContext db) => db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[MarketplaceOrders]', N'U') IS NULL
BEGIN
 CREATE TABLE [MarketplaceOrders](
  [MarketplaceOrderId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [OrderNumber] nvarchar(40) NOT NULL,
  [UserId] int NOT NULL,
  [PharmacyId] int NOT NULL,
  [ShippingAddress] nvarchar(220) NOT NULL,
  [City] nvarchar(100) NOT NULL,
  [PhoneNumber] nvarchar(30) NOT NULL,
  [DeliveryNotes] nvarchar(500) NULL,
  [PaymentMethod] nvarchar(30) NOT NULL DEFAULT N'Cash on Delivery',
  [Status] nvarchar(30) NOT NULL DEFAULT N'Pending',
  [Subtotal] decimal(18,2) NOT NULL,
  [DeliveryFee] decimal(18,2) NOT NULL,
  [TotalAmount] decimal(18,2) NOT NULL,
  [OrderDate] datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
  [AcceptedAt] datetime2 NULL,
  [OutForDeliveryAt] datetime2 NULL,
  [DeliveredAt] datetime2 NULL,
  CONSTRAINT [FK_MarketplaceOrders_User_UserId] FOREIGN KEY([UserId]) REFERENCES [User]([UserId]),
  CONSTRAINT [FK_MarketplaceOrders_Pharmacies_PharmacyId] FOREIGN KEY([PharmacyId]) REFERENCES [Pharmacies]([PharmacyId])
 );
 CREATE UNIQUE INDEX [IX_MarketplaceOrders_OrderNumber] ON [MarketplaceOrders]([OrderNumber]);
 CREATE INDEX [IX_MarketplaceOrders_UserId] ON [MarketplaceOrders]([UserId]);
 CREATE INDEX [IX_MarketplaceOrders_PharmacyId] ON [MarketplaceOrders]([PharmacyId]);
END;
IF OBJECT_ID(N'[MarketplaceOrderItems]', N'U') IS NULL
BEGIN
 CREATE TABLE [MarketplaceOrderItems](
  [MarketplaceOrderItemId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [MarketplaceOrderId] int NOT NULL,
  [PharmacyProductId] int NOT NULL,
  [ProductId] int NOT NULL,
  [ProductName] nvarchar(180) NOT NULL,
  [Quantity] int NOT NULL,
  [UnitPrice] decimal(18,2) NOT NULL,
  [LineTotal] decimal(18,2) NOT NULL,
  [RequiresPrescription] bit NOT NULL DEFAULT 0,
  CONSTRAINT [FK_MarketplaceOrderItems_MarketplaceOrders_MarketplaceOrderId] FOREIGN KEY([MarketplaceOrderId]) REFERENCES [MarketplaceOrders]([MarketplaceOrderId]) ON DELETE CASCADE,
  CONSTRAINT [FK_MarketplaceOrderItems_PharmacyProducts_PharmacyProductId] FOREIGN KEY([PharmacyProductId]) REFERENCES [PharmacyProducts]([PharmacyProductId]),
  CONSTRAINT [FK_MarketplaceOrderItems_Product_ProductId] FOREIGN KEY([ProductId]) REFERENCES [Product]([ProductId])
 );
 CREATE INDEX [IX_MarketplaceOrderItems_MarketplaceOrderId] ON [MarketplaceOrderItems]([MarketplaceOrderId]);
END;");
}
