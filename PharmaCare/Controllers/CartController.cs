namespace PharmaCare.Controllers
{

    public class CartController : Controller
    {
        private readonly ICartRepository _cartRepository;
        private readonly IProductRepository _productRepository;

        /* Constructor with dependency injection for cart and product operations */
        public CartController(ICartRepository cartRepository, IProductRepository productRepository)
        {
            _cartRepository = cartRepository;
            _productRepository = productRepository;
        }

        /* Redirect cart index requests to frontend cart page */
        public IActionResult Index()
        {
            return RedirectToAction("Cart", "FrontEnd");
        }

        /* AJAX endpoint to get current cart count - NEW METHOD */
        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = true, count = 0 });
            }

            try
            {
                var itemCount = await _cartRepository.GetCartItemCountAsync(userId.Value);
                return Json(new { success = true, count = itemCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message, count = 0 });
            }
        }

        /* AJAX endpoint to add products to cart with stock validation */
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            /* Authentication check for cart operations */
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Please log in to add items to cart", redirect = Url.Action("Login", "Account") });
            }

            try
            {
                /* Validate product existence and stock availability */
                var product = _productRepository.Find(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                // Get current cart to check existing quantity
                var cart = await _cartRepository.GetCartByUserIdAsync(userId.Value);
                var existingItem = cart.CartItems?.FirstOrDefault(ci => ci.ProductId == productId);
                var currentQuantity = existingItem?.Quantity ?? 0;
                var newTotalQuantity = currentQuantity + quantity;

                if (product.Stock < newTotalQuantity)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Sorry, only {product.Stock} units available in stock. You currently have {currentQuantity} in your cart."
                    });
                }

                /* Add item to cart and return updated cart count */
                await _cartRepository.AddToCartAsync(userId.Value, productId, newTotalQuantity);
                var itemCount = await _cartRepository.GetCartItemCountAsync(userId.Value);

                return Json(new { success = true, message = "Item added to cart", cartCount = itemCount });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> UpdateCart(int productId, int quantity)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Please log in to update your cart" });
            }

            try
            {
                /* Validate stock before updating quantity */
                var product = _productRepository.Find(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                if (product.Stock < quantity)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Cannot update quantity. Only {product.Stock} units available in stock."
                    });
                }

                /* Use UpdateCartItemAsync instead of AddToCartAsync for updates */
                var updateResult = await _cartRepository.UpdateCartItemAsync(userId.Value, productId, quantity);

                if (!updateResult)
                {
                    return Json(new { success = false, message = "Failed to update cart item" });
                }

                var cartViewModel = await _cartRepository.GetCartViewModelAsync(userId.Value);

                return Json(new
                {
                    success = true,
                    message = "Cart updated",
                    cartCount = cartViewModel.ItemCount,
                    subtotal = cartViewModel.SubTotal.ToString("0.00"),
                    tax = cartViewModel.Tax.ToString("0.00"),
                    shipping = cartViewModel.ShippingCost.ToString("0.00"),
                    total = cartViewModel.Total.ToString("0.00")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Please log in to update your cart" });
            }

            try
            {
                /* Remove item and return updated cart totals */
                var removeResult = await _cartRepository.RemoveFromCartAsync(userId.Value, productId);

                if (!removeResult)
                {
                    return Json(new { success = false, message = "Item not found in cart" });
                }

                var cartViewModel = await _cartRepository.GetCartViewModelAsync(userId.Value);

                return Json(new
                {
                    success = true,
                    message = "Item removed from cart",
                    cartCount = cartViewModel.ItemCount,
                    subtotal = cartViewModel.SubTotal.ToString("0.00"),
                    tax = cartViewModel.Tax.ToString("0.00"),
                    shipping = cartViewModel.ShippingCost.ToString("0.00"),
                    total = cartViewModel.Total.ToString("0.00")
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        /* AJAX endpoint to clear entire cart */
        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Please log in to update your cart" });
            }

            try
            {
                var clearResult = await _cartRepository.ClearCartAsync(userId.Value);

                if (!clearResult)
                {
                    return Json(new { success = false, message = "Cart is already empty" });
                }

                return Json(new { success = true, message = "Cart cleared" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /* API endpoint to check if product is already in cart */
        [HttpGet]
        public async Task<IActionResult> IsProductInCart(int productId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { inCart = false });
            }

            try
            {
                /* Check cart for specific product and return quantity if found */
                var cart = await _cartRepository.GetCartByUserIdAsync(userId.Value);
                var cartItem = cart.CartItems?.FirstOrDefault(i => i.ProductId == productId);

                if (cartItem != null)
                {
                    return Json(new
                    {
                        inCart = true,
                        quantity = cartItem.Quantity
                    });
                }
                else
                {
                    return Json(new { inCart = false });
                }
            }
            catch
            {
                return Json(new { inCart = false });
            }
        }

        /* Redirect helper for frontend cart access */
        public IActionResult RedirectToFrontEndCart()
        {
            return RedirectToAction("Cart", "FrontEnd");
        }
    }
}