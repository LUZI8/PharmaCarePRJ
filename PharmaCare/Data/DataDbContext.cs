using Microsoft.EntityFrameworkCore;
using PharmaCare.Models;

namespace PharmaCare.Data
{
    public class DataDbContext : DbContext
    {
        public DataDbContext(DbContextOptions<DataDbContext> options) : base(options) { }

        public DbSet<Category> Category { get; set; }
        public DbSet<Product> Product { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
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

            modelBuilder.Entity<Category>().HasIndex(c => c.CategoryName).IsUnique();
            modelBuilder.Entity<Product>().HasIndex(p => new { p.ProductName, p.CategoryID }).IsUnique();

            modelBuilder.Entity<Product>().Property(p => p.SKU).HasMaxLength(50);
            modelBuilder.Entity<Product>().Property(p => p.Barcode).HasMaxLength(64);
            modelBuilder.Entity<Product>().Property(p => p.Manufacturer).HasMaxLength(150);
            modelBuilder.Entity<Product>().Property(p => p.ReorderLevel).HasDefaultValue(10);

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.SKU)
                .IsUnique()
                .HasFilter("[SKU] IS NOT NULL");

            modelBuilder.Entity<Product>()
                .HasIndex(p => p.Barcode)
                .IsUnique()
                .HasFilter("[Barcode] IS NOT NULL");

            modelBuilder.Entity<Product>().Property(p => p.RequiresPrescription).IsRequired();
            modelBuilder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<CartItem>().Property(ci => ci.Price).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<OrderItem>().Property(oi => oi.Price).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ProductImage>().Property(i => i.ImageUrl).HasMaxLength(500).IsRequired();
            modelBuilder.Entity<ProductImage>().HasIndex(i => new { i.ProductId, i.DisplayOrder });
            modelBuilder.Entity<ProductImage>()
                .HasOne(i => i.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Cart>().HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId);
            modelBuilder.Entity<CartItem>().HasOne(ci => ci.Cart).WithMany(c => c.CartItems).HasForeignKey(ci => ci.CartId);
            modelBuilder.Entity<CartItem>().HasOne(ci => ci.Product).WithMany().HasForeignKey(ci => ci.ProductId);
            modelBuilder.Entity<Order>().HasOne(o => o.User).WithMany().HasForeignKey(o => o.UserId);
            modelBuilder.Entity<OrderItem>().HasOne(oi => oi.Order).WithMany(o => o.OrderItems).HasForeignKey(oi => oi.OrderId);
            modelBuilder.Entity<OrderItem>().HasOne(oi => oi.Product).WithMany().HasForeignKey(oi => oi.ProductId);

            modelBuilder.Entity<ContactMessage>()
                .HasOne(cm => cm.User).WithMany().HasForeignKey(cm => cm.UserId).OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PrescriptionReservation>()
                .HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PrescriptionReservation>()
                .HasOne(r => r.Product).WithMany().HasForeignKey(r => r.ProductId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryID).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
