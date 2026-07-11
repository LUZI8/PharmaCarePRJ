namespace PharmaCare.Models
{
    /* Entity model representing user shopping cart with temporal tracking */
    public class Cart
    {
        /* Primary key for cart identification */
        public int CartId { get; set; }

        /* Foreign key linking cart to specific user */
        public int UserId { get; set; }

        /* Timestamp when cart was initially created */
        public DateTime CreatedAt { get; set; }

        /* Timestamp for last cart modification */
        public DateTime UpdatedAt { get; set; }

        /* Boolean flag indicating if cart is currently active */
        public bool IsActive { get; set; }

        /* Navigation property to associated user entity */
        public User User { get; set; }

        /* Collection navigation property for cart items */
        public List<CartItem> CartItems { get; set; }
    }
}