namespace PharmaCare.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly DataDbContext _dbContext;

        public CategoryController(ICategoryRepository categoryRepository, DataDbContext dbContext)
        {
            _categoryRepository = categoryRepository;
            _dbContext = dbContext;
        }

        private void SetAdminViewBagProperties()
        {
            ViewBag.AdminName = HttpContext.Session.GetString("UserName") ?? "Admin";
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");
        }

        public ActionResult Index()
        {
            try
            {
                SetAdminViewBagProperties();

                var categories = _categoryRepository.View();

                // Eagerly load all products for each category to calculate and display product counts in the UI
                foreach (var category in categories)
                {
                    category.Products = _dbContext.Product
                        .Where(p => p.CategoryID == category.CategoryID)
                        .ToList();
                }

                return View(categories);
            }
            catch (Exception ex)
            {
                SetAdminViewBagProperties();

                Debug.WriteLine($"Error in Category Index: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading categories.";
                return View(new List<Category>());
            }
        }

        public ActionResult Create()
        {
            SetAdminViewBagProperties();

            return View(new Category());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Category category)
        {
            try
            {
                SetAdminViewBagProperties();

                // Perform case-insensitive duplicate name check to prevent creating categories with existing names
                bool categoryExists = _dbContext.Category
                    .Any(c => c.CategoryName.ToLower() == category.CategoryName.ToLower());

                if (categoryExists)
                {
                    ModelState.AddModelError("CategoryName", "A category with this name already exists.");
                    return View(category);
                }

                // Begin database transaction to ensure data consistency during category creation process
                using (var transaction = _dbContext.Database.BeginTransaction())
                {
                    try
                    {
                        Debug.WriteLine($"Creating category: {category.CategoryName}");

                        var newCategory = new Category
                        {
                            CategoryName = category.CategoryName
                        };

                        _dbContext.Category.Add(newCategory);
                        _dbContext.SaveChanges();

                        // Commit transaction only after all database operations complete successfully
                        transaction.Commit();

                        TempData["SuccessMessage"] = "Category created successfully!";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (Exception ex)
                    {
                        // Rollback all database changes if any operation within the transaction fails
                        transaction.Rollback();
                        Debug.WriteLine($"Transaction error: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                        }

                        ModelState.AddModelError("", $"Database error: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            ModelState.AddModelError("", $"Details: {ex.InnerException.Message}");
                        }
                    }
                }

                return View(category);
            }
            catch (Exception ex)
            {
                SetAdminViewBagProperties();

                Debug.WriteLine($"Outer error: {ex.Message}");
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                return View(category);
            }
        }

        public ActionResult Details(int id)
        {
            try
            {
                SetAdminViewBagProperties();

                var category = _categoryRepository.Find(id);
                if (category == null)
                {
                    return NotFound();
                }

                // Load all related products to display them in the category details view
                category.Products = _dbContext.Product
                    .Where(p => p.CategoryID == category.CategoryID)
                    .ToList();

                return View(category);
            }
            catch (Exception ex)
            {
                SetAdminViewBagProperties();

                Debug.WriteLine($"Error in Category Details: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading category details.";
                return RedirectToAction(nameof(Index));
            }
        }

        public ActionResult Edit(int id)
        {
            try
            {
                SetAdminViewBagProperties();

                var category = _categoryRepository.Find(id);
                if (category == null)
                {
                    return NotFound();
                }

                // Preload products to show context information during the editing process
                category.Products = _dbContext.Product
                    .Where(p => p.CategoryID == category.CategoryID)
                    .ToList();

                return View(category);
            }
            catch (Exception ex)
            {
                SetAdminViewBagProperties();

                Debug.WriteLine($"Error in Category Edit: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading the category.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, Category category)
        {
            try
            {
                SetAdminViewBagProperties();

                // Check for duplicate names while excluding the current category being edited from the search
                bool categoryExists = _dbContext.Category
                    .Any(c => c.CategoryName.ToLower() == category.CategoryName.ToLower()
                          && c.CategoryID != id);

                if (categoryExists)
                {
                    ModelState.AddModelError("CategoryName", "A category with this name already exists.");

                    category.Products = _dbContext.Product
                        .Where(p => p.CategoryID == id)
                        .ToList();

                    return View(category);
                }

                if (ModelState.IsValid)
                {
                    _categoryRepository.Update(id, category);
                    TempData["SuccessMessage"] = "Category updated successfully!";
                    return RedirectToAction(nameof(Index));
                }

                category.Products = _dbContext.Product
                    .Where(p => p.CategoryID == id)
                    .ToList();

                return View(category);
            }
            catch (Exception ex)
            {
                SetAdminViewBagProperties();

                Debug.WriteLine($"Error editing category: {ex.Message}");
                ModelState.AddModelError("", $"An error occurred while updating the category: {ex.Message}");

                category.Products = _dbContext.Product
                    .Where(p => p.CategoryID == id)
                    .ToList();

                return View(category);
            }
        }

        public ActionResult Delete(int id)
        {
            try
            {
                SetAdminViewBagProperties();

                var category = _categoryRepository.Find(id);
                if (category == null)
                {
                    return NotFound();
                }

                category.Products = _dbContext.Product
                    .Where(p => p.CategoryID == category.CategoryID)
                    .ToList();

                return View(category);
            }
            catch (Exception ex)
            {
                SetAdminViewBagProperties();

                Debug.WriteLine($"Error in Category Delete: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while loading the category.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, Category collection)
        {
            try
            {
                SetAdminViewBagProperties();

                // Check if category has associated products to maintain referential integrity and prevent orphaned products
                var productsCount = _dbContext.Product.Count(p => p.CategoryID == id);

                if (productsCount > 0)
                {
                    ModelState.AddModelError("", $"Cannot delete category. It has {productsCount} associated products. Please reassign or delete these products first.");

                    var category = _categoryRepository.Find(id);
                    if (category == null)
                    {
                        return NotFound();
                    }

                    category.Products = _dbContext.Product
                        .Where(p => p.CategoryID == category.CategoryID)
                        .ToList();

                    return View(category);
                }

                _categoryRepository.Delete(id);
                TempData["SuccessMessage"] = "Category deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                SetAdminViewBagProperties();

                Debug.WriteLine($"Error deleting category: {ex.Message}");
                ModelState.AddModelError("", $"An error occurred while deleting the category: {ex.Message}");

                var category = _categoryRepository.Find(id);
                if (category == null)
                {
                    return NotFound();
                }

                category.Products = _dbContext.Product
                    .Where(p => p.CategoryID == category.CategoryID)
                    .ToList();

                return View(category);
            }
        }
    }
}