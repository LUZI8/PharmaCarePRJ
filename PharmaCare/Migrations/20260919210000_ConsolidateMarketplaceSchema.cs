using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PharmaCare.Data;

#nullable disable

namespace PharmaCare.Migrations;

[DbContext(typeof(DataDbContext))]
[Migration("20260919210000_ConsolidateMarketplaceSchema")]
public sealed class ConsolidateMarketplaceSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF COL_LENGTH('Product','SKU') IS NULL ALTER TABLE [Product] ADD [SKU] nvarchar(50) NULL;
IF COL_LENGTH('Product','Barcode') IS NULL ALTER TABLE [Product] ADD [Barcode] nvarchar(64) NULL;
IF COL_LENGTH('Product','Manufacturer') IS NULL ALTER TABLE [Product] ADD [Manufacturer] nvarchar(150) NULL;
IF COL_LENGTH('Product','ReorderLevel') IS NULL ALTER TABLE [Product] ADD [ReorderLevel] int NOT NULL CONSTRAINT [DF_Product_ReorderLevel] DEFAULT 10;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Product_SKU' AND object_id=OBJECT_ID('Product'))
 CREATE UNIQUE INDEX [IX_Product_SKU] ON [Product]([SKU]) WHERE [SKU] IS NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_Product_Barcode' AND object_id=OBJECT_ID('Product'))
 CREATE UNIQUE INDEX [IX_Product_Barcode] ON [Product]([Barcode]) WHERE [Barcode] IS NOT NULL;

IF OBJECT_ID(N'[ProductImages]', N'U') IS NULL
BEGIN
 CREATE TABLE [ProductImages](
  [ProductImageId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [ProductId] int NOT NULL,
  [ImageUrl] nvarchar(500) NOT NULL,
  [DisplayOrder] int NOT NULL DEFAULT 0,
  [IsPrimary] bit NOT NULL DEFAULT 0,
  [CreatedAt] datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT [FK_ProductImages_Product] FOREIGN KEY([ProductId]) REFERENCES [Product]([ProductId]) ON DELETE CASCADE
 );
 CREATE INDEX [IX_ProductImages_ProductId_DisplayOrder] ON [ProductImages]([ProductId],[DisplayOrder]);
END;

IF OBJECT_ID(N'[Pharmacies]', N'U') IS NULL
BEGIN
 CREATE TABLE [Pharmacies](
  [PharmacyId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [Name] nvarchar(150) NOT NULL,[LogoUrl] nvarchar(500) NULL,[Address] nvarchar(220) NOT NULL,[City] nvarchar(100) NOT NULL,
  [Phone] nvarchar(30) NULL,[Email] nvarchar(180) NULL,[Latitude] decimal(9,6) NULL,[Longitude] decimal(9,6) NULL,
  [Rating] decimal(4,2) NOT NULL DEFAULT 4.5,[RatingCount] int NOT NULL DEFAULT 0,[EstimatedDeliveryMinutes] int NOT NULL DEFAULT 30,
  [DeliveryFee] decimal(18,2) NOT NULL DEFAULT 0,[MinimumOrder] decimal(18,2) NOT NULL DEFAULT 0,[IsOpen] bit NOT NULL DEFAULT 1,
  [IsActive] bit NOT NULL DEFAULT 1,[IsVerified] bit NOT NULL DEFAULT 1,[Description] nvarchar(1000) NULL,
  [CreatedAt] datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),[UpdatedAt] datetime2 NULL
 );
 CREATE INDEX [IX_Pharmacies_Name] ON [Pharmacies]([Name]);
END;

IF OBJECT_ID(N'[PharmacyProducts]', N'U') IS NULL
BEGIN
 CREATE TABLE [PharmacyProducts](
  [PharmacyProductId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,[PharmacyId] int NOT NULL,[ProductId] int NOT NULL,
  [Price] decimal(18,2) NOT NULL,[CompareAtPrice] decimal(18,2) NULL,[Stock] int NOT NULL,[ReorderLevel] int NOT NULL DEFAULT 10,
  [IsAvailable] bit NOT NULL DEFAULT 1,[IsFeatured] bit NOT NULL DEFAULT 0,[ExpiryDate] datetime2 NULL,[UpdatedAt] datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT [FK_PharmacyProducts_Pharmacies_PharmacyId] FOREIGN KEY([PharmacyId]) REFERENCES [Pharmacies]([PharmacyId]) ON DELETE CASCADE,
  CONSTRAINT [FK_PharmacyProducts_Product_ProductId] FOREIGN KEY([ProductId]) REFERENCES [Product]([ProductId]) ON DELETE CASCADE
 );
 CREATE UNIQUE INDEX [IX_PharmacyProducts_PharmacyId_ProductId] ON [PharmacyProducts]([PharmacyId],[ProductId]);
 CREATE INDEX [IX_PharmacyProducts_ProductId] ON [PharmacyProducts]([ProductId]);
END;

IF OBJECT_ID(N'[PharmacyHours]', N'U') IS NULL
BEGIN
 CREATE TABLE [PharmacyHours](
  [PharmacyHourId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,[PharmacyId] int NOT NULL,[DayOfWeek] int NOT NULL,
  [OpensAt] time NOT NULL,[ClosesAt] time NOT NULL,[IsClosed] bit NOT NULL DEFAULT 0,
  CONSTRAINT [FK_PharmacyHours_Pharmacies_PharmacyId] FOREIGN KEY([PharmacyId]) REFERENCES [Pharmacies]([PharmacyId]) ON DELETE CASCADE
 );
 CREATE UNIQUE INDEX [IX_PharmacyHours_PharmacyId_DayOfWeek] ON [PharmacyHours]([PharmacyId],[DayOfWeek]);
END;

IF OBJECT_ID(N'[PharmacyDeliveryZones]', N'U') IS NULL
BEGIN
 CREATE TABLE [PharmacyDeliveryZones](
  [PharmacyDeliveryZoneId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,[PharmacyId] int NOT NULL,[ZoneName] nvarchar(120) NOT NULL,
  [DeliveryFee] decimal(18,2) NOT NULL,[EstimatedMinutes] int NOT NULL DEFAULT 30,[IsActive] bit NOT NULL DEFAULT 1,
  CONSTRAINT [FK_PharmacyDeliveryZones_Pharmacies_PharmacyId] FOREIGN KEY([PharmacyId]) REFERENCES [Pharmacies]([PharmacyId]) ON DELETE CASCADE
 );
 CREATE INDEX [IX_PharmacyDeliveryZones_PharmacyId] ON [PharmacyDeliveryZones]([PharmacyId]);
END;

IF OBJECT_ID(N'[PharmacyStaff]', N'U') IS NULL
BEGIN
 CREATE TABLE [PharmacyStaff](
  [PharmacyStaffId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,[PharmacyId] int NOT NULL,[UserId] int NOT NULL,[Role] nvarchar(40) NOT NULL,[IsActive] bit NOT NULL DEFAULT 1,
  CONSTRAINT [FK_PharmacyStaff_Pharmacies_PharmacyId] FOREIGN KEY([PharmacyId]) REFERENCES [Pharmacies]([PharmacyId]) ON DELETE CASCADE,
  CONSTRAINT [FK_PharmacyStaff_User_UserId] FOREIGN KEY([UserId]) REFERENCES [User]([UserId]) ON DELETE CASCADE
 );
 CREATE UNIQUE INDEX [IX_PharmacyStaff_PharmacyId_UserId] ON [PharmacyStaff]([PharmacyId],[UserId]);
 CREATE INDEX [IX_PharmacyStaff_UserId] ON [PharmacyStaff]([UserId]);
END;

IF OBJECT_ID(N'[MarketplaceOrders]', N'U') IS NULL
BEGIN
 CREATE TABLE [MarketplaceOrders](
  [MarketplaceOrderId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,[OrderNumber] nvarchar(40) NOT NULL,[UserId] int NOT NULL,[PharmacyId] int NOT NULL,
  [ShippingAddress] nvarchar(220) NOT NULL,[City] nvarchar(100) NOT NULL,[PhoneNumber] nvarchar(30) NOT NULL,[DeliveryNotes] nvarchar(500) NULL,
  [PaymentMethod] nvarchar(30) NOT NULL DEFAULT N'Cash on Delivery',[Status] nvarchar(30) NOT NULL DEFAULT N'Pending',
  [Subtotal] decimal(18,2) NOT NULL,[DeliveryFee] decimal(18,2) NOT NULL,[TotalAmount] decimal(18,2) NOT NULL,
  [OrderDate] datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),[AcceptedAt] datetime2 NULL,[OutForDeliveryAt] datetime2 NULL,[DeliveredAt] datetime2 NULL,
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
  [MarketplaceOrderItemId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,[MarketplaceOrderId] int NOT NULL,[PharmacyProductId] int NOT NULL,[ProductId] int NOT NULL,
  [ProductName] nvarchar(180) NOT NULL,[Quantity] int NOT NULL,[UnitPrice] decimal(18,2) NOT NULL,[LineTotal] decimal(18,2) NOT NULL,[RequiresPrescription] bit NOT NULL DEFAULT 0,
  CONSTRAINT [FK_MarketplaceOrderItems_MarketplaceOrders_MarketplaceOrderId] FOREIGN KEY([MarketplaceOrderId]) REFERENCES [MarketplaceOrders]([MarketplaceOrderId]) ON DELETE CASCADE,
  CONSTRAINT [FK_MarketplaceOrderItems_PharmacyProducts_PharmacyProductId] FOREIGN KEY([PharmacyProductId]) REFERENCES [PharmacyProducts]([PharmacyProductId]),
  CONSTRAINT [FK_MarketplaceOrderItems_Product_ProductId] FOREIGN KEY([ProductId]) REFERENCES [Product]([ProductId])
 );
 CREATE INDEX [IX_MarketplaceOrderItems_MarketplaceOrderId] ON [MarketplaceOrderItems]([MarketplaceOrderId]);
END;

IF OBJECT_ID(N'[MarketplacePrescriptionRequests]', N'U') IS NULL
BEGIN
 CREATE TABLE [MarketplacePrescriptionRequests](
  [MarketplacePrescriptionRequestId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,[RequestNumber] nvarchar(40) NOT NULL,[UserId] int NOT NULL,[PharmacyId] int NOT NULL,
  [PharmacyProductId] int NOT NULL,[ProductId] int NOT NULL,[Quantity] int NOT NULL DEFAULT 1,[ContactPhone] nvarchar(30) NOT NULL,[CustomerNote] nvarchar(500) NULL,
  [Status] nvarchar(30) NOT NULL DEFAULT N'Requested',[RequestedAt] datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),[ExpiresAt] datetime2 NOT NULL,[ReviewedAt] datetime2 NULL,[StaffNote] nvarchar(500) NULL,
  CONSTRAINT [FK_MarketplacePrescriptionRequests_User_UserId] FOREIGN KEY([UserId]) REFERENCES [User]([UserId]),
  CONSTRAINT [FK_MarketplacePrescriptionRequests_Pharmacies_PharmacyId] FOREIGN KEY([PharmacyId]) REFERENCES [Pharmacies]([PharmacyId]),
  CONSTRAINT [FK_MarketplacePrescriptionRequests_PharmacyProducts_PharmacyProductId] FOREIGN KEY([PharmacyProductId]) REFERENCES [PharmacyProducts]([PharmacyProductId]),
  CONSTRAINT [FK_MarketplacePrescriptionRequests_Product_ProductId] FOREIGN KEY([ProductId]) REFERENCES [Product]([ProductId])
 );
 CREATE UNIQUE INDEX [IX_MarketplacePrescriptionRequests_RequestNumber] ON [MarketplacePrescriptionRequests]([RequestNumber]);
 CREATE INDEX [IX_MarketplacePrescriptionRequests_PharmacyId_Status] ON [MarketplacePrescriptionRequests]([PharmacyId],[Status]);
 CREATE INDEX [IX_MarketplacePrescriptionRequests_UserId] ON [MarketplacePrescriptionRequests]([UserId]);
END;

IF OBJECT_ID(N'[MarketplaceOrderStatusHistory]', N'U') IS NULL
BEGIN
 CREATE TABLE [MarketplaceOrderStatusHistory](
  [MarketplaceOrderStatusHistoryId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,[MarketplaceOrderId] int NOT NULL,[Status] nvarchar(40) NOT NULL,
  [ChangedByUserId] int NULL,[Notes] nvarchar(500) NULL,[ChangedAt] datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT [FK_MarketplaceOrderStatusHistory_MarketplaceOrders_Order] FOREIGN KEY([MarketplaceOrderId]) REFERENCES [MarketplaceOrders]([MarketplaceOrderId]) ON DELETE CASCADE,
  CONSTRAINT [FK_MarketplaceOrderStatusHistory_User_ChangedBy] FOREIGN KEY([ChangedByUserId]) REFERENCES [User]([UserId])
 );
 CREATE INDEX [IX_MarketplaceOrderStatusHistory_Order_Time] ON [MarketplaceOrderStatusHistory]([MarketplaceOrderId],[ChangedAt]);
END;

IF OBJECT_ID(N'[CustomerAddresses]', N'U') IS NULL
BEGIN
 CREATE TABLE [CustomerAddresses](
  [CustomerAddressId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,[UserId] int NOT NULL,[Label] nvarchar(40) NOT NULL,[City] nvarchar(100) NOT NULL,
  [Area] nvarchar(100) NULL,[Street] nvarchar(180) NOT NULL,[Building] nvarchar(60) NULL,[Floor] nvarchar(30) NULL,[Apartment] nvarchar(30) NULL,
  [Landmark] nvarchar(180) NULL,[DeliveryInstructions] nvarchar(500) NULL,[Latitude] decimal(9,6) NULL,[Longitude] decimal(9,6) NULL,[IsDefault] bit NOT NULL DEFAULT 0,
  [CreatedAt] datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),CONSTRAINT [FK_CustomerAddresses_User] FOREIGN KEY([UserId]) REFERENCES [User]([UserId]) ON DELETE CASCADE
 );
 CREATE INDEX [IX_CustomerAddresses_User_Default] ON [CustomerAddresses]([UserId],[IsDefault]);
END;

IF OBJECT_ID(N'[MarketplaceNotifications]', N'U') IS NULL
BEGIN
 CREATE TABLE [MarketplaceNotifications](
  [MarketplaceNotificationId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,[UserId] int NOT NULL,[Type] nvarchar(80) NOT NULL,[Title] nvarchar(160) NOT NULL,
  [Message] nvarchar(1000) NOT NULL,[ActionUrl] nvarchar(500) NULL,[IsRead] bit NOT NULL DEFAULT 0,[CreatedAt] datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT [FK_MarketplaceNotifications_User] FOREIGN KEY([UserId]) REFERENCES [User]([UserId]) ON DELETE CASCADE
 );
 CREATE INDEX [IX_MarketplaceNotifications_User_Read_Time] ON [MarketplaceNotifications]([UserId],[IsRead],[CreatedAt]);
END;

IF OBJECT_ID(N'[MarketplaceAuditLogs]', N'U') IS NULL
BEGIN
 CREATE TABLE [MarketplaceAuditLogs](
  [MarketplaceAuditLogId] bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,[UserId] int NULL,[Action] nvarchar(80) NOT NULL,[EntityName] nvarchar(80) NOT NULL,
  [EntityId] nvarchar(80) NULL,[Details] nvarchar(1000) NULL,[CreatedAt] datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT [FK_MarketplaceAuditLogs_User] FOREIGN KEY([UserId]) REFERENCES [User]([UserId])
 );
 CREATE INDEX [IX_MarketplaceAuditLogs_Entity] ON [MarketplaceAuditLogs]([EntityName],[EntityId],[CreatedAt]);
END;

IF OBJECT_ID(N'[MarketplaceDeliveryAssignments]', N'U') IS NULL
BEGIN
 CREATE TABLE [MarketplaceDeliveryAssignments](
  [MarketplaceDeliveryAssignmentId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,[MarketplaceOrderId] int NOT NULL,[DriverUserId] int NOT NULL,[Status] nvarchar(30) NOT NULL,
  [AssignedAt] datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),[ArrivedAtPharmacy] datetime2 NULL,[PickedUpAt] datetime2 NULL,[StartedDeliveryAt] datetime2 NULL,
  [DeliveredAt] datetime2 NULL,[ProblemNote] nvarchar(500) NULL,
  CONSTRAINT [FK_MarketplaceDeliveryAssignments_Order] FOREIGN KEY([MarketplaceOrderId]) REFERENCES [MarketplaceOrders]([MarketplaceOrderId]) ON DELETE CASCADE,
  CONSTRAINT [FK_MarketplaceDeliveryAssignments_Driver] FOREIGN KEY([DriverUserId]) REFERENCES [User]([UserId])
 );
 CREATE UNIQUE INDEX [IX_MarketplaceDeliveryAssignments_Order] ON [MarketplaceDeliveryAssignments]([MarketplaceOrderId]);
 CREATE INDEX [IX_MarketplaceDeliveryAssignments_Driver_Status] ON [MarketplaceDeliveryAssignments]([DriverUserId],[Status]);
END;

IF OBJECT_ID(N'[MarketplacePrescriptionFile]', N'U') IS NULL AND OBJECT_ID(N'[MarketplacePrescriptionFiles]', N'U') IS NULL
BEGIN
 CREATE TABLE [MarketplacePrescriptionFile](
  [MarketplacePrescriptionFileId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,[MarketplacePrescriptionRequestId] int NOT NULL,[FileUrl] nvarchar(500) NOT NULL,
  [OriginalFileName] nvarchar(255) NOT NULL,[ContentType] nvarchar(100) NOT NULL,[FileSizeBytes] bigint NOT NULL,[UploadedAt] datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT [FK_MarketplacePrescriptionFile_Request] FOREIGN KEY([MarketplacePrescriptionRequestId]) REFERENCES [MarketplacePrescriptionRequests]([MarketplacePrescriptionRequestId]) ON DELETE CASCADE
 );
 CREATE INDEX [IX_MarketplacePrescriptionFile_Request] ON [MarketplacePrescriptionFile]([MarketplacePrescriptionRequestId]);
END;
");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally non-destructive. This migration consolidates schema that may already contain
        // live marketplace data from earlier development bootstrappers.
    }
}
