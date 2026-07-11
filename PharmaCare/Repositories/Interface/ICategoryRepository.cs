namespace PharmaCare.Repositories.Interface
{
    /* Repository interface for category CRUD operations using synchronous methods */
    public interface ICategoryRepository
    {
        /* Create a new category in the database */
        void Add(Category category);

        /* Update existing category by ID with new category data */
        void Update(int id, Category category);

        /* Delete category by ID from the database */
        void Delete(int Id);

        /* Retrieve single category by ID - returns null if not found */
        Category Find(int Id);

        /* Retrieve all categories as a list for display or selection */
        List<Category> View();
    }
}