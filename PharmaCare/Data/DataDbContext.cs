using Microsoft.EntityFrameworkCore;
using PharmaCare.Models;

namespace PharmaCare.Data
{
    /* Entity Framework DbContext for PharmaCare application with comprehensive database configuration */
    public class DataDbContext : DbContext
    {
        /* Constructor accepting DbContext options for dependency injection configuration */
        public DataDbContext(DbContextOptions<DataDbContext> options) : base(options)
        {
        }

        /* DbSet properties defining database tables and entity collections */
        public DbSet<Category> Category { get; set; }
        public DbSet<Product> Product { get; set; }
        public DbSet<Order> Order { get; set; }
        public DbSet<User> User { get; set; }
        public DbSet<Cart> Cart { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<ContactMessage> ContactMessages { get; set; }
        public DbSet<PrescriptionReservation> PrescriptionReservations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            /* Create unique index on category name to prevent duplicate categories */
            modelBuilder.Entity<Category>()
                .HasIndex(c => c.CategoryName)
                .IsUnique();

            /* Composite unique index preventing duplicate product names within same category */
            modelBuilder.Entity<Product>()
                .HasIndex(p => new { p.ProductName, p.CategoryID })
                .IsUnique();

            /* Explicit requirement for prescription flag to prevent null values */
            modelBuilder.Entity<Product>()
                .Property(p => p.RequiresPrescription)
                .IsRequired();

            /* Configure decimal precision for monetary values to ensure accurate calculations */
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            /* Configure one-to-many relationship between User and Cart */
            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId);

            /* Configure one-to-many relationship between Cart and CartItems */
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(ci => ci.CartId);

            /* Configure relationship between CartItem and Product */
            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany()
                .HasForeignKey(ci => ci.ProductId);

            /* Configure one-to-many relationship between User and Orders */
            modelBuilder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId);

            /* Configure one-to-many relationship between Order and OrderItems */
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId);

            /* Configure relationship between OrderItem and Product */
            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId);

            /* Configure ContactMessage-User relationship with SET NULL on delete */
            modelBuilder.Entity<ContactMessage>()
                .HasOne(cm => cm.User)
                .WithMany()
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            /* Configure prescription reservation relationships with cascade delete */
            modelBuilder.Entity<PrescriptionReservation>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PrescriptionReservation>()
                .HasOne(r => r.Product)
                .WithMany()
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            /* Configure Product-Category relationship with restricted delete to protect data integrity */
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}