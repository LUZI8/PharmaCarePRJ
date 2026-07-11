namespace PharmaCare.Repositories.Interface
{
    /* Repository interface defining cart data access operations */
    public interface ICartRepository
    {
        /* Retrieve the complete cart entity with items for a specific user */
        Task<Cart> GetCartByUserIdAsync(int userId);

        /* Get formatted cart data as ViewModel with calculated totals for display */
        Task<CartViewModel> GetCartViewModelAsync(int userId);

        /* Add a product to user's cart or update quantity if already exists */
        Task<CartItem> AddToCartAsync(int userId, int productId, int quantity);

        /* Update quantity of existing cart item - returns success status */
        Task<bool> UpdateCartItemAsync(int userId, int productId, int quantity);

        /* Remove specific product from user's cart - returns success status */
        Task<bool> RemoveFromCartAsync(int userId, int productId);

        /* Remove all items from user's cart - returns success status */
        Task<bool> ClearCartAsync(int userId);

        /* Get total count of items in user's cart for navigation display */
        Task<int> GetCartItemCountAsync(int userId);
    }
}