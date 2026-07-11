namespace PharmaCare.Repositories.Repository
{
    /* Repository implementation for comprehensive order management with business logic */
    public class OrderRepository : IOrderRepository
    {
        private readonly DataDbContext _context;
        private readonly ICartRepository _cartRepository;
        private readonly IProductRepository _productRepository;

        /* Constructor with multiple repository dependencies for complex operations */
        public OrderRepository(
            DataDbContext context,
            ICartRepository cartRepository,
            IProductRepository productRepository)
        {
            _context = context;
            _cartRepository = cartRepository;
            _productRepository = productRepository;
        }

        /* Retrieve all orders with related entities for admin management */
        public async Task<List<Order>> GetAllOrdersAsync()
        {
            /* Include user and order items with products for complete order data */
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        /* Get orders for specific user for order history display */
        public async Task<List<Order>> GetUserOrdersAsync(int userId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        /* Retrieve single order by ID with all related data */
        public async Task<Order> GetOrderByIdAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        /* Find order by customer-facing order number */
        public async Task<Order> GetOrderByNumberAsync(string orderNumber)
        {
            return await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
        }

        /* Complex order creation from cart with inventory management and validation */
        public async Task<Order> CreateOrderFromCartAsync(int userId, string shippingAddress, string city, string phoneNumber, string paymentMethod)
        {
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);

            /* Validate cart has items before proceeding */
            if (cart.CartItems == null || !cart.CartItems.Any())
            {
                return null;
            }

            /* Pre-validate stock availability to prevent overselling */
            foreach (var cartItem in cart.CartItems)
            {
                var product = _productRepository.Find(cartItem.ProductId);
                if (product == null || product.Stock < cartItem.Quantity)
                {
                    throw new Exception($"Not enough stock for {cartItem.Product.ProductName}. Available: {product?.Stock ?? 0}, Requested: {cartItem.Quantity}");
                }
            }

            /* Generate unique order number for tracking */
            string orderNumber = GenerateOrderNumber();

            var orderItems = new List<OrderItem>();
            decimal totalAmount = 0;

            /* Convert cart items to order items and update inventory */
            foreach (var cartItem in cart.CartItems)
            {
                var orderItem = new OrderItem
                {
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.Product.ProductName,
                    Quantity = cartItem.Quantity,
                    Price = cartItem.Price,
                    CreatedAt = DateTime.Now
                };

                orderItems.Add(orderItem);
                totalAmount += cartItem.Price * cartItem.Quantity;

                /* Decrease product stock and update product entity */
                var product = _productRepository.Find(cartItem.ProductId);
                if (product != null)
                {
                    product.Stock -= cartItem.Quantity;
                    if (product.Stock < 0) product.Stock = 0;
                    product.UpdatedAt = DateTime.Now;
                    _productRepository.Update(product.ProductId, product);
                }
            }

            /* Apply business rules for tax and shipping */
            totalAmount += Math.Round(totalAmount * 0.05m, 2);
            /* Add shipping cost for orders under $50 */
            if (totalAmount <= 50)
            {
                totalAmount += 5.99m;
            }

            /* Create order entity with all details */
            var order = new Order
            {
                UserId = userId,
                OrderNumber = orderNumber,
                TotalAmount = totalAmount,
                Status = "Pending",
                OrderDate = DateTime.Now,
                ShippingAddress = shippingAddress,
                City = city,
                PhoneNumber = phoneNumber,
                PaymentMethod = paymentMethod,
                IsPaid = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                OrderItems = orderItems
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            /* Clear cart after successful order creation */
            await _cartRepository.ClearCartAsync(userId);

            return order;
        }

        /* Update order status with automatic timestamp handling */
        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);

            if (order == null)
            {
                return false;
            }

            order.Status = status;
            order.UpdatedAt = DateTime.Now;

            /* Automatically set delivery timestamp when status changes to delivered */
            if (status == "Delivered")
            {
                order.DeliveredAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return true;
        }

        /* Generate recent orders as ViewModels for dashboard display */
        public async Task<List<OrderViewModel>> GetRecentOrdersAsync(int count)
        {
            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .Take(count)
                .ToListAsync();

            /* Transform entities to ViewModels with calculated properties */
            return orders.Select(o => new OrderViewModel
            {
                OrderId = o.OrderId,
                OrderNumber = o.OrderNumber,
                CustomerName = $"{o.User.FirstName} {o.User.LastName}",
                CustomerEmail = o.User.Email,
                Status = o.Status,
                TotalAmount = o.TotalAmount,
                OrderDate = o.OrderDate,
                ItemCount = o.OrderItems.Count
            }).ToList();
        }

        /* Generate order statistics dictionary for dashboard metrics */
        public async Task<Dictionary<string, int>> GetOrderStatisticsAsync()
        {
            var allOrders = await _context.Orders.ToListAsync();

            /* Count orders by status using LINQ grouping */
            return new Dictionary<string, int>
            {
                { "Total", allOrders.Count },
                { "Pending", allOrders.Count(o => o.Status == "Pending") },
                { "Processing", allOrders.Count(o => o.Status == "Processing") },
                { "Shipped", allOrders.Count(o => o.Status == "Shipped") },
                { "Delivered", allOrders.Count(o => o.Status == "Delivered") },
                { "Cancelled", allOrders.Count(o => o.Status == "Cancelled") }
            };
        }

        /* Generate unique order number with date and GUID components */
        private string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }

        /* Comprehensive order update including payment and delivery status */
        public async Task UpdateOrderStatusAndPaymentAsync(int orderId, string status, bool isPaid, DateTime? paidAt, DateTime? deliveredAt)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                order.IsPaid = isPaid;
                order.PaidAt = paidAt;
                order.DeliveredAt = deliveredAt;

                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
            }
        }
    }
}