namespace PharmaCare.Controllers
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class AdminController : Controller
    {
        private readonly DataDbContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly IOrderRepository _orderRepository;

        public AdminController(DataDbContext context, ILogger<AdminController> logger, IOrderRepository orderRepository)
        {
            _context = context;
            _logger = logger;
            _orderRepository = orderRepository;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);
            var userRole = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(userRole) || (userRole != "Admin" && userRole != "Pharmacist"))
            {
                context.HttpContext.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                context.HttpContext.Response.Headers["Pragma"] = "no-cache";
                context.HttpContext.Response.Headers["Expires"] = "0";
                context.Result = Redirect("/Account/Login");
            }
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userRole = HttpContext.Session.GetString("UserRole");
                if (string.IsNullOrEmpty(userRole) || (userRole != "Admin" && userRole != "Pharmacist"))
                    return Redirect("/Account/Login");

                var now = DateTime.Now;
                var today = now.Date;
                var tomorrow = today.AddDays(1);
                var expiryThreshold = today.AddDays(90);

                var activeProducts = await _context.Product.AsNoTracking()
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Stock)
                    .ToListAsync();

                // Keep dashboard aggregates in SQL instead of loading the full orders table into memory.
                var orderCountTask = _context.Orders.AsNoTracking().CountAsync();
                var ordersTodayTask = _context.Orders.AsNoTracking().CountAsync(o => o.OrderDate >= today && o.OrderDate < tomorrow);
                var totalRevenueTask = _context.Orders.AsNoTracking().Where(o => o.Status != "Cancelled")
                    .SumAsync(o => (decimal?)o.TotalAmount);
                var revenueTodayTask = _context.Orders.AsNoTracking()
                    .Where(o => o.Status != "Cancelled" && o.OrderDate >= today && o.OrderDate < tomorrow)
                    .SumAsync(o => (decimal?)o.TotalAmount);
                var customerCountTask = _context.User.AsNoTracking().CountAsync(u => u.Role == "Customer" && u.IsActive);
                var pickupCountTask = _context.PrescriptionReservations.AsNoTracking().CountAsync(r => r.Status == "Reserved");
                var feedbackCountTask = _context.ContactMessages.AsNoTracking().CountAsync();

                await Task.WhenAll(orderCountTask, ordersTodayTask, totalRevenueTask, revenueTodayTask, customerCountTask, pickupCountTask, feedbackCountTask);

                var lowStock = activeProducts.Where(p => p.Stock > 0 && p.Stock <= Math.Max(10, p.ReorderLevel)).ToList();
                var outOfStock = activeProducts.Where(p => p.Stock <= 0).ToList();
                var expiringSoon = activeProducts
                    .Where(p => p.ExpiryDate >= today && p.ExpiryDate <= expiryThreshold)
                    .OrderBy(p => p.ExpiryDate)
                    .ToList();

                var viewModel = new DashboardViewModel
                {
                    Products = activeProducts,
                    RecentOrders = await _orderRepository.GetRecentOrdersAsync(5),
                    OrderCount = orderCountTask.Result,
                    InventoryCount = activeProducts.Count,
                    CustomerCount = customerCountTask.Result,
                    LowStockCount = lowStock.Count,
                    OutOfStockCount = outOfStock.Count,
                    ExpiringSoonCount = expiringSoon.Count,
                    PendingPickupCount = pickupCountTask.Result,
                    FeedbackCount = feedbackCountTask.Result,
                    OrdersToday = ordersTodayTask.Result,
                    TotalRevenue = totalRevenueTask.Result ?? 0m,
                    RevenueToday = revenueTodayTask.Result ?? 0m,
                    LowStockProducts = outOfStock.Concat(lowStock).Take(5).ToList(),
                    ExpiringProducts = expiringSoon.Take(5).ToList()
                };

                ViewBag.OrderStatistics = await _orderRepository.GetOrderStatisticsAsync();
                ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
                ViewBag.UserRole = userRole;
                ViewBag.FeedbackCount = viewModel.FeedbackCount;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        public IActionResult Privacy()
        {
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> Feedback()
        {
            try
            {
                var userRole = HttpContext.Session.GetString("UserRole");
                var messages = await _context.ContactMessages.AsNoTracking().Include(m => m.User)
                    .OrderByDescending(m => m.DateSubmitted).ToListAsync();

                if (userRole == "Pharmacist")
                {
                    messages = messages.Where(m =>
                        !(m.Subject?.ToLower().Contains("password reset") == true ||
                          m.Subject?.ToLower().Trim() == "password reset request")).ToList();
                }

                ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
                ViewBag.UserRole = userRole;
                ViewBag.FeedbackCount = messages.Count;
                return View(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedback messages");
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }
    }
}
