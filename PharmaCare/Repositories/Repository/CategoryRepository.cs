namespace PharmaCare.Repositories.Repository
{
    /* Repository implementation for category CRUD operations using raw SQL and Entity Framework */
    public class CategoryRepository : ICategoryRepository
    {
        private DataDbContext dbContext;

        /* Constructor injection for database context */
        public CategoryRepository(DataDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        /* Add new category with exception handling and debug logging */
        public void Add(Category category)
        {
            try
            {
                dbContext.Category.Add(category);
                dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                /* Debug logging for development troubleshooting */
                Debug.WriteLine($"Error in CategoryRepository.Add: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        /* Delete category using raw SQL for reliable execution */
        public void Delete(int Id)
        {
            try
            {
                /* Use parameterized raw SQL to prevent SQL injection */
                string sql = "DELETE FROM Category WHERE CategoryID = @p0";
                dbContext.Database.ExecuteSqlRaw(sql, Id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CategoryRepository.Delete: {ex.Message}");
                throw;
            }
        }

        /* Find single category by ID using raw SQL query */
        public Category Find(int Id)
        {
            try
            {
                /* Raw SQL query with parameter binding for security */
                return dbContext.Category.FromSqlRaw(
                    "SELECT CategoryID, CategoryName FROM Category WHERE CategoryID = {0}", Id)
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CategoryRepository.Find: {ex.Message}");
                /* Return null instead of crashing application */
                return null;
            }
        }

        /* Update category using parameterized raw SQL */
        public void Update(int id, Category category)
        {
            try
            {
                /* Parameterized SQL update to prevent injection attacks */
                string sql = "UPDATE Category SET CategoryName = @p0 WHERE CategoryID = @p1";
                dbContext.Database.ExecuteSqlRaw(sql, category.CategoryName, id);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CategoryRepository.Update: {ex.Message}");
                throw;
            }
        }

        /* Retrieve all categories using raw SQL with graceful error handling */
        public List<Category> View()
        {
            try
            {
                /* Raw SQL query to fetch all categories */
                var categories = dbContext.Category
                    .FromSqlRaw("SELECT CategoryID, CategoryName FROM Category")
                    .ToList();

                return categories;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in CategoryRepository.View: {ex.Message}");
                /* Return empty list to prevent null reference exceptions */
                return new List<Category>();
            }
        }
    }
}