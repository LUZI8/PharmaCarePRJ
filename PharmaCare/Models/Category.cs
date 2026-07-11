namespace PharmaCare.Models
{
    /* Entity model for product categorization with validation attributes */
    public class Category
    {
        /* Primary key with explicit Key attribute */
        [Key]
        public int CategoryID { get; set; }

        /* Category name with required validation and length constraint */
        [Required(ErrorMessage = "Category name is required")]
        [StringLength(100, ErrorMessage = "Category name cannot exceed 100 characters")]
        public string CategoryName { get; set; }

        /* Collection of products in this category - excluded from database mapping */
        [NotMapped]
        public virtual ICollection<Product> Products { get; set; }

        /* Constructor initializing the products collection to prevent null reference */
        public Category()
        {
            Products = new List<Product>();
        }
    }
}