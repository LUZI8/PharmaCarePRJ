namespace PharmaCare.ViewModels
{
    public class ProductViewModel
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200, ErrorMessage = "Product name cannot exceed 200 characters")]
        public string ProductName { get; set; }

        [Required(ErrorMessage = "Category is required")]
        public int CategoryID { get; set; }
        public Category Category { get; set; }

        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 9999.99, ErrorMessage = "Price must be between $0.01 and $9999.99")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stock is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
        public int Stock { get; set; }

        [StringLength(50, ErrorMessage = "SKU cannot exceed 50 characters")]
        public string? SKU { get; set; }

        [StringLength(64, ErrorMessage = "Barcode cannot exceed 64 characters")]
        public string? Barcode { get; set; }

        [StringLength(150, ErrorMessage = "Manufacturer cannot exceed 150 characters")]
        public string? Manufacturer { get; set; }

        [Range(0, 100000, ErrorMessage = "Reorder level cannot be negative")]
        [Display(Name = "Low Stock Threshold")]
        public int ReorderLevel { get; set; } = 10;

        public string? ImageUrl { get; set; }
        public IFormFile? File { get; set; }

        /* Storefront gallery abstraction. It currently uses the primary image and is ready
           to accept additional images later without changing the product-page markup. */
        public IEnumerable<string> GalleryImages => string.IsNullOrWhiteSpace(ImageUrl)
            ? Enumerable.Empty<string>()
            : new[] { ImageUrl };

        public bool IsActive { get; set; }
        public bool RequiresPrescription { get; set; } = false;

        [StringLength(500, ErrorMessage = "Prescription note cannot exceed 500 characters")]
        public string? PrescriptionNote { get; set; }

        [Required(ErrorMessage = "Expiry date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Expiry Date")]
        [FutureDate(ErrorMessage = "Expiry date must be in the future")]
        public DateTime ExpiryDate { get; set; } = DateTime.Now.AddYears(2);

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public SelectList ListOfCategories { get; set; }

        public string ExpiryStatus
        {
            get
            {
                var days = (ExpiryDate.Date - DateTime.Now.Date).Days;
                if (days < 0) return "expired";
                if (days <= 30) return "expiring-soon";
                if (days <= 90) return "expiring-warning";
                return "good";
            }
        }

        public string ExpiryBadgeClass => ExpiryStatus switch
        {
            "expired" => "badge-danger",
            "expiring-soon" => "badge-warning",
            "expiring-warning" => "badge-info",
            _ => "badge-success"
        };

        public string ExpiryDisplayText
        {
            get
            {
                var days = (ExpiryDate.Date - DateTime.Now.Date).Days;
                if (days < 0) return "Expired";
                if (days <= 30) return $"Expires in {days} days";
                if (days <= 90) return "Expires within 90 days";
                return "Good";
            }
        }

        public bool IsExpired => ExpiryDate.Date < DateTime.Now.Date;
        public bool IsExpiringSoon => !IsExpired && (ExpiryDate.Date - DateTime.Now.Date).Days <= 30;
        public bool IsLowStock => Stock > 0 && Stock <= ReorderLevel;
        public bool IsOutOfStock => Stock <= 0;
    }

    public class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            return value is DateTime dateTime && dateTime.Date > DateTime.Now.Date;
        }
    }
}