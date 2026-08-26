namespace PharmaCare.Models;

public class MarketplacePrescriptionFile
{
    public int MarketplacePrescriptionFileId { get; set; }
    public int MarketplacePrescriptionRequestId { get; set; }
    [Required, MaxLength(500)] public string FileUrl { get; set; } = string.Empty;
    [Required, MaxLength(255)] public string OriginalFileName { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.Now;
    public MarketplacePrescriptionRequest Request { get; set; } = null!;
}
