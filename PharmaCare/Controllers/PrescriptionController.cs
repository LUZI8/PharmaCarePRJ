namespace PharmaCare.Controllers
{
    public class PrescriptionController : Controller
    {
        private readonly DataDbContext _context;
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUserRepository _userRepository;
        private readonly IExpiredReservationsService _expiredReservationsService;

        public PrescriptionController(
            DataDbContext context,
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IUserRepository userRepository,
            IExpiredReservationsService expiredReservationsService)
        {
            _context = context;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _userRepository = userRepository;
            _expiredReservationsService = expiredReservationsService;
        }

        private void LoadCategories()
        {
            var categories = _categoryRepository.View();
            ViewBag.Categories = categories;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            // Prevent browser caching of prescription pages for security and data freshness
            context.HttpContext.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            context.HttpContext.Response.Headers["Pragma"] = "no-cache";
            context.HttpContext.Response.Headers["Expires"] = "0";

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                context.Result = RedirectToAction("Login", "Account");
            }
        }

        public async Task<IActionResult> Confirm(int id, int quantity = 1)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("ShopSingle", "FrontEnd", new { id = id }) });
            }

            LoadCategories();

            var product = _productRepository.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            // Verify product actually requires prescription before proceeding with reservation
            if (!product.RequiresPrescription)
            {
                return RedirectToAction("ShopSingle", "FrontEnd", new { id = id });
            }

            if (Request.Query.ContainsKey("quantity"))
            {
                int.TryParse(Request.Query["quantity"], out quantity);
                if (quantity < 1) quantity = 1;
            }

            if (product.Stock < quantity)
            {
                TempData["ErrorMessage"] = $"Not enough stock available. Only {product.Stock} units in stock.";
                return RedirectToAction("ShopSingle", "FrontEnd", new { id = id });
            }

            var user = await _userRepository.GetByIdAsync(userId.Value);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new PrescriptionConfirmViewModel
            {
                Product = product,
                Quantity = quantity,
                TotalPrice = product.Price * quantity,
                User = user,
                ExpiryDate = DateTime.Now.AddDays(3)
            };

            ViewBag.IsLoggedIn = true;
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReservation(int productId, int quantity, string phoneNumber)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var product = _productRepository.Find(productId);
            if (product == null)
            {
                return NotFound();
            }

            if (!product.RequiresPrescription)
            {
                TempData["ErrorMessage"] = "This product does not require a prescription.";
                return RedirectToAction("ShopSingle", "FrontEnd", new { id = productId });
            }

            if (quantity <= 0)
            {
                TempData["ErrorMessage"] = "Please select a valid quantity.";
                return RedirectToAction("Confirm", new { id = productId });
            }

            if (product.Stock < quantity)
            {
                TempData["ErrorMessage"] = "Not enough stock available.";
                return RedirectToAction("Confirm", new { id = productId });
            }

            try
            {
                string reservationNumber = GenerateReservationNumber();

                var reservation = new PrescriptionReservation
                {
                    UserId = userId.Value,
                    ProductId = productId,
                    Quantity = quantity,
                    ReservationNumber = reservationNumber,
                    Status = "Reserved",
                    ReservationDate = DateTime.Now,
                    ExpiryDate = DateTime.Now.AddDays(3),
                    Notes = !string.IsNullOrEmpty(product.PrescriptionNote) ?
                           product.PrescriptionNote :
                           "Please bring your prescription to the pharmacy to complete this purchase."
                };

                _context.PrescriptionReservations.Add(reservation);

                // Immediately reduce product stock to prevent overselling of reserved items
                product.Stock -= quantity;
                _productRepository.Update(product.ProductId, product);

                await _context.SaveChangesAsync();

                return RedirectToAction("ReservationComplete", new { id = reservation.ReservationId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating reservation: {ex.Message}";
                return RedirectToAction("Confirm", new { id = productId });
            }
        }

        public async Task<IActionResult> ReservationComplete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            LoadCategories();

            var reservation = await _context.PrescriptionReservations
                .Include(r => r.Product)
                .FirstOrDefaultAsync(r => r.ReservationId == id && r.UserId == userId.Value);

            if (reservation == null)
            {
                return NotFound();
            }

            ViewBag.IsLoggedIn = true;
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");

            return View(reservation);
        }

        [HttpPost]
        public async Task<IActionResult> Reserve(int productId, int quantity)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "You must be logged in to reserve prescription items." });
            }

            var product = _productRepository.Find(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found." });
            }

            if (!product.RequiresPrescription)
            {
                return Json(new { success = false, message = "This product does not require a prescription." });
            }

            if (quantity <= 0)
            {
                return Json(new { success = false, message = "Please select a valid quantity." });
            }

            if (product.Stock < quantity)
            {
                return Json(new { success = false, message = "Not enough stock available." });
            }

            try
            {
                string reservationNumber = GenerateReservationNumber();

                var reservation = new PrescriptionReservation
                {
                    UserId = userId.Value,
                    ProductId = productId,
                    Quantity = quantity,
                    ReservationNumber = reservationNumber,
                    Status = "Reserved",
                    ReservationDate = DateTime.Now,
                    ExpiryDate = DateTime.Now.AddDays(3),
                    Notes = !string.IsNullOrEmpty(product.PrescriptionNote) ?
                           product.PrescriptionNote :
                           "Please bring your prescription to the pharmacy to complete this purchase."
                };

                _context.PrescriptionReservations.Add(reservation);

                product.Stock -= quantity;
                _productRepository.Update(product.ProductId, product);

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Prescription medication reserved successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error reserving prescription: {ex.Message}" });
            }
        }

        public async Task<IActionResult> MyReservations()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            LoadCategories();

            var reservations = await _context.PrescriptionReservations
                .Include(r => r.Product)
                .Where(r => r.UserId == userId.Value)
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();

            ViewBag.IsLoggedIn = true;
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");

            return View(reservations);
        }

        public async Task<IActionResult> CancelReservation(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var reservation = await _context.PrescriptionReservations
                .Include(r => r.Product)
                .FirstOrDefaultAsync(r => r.ReservationId == id && r.UserId == userId.Value);

            if (reservation == null)
            {
                TempData["ErrorMessage"] = "Reservation not found.";
                return RedirectToAction("MyReservations");
            }

            reservation.Status = "Cancelled";

            // Return reserved stock back to product inventory when reservation is cancelled
            if (reservation.Product != null)
            {
                reservation.Product.Stock += reservation.Quantity;
                _productRepository.Update(reservation.Product.ProductId, reservation.Product);
            }

            _context.PrescriptionReservations.Update(reservation);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Reservation cancelled successfully.";
            return RedirectToAction("MyReservations");
        }

        public async Task<IActionResult> ManageReservations()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin" && userRole != "Pharmacist")
            {
                return RedirectToAction("Login", "Account");
            }

            LoadCategories();

            var reservations = await _context.PrescriptionReservations
                .Include(r => r.Product)
                .Include(r => r.User)
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();

            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
            ViewBag.UserRole = userRole;

            return View(reservations);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateReservationStatus(int id, string status)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin" && userRole != "Pharmacist")
            {
                return RedirectToAction("Login", "Account");
            }

            var reservation = await _context.PrescriptionReservations
                .Include(r => r.Product)
                .FirstOrDefaultAsync(r => r.ReservationId == id);

            if (reservation == null)
            {
                TempData["ErrorMessage"] = "Reservation not found.";
                return RedirectToAction("ManageReservations");
            }

            var oldStatus = reservation.Status;
            reservation.Status = status;

            if (status == "Completed")
            {
                reservation.CompletedDate = DateTime.Now;
            }

            // Return stock to inventory when cancelling a previously reserved item
            if (oldStatus == "Reserved" && status == "Cancelled")
            {
                if (reservation.Product != null)
                {
                    reservation.Product.Stock += reservation.Quantity;
                    _productRepository.Update(reservation.Product.ProductId, reservation.Product);
                }
            }

            _context.PrescriptionReservations.Update(reservation);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Reservation status updated successfully.";
            return RedirectToAction("ManageReservations");
        }

        private string GenerateReservationNumber()
        {
            // Generate unique reservation number with date prefix and random identifier for tracking
            return $"RX-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }

        [HttpPost]
        public async Task<IActionResult> CancelReservationAdmin(int id)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin" && userRole != "Pharmacist")
            {
                return RedirectToAction("Login", "Account");
            }

            var reservation = await _context.PrescriptionReservations
                .Include(r => r.Product)
                .FirstOrDefaultAsync(r => r.ReservationId == id);

            if (reservation == null)
            {
                TempData["ErrorMessage"] = "Reservation not found.";
                return RedirectToAction("Pickups");
            }

            reservation.Status = "Cancelled";

            if (reservation.Product != null)
            {
                reservation.Product.Stock += reservation.Quantity;
                _productRepository.Update(reservation.Product.ProductId, reservation.Product);
            }

            _context.PrescriptionReservations.Update(reservation);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Reservation cancelled successfully.";
            return RedirectToAction("Pickups");
        }

        public async Task<IActionResult> Pickups()
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin" && userRole != "Pharmacist")
            {
                return RedirectToAction("Login", "Account");
            }

            // Check and process expired reservations before displaying current pickup list
            await _expiredReservationsService.CheckExpiredReservationsNow();

            LoadCategories();

            var reservations = await _context.PrescriptionReservations
                .Include(r => r.Product)
                .Include(r => r.User)
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();

            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
            ViewBag.UserRole = userRole;

            return View(reservations);
        }
    }
}