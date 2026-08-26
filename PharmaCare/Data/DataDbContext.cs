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
        public DbSet<Pharmacy> Pharmacies { get; set; }
        public DbSet<PharmacyProduct> PharmacyProducts { get; set; }
        public DbSet<PharmacyHour> PharmacyHours { get; set; }
        public DbSet<PharmacyDeliveryZone> PharmacyDeliveryZones { get; set; }
        public DbSet<PharmacyStaff> PharmacyStaff { get; set; }
        public DbSet<MarketplaceOrder> MarketplaceOrders { get; set; }
        public DbSet<MarketplaceOrderItem> MarketplaceOrderItems { get; set; }
        public DbSet<MarketplacePrescriptionRequest> MarketplacePrescriptionRequests { get; set; }
        public DbSet<MarketplaceOrderStatusHistory> MarketplaceOrderStatusHistory { get; set; }
        public DbSet<CustomerAddress> CustomerAddresses { get; set; }
        public DbSet<MarketplaceNotification> MarketplaceNotifications { get; set; }
        public DbSet<MarketplaceAuditLog> MarketplaceAuditLogs { get; set; }
        public DbSet<MarketplaceDeliveryAssignment> MarketplaceDeliveryAssignments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>().HasIndex(c => c.CategoryName).IsUnique();
            modelBuilder.Entity<Product>().HasIndex(p => new { p.ProductName, p.CategoryID }).IsUnique();
            modelBuilder.Entity<Product>().Property(p => p.SKU).HasMaxLength(50);
            modelBuilder.Entity<Product>().Property(p => p.Barcode).HasMaxLength(64);
            modelBuilder.Entity<Product>().Property(p => p.Manufacturer).HasMaxLength(150);
            modelBuilder.Entity<Product>().Property(p => p.ReorderLevel).HasDefaultValue(10);
            modelBuilder.Entity<Product>().HasIndex(p => p.SKU).IsUnique().HasFilter("[SKU] IS NOT NULL");
            modelBuilder.Entity<Product>().HasIndex(p => p.Barcode).IsUnique().HasFilter("[Barcode] IS NOT NULL");
            modelBuilder.Entity<Product>().Property(p => p.RequiresPrescription).IsRequired();
            modelBuilder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<CartItem>().Property(ci => ci.Price).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Order>().Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<OrderItem>().Property(oi => oi.Price).HasColumnType("decimal(18,2)");

            modelBuilder.Entity<ProductImage>().Property(i => i.ImageUrl).HasMaxLength(500).IsRequired();
            modelBuilder.Entity<ProductImage>().HasIndex(i => new { i.ProductId, i.DisplayOrder });
            modelBuilder.Entity<ProductImage>().HasOne(i => i.Product).WithMany(p => p.Images).HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Cart>().HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId);
            modelBuilder.Entity<CartItem>().HasOne(ci => ci.Cart).WithMany(c => c.CartItems).HasForeignKey(ci => ci.CartId);
            modelBuilder.Entity<CartItem>().HasOne(ci => ci.Product).WithMany().HasForeignKey(ci => ci.ProductId);
            modelBuilder.Entity<Order>().HasOne(o => o.User).WithMany().HasForeignKey(o => o.UserId);
            modelBuilder.Entity<OrderItem>().HasOne(oi => oi.Order).WithMany(o => o.OrderItems).HasForeignKey(oi => oi.OrderId);
            modelBuilder.Entity<OrderItem>().HasOne(oi => oi.Product).WithMany().HasForeignKey(oi => oi.ProductId);
            modelBuilder.Entity<ContactMessage>().HasOne(cm => cm.User).WithMany().HasForeignKey(cm => cm.UserId).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<PrescriptionReservation>().HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PrescriptionReservation>().HasOne(r => r.Product).WithMany().HasForeignKey(r => r.ProductId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Product>().HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryID).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Pharmacy>().HasIndex(p => p.Name);
            modelBuilder.Entity<Pharmacy>().Property(p => p.DeliveryFee).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Pharmacy>().Property(p => p.MinimumOrder).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Pharmacy>().Property(p => p.Rating).HasColumnType("decimal(4,2)");
            modelBuilder.Entity<Pharmacy>().Property(p => p.Latitude).HasColumnType("decimal(9,6)");
            modelBuilder.Entity<Pharmacy>().Property(p => p.Longitude).HasColumnType("decimal(9,6)");

            modelBuilder.Entity<PharmacyProduct>().HasIndex(x => new { x.PharmacyId, x.ProductId }).IsUnique();
            modelBuilder.Entity<PharmacyProduct>().Property(x => x.Price).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<PharmacyProduct>().Property(x => x.CompareAtPrice).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<PharmacyProduct>().HasOne(x => x.Pharmacy).WithMany(p => p.Products).HasForeignKey(x => x.PharmacyId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PharmacyProduct>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PharmacyHour>().HasIndex(x => new { x.PharmacyId, x.DayOfWeek }).IsUnique();
            modelBuilder.Entity<PharmacyHour>().HasOne(x => x.Pharmacy).WithMany(p => p.Hours).HasForeignKey(x => x.PharmacyId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PharmacyDeliveryZone>().Property(x => x.DeliveryFee).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<PharmacyDeliveryZone>().HasOne(x => x.Pharmacy).WithMany(p => p.DeliveryZones).HasForeignKey(x => x.PharmacyId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PharmacyStaff>().HasIndex(x => new { x.PharmacyId, x.UserId }).IsUnique();
            modelBuilder.Entity<PharmacyStaff>().HasOne(x => x.Pharmacy).WithMany(p => p.Staff).HasForeignKey(x => x.PharmacyId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PharmacyStaff>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MarketplaceOrder>().HasIndex(x => x.OrderNumber).IsUnique();
            modelBuilder.Entity<MarketplaceOrder>().Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<MarketplaceOrder>().Property(x => x.DeliveryFee).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<MarketplaceOrder>().Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<MarketplaceOrder>().HasOne(x => x.Pharmacy).WithMany(p => p.MarketplaceOrders).HasForeignKey(x => x.PharmacyId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MarketplaceOrder>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MarketplaceOrderItem>().Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<MarketplaceOrderItem>().Property(x => x.LineTotal).HasColumnType("decimal(18,2)");
            modelBuilder.Entity<MarketplaceOrderItem>().HasOne(x => x.MarketplaceOrder).WithMany(o => o.Items).HasForeignKey(x => x.MarketplaceOrderId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<MarketplaceOrderItem>().HasOne(x => x.PharmacyProduct).WithMany().HasForeignKey(x => x.PharmacyProductId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MarketplaceOrderItem>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MarketplacePrescriptionRequest>().HasIndex(x => x.RequestNumber).IsUnique();
            modelBuilder.Entity<MarketplacePrescriptionRequest>().HasIndex(x => new { x.PharmacyId, x.Status });
            modelBuilder.Entity<MarketplacePrescriptionRequest>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MarketplacePrescriptionRequest>().HasOne(x => x.Pharmacy).WithMany().HasForeignKey(x => x.PharmacyId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MarketplacePrescriptionRequest>().HasOne(x => x.PharmacyProduct).WithMany().HasForeignKey(x => x.PharmacyProductId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MarketplacePrescriptionRequest>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MarketplaceOrderStatusHistory>().HasIndex(x => new { x.MarketplaceOrderId, x.ChangedAt });
            modelBuilder.Entity<MarketplaceOrderStatusHistory>().HasOne(x => x.MarketplaceOrder).WithMany().HasForeignKey(x => x.MarketplaceOrderId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<MarketplaceOrderStatusHistory>().HasOne(x => x.ChangedByUser).WithMany().HasForeignKey(x => x.ChangedByUserId).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CustomerAddress>().HasIndex(x => new { x.UserId, x.IsDefault });
            modelBuilder.Entity<CustomerAddress>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<CustomerAddress>().Property(x => x.Latitude).HasColumnType("decimal(9,6)");
            modelBuilder.Entity<CustomerAddress>().Property(x => x.Longitude).HasColumnType("decimal(9,6)");

            modelBuilder.Entity<MarketplaceNotification>().HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
            modelBuilder.Entity<MarketplaceNotification>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MarketplaceAuditLog>().HasIndex(x => new { x.EntityName, x.EntityId, x.CreatedAt });
            modelBuilder.Entity<MarketplaceAuditLog>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<MarketplaceDeliveryAssignment>().HasIndex(x => x.MarketplaceOrderId).IsUnique();
            modelBuilder.Entity<MarketplaceDeliveryAssignment>().HasIndex(x => new { x.DriverUserId, x.Status });
            modelBuilder.Entity<MarketplaceDeliveryAssignment>().HasOne(x => x.MarketplaceOrder).WithMany().HasForeignKey(x => x.MarketplaceOrderId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<MarketplaceDeliveryAssignment>().HasOne(x => x.DriverUser).WithMany().HasForeignKey(x => x.DriverUserId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
