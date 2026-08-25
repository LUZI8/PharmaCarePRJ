$(document).ready(function () {
    var config = window.shopSingleData || {};

    function showToast(title, message, type) {
        type = type || 'success';
        var $toast = $('#toast-notification');
        $('#toast-title').text(title);
        $('#toast-message').text(message);
        $('.toast-icon').attr('class', 'fas toast-icon ' + (type === 'success' ? 'fa-check-circle' : type === 'warning' ? 'fa-exclamation-triangle' : 'fa-exclamation-circle'));
        $toast.removeClass('success warning error').addClass(type).addClass('show');
        clearTimeout(window.__pcToastTimer);
        window.__pcToastTimer = setTimeout(function () { $toast.removeClass('show'); }, 3600);
    }

    window.hideToast = function () { $('#toast-notification').removeClass('show'); };

    // Product gallery thumbnails.
    $(document).on('click', '.gallery-thumb', function () {
        var src = $(this).data('image');
        if (!src) return;
        $('.gallery-thumb').removeClass('active');
        $(this).addClass('active');
        var $main = $('#mainProductImage');
        $main.css('opacity', '.25');
        setTimeout(function () {
            $main.attr('src', src).css('opacity', '1');
            $('#productLightbox img').attr('src', src);
        }, 120);
    });

    // Zoom / lightbox.
    $('.gallery-zoom').on('click', function () {
        $('#productLightbox img').attr('src', $('#mainProductImage').attr('src'));
        $('#productLightbox').addClass('open').attr('aria-hidden', 'false');
        $('body').css('overflow', 'hidden');
    });
    $('.lightbox-close, #productLightbox').on('click', function (e) {
        if (e.target !== this && !$(e.target).closest('.lightbox-close').length) return;
        $('#productLightbox').removeClass('open').attr('aria-hidden', 'true');
        $('body').css('overflow', '');
    });

    // Lightweight tabs without a page reload.
    $('.product-tab').on('click', function () {
        var target = $(this).data('tab');
        $('.product-tab').removeClass('active');
        $(this).addClass('active');
        $('.product-tab-panel').removeClass('active');
        $('[data-panel="' + target + '"]').addClass('active');
    });

    if (!config.requiresPrescription) {
        $('.quantity-plus').on('click', function () {
            var $input = $('#quantity-input');
            var current = parseInt($input.val(), 10) || 1;
            var max = parseInt(config.maxStock, 10) || 1;
            if (current < max) $input.val(current + 1);
            else showToast('Stock limit reached', 'You selected the maximum quantity currently available.', 'warning');
        });

        $('.quantity-minus').on('click', function () {
            var $input = $('#quantity-input');
            var current = parseInt($input.val(), 10) || 1;
            if (current > 1) $input.val(current - 1);
        });

        $('#quantity-input').on('input', function () {
            var value = parseInt($(this).val(), 10);
            var max = parseInt(config.maxStock, 10) || 1;
            if (isNaN(value) || value < 1) $(this).val(1);
            else if (value > max) {
                $(this).val(max);
                showToast('Quantity adjusted', 'Quantity was adjusted to available stock.', 'warning');
            }
        }).on('keypress', function (e) {
            if (e.which === 13) { e.preventDefault(); $('#add-to-cart-btn').trigger('click'); return; }
            if (e.which < 48 || e.which > 57) e.preventDefault();
        });
    }

    $('#add-to-cart-btn').on('click', function () {
        var $button = $(this);
        if ($button.prop('disabled')) return;
        var quantity = parseInt($('#quantity-input').val(), 10) || 1;
        var original = $button.html();
        $button.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i><span>Adding...<small>Please wait</small></span>');

        $.ajax({
            url: config.addToCartUrl,
            type: 'POST',
            data: { productId: config.productId, quantity: quantity },
            timeout: 10000,
            beforeSend: function (xhr) {
                var token = $('input[name="__RequestVerificationToken"]').val();
                if (token) xhr.setRequestHeader('RequestVerificationToken', token);
            },
            success: function (response) {
                if (response.success) {
                    showToast('Added to cart', config.productName + ' is now in your cart.', 'success');
                    if (response.cartCount !== undefined) $('#cart-count, .cart-badge').text(response.cartCount);
                    $('#quantity-input').val(1);
                } else if (response.redirect) {
                    showToast('Sign in required', 'Please sign in to add products to your cart.', 'warning');
                    setTimeout(function () { window.location.href = response.redirect; }, 1200);
                } else {
                    showToast('Could not add product', response.message || 'Please try again.', 'error');
                }
            },
            error: function (xhr, status) {
                showToast('Connection problem', status === 'timeout' ? 'The request timed out. Please try again.' : 'We could not update your cart.', 'error');
            },
            complete: function () { $button.prop('disabled', false).html(original); }
        });
    });

    $(document).on('keydown', function (e) {
        if (e.key === 'Escape') {
            window.hideToast();
            $('#productLightbox').removeClass('open').attr('aria-hidden', 'true');
            $('body').css('overflow', '');
        }
    });
});