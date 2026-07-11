$(document).ready(function () {

    // 🔥 PRICE VALIDATION - Prevent values less than 1, only allow whole numbers
    $('input[name="minPrice"], input[name="maxPrice"]').on('input keydown paste', function (e) {
        var $input = $(this);

        // Prevent typing minus sign, plus sign, 'e' (scientific notation), and decimal point
        if (e.type === 'keydown') {
            // Allow: backspace, delete, tab, escape, enter
            if ($.inArray(e.keyCode, [46, 8, 9, 27, 13]) !== -1 ||
                // Allow: Ctrl+A, Ctrl+C, Ctrl+V, Ctrl+X
                (e.keyCode === 65 && e.ctrlKey === true) ||
                (e.keyCode === 67 && e.ctrlKey === true) ||
                (e.keyCode === 86 && e.ctrlKey === true) ||
                (e.keyCode === 88 && e.ctrlKey === true) ||
                // Allow: home, end, left, right, down, up
                (e.keyCode >= 35 && e.keyCode <= 40)) {
                // Let it happen, don't do anything
                return;
            }

            // Prevent: minus sign (45, 109, 189), plus sign (43, 107, 187), letter e (69, 101), decimal point (190, 110)
            if (e.keyCode === 45 || e.keyCode === 109 || e.keyCode === 189 ||
                e.keyCode === 43 || e.keyCode === 107 || e.keyCode === 187 ||
                e.keyCode === 69 || e.keyCode === 101 ||
                e.keyCode === 190 || e.keyCode === 110) {
                e.preventDefault();
                return false;
            }

            // Ensure that it is a number and stop the keypress
            if ((e.shiftKey || (e.keyCode < 48 || e.keyCode > 57)) && (e.keyCode < 96 || e.keyCode > 105)) {
                e.preventDefault();
            }
        }

        // Handle paste and input events
        setTimeout(function () {
            var value = parseInt($input.val());

            // If value is less than 1, empty, or not a number, set to 1
            if (value < 1 || isNaN(value)) {
                if ($input.val() !== '') { // Only set to 1 if there's actually a value
                    $input.val('1');
                    // Show brief warning
                    $input.addClass('border-warning');
                    setTimeout(function () {
                        $input.removeClass('border-warning');
                    }, 1000);
                }
            } else {
                // Remove any decimal part
                $input.val(Math.floor(value));
            }
        }, 10);
    });

    // Additional validation on blur to ensure no values less than 1 slip through
    $('input[name="minPrice"], input[name="maxPrice"]').on('blur', function () {
        var $input = $(this);
        var value = parseInt($input.val());

        if (value < 1 || isNaN(value)) {
            if ($input.val() !== '') { // Only set to 1 if there's actually a value
                $input.val('1');
            }
        } else {
            // Ensure it's a whole number
            $input.val(Math.floor(value));
        }
    });

    // 🔥 FORM VALIDATION - Before submission
    $('#shopFilterForm').on('submit', function (e) {
        var $minInput = $('input[name="minPrice"]');
        var $maxInput = $('input[name="maxPrice"]');
        var minPrice = parseInt($minInput.val()) || 0;
        var maxPrice = parseInt($maxInput.val()) || 0;

        // Force any values less than 1 to 1 (if they have a value)
        if ($minInput.val() !== '' && minPrice < 1) {
            $minInput.val('1');
            minPrice = 1;
        }

        if ($maxInput.val() !== '' && maxPrice < 1) {
            $maxInput.val('1');
            maxPrice = 1;
        }

        // Check if min > max (only if both have values greater than 0)
        if (minPrice > 0 && maxPrice > 0 && minPrice > maxPrice) {
            e.preventDefault();
            var swapConfirm = confirm('Minimum price cannot be greater than maximum price. Would you like to swap the values?');
            if (swapConfirm) {
                $minInput.val(maxPrice);
                $maxInput.val(minPrice);
                // Continue with submission
                $(this).off('submit').submit();
            } else {
                $minInput.focus().select();
            }
            return false;
        }
    });

    // Clear filters button
    $('.filter-section button.clear').on('click', function (e) {
        e.preventDefault();
        // Reset all form inputs
        $('#shopFilterForm input[type="text"], #shopFilterForm input[type="number"]').val('');
        $('#shopFilterForm select').val('');
        // Reset hidden inputs
        $('#sortInput').val('relevance');
        $('#prescriptionInput').val('false');
        $('#pageInput').val('1');
        // Reset button states
        $('.btn-group button').removeClass('active');
        $('.btn-group button[data-sort="relevance"]').addClass('active');
        $('#prescriptionFilterBtn').removeClass('active');
        // Remove any validation classes
        $('input').removeClass('is-invalid');
        // Submit the form
        $('#shopFilterForm').submit();
    });

    // Sort buttons functionality
    $('.btn-group button').on('click', function (e) {
        e.preventDefault();
        // Update active class
        $('.btn-group button').removeClass('active');
        $(this).addClass('active');
        // Set the sort value in the hidden input
        const sortBy = $(this).data('sort');
        $('#sortInput').val(sortBy);
        // Reset page to 1 when changing sort
        $('#pageInput').val('1');
        // Submit the form
        $('#shopFilterForm').submit();
    });

    // Prescription filter button
    $('#prescriptionFilterBtn').on('click', function (e) {
        e.preventDefault();
        // Toggle active class
        $(this).toggleClass('active');
        // Update hidden input value based on active state
        $('#prescriptionInput').val($(this).hasClass('active') ? 'true' : 'false');
        // Reset page to 1 when changing filter
        $('#pageInput').val('1');
        // Submit the form
        $('#shopFilterForm').submit();
    });

    // 🛒 IMPROVED ADD TO CART FUNCTIONALITY
    $('.btn-add-to-cart').on('click', function (e) {
        e.preventDefault();

        if ($(this).attr('disabled') || $(this).prop('disabled')) return;

        const $button = $(this);
        const productId = $button.data('id');
        const productName = $button.data('name');
        const productPrice = parseFloat($button.data('price'));

        console.log('Add to cart clicked:', { productId, productName, productPrice });

        // Check if URLs are available
        if (!window.shopUrls || !window.shopUrls.addToCart) {
            console.error('Shop URLs not found!');
            showToast('Configuration Error', 'Please refresh the page and try again.', 'error');
            return;
        }

        // Show loading state
        const originalText = $button.html();
        $button.html('<i class="fas fa-spinner fa-spin mr-1"></i> Adding...');
        $button.prop('disabled', true);

        console.log('Making AJAX request to:', window.shopUrls.addToCart);

        // AJAX call to add to cart
        $.ajax({
            url: window.shopUrls.addToCart,
            type: 'POST',
            data: { productId: productId, quantity: 1 },
            timeout: 10000, // 10 second timeout
            beforeSend: function (xhr) {
                // Add CSRF token if available
                var token = $('input[name="__RequestVerificationToken"]').val();
                if (token) {
                    xhr.setRequestHeader('RequestVerificationToken', token);
                }
            },
            success: function (response) {
                console.log('AJAX Success:', response);

                if (response.success) {
                    // 🎉 Show beautiful success notification
                    showToast('Added to Cart!', `${productName} - $${productPrice.toFixed(2)}`, 'success');

                    // Update cart count in header if element exists
                    if ($('#cart-count').length && response.cartCount !== undefined) {
                        $('#cart-count').text(response.cartCount);
                    }

                    // Update cart badge if exists
                    if ($('.cart-badge').length && response.cartCount !== undefined) {
                        $('.cart-badge').text(response.cartCount);
                    }

                    // Brief success state for button
                    $button.html('<i class="fas fa-check mr-1"></i> Added!');
                    $button.removeClass('btn-danger').addClass('btn-success');

                    setTimeout(() => {
                        $button.html(originalText);
                        $button.removeClass('btn-success').addClass('btn-danger');
                        $button.prop('disabled', false);
                    }, 2000);
                } else {
                    console.log('Server returned error:', response);

                    if (response.redirect) {
                        // User needs to login
                        showToast('Login Required', 'Please login to add items to cart', 'warning');
                        setTimeout(() => {
                            window.location.href = response.redirect;
                        }, 2000);
                    } else {
                        showToast('Error', response.message || 'Error adding to cart', 'error');
                    }

                    // Reset button immediately for error cases
                    $button.html(originalText);
                    $button.prop('disabled', false);
                }
            },
            error: function (xhr, status, error) {
                console.error('AJAX Error:', { xhr, status, error });

                // More specific error messages
                let errorMessage = 'Please check your connection and try again.';
                if (status === 'timeout') {
                    errorMessage = 'Request timed out. Please try again.';
                } else if (xhr.status === 401) {
                    errorMessage = 'Please login to continue.';
                } else if (xhr.status >= 500) {
                    errorMessage = 'Server error. Please try again later.';
                }

                showToast('Connection Error', errorMessage, 'error');

                // Reset button on error
                $button.html(originalText);
                $button.prop('disabled', false);
            },
            complete: function () {
                console.log('AJAX request completed');
                // Fallback: ensure button is always reset after 5 seconds
                setTimeout(() => {
                    if ($button.prop('disabled') && $button.html().includes('Adding')) {
                        console.log('Fallback: Resetting stuck button');
                        $button.html(originalText);
                        $button.prop('disabled', false);
                    }
                }, 5000);
            }
        });
    });

    // 🔥 IMPROVED PAGINATION - Handle page changes properly
    $('.site-block-27 a').on('click', function (e) {
        e.preventDefault();
        var url = $(this).attr('href');
        if (url && url !== '#' && url !== 'javascript:void(0)') {
            // Show loading indicator
            $('body').append('<div id="page-loading" style="position:fixed;top:0;left:0;width:100%;height:100%;background:rgba(0,0,0,0.1);z-index:9999;display:flex;align-items:center;justify-content:center;"><i class="fas fa-spinner fa-spin fa-2x"></i></div>');
            window.location.href = url;
        }
    });

    // Remove loading indicator if back button is used
    $(window).on('pageshow', function () {
        $('#page-loading').remove();
    });

    // Disable browser cache for better experience
    $.ajaxSetup({ cache: false });

    // 🎨 ENHANCED TOAST NOTIFICATION FUNCTION
    function showToast(title, message, type = 'success') {
        console.log('Showing toast:', { title, message, type });

        var $toast = $('#toast-notification');

        // Create toast if it doesn't exist
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
        var $message = $('#toast-message');
        var $icon = $('.toast-icon');

        // Set content
        $title.text(title);
        $message.text(message);

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

    // Make hideToast globally available
    window.hideToast = function () {
        console.log('Hiding toast');
        $('#toast-notification').removeClass('show');
    };

    // 🔥 KEYBOARD SHORTCUTS
    $(document).on('keydown', function (e) {
        // ESC key closes toast
        if (e.keyCode === 27) {
            hideToast();
        }
    });

    // Debug: Log when page is ready
    console.log('Shop.js loaded and enhanced, URLs available:', window.shopUrls);
});