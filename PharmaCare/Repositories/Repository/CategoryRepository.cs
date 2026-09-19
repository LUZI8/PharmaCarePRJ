namespace PharmaCare.Repositories.Repository
{
    /* Repository implementation for category CRUD operations using Entity Framework */
    public class CategoryRepository : ICategoryRepository
    {
        private readonly DataDbContext dbContext;
        private Dictionary<int, Category>? categoryCache;

        /* Constructor injection for database context */
        public CategoryRepository(DataDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        /* Load categories once per repository/request and reuse them. */
        private Dictionary<int, Category> GetCategoryCache()
        {
            if (categoryCache != null)
            {
                return categoryCache;
            }

            categoryCache = dbContext.Category
                .AsNoTracking()
                .ToDictionary(c => c.CategoryID);

            return categoryCache;
        }

        private void InvalidateCache()
        {
            categoryCache = null;
        }

        public void Add(Category category)
        {
            try
            {
                dbContext.Category.Add(category);
                dbContext.SaveChanges();
                InvalidateCache();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CategoryRepository.Add: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public void Delete(int Id)
        {
            try
            {
                var category = dbContext.Category.FirstOrDefault(c => c.CategoryID == Id);
                if (category == null)
                {
                    return;
                }

                dbContext.Category.Remove(category);
                dbContext.SaveChanges();
                InvalidateCache();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CategoryRepository.Delete: {ex.Message}");
                throw;
            }
        }

        /* Find from the in-request cache instead of issuing one SQL query per product. */
        public Category Find(int Id)
        {
            try
            {
                return GetCategoryCache().TryGetValue(Id, out var category)
                    ? category
                    : null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CategoryRepository.Find: {ex.Message}");
                return null;
            }
        }

        public void Update(int id, Category category)
        {
            try
            {
                var existing = dbContext.Category.FirstOrDefault(c => c.CategoryID == id);
                if (existing == null)
                {
                    return;
                }

                existing.CategoryName = category.CategoryName;
                dbContext.SaveChanges();
                InvalidateCache();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CategoryRepository.Update: {ex.Message}");
                throw;
            }
        }

        /* Return the same cached category set used by Find(). */
        public List<Category> View()
        {
            try
            {
                return GetCategoryCache().Values
                    .OrderBy(c => c.CategoryName)
                    .ToList();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CategoryRepository.View: {ex.Message}");
                return new List<Category>();
            }
        }
    }
}
