namespace PharmaCare.Controllers
{
    public class FrontEndController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ICartRepository _cartRepository;
        private readonly DataDbContext _context;
        private readonly ILogger<FrontEndController> _logger;

        public FrontEndController(
            IUserRepository userRepository,
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            ICartRepository cartRepository,
            DataDbContext context,
            ILogger<FrontEndController> logger)
        {
            _userRepository = userRepository;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _cartRepository = cartRepository;
            _context = context;
            _logger = logger;
        }

        private void LoadCategories()
        {
            var categories = _categoryRepository.View();
            ViewBag.Categories = categories;
        }

        public IActionResult Index() => RedirectToAction("Index", "Marketplace");

        // Legacy storefront route retained only as a compatibility redirect.
        // Updated Shop method to include ExpiryDate and handle expired products
        [HttpGet]
        public IActionResult Shop(int? category, string search, decimal? minPrice, decimal? maxPrice, string sort = "relevance", int page = 1, bool prescriptionOnly = false, bool includeExpired = false)
            => RedirectToAction("Index", "Marketplace", new { q = search, sort = sort == "price-asc" ? "cheapest" : "recommended" });

        // Legacy product-detail route redirects to the marketplace comparison page.
        // Updated ShopSingle method to include ExpiryDate
        public IActionResult ShopSingle(int id) => RedirectToAction("Compare", "Marketplace", new { id });

        public IActionResult About()
        {
            LoadCategories();
            ViewBag.IsLoggedIn = HttpContext.Session.GetInt32("UserId") != null;
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");

            return View();
        }

        public IActionResult Contact(string subject = null)
        {
            LoadCategories();
            ViewBag.IsLoggedIn = HttpContext.Session.GetInt32("UserId") != null;
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");

            ViewBag.PrefilledSubject = subject;

            return View();
        }

        public IActionResult Cart() => RedirectToAction("Index", "MarketplaceCart");

        public IActionResult Checkout() => RedirectToAction("Checkout", "MarketplaceCart");

        public IActionResult ThankYou() => RedirectToAction("Index", "MarketplaceOrders");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitContact(string c_fname, string c_lname, string c_email, string c_subject, string c_message)
        {
            try
            {
                var contactMessage = new ContactMessage
                {
                    FirstName = c_fname,
                    LastName = c_lname,
                    Email = c_email,
                    Subject = c_subject,
                    Message = c_message,
                    DateSubmitted = DateTime.UtcNow
                };

                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId.HasValue)
                {
                    contactMessage.UserId = userId.Value;
                    contactMessage.UserType = "User";
                }
                else
                {
                    contactMessage.UserType = "non-user";
                }

                _context.ContactMessages.Add(contactMessage);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thank you for your message. We will get back to you shortly.";
                return RedirectToAction("Contact");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting contact message");
                TempData["ErrorMessage"] = "An error occurred while submitting your message. Please try again.";
                return RedirectToAction("Contact");
            }
        }

        [HttpGet]
        public JsonResult SearchProducts(string query, decimal? minPrice, decimal? maxPrice, int? categoryId, string sort = "relevance", string categoryNames = null, bool includeExpired = false)
        {
            var products = _productRepository.View();
            var initialProducts = products.ToList();
            var filteredProducts = initialProducts.AsQueryable();

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                filteredProducts = filteredProducts.Where(p => p.CategoryID == categoryId.Value);
            }

            if (minPrice.HasValue)
            {
                filteredProducts = filteredProducts.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                filteredProducts = filteredProducts.Where(p => p.Price <= maxPrice.Value);
            }

            // Filter active products and optionally expired products
            filteredProducts = filteredProducts.Where(p => p.IsActive);

            if (!includeExpired)
            {
                filteredProducts = filteredProducts.Where(p => p.ExpiryDate.Date > DateTime.Now.Date);
            }

            if (!string.IsNullOrEmpty(categoryNames))
            {
                // Convert comma-separated category names to IDs for filtering products by multiple categories
                var categoryNameList = categoryNames.Split(',').Select(c => c.Trim()).ToList();
                var categoryIds = _categoryRepository.View()
                    .Where(c => categoryNameList.Contains(c.CategoryName))
                    .Select(c => c.CategoryID)
                    .ToList();

                if (categoryIds.Any())
                {
                    filteredProducts = filteredProducts.Where(p => categoryIds.Contains(p.CategoryID));
                }
            }

            var scoredProducts = new List<(Product Product, int Score)>();

            // Implement relevance scoring algorithm for search queries with weighted scoring system
            if (!string.IsNullOrEmpty(query))
            {
                var terms = query.ToLower().Split(' ').Where(t => t.Length > 1).ToArray();

                foreach (var product in filteredProducts)
                {
                    int score = 0;
                    var productName = product.ProductName != null ? product.ProductName.ToLower() : "";
                    var description = product.Description != null ? product.Description.ToLower() : "";
                    var categoryName = GetCategoryName(product.CategoryID).ToLower();

                    if (productName.Equals(query.ToLower()))
                    {
                        score += 100;
                    }
                    else if (productName.StartsWith(query.ToLower()))
                    {
                        score += 75;
                    }
                    else if (productName.Contains(query.ToLower()))
                    {
                        score += 50;
                    }

                    if (categoryName.Contains(query.ToLower()))
                    {
                        score += 40;
                    }

                    foreach (var term in terms)
                    {
                        if (productName.Contains(term))
                        {
                            score += 20;
                        }

                        if (description.Contains(term))
                        {
                            score += 10;
                        }

                        if (categoryName.Contains(term))
                        {
                            score += 15;
                        }
                    }

                    if (score > 0)
                    {
                        scoredProducts.Add((product, score));
                    }
                }

                filteredProducts = scoredProducts.OrderByDescending(x => x.Score).Select(x => x.Product).AsQueryable();
            }

            // Apply sorting logic for non-relevance based sorting options
            if (!string.IsNullOrEmpty(query) && sort == "relevance")
            {
                // Already sorted by relevance above
            }
            else
            {
                switch (sort)
                {
                    case "name-asc":
                        filteredProducts = filteredProducts.OrderBy(p => p.ProductName);
                        break;
                    case "name-desc":
                        filteredProducts = filteredProducts.OrderByDescending(p => p.ProductName);
                        break;
                    case "price-asc":
                        filteredProducts = filteredProducts.OrderBy(p => p.Price);
                        break;
                    case "price-desc":
                        filteredProducts = filteredProducts.OrderByDescending(p => p.Price);
                        break;
                    case "expiry-asc":
                        filteredProducts = filteredProducts.OrderBy(p => p.ExpiryDate);
                        break;
                    case "expiry-desc":
                        filteredProducts = filteredProducts.OrderByDescending(p => p.ExpiryDate);
                        break;
                    case "relevance":
                    default:
                        if (string.IsNullOrEmpty(query))
                        {
                            filteredProducts = filteredProducts.OrderByDescending(p => p.CreatedAt);
                        }
                        break;
                }
            }

            var results = filteredProducts.Select(p => new {
                id = p.ProductId,
                name = p.ProductName,
                price = p.Price,
                image = string.IsNullOrEmpty(p.ImageUrl) ? "/assets/images/product_01.png" : p.ImageUrl,
                stock = p.Stock,
                category = GetCategoryName(p.CategoryID),
                categoryId = p.CategoryID,
                description = p.Description,
                expiryDate = p.ExpiryDate.ToString("yyyy-MM-dd"),
                isExpired = p.ExpiryDate.Date < DateTime.Now.Date,
                isExpiringSoon = p.ExpiryDate.Date >= DateTime.Now.Date && (p.ExpiryDate.Date - DateTime.Now.Date).Days <= 30,
                daysUntilExpiry = (p.ExpiryDate.Date - DateTime.Now.Date).Days
            }).ToList();
            return Json(results);
        }
        private string GetCategoryName(int categoryId)
        {
            var category = _categoryRepository.Find(categoryId);
            return category?.CategoryName ?? "Uncategorized";
        }

        [HttpGet]
        public JsonResult GetCategories()
        {
            var categories = _categoryRepository.View();
            return Json(categories);
        }
    }
}