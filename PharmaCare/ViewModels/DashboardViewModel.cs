namespace PharmaCare.Models
{
    public class DashboardViewModel
    {
        public List<Product> Products { get; set; } = new();
        public List<OrderViewModel> RecentOrders { get; set; } = new();
        public List<Product> LowStockProducts { get; set; } = new();
        public List<Product> ExpiringProducts { get; set; } = new();

        public int OrderCount { get; set; }
        public int InventoryCount { get; set; }
        public int CustomerCount { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public int ExpiringSoonCount { get; set; }
        public int PendingPickupCount { get; set; }
        public int FeedbackCount { get; set; }
        public int OrdersToday { get; set; }

        public decimal TotalRevenue { get; set; }
        public decimal RevenueToday { get; set; }
    }
}
