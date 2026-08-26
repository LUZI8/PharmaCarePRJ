namespace PharmaCare.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository ProductRepository;
        private readonly ICategoryRepository categoryRepository;
        private readonly IFileHelper fileHelper;
        private readonly IWebHostEnvironment env;
        private readonly ILogger<ProductController> _logger;
        private readonly DataDbContext _context;

        public ProductController(IProductRepository productRepository, ICategoryRepository categoryRepository, IFileHelper fileHelper, IWebHostEnvironment environment, ILogger<ProductController> logger, DataDbContext context)
        {
            ProductRepository = productRepository;
            this.categoryRepository = categoryRepository;
            this.fileHelper = fileHelper;
            env = environment;
            _logger = logger;
            _context = context;
        }

        private void SetAdminViewBagProperties()
        {
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
        }

        private ProductViewModel ToViewModel(Product p)
        {
            var category = categoryRepository.Find(p.CategoryID) ?? new Category { CategoryName = "Unknown" };
            var galleryEntities = _context.ProductImages
                .Where(i => i.ProductId == p.ProductId)
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.DisplayOrder)
                .ToList();

            var gallery = galleryEntities.Select(i => new ProductImageViewModel
            {
                ProductImageId = i.ProductImageId,
                ImageUrl = NormalizeImageUrl(i.ImageUrl),
                DisplayOrder = i.DisplayOrder,
                IsPrimary = i.IsPrimary
            }).ToList();

            return new ProductViewModel
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                CategoryID = p.CategoryID,
                Category = category,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock,
                SKU = p.SKU,
                Barcode = p.Barcode,
                Manufacturer = p.Manufacturer,
                ReorderLevel = p.ReorderLevel,
                ImageUrl = NormalizeImageUrl(p.ImageUrl),
                ExistingImages = gallery,
                PrimaryImageId = gallery.FirstOrDefault(x => x.IsPrimary)?.ProductImageId,
                IsActive = p.IsActive,
                RequiresPrescription = p.RequiresPrescription,
                PrescriptionNote = p.PrescriptionNote,
                ExpiryDate = p.ExpiryDate,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            };
        }

        private static string NormalizeImageUrl(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return "/images/product_01.png";
            if (imageUrl.StartsWith("/") || imageUrl.StartsWith("http")) return imageUrl;
            return "/Images/" + imageUrl;
        }

        public ActionResult Index()
        {
            SetAdminViewBagProperties();
            var products = ProductRepository.View().ToList();
            var models = products.Select(ToViewModel).ToList();
            return View(models);
        }

        public ActionResult Details(int id)
        {
            SetAdminViewBagProperties();
            var product = ProductRepository.Find(id);
            return product == null ? NotFound() : View(ToViewModel(product));
        }

        public ActionResult Create()
        {
            SetAdminViewBagProperties();
            return View(new ProductViewModel
            {
                RequiresPrescription = false,
                IsActive = true,
                ReorderLevel = 10,
                ExpiryDate = DateTime.Now.AddYears(2),
                ListOfCategories = new SelectList(categoryRepository.View(), "CategoryID", "CategoryName")
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection form)
        {
            SetAdminViewBagProperties();
            var model = BuildFormModel(form);
            model.ListOfCategories = new SelectList(categoryRepository.View(), "CategoryID", "CategoryName");
            if (!ValidateProduct(model, null)) return View(model);

            try
            {
                var selectedFiles = form.Files.GetFiles("galleryFiles")
                    .Where(f => f != null && f.Length > 0)
                    .Take(5)
                    .ToList();

                if (!selectedFiles.Any() && model.File != null) selectedFiles.Add(model.File);

                var savedImages = new List<string>();
                foreach (var file in selectedFiles)
                {
                    var saved = SaveImage(file, null);
                    if (saved == "Error")
                    {
                        foreach (var image in savedImages) DeletePhysicalImage(image);
                        ModelState.AddModelError("", "One or more product images could not be saved. Please try again.");
                        return View(model);
                    }
                    if (!string.IsNullOrWhiteSpace(saved)) savedImages.Add(saved);
                }

                int.TryParse(form["primaryImageIndex"], out var primaryImageIndex);
                if (primaryImageIndex < 0 || primaryImageIndex >= savedImages.Count) primaryImageIndex = 0;
                var primaryImage = savedImages.Any() ? savedImages[primaryImageIndex] : "/images/product_01.png";

                var product = new Product
                {
                    ProductName = model.ProductName.Trim(),
                    CategoryID = model.CategoryID,
                    Description = model.Description?.Trim() ?? string.Empty,
                    Price = model.Price,
                    Stock = model.Stock,
                    SKU = Clean(model.SKU),
                    Barcode = Clean(model.Barcode),
                    Manufacturer = Clean(model.Manufacturer),
                    ReorderLevel = model.ReorderLevel,
                    ImageUrl = primaryImage,
                    IsActive = model.IsActive,
                    RequiresPrescription = model.RequiresPrescription,
                    PrescriptionNote = model.RequiresPrescription ? Clean(model.PrescriptionNote) : null,
                    ExpiryDate = model.ExpiryDate,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                ProductRepository.Add(product);

                for (var i = 0; i < savedImages.Count; i++)
                {
                    _context.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.ProductId,
                        ImageUrl = savedImages[i],
                        DisplayOrder = i,
                        IsPrimary = i == primaryImageIndex,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                if (savedImages.Any()) _context.SaveChanges();

                TempData["SuccessMessage"] = savedImages.Count > 1
                    ? $"Product created successfully with {savedImages.Count} images."
                    : "Product created successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                ModelState.AddModelError("", "Unable to create the product. Check SKU/barcode uniqueness and try again.");
                return View(model);
            }
        }

        public ActionResult Edit(int id)
        {
            SetAdminViewBagProperties();
            var product = ProductRepository.Find(id);
            if (product == null) return NotFound();
            var model = ToViewModel(product);
            model.ListOfCategories = new SelectList(categoryRepository.View(), "CategoryID", "CategoryName");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection form)
        {
            SetAdminViewBagProperties();
            var existing = ProductRepository.Find(id);
            if (existing == null) return NotFound();

            var model = BuildFormModel(form);
            model.ProductId = id;
            model.CreatedAt = existing.CreatedAt;
            model.ImageUrl = NormalizeImageUrl(existing.ImageUrl);
            model.ListOfCategories = new SelectList(categoryRepository.View(), "CategoryID", "CategoryName");

            if (!ValidateProduct(model, id))
            {
                var current = ToViewModel(existing);
                model.ExistingImages = current.ExistingImages;
                model.PrimaryImageId = current.PrimaryImageId;
                model.UpdatedAt = existing.UpdatedAt;
                return View(model);
            }

            try
            {
                existing.ProductName = model.ProductName.Trim();
                existing.CategoryID = model.CategoryID;
                existing.Description = model.Description?.Trim() ?? string.Empty;
                existing.Price = model.Price;
                existing.Stock = model.Stock;
                existing.SKU = Clean(model.SKU);
                existing.Barcode = Clean(model.Barcode);
                existing.Manufacturer = Clean(model.Manufacturer);
                existing.ReorderLevel = model.ReorderLevel;
                existing.IsActive = model.IsActive;
                existing.RequiresPrescription = model.RequiresPrescription;
                existing.PrescriptionNote = model.RequiresPrescription ? Clean(model.PrescriptionNote) : null;
                existing.ExpiryDate = model.ExpiryDate;
                existing.UpdatedAt = DateTime.Now;

                ProductRepository.Update(id, existing);
                TempData["SuccessMessage"] = "Product updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                var current = ToViewModel(existing);
                model.ExistingImages = current.ExistingImages;
                model.PrimaryImageId = current.PrimaryImageId;
                ModelState.AddModelError("", "Unable to update the product. Check SKU/barcode uniqueness and try again.");
                return View(model);
            }
        }

        // Kept only for old bookmarks/links. Gallery management now lives inside Edit Product.
        public ActionResult ManageImages(int id)
        {
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadImages(int id, List<IFormFile> galleryFiles)
        {
            var product = ProductRepository.Find(id);
            if (product == null) return NotFound();

            var existingCount = _context.ProductImages.Count(i => i.ProductId == id);
            var files = (galleryFiles ?? new List<IFormFile>())
                .Where(f => f != null && f.Length > 0)
                .Take(Math.Max(0, 5 - existingCount))
                .ToList();

            if (!files.Any())
            {
                TempData["ErrorMessage"] = existingCount >= 5
                    ? "A product can have up to 5 gallery images."
                    : "Choose at least one image.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            var nextOrder = _context.ProductImages
                .Where(i => i.ProductId == id)
                .Select(i => (int?)i.DisplayOrder)
                .Max() ?? -1;

            foreach (var file in files)
            {
                var saved = SaveImage(file, null);
                if (saved == "Error") continue;

                nextOrder++;
                _context.ProductImages.Add(new ProductImage
                {
                    ProductId = id,
                    ImageUrl = saved!,
                    DisplayOrder = nextOrder,
                    IsPrimary = existingCount == 0 && nextOrder == 0,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _context.SaveChanges();

            var primary = _context.ProductImages.FirstOrDefault(i => i.ProductId == id && i.IsPrimary);
            if (primary != null)
            {
                product.ImageUrl = primary.ImageUrl;
                product.UpdatedAt = DateTime.Now;
                ProductRepository.Update(id, product);
            }

            TempData["SuccessMessage"] = "Product images updated.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SetPrimaryImage(int id, int imageId)
        {
            var product = ProductRepository.Find(id);
            var selected = _context.ProductImages.FirstOrDefault(i => i.ProductImageId == imageId && i.ProductId == id);
            if (product == null || selected == null) return NotFound();

            foreach (var image in _context.ProductImages.Where(i => i.ProductId == id))
                image.IsPrimary = image.ProductImageId == imageId;

            _context.SaveChanges();
            product.ImageUrl = selected.ImageUrl;
            product.UpdatedAt = DateTime.Now;
            ProductRepository.Update(id, product);

            TempData["SuccessMessage"] = "Primary image changed.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteGalleryImage(int id, int imageId)
        {
            var product = ProductRepository.Find(id);
            var image = _context.ProductImages.FirstOrDefault(i => i.ProductImageId == imageId && i.ProductId == id);
            if (product == null || image == null) return NotFound();

            var wasPrimary = image.IsPrimary;
            DeletePhysicalImage(image.ImageUrl);
            _context.ProductImages.Remove(image);
            _context.SaveChanges();

            if (wasPrimary)
            {
                var replacement = _context.ProductImages
                    .Where(i => i.ProductId == id)
                    .OrderBy(i => i.DisplayOrder)
                    .FirstOrDefault();

                if (replacement != null)
                {
                    replacement.IsPrimary = true;
                    product.ImageUrl = replacement.ImageUrl;
                    _context.SaveChanges();
                }
                else
                {
                    product.ImageUrl = "/images/product_01.png";
                }
            }

            product.UpdatedAt = DateTime.Now;
            ProductRepository.Update(id, product);
            TempData["SuccessMessage"] = "Image removed from the product.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        private ProductViewModel BuildFormModel(IFormCollection form)
        {
            int.TryParse(form["CategoryID"], out var categoryId);
            decimal.TryParse(form["Price"], out var price);
            int.TryParse(form["Stock"], out var stock);
            int.TryParse(form["ReorderLevel"], out var reorderLevel);
            DateTime.TryParse(form["ExpiryDate"], out var expiryDate);

            return new ProductViewModel
            {
                ProductName = form["ProductName"],
                Description = form["Description"],
                CategoryID = categoryId,
                Price = price,
                Stock = stock,
                SKU = form["SKU"],
                Barcode = form["Barcode"],
                Manufacturer = form["Manufacturer"],
                ReorderLevel = reorderLevel < 0 ? 0 : reorderLevel,
                IsActive = form.Keys.Contains("IsActive"),
                RequiresPrescription = form.Keys.Contains("RequiresPrescription"),
                PrescriptionNote = form["PrescriptionNote"],
                ExpiryDate = expiryDate == default ? DateTime.Now.AddYears(2) : expiryDate,
                File = form.Files.Count > 0 ? form.Files[0] : null
            };
        }

        private bool ValidateProduct(ProductViewModel model, int? currentId)
        {
            if (string.IsNullOrWhiteSpace(model.ProductName)) ModelState.AddModelError("ProductName", "Product name is required");
            if (model.CategoryID <= 0) ModelState.AddModelError("CategoryID", "Category is required");
            if (model.Price <= 0) ModelState.AddModelError("Price", "Price must be greater than 0");
            if (model.Stock < 0) ModelState.AddModelError("Stock", "Stock cannot be negative");
            if (model.ReorderLevel < 0) ModelState.AddModelError("ReorderLevel", "Low stock threshold cannot be negative");
            if (model.ExpiryDate.Date <= DateTime.Now.Date) ModelState.AddModelError("ExpiryDate", "Expiry date must be in the future");

            var products = ProductRepository.View();
            if (products.Any(p => p.ProductId != currentId && p.CategoryID == model.CategoryID && p.ProductName.Equals(model.ProductName?.Trim(), StringComparison.OrdinalIgnoreCase)))
                ModelState.AddModelError("ProductName", "A product with this name already exists in the selected category.");

            var sku = Clean(model.SKU);
            if (sku != null && products.Any(p => p.ProductId != currentId && string.Equals(p.SKU, sku, StringComparison.OrdinalIgnoreCase)))
                ModelState.AddModelError("SKU", "This SKU is already assigned to another product.");

            var barcode = Clean(model.Barcode);
            if (barcode != null && products.Any(p => p.ProductId != currentId && string.Equals(p.Barcode, barcode, StringComparison.OrdinalIgnoreCase)))
                ModelState.AddModelError("Barcode", "This barcode is already assigned to another product.");

            return ModelState.IsValid;
        }

        private string? SaveImage(IFormFile? file, string? oldImage)
        {
            if (file == null) return oldImage ?? "/images/product_01.png";
            var imageName = fileHelper.SaveImage(file, oldImage ?? string.Empty, "Images");
            if (imageName == "Error") return "Error";
            return NormalizeImageUrl(imageName);
        }

        private void DeletePhysicalImage(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl) || imageUrl.StartsWith("http") || imageUrl == "/images/product_01.png") return;
            var fileName = imageUrl.StartsWith("/Images/") ? imageUrl.Substring("/Images/".Length) : imageUrl.TrimStart('/');
            var fullPath = Path.Combine(env.WebRootPath, "Images", fileName);
            if (System.IO.File.Exists(fullPath))
            {
                try { System.IO.File.Delete(fullPath); }
                catch { }
            }
        }

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        public ActionResult Delete(int id)
        {
            SetAdminViewBagProperties();
            var product = ProductRepository.Find(id);
            if (product == null) return NotFound();
            product.Category = categoryRepository.Find(product.CategoryID);
            product.ImageUrl = NormalizeImageUrl(product.ImageUrl);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, Product collection)
        {
            try
            {
                foreach (var image in _context.ProductImages.Where(i => i.ProductId == id).ToList())
                    DeletePhysicalImage(image.ImageUrl);

                var product = ProductRepository.Find(id);
                if (product != null) DeletePhysicalImage(product.ImageUrl);

                ProductRepository.Delete(id);
                TempData["SuccessMessage"] = "Product deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                TempData["ErrorMessage"] = "Unable to delete this product because it may be used by orders or reservations.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}