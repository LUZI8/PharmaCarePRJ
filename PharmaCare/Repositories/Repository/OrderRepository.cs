namespace PharmaCare.Repositories.Repository
{
    public class OrderRepository : IOrderRepository
    {
        private readonly DataDbContext _context;
        private readonly ICartRepository _cartRepository;

        public OrderRepository(
            DataDbContext context,
            ICartRepository cartRepository,
            IProductRepository productRepository)
        {
            _context = context;
            _cartRepository = cartRepository;
        }

        public async Task<List<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsSplitQuery()
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<List<Order>> GetUserOrdersAsync(int userId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsSplitQuery()
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order> GetOrderByIdAsync(int orderId)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<Order> GetOrderByNumberAsync(string orderNumber)
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
        }

        public async Task<Order> CreateOrderFromCartAsync(int userId, string shippingAddress, string city, string phoneNumber, string paymentMethod)
        {
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);
            if (cart.CartItems == null || cart.CartItems.Count == 0) return null;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var productIds = cart.CartItems.Select(i => i.ProductId).Distinct().ToList();
                var products = await _context.Product
                    .Where(p => productIds.Contains(p.ProductId))
                    .ToDictionaryAsync(p => p.ProductId);

                var now = DateTime.Now;
                var orderItems = new List<OrderItem>();
                decimal subtotal = 0m;

                foreach (var cartItem in cart.CartItems)
                {
                    if (!products.TryGetValue(cartItem.ProductId, out var product) ||
                        !product.IsActive || product.RequiresPrescription || product.ExpiryDate <= now)
                    {
                        throw new InvalidOperationException($"{cartItem.Product?.ProductName ?? "A product"} is not currently available for online checkout.");
                    }

                    if (cartItem.Quantity <= 0 || product.Stock < cartItem.Quantity)
                    {
                        throw new InvalidOperationException($"Not enough stock for {product.ProductName}. Available: {product.Stock}, Requested: {cartItem.Quantity}.");
                    }

                    // Checkout uses the current catalog price so stale cart snapshots cannot undercharge an order.
                    var unitPrice = product.Price;
                    orderItems.Add(new OrderItem
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        Quantity = cartItem.Quantity,
                        Price = unitPrice,
                        CreatedAt = now
                    });

                    subtotal += unitPrice * cartItem.Quantity;
                    product.Stock -= cartItem.Quantity;
                    product.UpdatedAt = now;
                }

                var tax = Math.Round(subtotal * 0.05m, 2);
                var preShipping = subtotal + tax;
                var shipping = preShipping >= 50m ? 0m : 5.99m;

                var order = new Order
                {
                    UserId = userId,
                    OrderNumber = GenerateOrderNumber(),
                    TotalAmount = preShipping + shipping,
                    Status = "Pending",
                    OrderDate = now,
                    ShippingAddress = shippingAddress,
                    City = city,
                    PhoneNumber = phoneNumber,
                    PaymentMethod = paymentMethod,
                    IsPaid = false,
                    CreatedAt = now,
                    UpdatedAt = now,
                    OrderItems = orderItems
                };

                _context.Orders.Add(order);
                _context.CartItems.RemoveRange(cart.CartItems);
                cart.UpdatedAt = now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return order;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            order.Status = status;
            order.UpdatedAt = DateTime.Now;
            if (status == "Delivered") order.DeliveredAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<OrderViewModel>> GetRecentOrdersAsync(int count)
        {
            return await _context.Orders
                .AsNoTracking()
                .OrderByDescending(o => o.OrderDate)
                .Take(count)
                .Select(o => new OrderViewModel
                {
                    OrderId = o.OrderId,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.User.FirstName + " " + o.User.LastName,
                    CustomerEmail = o.User.Email,
                    Status = o.Status,
                    TotalAmount = o.TotalAmount,
                    OrderDate = o.OrderDate,
                    ItemCount = o.OrderItems.Count
                })
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetOrderStatisticsAsync()
        {
            var grouped = await _context.Orders
                .AsNoTracking()
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            var total = grouped.Values.Sum();
            return new Dictionary<string, int>
            {
                ["Total"] = total,
                ["Pending"] = grouped.GetValueOrDefault("Pending"),
                ["Processing"] = grouped.GetValueOrDefault("Processing"),
                ["Shipped"] = grouped.GetValueOrDefault("Shipped"),
                ["Delivered"] = grouped.GetValueOrDefault("Delivered"),
                ["Cancelled"] = grouped.GetValueOrDefault("Cancelled")
            };
        }

        private string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        }

        public async Task UpdateOrderStatusAndPaymentAsync(int orderId, string status, bool isPaid, DateTime? paidAt, DateTime? deliveredAt)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return;

            order.Status = status;
            order.IsPaid = isPaid;
            order.PaidAt = paidAt;
            order.DeliveredAt = deliveredAt;
            order.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}