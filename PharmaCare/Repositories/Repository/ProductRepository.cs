namespace PharmaCare.Repositories.Repository
{
    /* Repository implementation for product CRUD operations with category relationship management */
    public class ProductRepository : IProductRepository
    {
        private DataDbContext DataDbContext;
        private readonly ICategoryRepository _categoryRepository;

        /* Constructor with database context and category repository for relationship loading */
        public ProductRepository(DataDbContext dataDbContext, ICategoryRepository categoryRepository)
        {
            DataDbContext = dataDbContext;
            _categoryRepository = categoryRepository;
        }

        /* Add new product to database using Entity Framework */
        public void Add(Product product)
        {
            DataDbContext.Product.Add(product);
            DataDbContext.SaveChanges();
        }

        /* Delete product by ID with entity retrieval first */
        public void Delete(int Id)
        {
            var product = Find(Id);
            DataDbContext.Product.Remove(product);
            DataDbContext.SaveChanges();
        }

        /* Find product by ID and manually load category relationship */
        public Product Find(int Id)
        {
            var product = DataDbContext.Product.FirstOrDefault(x => x.ProductId == Id);

            /* Manually load category relationship using category repository */
            if (product != null)
            {
                product.Category = _categoryRepository.Find(product.CategoryID);
            }

            return product;
        }

        /* Update product entity using Entity Framework change tracking */
        public void Update(int id, Product product)
        {
            DataDbContext.Product.Update(product);
            DataDbContext.SaveChanges();
        }

        /* Retrieve all products with category relationships loaded */
        public List<Product> View()
        {
            var products = DataDbContext.Product.ToList();

            /* Manually load category information for each product to avoid lazy loading issues */
            foreach (var product in products)
            {
                product.Category = _categoryRepository.Find(product.CategoryID);
            }

            return products;
        }
    }
}