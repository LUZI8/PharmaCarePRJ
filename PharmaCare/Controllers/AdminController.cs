namespace PharmaCare.Controllers
{
    /* Admin controller with response caching disabled for security */
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class AdminController : Controller
    {
        private readonly DataDbContext _context;
        private readonly ILogger<AdminController> _logger;
        private readonly IOrderRepository _orderRepository;

        /* Constructor with dependency injection for database context, logging, and order operations */
        public AdminController(DataDbContext context, ILogger<AdminController> logger, IOrderRepository orderRepository)
        {
            _context = context;
            _logger = logger;
            _orderRepository = orderRepository;
        }

        /* Global action filter for admin authorization and cache prevention */
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            /* Role-based access control - Admin and Pharmacist only */
            var userRole = HttpContext.Session.GetString("UserRole");
            Console.WriteLine($"Admin access check: UserRole = {userRole}");

            if (string.IsNullOrEmpty(userRole) || (userRole != "Admin" && userRole != "Pharmacist"))
            {
                Console.WriteLine("Access denied to Admin controller - redirecting to login");

                /* Prevent caching of secure pages after logout */
                context.HttpContext.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                context.HttpContext.Response.Headers["Pragma"] = "no-cache";
                context.HttpContext.Response.Headers["Expires"] = "0";

                context.Result = Redirect("/Account/Login");
            }

            /* Apply cache prevention to authentication-related actions */
            var actionName = context.ActionDescriptor.RouteValues["action"]?.ToLower();
            if (actionName == "logout" || actionName == "login")
            {
                context.HttpContext.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
                context.HttpContext.Response.Headers["Pragma"] = "no-cache";
                context.HttpContext.Response.Headers["Expires"] = "0";
            }
        }

        /* Admin dashboard with comprehensive data aggregation */
        public IActionResult Index()
        {
            try
            {
                Console.WriteLine("Admin Index action executing");

                /* Double-check authorization for critical admin functions */
                var userRole = HttpContext.Session.GetString("UserRole");
                if (string.IsNullOrEmpty(userRole) || (userRole != "Admin" && userRole != "Pharmacist"))
                {
                    return Redirect("/Account/Login");
                }

                /* Initialize dashboard ViewModel with products list - ONLY show active products in dashboard */
                var viewModel = new DashboardViewModel
                {
                    Products = _context.Product.Where(p => p.IsActive).ToList()
                };

                try
                {
                    /* Load dashboard statistics with error handling */
                    viewModel.RecentOrders = _orderRepository.GetRecentOrdersAsync(5).Result;
                    viewModel.OrderCount = _context.Orders.Count();
                    viewModel.InventoryCount = _context.Product.Where(p => p.IsActive).Count(); // Only count active products

                    /* Load order statistics for dashboard metrics */
                    ViewBag.OrderStatistics = _orderRepository.GetOrderStatisticsAsync().Result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading order data: {ex.Message}");
                    _logger.LogError(ex, "Error loading order data");
                }

                /* Set ViewBag data for layout display */
                ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
                ViewBag.UserRole = userRole;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Admin Index: {ex.Message}");
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        /* Privacy page with role validation */
        public IActionResult Privacy()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole) || (userRole != "Admin" && userRole != "Pharmacist"))
            {
                return Redirect("/Account/Login");
            }

            ViewBag.GG = "this is privacy page";
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
            return View();
        }

        /* Error page with response caching disabled */
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /* Feedback management with role-based content filtering */
        public async Task<IActionResult> Feedback()
        {
            try
            {
                var userRole = HttpContext.Session.GetString("UserRole");
                if (string.IsNullOrEmpty(userRole) || (userRole != "Admin" && userRole != "Pharmacist"))
                {
                    return Redirect("/Account/Login");
                }

                /* Load contact messages with user information */
                var messages = await _context.ContactMessages
                    .Include(m => m.User)
                    .OrderByDescending(m => m.DateSubmitted)
                    .ToListAsync();

                /* Filter sensitive messages from Pharmacist view */
                if (userRole == "Pharmacist")
                {
                    messages = messages.Where(m =>
                        !(m.Subject?.ToLower().Contains("password reset") == true ||
                          m.Subject?.ToLower().Contains("password reset request") == true ||
                          m.Subject?.ToLower().Trim() == "password reset request")
                    ).ToList();
                }

                ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
                ViewBag.UserRole = userRole;
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