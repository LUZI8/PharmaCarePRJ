namespace PharmaCare.Controllers
{
    public class OrderController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICartRepository _cartRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;

        public OrderController(
            IOrderRepository orderRepository,
            ICartRepository cartRepository,
            IUserRepository userRepository,
            ICategoryRepository categoryRepository,
            IProductRepository productRepository)
        {
            _orderRepository = orderRepository;
            _cartRepository = cartRepository;
            _userRepository = userRepository;
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
        }

        private void LoadCategories()
        {
            var categories = _categoryRepository.View();
            ViewBag.Categories = categories;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            // Set cache control headers to prevent browser caching of sensitive order information
            context.HttpContext.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.HttpContext.Response.Headers["Pragma"] = "no-cache";
            context.HttpContext.Response.Headers["Expires"] = "0";

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                context.Result = RedirectToAction("Login", "Account");
            }
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            LoadCategories();

            ViewBag.IsLoggedIn = true;
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserRole = userRole;
            ViewBag.HideCategories = false;

            // Fetch orders based on user role - admins see all orders, regular users see only their own
            var orders = userRole == "Admin"
                ? await _orderRepository.GetAllOrdersAsync()
                : await _orderRepository.GetUserOrdersAsync(userId.Value);

            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            LoadCategories();

            var order = await _orderRepository.GetOrderByIdAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            // Implement security check to ensure only authorized users can view order details
            if (userRole != "Admin" && userRole != "Pharmacist" && order.UserId != userId)
            {
                TempData["ErrorMessage"] = "You are not authorized to view this order.";
                return RedirectToAction("Index");
            }

            ViewBag.IsLoggedIn = true;
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserRole = userRole;
            ViewBag.HideCategories = false;

            return View(order);
        }

        public async Task<IActionResult> Checkout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            LoadCategories();

            var cartViewModel = await _cartRepository.GetCartViewModelAsync(userId.Value);

            if (cartViewModel.ItemCount == 0)
            {
                TempData["ErrorMessage"] = "Your cart is empty!";
                return RedirectToAction("Index", "Cart");
            }

            var user = await _userRepository.GetByIdAsync(userId.Value);

            ViewBag.IsLoggedIn = true;
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            ViewBag.HideCategories = false;

            ViewBag.User = user;
            ViewBag.Cart = cartViewModel;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(string shippingAddress, string city, string phoneNumber, string paymentMethod)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(shippingAddress) || string.IsNullOrEmpty(city) ||
                string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(paymentMethod))
            {
                TempData["ErrorMessage"] = "Please fill all required fields";
                return RedirectToAction("Checkout");
            }

            try
            {
                var cartViewModel = await _cartRepository.GetCartViewModelAsync(userId.Value);

                if (cartViewModel.ItemCount == 0)
                {
                    TempData["ErrorMessage"] = "Your cart is empty!";
                    return RedirectToAction("Index", "Cart");
                }

                var order = await _orderRepository.CreateOrderFromCartAsync(
                    userId.Value, shippingAddress, city, phoneNumber, paymentMethod);

                if (order == null)
                {
                    TempData["ErrorMessage"] = "Failed to create order!";
                    return RedirectToAction("Index", "Cart");
                }

                TempData["OrderSuccessMessage"] = "Order placed successfully!";
                TempData["OrderNumber"] = order.OrderNumber;

                return RedirectToAction("ThankYou", new { id = order.OrderId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error placing order: {ex.Message}";
                return RedirectToAction("Checkout");
            }
        }

        private async Task UpdateProductStockAsync(ICollection<OrderItem> orderItems)
        {
            foreach (var item in orderItems)
            {
                var product = _productRepository.Find(item.ProductId);
                if (product != null)
                {
                    // Decrease product stock by ordered quantity and prevent negative stock values
                    product.Stock -= item.Quantity;

                    if (product.Stock < 0)
                    {
                        product.Stock = 0;
                    }

                    _productRepository.Update(product.ProductId, product);
                }
            }
        }

        public async Task<IActionResult> ThankYou(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            LoadCategories();

            var order = await _orderRepository.GetOrderByIdAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            ViewBag.IsLoggedIn = true;
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            ViewBag.HideCategories = false;

            ViewBag.OrderSuccessMessage = TempData["OrderSuccessMessage"];

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, string status, string returnUrl = null)
        {
            // Restrict order status updates to Admin and Pharmacist roles only
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin" && userRole != "Pharmacist")
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "You are not authorized to update order status." });
                }
                TempData["ErrorMessage"] = "You are not authorized to update order status.";
                return RedirectToAction("Details", new { id = orderId });
            }

            try
            {
                var order = await _orderRepository.GetOrderByIdAsync(orderId);
                if (order == null)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = "Order not found." });
                    }
                    TempData["ErrorMessage"] = "Order not found.";
                    return RedirectToAction("Details", new { id = orderId });
                }

                // Automatically set payment status and delivery timestamps based on order status
                bool isPaid = false;
                DateTime? paidAt = null;
                DateTime? deliveredAt = null;

                if (status.Equals("Delivered", StringComparison.OrdinalIgnoreCase))
                {
                    isPaid = true;
                    paidAt = DateTime.Now;
                    deliveredAt = DateTime.Now;
                }
                else
                {
                    isPaid = false;
                    paidAt = null;
                    deliveredAt = null;
                }

                await _orderRepository.UpdateOrderStatusAndPaymentAsync(orderId, status, isPaid, paidAt, deliveredAt);

                // Handle both AJAX and regular form submissions with appropriate response formats
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var statusClass = status.ToLower() switch
                    {
                        "pending" => "warning",
                        "processing" => "info",
                        "shipped" => "primary",
                        "delivered" => "success",
                        "cancelled" => "danger",
                        _ => "secondary"
                    };

                    return Json(new
                    {
                        success = true,
                        message = "Order status updated successfully!",
                        newStatus = status,
                        statusClass = statusClass
                    });
                }

                TempData["SuccessMessage"] = "Order status updated successfully!";

                if (!string.IsNullOrEmpty(returnUrl) && returnUrl.Contains("ManageOrders"))
                {
                    return RedirectToAction("ManageOrders");
                }

                return RedirectToAction("Details", new { id = orderId });
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = $"Error updating order status: {ex.Message}" });
                }

                TempData["ErrorMessage"] = $"Error updating order status: {ex.Message}";
                return RedirectToAction("Details", new { id = orderId });
            }
        }

        public async Task<IActionResult> ManageOrders()
        {
            // Restrict access to order management for Admin and Pharmacist roles only
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin" && userRole != "Pharmacist")
            {
                TempData["ErrorMessage"] = "You are not authorized to access this page.";
                return RedirectToAction("Login", "Account");
            }

            LoadCategories();

            var orders = await _orderRepository.GetAllOrdersAsync();
            var orderStatistics = await _orderRepository.GetOrderStatisticsAsync();

            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
            ViewBag.UserRole = userRole;
            ViewBag.OrderStatistics = orderStatistics;
            ViewBag.HideCategories = false;

            return View(orders);
        }

        [HttpGet]
        public JsonResult GetCategories()
        {
            var categories = _categoryRepository.View();
            return Json(categories);
        }
    }
}