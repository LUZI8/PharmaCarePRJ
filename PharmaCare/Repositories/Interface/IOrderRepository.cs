namespace PharmaCare.Repositories.Interface
{
    /* Repository interface for comprehensive order management operations */
    public interface IOrderRepository
    {
        /* Retrieve all orders in the system for admin management */
        Task<List<Order>> GetAllOrdersAsync();

        /* Get all orders for a specific user for order history display */
        Task<List<Order>> GetUserOrdersAsync(int userId);

        /* Retrieve single order by unique order ID */
        Task<Order> GetOrderByIdAsync(int orderId);

        /* Find order using human-readable order number for customer lookup */
        Task<Order> GetOrderByNumberAsync(string orderNumber);

        /* Create new order from user's cart with shipping and payment details */
        Task<Order> CreateOrderFromCartAsync(int userId, string shippingAddress, string city, string phoneNumber, string paymentMethod);

        /* Update order status (Pending, Processing, Shipped, etc.) - returns success status */
        Task<bool> UpdateOrderStatusAsync(int orderId, string status);

        /* Get recent orders as ViewModels for dashboard display with specified count limit */
        Task<List<OrderViewModel>> GetRecentOrdersAsync(int count);

        /* Generate order statistics dictionary with status counts for dashboard metrics */
        Task<Dictionary<string, int>> GetOrderStatisticsAsync();

        /* Comprehensive order update including status, payment status, and timestamps */
        Task UpdateOrderStatusAndPaymentAsync(int orderId, string status, bool isPaid, DateTime? paidAt, DateTime? deliveredAt);
    }
}