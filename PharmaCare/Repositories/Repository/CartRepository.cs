namespace PharmaCare.Repositories.Repository
{
    public class CartRepository : ICartRepository
    {
        private readonly DataDbContext _context;

        public CartRepository(DataDbContext context)
        {
            _context = context;
        }

        public async Task<Cart> GetCartByUserIdAsync(int userId)
        {
            var cart = await _context.Cart
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);

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

        public async Task<CartViewModel> GetCartViewModelAsync(int userId)
        {
            var cart = await GetCartByUserIdAsync(userId);
            var model = new CartViewModel
            {
                Items = new List<CartItemViewModel>(),
                SubTotal = 0,
                Tax = 0,
                ShippingCost = 0,
                Total = 0,
                ItemCount = 0
            };

            foreach (var item in cart.CartItems ?? new List<CartItem>())
            {
                if (item.Product == null) continue;
                var lineTotal = item.Price * item.Quantity;
                model.Items.Add(new CartItemViewModel
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product.ProductName,
                    ImageUrl = item.Product.ImageUrl,
                    Quantity = item.Quantity,
                    Price = item.Price,
                    Total = lineTotal
                });
                model.SubTotal += lineTotal;
                model.ItemCount += item.Quantity;
            }

            if (model.ItemCount > 0)
            {
                model.Tax = Math.Round(model.SubTotal * 0.05m, 2);
                var orderTotal = model.SubTotal + model.Tax;
                model.ShippingCost = orderTotal >= 50 ? 0 : 5.99m;
                model.Total = orderTotal + model.ShippingCost;
            }

            return model;
        }

        public async Task<CartItem> AddToCartAsync(int userId, int productId, int quantity)
        {
            quantity = Math.Max(1, quantity);
            var product = await GetSellableProductAsync(productId);
            if (product == null || quantity > product.Stock) return null;

            var cart = await GetCartByUserIdAsync(userId);
            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);

            if (cartItem != null)
            {
                cartItem.Quantity = quantity;
                cartItem.Price = product.Price;
                cartItem.UpdatedAt = DateTime.Now;
            }
            else
            {
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

            cart.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return cartItem;
        }

        public async Task<CartItem> IncrementCartItemAsync(int userId, int productId, int quantityToAdd = 1)
        {
            quantityToAdd = Math.Max(1, quantityToAdd);
            var product = await GetSellableProductAsync(productId);
            if (product == null) return null;

            var cart = await GetCartByUserIdAsync(userId);
            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
            var desiredQuantity = (cartItem?.Quantity ?? 0) + quantityToAdd;
            if (desiredQuantity > product.Stock) return null;

            if (cartItem != null)
            {
                cartItem.Quantity = desiredQuantity;
                cartItem.Price = product.Price;
                cartItem.UpdatedAt = DateTime.Now;
            }
            else
            {
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

        public async Task<bool> UpdateCartItemAsync(int userId, int productId, int quantity)
        {
            if (quantity <= 0) return await RemoveFromCartAsync(userId, productId);

            var product = await GetSellableProductAsync(productId);
            if (product == null || quantity > product.Stock) return false;

            var cart = await GetCartByUserIdAsync(userId);
            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
            if (cartItem == null) return false;

            cartItem.Quantity = quantity;
            cartItem.Price = product.Price;
            cartItem.UpdatedAt = DateTime.Now;
            cart.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveFromCartAsync(int userId, int productId)
        {
            var cart = await GetCartByUserIdAsync(userId);
            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId);
            if (cartItem == null) return false;

            _context.CartItems.Remove(cartItem);
            cart.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ClearCartAsync(int userId)
        {
            var cart = await GetCartByUserIdAsync(userId);
            if (cart.CartItems == null || !cart.CartItems.Any()) return false;

            _context.CartItems.RemoveRange(cart.CartItems);
            cart.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetCartItemCountAsync(int userId)
        {
            // Header/floating-cart count is requested on many pages: do a single SUM query instead of
            // loading the cart, every cart item and every product (and never create an empty cart here).
            return await _context.CartItems
                .AsNoTracking()
                .Where(ci => ci.Cart.UserId == userId && ci.Cart.IsActive)
                .SumAsync(ci => (int?)ci.Quantity) ?? 0;
        }

        private Task<Product> GetSellableProductAsync(int productId)
        {
            var now = DateTime.Now;
            return _context.Product.FirstOrDefaultAsync(p =>
                p.ProductId == productId &&
                p.IsActive &&
                !p.RequiresPrescription &&
                p.Stock > 0 &&
                p.ExpiryDate > now);
        }
    }
}