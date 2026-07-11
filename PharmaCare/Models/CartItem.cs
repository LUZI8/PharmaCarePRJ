namespace PharmaCare.Models
{
    /* Entity model representing individual items within a shopping cart */
    public class CartItem
    {
        /* Primary key for cart item identification */
        public int CartItemId { get; set; }

        /* Foreign key linking to parent cart */
        public int CartId { get; set; }

        /* Foreign key linking to product entity */
        public int ProductId { get; set; }

        /* Quantity of this product in the cart */
        public int Quantity { get; set; }

        /* Price per unit stored at time of addition to cart */
        public decimal Price { get; set; }

        /* Timestamp when item was added to cart */
        public DateTime CreatedAt { get; set; }

        /* Timestamp for last quantity or price update */
        public DateTime UpdatedAt { get; set; }

        /* Navigation property to parent cart entity */
        public Cart Cart { get; set; }

        /* Navigation property to associated product entity */
        public Product Product { get; set; }
    }
}