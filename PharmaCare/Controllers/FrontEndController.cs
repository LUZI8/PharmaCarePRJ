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

        public IActionResult Index()
        {
            LoadCategories();

            // Filter active products excluding prescription medicines AND expired products for homepage featured section
            var allProducts = _productRepository.View();
            var featuredProducts = allProducts
                .Where(p => p.IsActive && !p.RequiresPrescription && p.ExpiryDate.Date > DateTime.Now.Date) // Exclude expired products
                .OrderByDescending(p => p.CreatedAt)
                .Take(6)
                .ToList();

            List<ProductViewModel> productViewModels = new List<ProductViewModel>();
            foreach (var product in featuredProducts)
            {
                ProductViewModel model = new ProductViewModel
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    CategoryID = product.CategoryID,
                    Description = product.Description,
                    Price = product.Price,
                    Stock = product.Stock,
                    ImageUrl = product.ImageUrl,
                    IsActive = product.IsActive,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt,
                    RequiresPrescription = product.RequiresPrescription,
                    PrescriptionNote = product.PrescriptionNote,
                    ExpiryDate = product.ExpiryDate // Include expiry date
                };

                model.Category = _categoryRepository.Find(product.CategoryID);
                productViewModels.Add(model);
            }

            ViewBag.IsLoggedIn = HttpContext.Session.GetInt32("UserId") != null;
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
            ViewBag.FeaturedProducts = productViewModels;

            return View();
        }

        // Updated Shop method to include ExpiryDate and handle expired products
        [HttpGet]
        public IActionResult Shop(int? category, string search, decimal? minPrice, decimal? maxPrice, string sort = "relevance", int page = 1, bool prescriptionOnly = false, bool includeExpired = false)
        {
            LoadCategories();

            // Validate and correct invalid price range inputs to prevent system errors
            if (minPrice.HasValue && minPrice.Value < 1)
            {
                minPrice = 1;
            }

            if (maxPrice.HasValue && maxPrice.Value < 1)
            {
                maxPrice = 1;
            }

            // Swap min and max prices if user entered them in wrong order
            if (minPrice.HasValue && maxPrice.HasValue && minPrice.Value > maxPrice.Value)
            {
                var temp = minPrice;
                minPrice = maxPrice;
                maxPrice = temp;
            }

            int pageSize = 9;
            var products = _productRepository.View();

            // IMPORTANT: Filter out inactive products and expired products (unless specifically requested)
            var activeProducts = products.Where(p => p.IsActive).ToList();

            // Filter expired products unless specifically requested to include them
            if (!includeExpired)
            {
                activeProducts = activeProducts.Where(p => p.ExpiryDate.Date > DateTime.Now.Date).ToList();
            }

            List<Product> filteredProducts;

            // Calculate search relevance scores when searching to show most relevant results first
            if (!string.IsNullOrEmpty(search) && sort == "relevance")
            {
                var scoredProducts = new List<(Product Product, int Score)>();
                var terms = search.ToLower().Split(' ').Where(t => t.Length > 1).ToArray();

                foreach (var product in activeProducts)
                {
                    int score = 0;
                    var productName = product.ProductName?.ToLower() ?? "";
                    var description = product.Description?.ToLower() ?? "";

                    // Score exact matches highest, then prefix matches, then contains matches
                    if (productName.Equals(search.ToLower()))
                    {
                        score += 100;
                    }
                    else if (productName.StartsWith(search.ToLower()))
                    {
                        score += 75;
                    }
                    else if (productName.Contains(search.ToLower()))
                    {
                        score += 50;
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
                    }

                    if (score > 0)
                    {
                        scoredProducts.Add((product, score));
                    }
                }

                filteredProducts = scoredProducts.OrderByDescending(x => x.Score).Select(x => x.Product).ToList();
            }
            else
            {
                filteredProducts = activeProducts.ToList();

                if (!string.IsNullOrEmpty(search))
                {
                    filteredProducts = filteredProducts.Where(p =>
                        p.ProductName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        (p.Description != null && p.Description.Contains(search, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                }

                // Apply different sorting options based on user selection
                switch (sort)
                {
                    case "name-asc":
                        filteredProducts = filteredProducts.OrderBy(p => p.ProductName).ToList();
                        break;
                    case "name-desc":
                        filteredProducts = filteredProducts.OrderByDescending(p => p.ProductName).ToList();
                        break;
                    case "price-asc":
                        filteredProducts = filteredProducts.OrderBy(p => p.Price).ToList();
                        break;
                    case "price-desc":
                        filteredProducts = filteredProducts.OrderByDescending(p => p.Price).ToList();
                        break;
                    case "expiry-asc":
                        filteredProducts = filteredProducts.OrderBy(p => p.ExpiryDate).ToList();
                        break;
                    case "expiry-desc":
                        filteredProducts = filteredProducts.OrderByDescending(p => p.ExpiryDate).ToList();
                        break;
                    case "relevance":
                    default:
                        if (string.IsNullOrEmpty(search))
                        {
                            filteredProducts = filteredProducts.OrderByDescending(p => p.CreatedAt).ToList();
                        }
                        break;
                }
            }

            // Apply category, price range, and prescription filters to narrow down results
            if (category.HasValue && category.Value > 0)
            {
                filteredProducts = filteredProducts.Where(p => p.CategoryID == category.Value).ToList();
            }

            if (minPrice.HasValue && minPrice.Value >= 1)
            {
                filteredProducts = filteredProducts.Where(p => p.Price >= minPrice.Value).ToList();
            }

            if (maxPrice.HasValue && maxPrice.Value >= 1)
            {
                filteredProducts = filteredProducts.Where(p => p.Price <= maxPrice.Value).ToList();
            }

            if (prescriptionOnly)
            {
                filteredProducts = filteredProducts.Where(p => p.RequiresPrescription).ToList();
            }

            int totalItems = filteredProducts.Count;
            int startIndex = (page - 1) * pageSize;

            // Reset to first page if calculated start index exceeds available items
            if (startIndex >= totalItems && totalItems > 0)
            {
                page = 1;
                startIndex = 0;
            }

            var paginatedProducts = filteredProducts
                .Skip(startIndex)
                .Take(pageSize)
                .ToList();

            var viewModels = paginatedProducts.Select(p => new ProductViewModel
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock,
                ImageUrl = !string.IsNullOrEmpty(p.ImageUrl) ? p.ImageUrl : "/images/product_01.png",
                IsActive = p.IsActive,
                CategoryID = p.CategoryID,
                RequiresPrescription = p.RequiresPrescription,
                PrescriptionNote = p.PrescriptionNote,
                ExpiryDate = p.ExpiryDate // Include expiry date
            }).ToList();

            // Populate category information for each product view model for display purposes
            foreach (var viewModel in viewModels)
            {
                viewModel.Category = _categoryRepository.Find(viewModel.CategoryID);
            }

            ViewBag.Categories = _categoryRepository.View();
            ViewBag.SelectedCategory = category;
            ViewBag.SearchTerm = search;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.SortOption = sort;
            ViewBag.PrescriptionOnly = prescriptionOnly;
            ViewBag.IncludeExpired = includeExpired;
            ViewBag.IsLoggedIn = HttpContext.Session.GetInt32("UserId") != null;
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");

            ViewBag.PageNumber = page;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;

            return View(viewModels);
        }

        // Updated ShopSingle method to include ExpiryDate
        public IActionResult ShopSingle(int id)
        {
            LoadCategories();
            var product = _productRepository.Find(id);

            if (product == null)
            {
                return NotFound();
            }

            var model = new ProductViewModel
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                CategoryID = product.CategoryID,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                ImageUrl = product.ImageUrl,
                IsActive = product.IsActive,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                RequiresPrescription = product.RequiresPrescription,
                PrescriptionNote = product.PrescriptionNote,
                ExpiryDate = product.ExpiryDate // Include expiry date
            };

            model.Category = _categoryRepository.Find(product.CategoryID);

            // Get related products from the same category excluding the current product and expired products for recommendations
            var relatedProducts = _productRepository.View()
                .Where(p => p.CategoryID == product.CategoryID &&
                           p.ProductId != product.ProductId &&
                           p.IsActive &&
                           p.ExpiryDate.Date > DateTime.Now.Date) // Exclude expired products
                .Take(3)
                .ToList();

            List<ProductViewModel> relatedProductModels = new List<ProductViewModel>();
            foreach (var relatedProduct in relatedProducts)
            {
                ProductViewModel relatedModel = new ProductViewModel
                {
                    ProductId = relatedProduct.ProductId,
                    ProductName = relatedProduct.ProductName,
                    CategoryID = relatedProduct.CategoryID,
                    Description = relatedProduct.Description,
                    Price = relatedProduct.Price,
                    Stock = relatedProduct.Stock,
                    ImageUrl = relatedProduct.ImageUrl,
                    IsActive = relatedProduct.IsActive,
                    RequiresPrescription = relatedProduct.RequiresPrescription,
                    PrescriptionNote = relatedProduct.PrescriptionNote,
                    ExpiryDate = relatedProduct.ExpiryDate // Include expiry date
                };

                relatedProductModels.Add(relatedModel);
            }

            ViewBag.RelatedProducts = relatedProductModels;
            ViewBag.IsLoggedIn = HttpContext.Session.GetInt32("UserId") != null;
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");

            return View(model);
        }
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

        public async Task<IActionResult> Cart()
        {
            LoadCategories();
            ViewBag.IsLoggedIn = HttpContext.Session.GetInt32("UserId") != null;
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                // Return empty cart structure for users who are not logged in
                return View(new CartViewModel
                {
                    Items = new List<CartItemViewModel>(),
                    SubTotal = 0,
                    Tax = 0,
                    ShippingCost = 0,
                    Total = 0,
                    ItemCount = 0
                });
            }

            var cartViewModel = await _cartRepository.GetCartViewModelAsync(userId.Value);
            return View(cartViewModel);
        }

        public IActionResult Checkout()
        {
            LoadCategories();
            ViewBag.IsLoggedIn = HttpContext.Session.GetInt32("UserId") != null;
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");

            return View();
        }

        public IActionResult ThankYou()
        {
            LoadCategories();
            ViewBag.IsLoggedIn = HttpContext.Session.GetInt32("UserId") != null;
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");

            return View();
        }

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