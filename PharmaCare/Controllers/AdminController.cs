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
                var expiryThreshold = today.AddDays(90);

                var activeProducts = await _context.Product
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Stock)
                    .ToListAsync();

                var orders = await _context.Orders.AsNoTracking().ToListAsync();

                var viewModel = new DashboardViewModel
                {
                    Products = activeProducts,
                    RecentOrders = await _orderRepository.GetRecentOrdersAsync(5),
                    OrderCount = orders.Count,
                    InventoryCount = activeProducts.Count,
                    CustomerCount = await _context.User.CountAsync(u => u.Role == "Customer" && u.IsActive),
                    LowStockCount = activeProducts.Count(p => p.Stock > 0 && p.Stock <= 10),
                    OutOfStockCount = activeProducts.Count(p => p.Stock <= 0),
                    ExpiringSoonCount = activeProducts.Count(p => p.ExpiryDate >= today && p.ExpiryDate <= expiryThreshold),
                    PendingPickupCount = await _context.PrescriptionReservations.CountAsync(r => r.Status == "Reserved"),
                    FeedbackCount = await _context.ContactMessages.CountAsync(),
                    OrdersToday = orders.Count(o => o.OrderDate.Date == today),
                    TotalRevenue = orders.Where(o => o.Status != "Cancelled").Sum(o => o.TotalAmount),
                    RevenueToday = orders.Where(o => o.Status != "Cancelled" && o.OrderDate.Date == today).Sum(o => o.TotalAmount),
                    LowStockProducts = activeProducts.Where(p => p.Stock <= 10).Take(5).ToList(),
                    ExpiringProducts = activeProducts.Where(p => p.ExpiryDate >= today && p.ExpiryDate <= expiryThreshold)
                        .OrderBy(p => p.ExpiryDate).Take(5).ToList()
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
                var messages = await _context.ContactMessages.Include(m => m.User)
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
