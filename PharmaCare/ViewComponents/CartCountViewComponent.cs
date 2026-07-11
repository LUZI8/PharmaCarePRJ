namespace PharmaCare.ViewComponents
{
    /* ViewComponent for displaying cart item count in the navigation header */
    public class CartCountViewComponent : ViewComponent
    {
        private readonly ICartRepository _cartRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /* Constructor injection for cart repository and HTTP context access */
        public CartCountViewComponent(ICartRepository cartRepository, IHttpContextAccessor httpContextAccessor)
        {
            _cartRepository = cartRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        /* Main method called when ViewComponent is rendered - returns cart count as string content */
        public async Task<IViewComponentResult> InvokeAsync()
        {
            /* Retrieve user ID from session - returns null if user not logged in */
            var userId = _httpContextAccessor.HttpContext.Session.GetInt32("UserId");

            /* Return "0" if user is not authenticated */
            if (userId == null)
            {
                return Content("0");
            }

            /* Get cart item count from repository using authenticated user ID */
            int cartCount = await _cartRepository.GetCartItemCountAsync(userId.Value);
            /* Return count as string content for display in view */
            return Content(cartCount.ToString());
        }
    }
}