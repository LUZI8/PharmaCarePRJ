namespace PharmaCare.Data;

public static class MarketplaceOperationsBootstrapper
{
    public static async Task EnsureAsync(DataDbContext db)
    {
        const string sql = @"
IF OBJECT_ID(N'[MarketplaceOrderStatusHistory]', N'U') IS NULL
BEGIN
 CREATE TABLE [MarketplaceOrderStatusHistory](
  [MarketplaceOrderStatusHistoryId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [MarketplaceOrderId] int NOT NULL,
  [Status] nvarchar(40) NOT NULL,
  [ChangedByUserId] int NULL,
  [Notes] nvarchar(500) NULL,
  [ChangedAt] datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT [FK_MarketplaceOrderStatusHistory_MarketplaceOrders_Order] FOREIGN KEY([MarketplaceOrderId]) REFERENCES [MarketplaceOrders]([MarketplaceOrderId]) ON DELETE CASCADE,
  CONSTRAINT [FK_MarketplaceOrderStatusHistory_User_ChangedBy] FOREIGN KEY([ChangedByUserId]) REFERENCES [User]([UserId])
 );
 CREATE INDEX [IX_MarketplaceOrderStatusHistory_Order_Time] ON [MarketplaceOrderStatusHistory]([MarketplaceOrderId],[ChangedAt]);
END;

IF OBJECT_ID(N'[CustomerAddresses]', N'U') IS NULL
BEGIN
 CREATE TABLE [CustomerAddresses](
  [CustomerAddressId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [UserId] int NOT NULL,
  [Label] nvarchar(40) NOT NULL,
  [City] nvarchar(100) NOT NULL,
  [Area] nvarchar(100) NULL,
  [Street] nvarchar(180) NOT NULL,
  [Building] nvarchar(60) NULL,
  [Floor] nvarchar(30) NULL,
  [Apartment] nvarchar(30) NULL,
  [Landmark] nvarchar(180) NULL,
  [DeliveryInstructions] nvarchar(500) NULL,
  [Latitude] decimal(9,6) NULL,
  [Longitude] decimal(9,6) NULL,
  [IsDefault] bit NOT NULL DEFAULT 0,
  [CreatedAt] datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT [FK_CustomerAddresses_User] FOREIGN KEY([UserId]) REFERENCES [User]([UserId]) ON DELETE CASCADE
 );
 CREATE INDEX [IX_CustomerAddresses_User_Default] ON [CustomerAddresses]([UserId],[IsDefault]);
END;

IF OBJECT_ID(N'[MarketplaceNotifications]', N'U') IS NULL
BEGIN
 CREATE TABLE [MarketplaceNotifications](
  [MarketplaceNotificationId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [UserId] int NOT NULL,
  [Type] nvarchar(80) NOT NULL,
  [Title] nvarchar(160) NOT NULL,
  [Message] nvarchar(1000) NOT NULL,
  [ActionUrl] nvarchar(500) NULL,
  [IsRead] bit NOT NULL DEFAULT 0,
  [CreatedAt] datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT [FK_MarketplaceNotifications_User] FOREIGN KEY([UserId]) REFERENCES [User]([UserId]) ON DELETE CASCADE
 );
 CREATE INDEX [IX_MarketplaceNotifications_User_Read_Time] ON [MarketplaceNotifications]([UserId],[IsRead],[CreatedAt]);
END;

IF OBJECT_ID(N'[MarketplaceAuditLogs]', N'U') IS NULL
BEGIN
 CREATE TABLE [MarketplaceAuditLogs](
  [MarketplaceAuditLogId] bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [UserId] int NULL,
  [Action] nvarchar(80) NOT NULL,
  [EntityName] nvarchar(80) NOT NULL,
  [EntityId] nvarchar(80) NULL,
  [Details] nvarchar(1000) NULL,
  [CreatedAt] datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT [FK_MarketplaceAuditLogs_User] FOREIGN KEY([UserId]) REFERENCES [User]([UserId])
 );
 CREATE INDEX [IX_MarketplaceAuditLogs_Entity] ON [MarketplaceAuditLogs]([EntityName],[EntityId],[CreatedAt]);
END;

IF OBJECT_ID(N'[MarketplaceDeliveryAssignments]', N'U') IS NULL
BEGIN
 CREATE TABLE [MarketplaceDeliveryAssignments](
  [MarketplaceDeliveryAssignmentId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [MarketplaceOrderId] int NOT NULL,
  [DriverUserId] int NOT NULL,
  [Status] nvarchar(30) NOT NULL,
  [AssignedAt] datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
  [ArrivedAtPharmacy] datetime2 NULL,
  [PickedUpAt] datetime2 NULL,
  [StartedDeliveryAt] datetime2 NULL,
  [DeliveredAt] datetime2 NULL,
  [ProblemNote] nvarchar(500) NULL,
  CONSTRAINT [FK_MarketplaceDeliveryAssignments_Order] FOREIGN KEY([MarketplaceOrderId]) REFERENCES [MarketplaceOrders]([MarketplaceOrderId]) ON DELETE CASCADE,
  CONSTRAINT [FK_MarketplaceDeliveryAssignments_Driver] FOREIGN KEY([DriverUserId]) REFERENCES [User]([UserId])
 );
 CREATE UNIQUE INDEX [IX_MarketplaceDeliveryAssignments_Order] ON [MarketplaceDeliveryAssignments]([MarketplaceOrderId]);
 CREATE INDEX [IX_MarketplaceDeliveryAssignments_Driver_Status] ON [MarketplaceDeliveryAssignments]([DriverUserId],[Status]);
END;";

        await db.Database.ExecuteSqlRawAsync(sql);
    }
}
