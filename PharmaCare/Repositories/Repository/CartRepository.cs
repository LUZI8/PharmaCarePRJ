namespace PharmaCare.Repositories.Repository
{
    /* Repository implementation for cart data access operations with Entity Framework */
    public class CartRepository : ICartRepository
    {
        private readonly DataDbContext _context;

        /* Constructor injection for database context */
        public CartRepository(DataDbContext context)
        {
            _context = context;
        }

        /* Retrieve cart with related entities using Include for eager loading */
        public async Task<Cart> GetCartByUserIdAsync(int userId)
        {
            /* Load cart with cart items and their associated products in single query */
            var cart = await _context.Cart
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);

            /* Create new cart automatically if user doesn't have one */
            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsActive = true,
                    CartItems = new List<CartItem>()
                };
                _context.Cart.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }
        /* Generate formatted cart data with business logic calculations */
        public async Task<CartViewModel> GetCartViewModelAsync(int userId)
        {
            var cart = await GetCartByUserIdAsync(userId);

            /* Initialize ViewModel with default values */
            var cartViewModel = new CartViewModel
            {
                Items = new List<CartItemViewModel>(),
                SubTotal = 0,
                Tax = 0,
                ShippingCost = 0,
                Total = 0,
                ItemCount = 0
            };

            /* Process cart items if any exist */
            if (cart.CartItems != null && cart.CartItems.Any())
            {
                foreach (var item in cart.CartItems)
                {
                    /* Map entity to ViewModel with calculated totals */
                    var cartItemViewModel = new CartItemViewModel
                    {
                        ProductId = item.ProductId,
                        ProductName = item.Product.ProductName,
                        ImageUrl = item.Product.ImageUrl,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        Total = item.Price * item.Quantity
                    };

                    cartViewModel.Items.Add(cartItemViewModel);
                    cartViewModel.SubTotal += cartItemViewModel.Total;
                    cartViewModel.ItemCount += item.Quantity;
                }

                /* Apply business rules for tax and shipping calculations */
                cartViewModel.Tax = Math.Round(cartViewModel.SubTotal * 0.05m, 2);

                /* Simple shipping: Free above $50, otherwise $5.99 */
                decimal orderTotal = cartViewModel.SubTotal + cartViewModel.Tax;
                cartViewModel.ShippingCost = orderTotal >= 50 ? 0 : 5.99m;

                /* Calculate final total */
                cartViewModel.Total = orderTotal + cartViewModel.ShippingCost;
            }

            return cartViewModel;
        }

        /* Add product to cart or set specific quantity - FIXED for proper quantity handling */
        public async Task<CartItem> AddToCartAsync(int userId, int productId, int quantity)
        {
            var cart = await GetCartByUserIdAsync(userId);
            var product = await _context.Product.FindAsync(productId);

            /* Return null if product doesn't exist */
            if (product == null)
            {
                return null;
            }

            /* Check if product already exists in cart */
            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);

            if (cartItem != null)
            {
                /* Set the exact quantity (not add to existing) - this is for setting specific amounts */
                cartItem.Quantity = quantity;
                cartItem.UpdatedAt = DateTime.Now;
            }
            else
            {
                /* Create new cart item with current product price */
                cartItem = new CartItem
                {
                    CartId = cart.CartId,
                    ProductId = productId,
                    Quantity = quantity,
                    Price = product.Price,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                cart.CartItems.Add(cartItem);
            }

            /* Update cart timestamp and save changes */
            cart.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return cartItem;
        }

        /* Add a method specifically for incrementing cart items */
        public async Task<CartItem> IncrementCartItemAsync(int userId, int productId, int quantityToAdd = 1)
        {
            var cart = await GetCartByUserIdAsync(userId);
            var product = await _context.Product.FindAsync(productId);

            if (product == null)
            {
                return null;
            }

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);

            if (cartItem != null)
            {
                /* Add to existing quantity */
                cartItem.Quantity += quantityToAdd;
                cartItem.UpdatedAt = DateTime.Now;
            }
            else
            {
                /* Create new cart item */
                cartItem = new CartItem
                {
                    CartId = cart.CartId,
                    ProductId = productId,
                    Quantity = quantityToAdd,
                    Price = product.Price,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                cart.CartItems.Add(cartItem);
            }

            cart.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return cartItem;
        }

        /* Update quantity of existing cart item with validation */
        public async Task<bool> UpdateCartItemAsync(int userId, int productId, int quantity)
        {
            var cart = await GetCartByUserIdAsync(userId);
            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);

            /* Return false if cart item doesn't exist */
            if (cartItem == null)
            {
                return false;
            }

            /* Remove item if quantity is zero or negative */
            if (quantity <= 0)
            {
                return await RemoveFromCartAsync(userId, productId);
            }

            /* Update quantity and timestamps */
            cartItem.Quantity = quantity;
            cartItem.UpdatedAt = DateTime.Now;
            cart.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return true;
        }

        /* Remove specific product from user's cart */
        public async Task<bool> RemoveFromCartAsync(int userId, int productId)
        {
            var cart = await GetCartByUserIdAsync(userId);
            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);

            /* Return false if item doesn't exist in cart */
            if (cartItem == null)
            {
                return false;
            }

            /* Remove cart item entity and update cart timestamp */
            _context.CartItems.Remove(cartItem);
            cart.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return true;
        }

        /* Clear all items from user's cart */
        public async Task<bool> ClearCartAsync(int userId)
        {
            var cart = await GetCartByUserIdAsync(userId);

            /* Return false if cart is already empty */
            if (cart.CartItems == null || !cart.CartItems.Any())
            {
                return false;
            }

            /* Remove all cart items in single operation */
            _context.CartItems.RemoveRange(cart.CartItems);
            cart.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return true;
        }

        /* Calculate total quantity of items in cart for navigation display */
        public async Task<int> GetCartItemCountAsync(int userId)
        {
            var cart = await GetCartByUserIdAsync(userId);

            /* Sum all quantities or return 0 if cart is empty */
            return cart.CartItems?.Sum(ci => ci.Quantity) ?? 0;
        }
    }
}