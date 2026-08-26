namespace PharmaCare.Repositories.Repository
{
    /// <summary>
    /// Product repository with eager-loaded category relationships.
    /// Loading categories with Include removes the previous N+1 query pattern that caused
    /// one extra Category query for every product rendered in the storefront/admin UI.
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        private readonly DataDbContext DataDbContext;

        public ProductRepository(DataDbContext dataDbContext, ICategoryRepository categoryRepository)
        {
            DataDbContext = dataDbContext;
        }

        public void Add(Product product)
        {
            DataDbContext.Product.Add(product);
            DataDbContext.SaveChanges();
        }

        public void Delete(int Id)
        {
            var product = Find(Id);
            if (product == null) return;
            DataDbContext.Product.Remove(product);
            DataDbContext.SaveChanges();
        }

        public Product Find(int Id)
        {
            return DataDbContext.Product
                .Include(p => p.Category)
                .Include(p => p.Images)
                .FirstOrDefault(x => x.ProductId == Id);
        }

        public void Update(int id, Product product)
        {
            DataDbContext.Product.Update(product);
            DataDbContext.SaveChanges();
        }

        public List<Product> View()
        {
            return DataDbContext.Product
                .Include(p => p.Category)
                .Include(p => p.Images)
                .AsSplitQuery()
                .ToList();
        }
    }
}