namespace PharmaCare.ViewModels
{
    /* Main cart ViewModel containing cart summary and collection of items */
    public class CartViewModel
    {
        /* Collection of cart items for display in cart table */
        public List<CartItemViewModel> Items { get; set; }

        /* Subtotal before tax and shipping calculations */
        public decimal SubTotal { get; set; }

        /* Tax amount calculated based on subtotal */
        public decimal Tax { get; set; }

        /* Shipping cost based on delivery location */
        public decimal ShippingCost { get; set; }

        /* Final total including subtotal, tax, and shipping */
        public decimal Total { get; set; }

        /* Total count of items in cart for display purposes */
        public int ItemCount { get; set; }
    }

    /* Individual cart item ViewModel representing a single product in the cart */
    public class CartItemViewModel
    {
        /* Product identifier for cart operations and linking */
        public int ProductId { get; set; }

        /* Product name for display in cart table */
        public string ProductName { get; set; }

        /* Product image URL for thumbnail display in cart */
        public string ImageUrl { get; set; }

        /* Quantity of this product in the cart */
        public int Quantity { get; set; }

        /* Unit price of the product */
        public decimal Price { get; set; }

        /* Calculated total for this cart item (Price × Quantity) */
        public decimal Total { get; set; }
    }
}