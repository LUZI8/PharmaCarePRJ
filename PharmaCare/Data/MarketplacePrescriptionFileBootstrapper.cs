namespace PharmaCare.Data;

public static class MarketplacePrescriptionFileBootstrapper
{
    public static Task EnsureAsync(DataDbContext db) => db.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[MarketplacePrescriptionFile]', N'U') IS NULL AND OBJECT_ID(N'[MarketplacePrescriptionFiles]', N'U') IS NULL
BEGIN
 CREATE TABLE [MarketplacePrescriptionFile](
  [MarketplacePrescriptionFileId] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
  [MarketplacePrescriptionRequestId] int NOT NULL,
  [FileUrl] nvarchar(500) NOT NULL,
  [OriginalFileName] nvarchar(255) NOT NULL,
  [ContentType] nvarchar(100) NOT NULL,
  [FileSizeBytes] bigint NOT NULL,
  [UploadedAt] datetime2 NOT NULL DEFAULT SYSUTCDATETIME(),
  CONSTRAINT [FK_MarketplacePrescriptionFile_Request] FOREIGN KEY([MarketplacePrescriptionRequestId]) REFERENCES [MarketplacePrescriptionRequests]([MarketplacePrescriptionRequestId]) ON DELETE CASCADE
 );
 CREATE INDEX [IX_MarketplacePrescriptionFile_Request] ON [MarketplacePrescriptionFile]([MarketplacePrescriptionRequestId]);
END;");
}
