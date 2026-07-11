// Combined Cart functionality - Cart operations + Cart count loader
(function () {
    'use strict';

    // ===========================================
    // CART PAGE FUNCTIONALITY (Plus/Minus/Remove)
    // ===========================================

    // Prevent multiple initializations
    if (window.cartJSInitialized) {
        console.log('Cart JS already initialized, skipping...');
        return;
    }
    window.cartJSInitialized = true;

    // Global flags to prevent concurrent requests
    var activeRequests = {};
    var isUpdating = false;

    // Debounce function to limit rapid calls
    function debounce(func, delay) {
        let timeoutId;
        return function (...args) {
            clearTimeout(timeoutId);
            timeoutId = setTimeout(() => func.apply(this, args), delay);
        };
    }

    // Initialize cart page functionality when DOM is ready
    function initializeCart() {
        console.log('Initializing cart functionality...');

        // IMPORTANT: Remove ALL existing cart event listeners to prevent conflicts
        $(document).off('click.cart');
        $(document).off('input.cart');
        $(document).off('click.cartUpdate');
        $(document).off('input.cartUpdate');
        $(document).off('click', '.js-btn-plus');
        $(document).off('click', '.js-btn-minus');
        $(document).off('click', '.remove-item');
        $(document).off('click', '.clear-cart');
        $(document).off('input', '.quantity-input');

        // Remove any direct element event handlers
        $('.js-btn-plus').off();
        $('.js-btn-minus').off();
        $('.quantity-input').off();
        $('.remove-item').off();
        $('.clear-cart').off();

        console.log('All existing event handlers removed');

        // Use event delegation with unique namespace
        $(document).on('click.cartSingle', '.js-btn-plus', function (e) {
            e.preventDefault();
            e.stopPropagation();
            e.stopImmediatePropagation();
            console.log('Plus button clicked');
            handleQuantityChange($(this), 'plus');
        });

        $(document).on('click.cartSingle', '.js-btn-minus', function (e) {
            e.preventDefault();
            e.stopPropagation();
            e.stopImmediatePropagation();
            console.log('Minus button clicked');
            handleQuantityChange($(this), 'minus');
        });

        $(document).on('input.cartSingle', '.quantity-input', debounce(function (e) {
            e.preventDefault();
            e.stopPropagation();
            e.stopImmediatePropagation();
            console.log('Quantity input changed');
            handleQuantityInput($(this));
        }, 500));

        $(document).on('click.cartSingle', '.remove-item', function (e) {
            e.preventDefault();
            e.stopPropagation();
            e.stopImmediatePropagation();
            console.log('Remove button clicked');
            handleRemoveItem($(this));
        });

        $(document).on('click.cartSingle', '.clear-cart', function (e) {
            e.preventDefault();
            e.stopPropagation();
            e.stopImmediatePropagation();
            console.log('Clear cart button clicked');
            handleClearCart();
        });

        console.log('New event handlers attached');
    }

    function handleQuantityChange($button, action) {
        console.log('handleQuantityChange called:', action);

        // Prevent rapid clicking
        if (isUpdating) {
            console.log('Update in progress, ignoring click');
            return;
        }

        var productId = $button.data('product-id');
        var $input = $button.closest('.input-group').find('.quantity-input');
        var currentVal = parseInt($input.val()) || 1;
        var newValue;

        if (action === 'plus') {
            newValue = currentVal + 1;
            console.log('Plus: ' + currentVal + ' -> ' + newValue);
        } else if (action === 'minus') {
            newValue = Math.max(1, currentVal - 1);
            console.log('Minus: ' + currentVal + ' -> ' + newValue);
        }

        // Update input immediately for better UX
        $input.val(newValue);

        // Call update function
        updateCartItem(productId, newValue);
    }

    function handleQuantityInput($input) {
        var $row = $input.closest('tr');
        var productId = $row.data('product-id');
        var quantity = parseInt($input.val());

        if (isNaN(quantity) || quantity < 1) {
            $input.val(1);
            quantity = 1;
        }

        updateCartItem(productId, quantity);
    }

    function handleRemoveItem($button) {
        var productId = $button.data('product-id');
        removeCartItem(productId);
    }

    function handleClearCart() {
        if (confirm('Are you sure you want to clear your cart?')) {
            clearCart();
        }
    }

    function updateCartItem(productId, quantity) {
        console.log('updateCartItem called: Product ID =', productId, 'Quantity =', quantity);

        // Check if cartUrls is available
        if (!window.cartUrls || !window.cartUrls.updateCart) {
            console.error('Cart URLs not found. Make sure the URLs are passed from the server.');
            alert('Error: Cart configuration not found.');
            return;
        }

        // Prevent duplicate requests for the same product
        var requestKey = 'update_' + productId;
        if (activeRequests[requestKey]) {
            console.log('Request already in progress for product', productId);
            return;
        }

        // Set flags
        activeRequests[requestKey] = true;
        isUpdating = true;

        // Disable buttons during update
        var $row = $('tr[data-product-id="' + productId + '"]');
        $row.find('button').prop('disabled', true);

        console.log('Sending AJAX request...');

        $.ajax({
            url: window.cartUrls.updateCart,
            type: 'POST',
            data: {
                productId: productId,
                quantity: quantity
            },
            timeout: 10000,
            success: function (response) {
                console.log('AJAX success:', response);
                if (response.success) {
                    console.log('Cart updated successfully. New count:', response.cartCount);

                    // Update cart totals
                    updateCartTotals(response);

                    // Update product total for this row
                    updateRowTotal(productId, quantity);

                    // Update cart count in header using unified function
                    updateCartCountDisplay(response.cartCount);

                    // Show success notification
                    showNotification('Cart updated successfully', 'success');
                } else {
                    console.error('Error updating cart:', response.message);
                    alert(response.message || 'Error updating cart');
                    location.reload();
                }
            },
            error: function (xhr, status, error) {
                console.error('AJAX error:', status, error);
                alert('Error updating cart. Please try again.');
                location.reload();
            },
            complete: function () {
                console.log('AJAX complete');
                // Always clear flags and re-enable buttons
                delete activeRequests[requestKey];
                isUpdating = false;
                $row.find('button').prop('disabled', false);
            }
        });
    }

    function removeCartItem(productId) {
        if (!window.cartUrls || !window.cartUrls.removeFromCart) {
            console.error('Cart URLs not found.');
            alert('Error: Cart configuration not found.');
            return;
        }

        var requestKey = 'remove_' + productId;
        if (activeRequests[requestKey]) {
            console.log('Remove request already in progress for product', productId);
            return;
        }

        activeRequests[requestKey] = true;

        $.ajax({
            url: window.cartUrls.removeFromCart,
            type: 'POST',
            data: { productId: productId },
            success: function (response) {
                if (response.success) {
                    var $row = $('tr[data-product-id="' + productId + '"]');
                    $row.fadeOut(300, function () {
                        $(this).remove();

                        updateCartTotals(response);
                        updateCartCountDisplay(response.cartCount); // Use unified function
                        showNotification('Item removed from cart', 'success');

                        if (response.cartCount === 0) {
                            location.reload();
                        }
                    });
                } else {
                    alert(response.message);
                }
            },
            error: function () {
                alert('Error removing item from cart. Please try again.');
            },
            complete: function () {
                delete activeRequests[requestKey];
            }
        });
    }

    function clearCart() {
        if (!window.cartUrls || !window.cartUrls.clearCart) {
            console.error('Cart URLs not found.');
            alert('Error: Cart configuration not found.');
            return;
        }

        if (activeRequests.clearCart) {
            console.log('Clear cart request already in progress');
            return;
        }

        activeRequests.clearCart = true;

        $.ajax({
            url: window.cartUrls.clearCart,
            type: 'POST',
            success: function (response) {
                if (response.success) {
                    updateCartCountDisplay(0); // Update count before reload
                    location.reload();
                } else {
                    alert(response.message);
                }
            },
            error: function () {
                alert('Error clearing cart. Please try again.');
            },
            complete: function () {
                delete activeRequests.clearCart;
            }
        });
    }

    function updateCartTotals(response) {
        $('#subtotal').text('$' + response.subtotal.toFixed(2));
        $('#tax').text('$' + response.tax.toFixed(2));
        $('#shipping').text('$' + response.shipping.toFixed(2));
        $('#total').text('$' + response.total.toFixed(2));
    }

    function updateRowTotal(productId, quantity) {
        var $row = $('tr[data-product-id="' + productId + '"]');
        var priceText = $row.find('.product-price .text-black').text();
        var price = parseFloat(priceText.replace('$', ''));

        if (!isNaN(price)) {
            var total = price * quantity;
            $row.find('.product-total .text-black').text('$' + total.toFixed(2));
        }
    }

    function showNotification(message, type) {
        type = type || 'info';

        if ($('#toast-notification').length && $('#toast-message').length) {
            $('#toast-message').text(message);
            $('#toast-notification').addClass('show');
            setTimeout(function () {
                $('#toast-notification').removeClass('show');
            }, 3000);
        } else {
            console.log('Notification:', message);
        }
    }

    // ===========================================
    // CART COUNT LOADER FUNCTIONALITY
    // ===========================================

    // Global variables for cart count loading
    var cartCountLoaded = false;

    // Function to get cart count URL from multiple possible sources
    function getCartCountUrl() {
        // Try different URL sources in order of preference
        if (window.cartUrls && window.cartUrls.getCartCount) {
            return window.cartUrls.getCartCount;
        }
        if (window.cartCountUrl) {
            return window.cartCountUrl;
        }
        // Fallback - construct URL if we know the base structure
        if (window.location) {
            var baseUrl = window.location.origin;
            return baseUrl + '/Cart/GetCartCount';
        }
        return null;
    }

    // Function to load and update cart count
    function loadCartCount() {
        var cartCountUrl = getCartCountUrl();

        // Prevent multiple simultaneous calls
        if (cartCountLoaded || !cartCountUrl) {
            if (!cartCountUrl) {
                console.warn('Cart count URL not found. Skipping cart count load.');
            }
            return;
        }

        cartCountLoaded = true;

        $.ajax({
            url: cartCountUrl,
            type: 'GET',
            cache: false,
            timeout: 10000, // 10 second timeout
            success: function (response) {
                if (response && response.success) {
                    updateCartCountDisplay(response.count);
                    console.log('Cart count loaded successfully:', response.count);
                } else {
                    console.error('Failed to load cart count:', response ? response.message : 'Invalid response');
                    updateCartCountDisplay(0);
                }
            },
            error: function (xhr, status, error) {
                console.error('Error loading cart count:', {
                    status: status,
                    error: error,
                    responseText: xhr.responseText
                });
                updateCartCountDisplay(0);
            },
            complete: function () {
                // Reset flag after request completes
                cartCountLoaded = false;
            }
        });
    }

    // UNIFIED function to update the cart count display
    function updateCartCountDisplay(count) {
        var $cartCount = $('#cart-count');
        if ($cartCount.length) {
            $cartCount.text(count);

            // Add visual feedback for updates
            $cartCount.addClass('cart-count-updated');
            setTimeout(function () {
                $cartCount.removeClass('cart-count-updated');
            }, 300);
        }

        // Also update any other cart count elements
        $('.cart-count').text(count);
    }

    // Function to force reload cart count (for external use)
    function refreshCartCount() {
        cartCountLoaded = false;
        loadCartCount();
    }

    // Handle page visibility changes
    function handleVisibilityChange() {
        if (!document.hidden) {
            // Page became visible, refresh cart count
            setTimeout(refreshCartCount, 100);
        }
    }

    // ===========================================
    // INITIALIZATION
    // ===========================================

    // Initialize when DOM is ready
    $(document).ready(function () {
        console.log('Combined cart script initialized');

        // Initialize cart page functionality (if on cart page)
        if ($('.js-btn-plus, .js-btn-minus, .quantity-input, .remove-item, .clear-cart').length > 0) {
            console.log('Cart page elements found, initializing cart functionality...');
            initializeCart();
        }

        // Initialize cart count loader (on all pages)
        console.log('Initializing cart count loader...');
        loadCartCount();

        // Handle browser back/forward navigation
        $(window).on('pageshow', function (e) {
            if (e.originalEvent && e.originalEvent.persisted) {
                console.log('Page loaded from cache, refreshing cart count...');
                refreshCartCount();
            }
        });

        // Handle page visibility changes (tab switching)
        if (typeof document.addEventListener !== 'undefined') {
            document.addEventListener('visibilitychange', handleVisibilityChange);
        }

        // Handle window focus (browser window switching)
        $(window).on('focus', function () {
            refreshCartCount();
        });
    });

    // Also initialize if called after DOM ready
    if (document.readyState === 'complete' || document.readyState === 'interactive') {
        console.log('Document already ready, initializing cart...');

        // Initialize cart page functionality (if on cart page)
        if ($('.js-btn-plus, .js-btn-minus, .quantity-input, .remove-item, .clear-cart').length > 0) {
            initializeCart();
        }

        // Load cart count
        loadCartCount();
    }

    // ===========================================
    // GLOBAL EXPORTS
    // ===========================================

    // Expose functions globally for other scripts to use
    window.cartCountLoader = {
        refresh: refreshCartCount,
        updateDisplay: updateCartCountDisplay,
        load: loadCartCount
    };

    // Backward compatibility
    window.updateCartCount = updateCartCountDisplay;
    window.refreshCartCount = refreshCartCount;

})();