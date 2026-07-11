namespace PharmaCare.Models
{
    /* ViewModel aggregating data for the admin dashboard display */
    public class DashboardViewModel
    {
        /* Collection of products for the products management table with default initialization */
        public List<Product> Products { get; set; } = new List<Product>();

        /* Collection of recent orders for the sidebar display with default initialization */
        public List<OrderViewModel> RecentOrders { get; set; } = new List<OrderViewModel>();

        /* Total count of orders for dashboard statistics card */
        public int OrderCount { get; set; }

        /* Total count of inventory items for dashboard statistics card */
        public int InventoryCount { get; set; }
    }
}