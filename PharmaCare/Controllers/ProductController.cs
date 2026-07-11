namespace PharmaCare.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository ProductRepository;
        private readonly ICategoryRepository categoryRepository;
        private readonly IFileHelper fileHelper;
        private readonly IWebHostEnvironment env;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IFileHelper fileHelper,
            IWebHostEnvironment environment,
            ILogger<ProductController> logger)
        {
            ProductRepository = productRepository;
            this.categoryRepository = categoryRepository;
            this.fileHelper = fileHelper;
            this.env = environment;
            _logger = logger;
        }

        private void SetAdminViewBagProperties()
        {
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
        }

        public ActionResult Index()
        {
            SetAdminViewBagProperties();

            var data = ProductRepository.View();
            List<ProductViewModel> list = new List<ProductViewModel>();
            foreach (var item in data)
            {
                ProductViewModel obj = new ProductViewModel();
                obj.ProductId = item.ProductId;
                obj.ProductName = item.ProductName;
                obj.CategoryID = item.CategoryID;

                obj.Category = categoryRepository.Find(item.CategoryID);
                if (obj.Category == null)
                {
                    // Provide fallback category name when category is missing from database to prevent null reference errors
                    obj.Category = new Category { CategoryName = "Unknown" };
                }

                obj.Description = item.Description;
                obj.Price = item.Price;
                obj.Stock = item.Stock;
                obj.ExpiryDate = item.ExpiryDate; // Add expiry date mapping

                obj.RequiresPrescription = item.RequiresPrescription;
                obj.PrescriptionNote = item.PrescriptionNote;

                // Normalize image URLs to ensure consistent web-accessible paths for display across different storage formats
                if (!string.IsNullOrEmpty(item.ImageUrl))
                {
                    if (!item.ImageUrl.StartsWith("/") && !item.ImageUrl.StartsWith("http"))
                    {
                        obj.ImageUrl = "/Images/" + item.ImageUrl;
                    }
                    else
                    {
                        obj.ImageUrl = item.ImageUrl;
                    }
                }
                else
                {
                    obj.ImageUrl = "/images/product_01.png";
                }

                obj.IsActive = item.IsActive;
                obj.UpdatedAt = item.UpdatedAt;
                obj.CreatedAt = item.CreatedAt;

                list.Add(obj);
            }
            return View(list);
        }

        public ActionResult Details(int id)
        {
            SetAdminViewBagProperties();

            var data = ProductRepository.Find(id);

            if (data == null)
            {
                return NotFound();
            }

            if (categoryRepository != null)
            {
                data.Category = categoryRepository.Find(data.CategoryID);
            }

            var viewModel = new ProductViewModel
            {
                ProductId = data.ProductId,
                ProductName = data.ProductName,
                Description = data.Description,
                Price = data.Price,
                Stock = data.Stock,
                CategoryID = data.CategoryID,
                IsActive = data.IsActive,
                RequiresPrescription = data.RequiresPrescription,
                PrescriptionNote = data.PrescriptionNote,
                ExpiryDate = data.ExpiryDate, // Add expiry date mapping
                CreatedAt = data.CreatedAt,
                UpdatedAt = data.UpdatedAt,
                Category = data.Category
            };

            // Normalize image URL path for consistent web display across different storage formats
            if (!string.IsNullOrEmpty(data.ImageUrl))
            {
                if (!data.ImageUrl.StartsWith("/") && !data.ImageUrl.StartsWith("http"))
                {
                    viewModel.ImageUrl = "/Images/" + data.ImageUrl;
                }
                else
                {
                    viewModel.ImageUrl = data.ImageUrl;
                }
            }
            else
            {
                viewModel.ImageUrl = "/images/product_01.png";
            }

            return View(viewModel);
        }

        public ActionResult Create()
        {
            SetAdminViewBagProperties();

            var obj = new ProductViewModel
            {
                RequiresPrescription = false,
                IsActive = true,
                ExpiryDate = DateTime.Now.AddYears(2), // Default expiry date
                ListOfCategories = new SelectList(categoryRepository.View(), "CategoryID", "CategoryName")
            };

            return View(obj);
        }

        // Updated Create POST method to handle ExpiryDate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection form)
        {
            try
            {
                SetAdminViewBagProperties();

                _logger.LogInformation("Create form submitted");

                var collection = new ProductViewModel
                {
                    ProductName = form["ProductName"],
                    Description = form["Description"],
                    CategoryID = int.Parse(form["CategoryID"]),
                    Price = decimal.Parse(form["Price"]),
                    Stock = int.Parse(form["Stock"]),
                    IsActive = form.Keys.Contains("IsActive"),
                    RequiresPrescription = form.Keys.Contains("RequiresPrescription"),
                    PrescriptionNote = form["PrescriptionNote"],
                    ExpiryDate = DateTime.Parse(form["ExpiryDate"]), // Parse expiry date
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                if (form.Files.Count > 0)
                {
                    collection.File = form.Files[0];
                }

                _logger.LogInformation($"Form processed - ProductName: {collection.ProductName}, ExpiryDate: {collection.ExpiryDate}");

                collection.ListOfCategories = new SelectList(categoryRepository.View(), "CategoryID", "CategoryName");

                // Validation checks
                if (string.IsNullOrEmpty(collection.ProductName))
                {
                    ModelState.AddModelError("ProductName", "Product name is required");
                    return View(collection);
                }

                if (collection.CategoryID <= 0)
                {
                    ModelState.AddModelError("CategoryID", "Category is required");
                    return View(collection);
                }

                if (collection.Price <= 0)
                {
                    ModelState.AddModelError("Price", "Price must be greater than 0");
                    return View(collection);
                }

                if (collection.Stock < 0)
                {
                    ModelState.AddModelError("Stock", "Stock cannot be negative");
                    return View(collection);
                }

                // Validate expiry date
                if (collection.ExpiryDate.Date <= DateTime.Now.Date)
                {
                    ModelState.AddModelError("ExpiryDate", "Expiry date must be in the future");
                    return View(collection);
                }

                // Prevent duplicate product names within the same category to maintain data integrity
                bool productExists = ProductRepository.View()
                    .Any(p => p.ProductName.ToLower() == collection.ProductName.ToLower()
                         && p.CategoryID == collection.CategoryID);

                if (productExists)
                {
                    ModelState.AddModelError("ProductName", "A product with this name already exists in the selected category.");
                    return View(collection);
                }

                string imageName = null;

                if (collection.File != null)
                {
                    imageName = fileHelper.SaveImage(collection.File, "", "Images");
                    if (imageName == "Error")
                    {
                        ModelState.AddModelError("File", "Error occurred while saving image. Please try again.");
                        return View(collection);
                    }

                    // Ensure image path has proper web-accessible format with leading slash
                    if (!string.IsNullOrEmpty(imageName) && !imageName.StartsWith("/") && !imageName.StartsWith("http"))
                    {
                        imageName = "/Images/" + imageName;
                    }
                }
                else
                {
                    imageName = "/images/product_01.png";
                }

                // Clear prescription note when product doesn't require prescription to maintain data consistency
                if (!collection.RequiresPrescription)
                {
                    collection.PrescriptionNote = null;
                }

                var obj = new Product
                {
                    ProductName = collection.ProductName,
                    CategoryID = collection.CategoryID,
                    Description = collection.Description ?? string.Empty,
                    Price = collection.Price,
                    Stock = collection.Stock,
                    ImageUrl = imageName,
                    IsActive = collection.IsActive,
                    RequiresPrescription = collection.RequiresPrescription,
                    PrescriptionNote = collection.PrescriptionNote,
                    ExpiryDate = collection.ExpiryDate, // Include expiry date
                    UpdatedAt = DateTime.Now,
                    CreatedAt = DateTime.Now
                };

                _logger.LogInformation($"Adding product to repository: {obj.ProductName}, ExpiryDate: {obj.ExpiryDate}");

                ProductRepository.Add(obj);
                _logger.LogInformation("Product added successfully");

                TempData["SuccessMessage"] = "Product created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                SetAdminViewBagProperties();

                _logger.LogError(ex, "Error creating product");

                var model = new ProductViewModel
                {
                    IsActive = true,
                    RequiresPrescription = false,
                    ExpiryDate = DateTime.Now.AddYears(2),
                    ListOfCategories = new SelectList(categoryRepository.View(), "CategoryID", "CategoryName")
                };

                ModelState.AddModelError("", $"Exception: {ex.Message}");
                return View(model);
            }
        }

        // Updated Edit method to include ExpiryDate
        public ActionResult Edit(int id)
        {
            SetAdminViewBagProperties();

            var product = ProductRepository.Find(id);

            if (product == null)
            {
                return NotFound();
            }

            var obj = new ProductViewModel
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                CategoryID = product.CategoryID,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                IsActive = product.IsActive,
                RequiresPrescription = product.RequiresPrescription,
                PrescriptionNote = product.PrescriptionNote,
                ExpiryDate = product.ExpiryDate, // Include expiry date
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                ListOfCategories = new SelectList(categoryRepository.View(), "CategoryID", "CategoryName")
            };

            // Format image URL for consistent display in edit form
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                if (!product.ImageUrl.StartsWith("/") && !product.ImageUrl.StartsWith("http"))
                {
                    obj.ImageUrl = "/Images/" + product.ImageUrl;
                }
                else
                {
                    obj.ImageUrl = product.ImageUrl;
                }
            }
            else
            {
                obj.ImageUrl = "/images/product_01.png";
            }

            return View(obj);
        }

        // Updated Edit POST method to handle ExpiryDate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection form)
        {
            try
            {
                SetAdminViewBagProperties();

                _logger.LogInformation($"Edit form submitted for product id: {id}");

                // Get existing product to preserve original data and maintain data integrity during updates
                var existingProduct = ProductRepository.Find(id);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                var collection = new ProductViewModel
                {
                    ProductId = id,
                    ProductName = form["ProductName"],
                    Description = form["Description"],
                    CategoryID = int.Parse(form["CategoryID"]),
                    Price = decimal.Parse(form["Price"]),
                    Stock = int.Parse(form["Stock"]),
                    IsActive = form.Keys.Contains("IsActive"),
                    RequiresPrescription = form.Keys.Contains("RequiresPrescription"),
                    PrescriptionNote = form["PrescriptionNote"],
                    ImageUrl = form["ImageUrl"],
                    ExpiryDate = DateTime.Parse(form["ExpiryDate"]), // Parse expiry date
                    CreatedAt = DateTime.Parse(form["CreatedAt"])
                };

                if (form.Files.Count > 0)
                {
                    collection.File = form.Files[0];
                }

                _logger.LogInformation($"Form processed - ProductName: {collection.ProductName}, ExpiryDate: {collection.ExpiryDate}");

                collection.ListOfCategories = new SelectList(categoryRepository.View(), "CategoryID", "CategoryName");

                // Validate expiry date
                if (collection.ExpiryDate.Date <= DateTime.Now.Date)
                {
                    ModelState.AddModelError("ExpiryDate", "Expiry date must be in the future");
                    return View(collection);
                }

                // Check for duplicate names while excluding the current product being edited
                bool productExists = ProductRepository.View()
                    .Any(p => p.ProductName.ToLower() == collection.ProductName.ToLower()
                         && p.CategoryID == collection.CategoryID
                         && p.ProductId != id);

                if (productExists)
                {
                    ModelState.AddModelError("ProductName", "A product with this name already exists in the selected category.");
                    return View(collection);
                }

                string imageName = collection.ImageUrl;

                if (collection.File != null)
                {
                    imageName = fileHelper.SaveImage(collection.File, collection.ImageUrl, "Images");
                    if (imageName == "Error")
                    {
                        ModelState.AddModelError("File", "Error occurred while saving image. Please try again.");
                        return View(collection);
                    }

                    if (!imageName.StartsWith("/") && !imageName.StartsWith("http"))
                    {
                        imageName = "/Images/" + imageName;
                    }
                }

                if (!collection.RequiresPrescription)
                {
                    collection.PrescriptionNote = null;
                }

                // Update existing product properties while preserving original creation timestamp
                existingProduct.ProductName = collection.ProductName;
                existingProduct.CategoryID = collection.CategoryID;
                existingProduct.Description = collection.Description ?? string.Empty;
                existingProduct.Price = collection.Price;
                existingProduct.Stock = collection.Stock;
                existingProduct.ImageUrl = imageName;
                existingProduct.IsActive = collection.IsActive;
                existingProduct.RequiresPrescription = collection.RequiresPrescription;
                existingProduct.PrescriptionNote = collection.PrescriptionNote;
                existingProduct.ExpiryDate = collection.ExpiryDate; // Update expiry date
                existingProduct.UpdatedAt = DateTime.Now;
                existingProduct.CreatedAt = collection.CreatedAt;

                _logger.LogInformation($"Updating product: {existingProduct.ProductName}, ExpiryDate: {existingProduct.ExpiryDate}");

                ProductRepository.Update(id, existingProduct);
                _logger.LogInformation("Product updated successfully");

                TempData["SuccessMessage"] = "Product updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                SetAdminViewBagProperties();

                _logger.LogError(ex, $"Error updating product with id: {id}");

                var existingProduct = ProductRepository.Find(id);
                var model = new ProductViewModel
                {
                    ProductId = id,
                    ListOfCategories = new SelectList(categoryRepository.View(), "CategoryID", "CategoryName")
                };

                if (existingProduct != null)
                {
                    model.ProductName = existingProduct.ProductName;
                    model.Description = existingProduct.Description;
                    model.CategoryID = existingProduct.CategoryID;
                    model.Price = existingProduct.Price;
                    model.Stock = existingProduct.Stock;
                    model.IsActive = existingProduct.IsActive;
                    model.RequiresPrescription = existingProduct.RequiresPrescription;
                    model.PrescriptionNote = existingProduct.PrescriptionNote;
                    model.ImageUrl = existingProduct.ImageUrl;
                    model.ExpiryDate = existingProduct.ExpiryDate; // Include expiry date
                    model.CreatedAt = existingProduct.CreatedAt;
                }

                ModelState.AddModelError("", $"Exception: {ex.Message}");
                return View(model);
            }
        }


        public ActionResult Delete(int id)
        {
            SetAdminViewBagProperties();

            var product = ProductRepository.Find(id);

            if (product == null)
            {
                return NotFound();
            }

            if (categoryRepository != null)
            {
                product.Category = categoryRepository.Find(product.CategoryID);
            }

            // Format image URL for display in delete confirmation view
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                if (!product.ImageUrl.StartsWith("/") && !product.ImageUrl.StartsWith("http"))
                {
                    product.ImageUrl = "/Images/" + product.ImageUrl;
                }
            }

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, Product collection)
        {
            try
            {
                var product = ProductRepository.Find(id);
                if (product != null)
                {
                    // Remove associated image file from filesystem when deleting product to free up storage space
                    if (!string.IsNullOrEmpty(product.ImageUrl) &&
                        !product.ImageUrl.StartsWith("http") &&
                        product.ImageUrl != "/images/product_01.png")
                    {
                        string fileName = product.ImageUrl;
                        if (fileName.StartsWith("/Images/"))
                        {
                            fileName = fileName.Substring("/Images/".Length);
                        }

                        string fullPath = Path.Combine(env.WebRootPath, "Images", fileName);

                        if (System.IO.File.Exists(fullPath))
                        {
                            try
                            {
                                System.IO.File.Delete(fullPath);
                            }
                            catch
                            {
                                // Continue with product deletion even if file deletion fails to prevent database inconsistency
                            }
                        }
                    }
                }

                ProductRepository.Delete(id);
                TempData["SuccessMessage"] = "Product deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}