namespace PharmaCare.ViewModels
{
    /* Comprehensive ViewModel for product management with validation attributes */
    public class ProductViewModel
    {
        /* Unique product identifier for database operations */
        public int ProductId { get; set; }

        /* Product name with required validation and length constraints */
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200, ErrorMessage = "Product name cannot exceed 200 characters")]
        public string ProductName { get; set; }

        /* Foreign key reference to Category with required validation */
        [Required(ErrorMessage = "Category is required")]
        public int CategoryID { get; set; }

        /* Navigation property for category relationship */
        public Category Category { get; set; }

        /* Product description with multiline text data annotation for proper form rendering */
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        /* Product price with required validation and realistic range constraints */
        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 9999.99, ErrorMessage = "Price must be between $0.01 and $9999.99")]
        public decimal Price { get; set; }

        /* Stock quantity with required validation and non-negative constraint */
        [Required(ErrorMessage = "Stock is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock must be a positive number")]
        public int Stock { get; set; }

        /* Nullable image URL for product photos */
        public string? ImageUrl { get; set; }

        /* Nullable file upload property for image handling in forms */
        public IFormFile? File { get; set; }

        /* Boolean flag indicating if product is active/available for sale */
        public bool IsActive { get; set; }

        /* Boolean flag indicating if product requires prescription with default false value */
        public bool RequiresPrescription { get; set; } = false;

        /* Nullable prescription note with length constraint for additional medication instructions */
        [StringLength(500, ErrorMessage = "Prescription note cannot exceed 500 characters")]
        public string? PrescriptionNote { get; set; }

        /* CRITICAL: Expiry date for pharmaceutical safety with validation */
        [Required(ErrorMessage = "Expiry date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Expiry Date")]
        [FutureDate(ErrorMessage = "Expiry date must be in the future")]
        public DateTime ExpiryDate { get; set; } = DateTime.Now.AddYears(2); // Default to 2 years from now

        /* Timestamp for product creation tracking */
        public DateTime CreatedAt { get; set; }

        /* Nullable timestamp for last update tracking */
        public DateTime? UpdatedAt { get; set; }

        /* SelectList for category dropdown in forms - not mapped to database */
        public SelectList ListOfCategories { get; set; }

        /* Helper property to determine expiry status for display purposes */
        public string ExpiryStatus
        {
            get
            {
                var daysUntilExpiry = (ExpiryDate.Date - DateTime.Now.Date).Days;

                if (daysUntilExpiry < 0)
                    return "expired";
                else if (daysUntilExpiry <= 30)
                    return "expiring-soon";
                else if (daysUntilExpiry <= 90)
                    return "expiring-warning";
                else
                    return "good";
            }
        }

        /* Helper property to get expiry badge class for UI styling */
        public string ExpiryBadgeClass
        {
            get
            {
                return ExpiryStatus switch
                {
                    "expired" => "badge-danger",
                    "expiring-soon" => "badge-warning",
                    "expiring-warning" => "badge-info",
                    _ => "badge-success"
                };
            }
        }

        /* Helper property to get expiry display text */
        public string ExpiryDisplayText
        {
            get
            {
                var daysUntilExpiry = (ExpiryDate.Date - DateTime.Now.Date).Days;

                if (daysUntilExpiry < 0)
                    return "Expired";
                else if (daysUntilExpiry <= 30)
                    return $"Expires in {daysUntilExpiry} days";
                else if (daysUntilExpiry <= 90)
                    return "Expires in 3 months";
                else
                    return "Good";
            }
        }

        /* Helper property to check if product is expired */
        public bool IsExpired => ExpiryDate.Date < DateTime.Now.Date;

        /* Helper property to check if product is expiring soon (within 30 days) */
        public bool IsExpiringSoon => !IsExpired && (ExpiryDate.Date - DateTime.Now.Date).Days <= 30;
    }

    /* Custom validation attribute for future dates */
    public class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value is DateTime dateTime)
            {
                return dateTime.Date > DateTime.Now.Date;
            }
            return false;
        }
    }
}