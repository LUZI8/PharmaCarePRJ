namespace PharmaCare.Models
{
    /* Entity model representing customer orders with comprehensive tracking */
    public class Order
    {
        /* Primary key for order identification */
        public int OrderId { get; set; }

        /* Foreign key linking order to customer */
        public int UserId { get; set; }

        /* Human-readable order number for customer reference */
        public string OrderNumber { get; set; }

        /* Total monetary value including tax and shipping */
        public decimal TotalAmount { get; set; }

        /* Current order status (Pending, Processing, Shipped, Delivered, Cancelled) */
        public string Status { get; set; }

        /* Date and time when order was placed */
        public DateTime OrderDate { get; set; }

        /* Delivery address for order shipment */
        public string ShippingAddress { get; set; }

        /* Delivery city for shipping calculations */
        public string City { get; set; }

        /* Contact phone number for delivery coordination */
        public string PhoneNumber { get; set; }

        /* Payment method used for order (Credit Card, Cash on Delivery, etc.) */
        public string PaymentMethod { get; set; }

        /* Boolean flag indicating payment completion status */
        public bool IsPaid { get; set; }

        /* Nullable timestamp for payment completion */
        public DateTime? PaidAt { get; set; }

        /* Nullable timestamp for delivery completion */
        public DateTime? DeliveredAt { get; set; }

        /* Timestamp when order record was created */
        public DateTime CreatedAt { get; set; }

        /* Timestamp for last order modification */
        public DateTime UpdatedAt { get; set; }

        /* Navigation property to customer entity */
        public User User { get; set; }

        /* Collection navigation property for order line items */
        public List<OrderItem> OrderItems { get; set; }
    }
}