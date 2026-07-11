$(document).ready(function () {
    // Check if shop single data is available
    if (!window.shopSingleData) {
        console.error('Shop single configuration not found. Make sure the data is passed from the server.');
        return;
    }

    var config = window.shopSingleData;

    // Only initialize quantity controls for non-prescription products
    if (!config.requiresPrescription) {
        // Quantity plus button handler
        $('.quantity-plus').on('click', function () {
            var $input = $('#quantity-input');
            var currentVal = parseInt($input.val()) || 1;
            var maxStock = config.maxStock || 999;

            if (currentVal < maxStock) {
                var newValue = currentVal + 1;
                $input.val(newValue);
            } else {
                showToast('Stock Warning', 'Maximum stock limit reached', 'warning');
            }
        });

        // Quantity minus button handler
        $('.quantity-minus').on('click', function () {
            var $input = $('#quantity-input');
            var currentVal = parseInt($input.val()) || 1;

            if (currentVal > 1) {
                var newValue = currentVal - 1;
                $input.val(newValue);
            }
        });

        // Quantity input change handler
        $('#quantity-input').on('input', function () {
            var quantity = parseInt($(this).val());
            var maxStock = config.maxStock || 999;

            // Validate the quantity value
            if (isNaN(quantity) || quantity < 1) {
                $(this).val(1);
            } else if (quantity > maxStock) {
                $(this).val(maxStock);
                showToast('Quantity Adjusted', 'Quantity adjusted to maximum available stock', 'warning');
            }
        });

        // Handle keyboard events for quantity input
        $('#quantity-input').on('keypress', function (e) {
            // Only allow numbers
            if (e.which < 48 || e.which > 57) {
                e.preventDefault();
            }
        });

        // Handle enter key press on quantity input
        $('#quantity-input').on('keypress', function (e) {
            if (e.which === 13) { // Enter key
                e.preventDefault();
                $('#add-to-cart-btn').click();
            }
        });
    }

    // Add to cart button handler (only for non-prescription products)
    $('#add-to-cart-btn').on('click', function () {
        if ($(this).attr('disabled') || $(this).hasClass('disabled')) {
            return;
        }

        var productId = config.productId;
        var productName = config.productName;
        var quantity = parseInt($('#quantity-input').val()) || 1;

        // Disable button temporarily to prevent double clicks
        var $button = $(this);
        $button.prop('disabled', true);

        // Show loading state
        var originalText = $button.html();
        $button.html('<i class="fas fa-spinner fa-spin mr-2"></i> Adding...');

        // AJAX call to add to cart
        $.ajax({
            url: config.addToCartUrl,
            type: 'POST',
            data: {
                productId: productId,
                quantity: quantity
            },
            beforeSend: function (xhr) {
                // Add CSRF token if available
                var token = $('input[name="__RequestVerificationToken"]').val();
                if (token) {
                    xhr.setRequestHeader('RequestVerificationToken', token);
                }
            },
            success: function (response) {
                if (response.success) {
                    // Show success notification (matching Shop page format)
                    showToast('Added to Cart!', `${productName} added to cart!`, 'success');

                    // Update cart count if element exists
                    if ($('#cart-count').length && response.cartCount !== undefined) {
                        $('#cart-count').text(response.cartCount);
                    }

                    // Update cart badge if exists
                    if ($('.cart-badge').length && response.cartCount !== undefined) {
                        $('.cart-badge').text(response.cartCount);
                    }

                    // Reset quantity to 1 after successful add
                    $('#quantity-input').val(1);
                } else {
                    if (response.redirect) {
                        // Show login required message before redirect
                        showToast('Login Required', 'Please login to add items to cart', 'warning');
                        setTimeout(function () {
                            window.location.href = response.redirect;
                        }, 2000);
                    } else {
                        // Show error message
                        showToast('Error', response.message || 'Error adding to cart', 'error');
                    }
                }
            },
            error: function (xhr, status, error) {
                console.error('AJAX error:', status, error);

                // More specific error messages
                let errorMessage = 'Error adding item to cart. Please try again.';
                if (status === 'timeout') {
                    errorMessage = 'Request timed out. Please try again.';
                } else if (xhr.status === 401) {
                    errorMessage = 'Please login to continue.';
                } else if (xhr.status >= 500) {
                    errorMessage = 'Server error. Please try again later.';
                }

                showToast('Connection Error', errorMessage, 'error');
            },
            complete: function () {
                // Re-enable button and restore original text
                $button.prop('disabled', false);
                $button.html(originalText);
            }
        });
    });

    // 🔥 ENHANCED TOAST NOTIFICATION FUNCTION (matching Shop page structure)
    function showToast(title, message, type = 'success') {
        console.log('Showing toast:', { title, message, type });

        var $toast = $('#toast-notification');

        // Create toast if it doesn't exist (matching Shop page structure)
        if ($toast.length === 0) {
            console.log('Creating toast notification element');
            const toastHTML = `
                <div id="toast-notification" class="toast-notification">
                    <div class="toast-content">
                        <i class="fas fa-check-circle toast-icon"></i>
                        <div class="toast-text">
                            <strong id="toast-title">Added to Cart!</strong>
                            <p id="toast-message">Product added successfully</p>
                        </div>
                    </div>
                    <button class="toast-close" onclick="hideToast()">&times;</button>
                </div>
            `;
            $('body').append(toastHTML);
            $toast = $('#toast-notification');
        }

        var $title = $('#toast-title');
        var $messageEl = $('#toast-message');
        var $icon = $('.toast-icon');

        // Set content
        $title.text(title);
        $messageEl.text(message);

        // Set type-specific styling and icons
        $toast.removeClass('error warning success').addClass(type);

        switch (type) {
            case 'success':
                $icon.removeClass().addClass('fas fa-check-circle toast-icon');
                break;
            case 'error':
                $icon.removeClass().addClass('fas fa-exclamation-circle toast-icon');
                break;
            case 'warning':
                $icon.removeClass().addClass('fas fa-exclamation-triangle toast-icon');
                break;
            default:
                $icon.removeClass().addClass('fas fa-info-circle toast-icon');
        }

        // Show toast with animation
        $toast.addClass('show');
        console.log('Toast shown');

        // Auto-hide after 4 seconds
        setTimeout(function () {
            hideToast();
        }, 4000);
    }

    // Initialize owl carousel for related products if it exists
    if ($('.owl-carousel').length) {
        $('.owl-carousel').owlCarousel({
            loop: true,
            margin: 30,
            nav: true,
            dots: false,
            navText: ['<i class="fas fa-chevron-left"></i>', '<i class="fas fa-chevron-right"></i>'],
            responsive: {
                0: {
                    items: 1
                },
                600: {
                    items: 2
                },
                1000: {
                    items: 3
                }
            }
        });
    }

    // Make hideToast globally available (matching Shop page)
    window.hideToast = function () {
        console.log('Hiding toast');
        $('#toast-notification').removeClass('show');
    };

    // ESC key to close toast
    $(document).on('keydown', function (e) {
        if (e.keyCode === 27) { // ESC key
            window.hideToast();
        }
    });
});