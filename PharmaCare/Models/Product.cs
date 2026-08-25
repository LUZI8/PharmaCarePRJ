namespace PharmaCare.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int CategoryID { get; set; }
        public Category Category { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }

        /* Inventory identifiers used by pharmacy staff and barcode scanners. */
        public string? SKU { get; set; }
        public string? Barcode { get; set; }
        public string? Manufacturer { get; set; }

        /* Per-product threshold instead of a hard-coded global low-stock value. */
        public int ReorderLevel { get; set; } = 10;

        /* Legacy primary image kept for backwards compatibility with cards and old data. */
        public string? ImageUrl { get; set; }
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();

        public bool IsActive { get; set; }
        public bool RequiresPrescription { get; set; }
        public string? PrescriptionNote { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
