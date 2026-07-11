namespace PharmaCare.Repositories.Interface
{
    /* Repository interface for product CRUD operations using synchronous methods */
    public interface IProductRepository
    {
        /* Create a new product in the database */
        void Add(Product product);

        /* Update existing product by ID with new product data */
        void Update(int id, Product product);

        /* Delete product by ID from the database */
        void Delete(int Id);

        /* Retrieve single product by ID - returns null if not found */
        Product Find(int Id);

        /* Retrieve all products as a list for display or management */
        List<Product> View();
    }
}