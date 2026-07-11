namespace PharmaCare.Models
{
    /* Entity model representing pharmaceutical products in the inventory system */
    public class Product
    {
        /* Primary key for product identification */
        public int ProductId { get; set; }

        /* Product display name for catalog and ordering */
        public string ProductName { get; set; }

        /* Foreign key linking to product category */
        public int CategoryID { get; set; }

        /* Navigation property to associated category entity */
        public Category Category { get; set; }

        /* Detailed product description for customer information */
        public string Description { get; set; }

        /* Unit price in decimal format for monetary calculations */
        public decimal Price { get; set; }

        /* Current inventory count for stock management */
        public int Stock { get; set; }

        /* Nullable URL path to product image */
        public string? ImageUrl { get; set; }

        /* Boolean flag indicating if product is available for sale */
        public bool IsActive { get; set; }

        /* Boolean flag indicating if valid prescription is required for purchase */
        public bool RequiresPrescription { get; set; }

        /* Nullable text for additional prescription-related instructions */
        public string? PrescriptionNote { get; set; }

        /* CRITICAL: Expiry date for pharmaceutical safety and regulatory compliance */
        public DateTime ExpiryDate { get; set; }

        /* Timestamp when product was initially created */
        public DateTime CreatedAt { get; set; }

        /* Nullable timestamp for last product information update */
        public DateTime? UpdatedAt { get; set; }
    }
}