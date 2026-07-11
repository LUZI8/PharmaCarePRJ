namespace PharmaCare.ViewModels
{
    /* ViewModel for order display containing essential order information */
    public class OrderViewModel
    {
        /* Unique order identifier for database operations */
        public int OrderId { get; set; }

        /* Human-readable order number for customer reference */
        public string OrderNumber { get; set; }

        /* Customer's full name for order identification */
        public string CustomerName { get; set; }

        /* Customer email address for communication */
        public string CustomerEmail { get; set; }

        /* Current order status (Pending, Processing, Shipped, etc.) */
        public string Status { get; set; }

        /* Total monetary value of the order */
        public decimal TotalAmount { get; set; }

        /* Date when the order was placed */
        public DateTime OrderDate { get; set; }

        /* Total number of items in the order */
        public int ItemCount { get; set; }

        /* Computed property that maps order status to Bootstrap CSS classes for consistent UI styling */
        public string StatusClass
        {
            get
            {
                /* Switch expression mapping status strings to Bootstrap badge color classes */
                return Status?.ToLower() switch
                {
                    "pending" => "warning",     // Yellow badge for pending orders
                    "processing" => "info",     // Blue badge for processing orders
                    "shipped" => "primary",     // Primary blue badge for shipped orders
                    "delivered" => "success",   // Green badge for delivered orders
                    "cancelled" => "danger",    // Red badge for cancelled orders
                    _ => "secondary"            // Gray badge for unknown/default status
                };
            }
        }
    }
}