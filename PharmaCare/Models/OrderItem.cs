namespace PharmaCare.Models
{
    /* Entity model representing individual line items within an order */
    public class OrderItem
    {
        /* Primary key for order item identification */
        public int OrderItemId { get; set; }

        /* Foreign key linking to parent order */
        public int OrderId { get; set; }

        /* Foreign key linking to product entity */
        public int ProductId { get; set; }

        /* Product name snapshot at time of order to preserve historical data */
        public string ProductName { get; set; }

        /* Quantity of product ordered */
        public int Quantity { get; set; }

        /* Unit price at time of order to preserve pricing history */
        public decimal Price { get; set; }

        /* Timestamp when order item was created */
        public DateTime CreatedAt { get; set; }

        /* Navigation property to parent order entity */
        public Order Order { get; set; }

        /* Navigation property to associated product entity */
        public Product Product { get; set; }
    }
}