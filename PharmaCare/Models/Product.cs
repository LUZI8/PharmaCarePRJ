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

        /* Primary storefront image plus optional gallery images. */
        public string? ImageUrl { get; set; }
        public string? ImageUrl2 { get; set; }
        public string? ImageUrl3 { get; set; }
        public string? ImageUrl4 { get; set; }

        public bool IsActive { get; set; }
        public bool RequiresPrescription { get; set; }
        public string? PrescriptionNote { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}