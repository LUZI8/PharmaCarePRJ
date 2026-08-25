$(function () {
    const selectors = {
        content: '#shop-content-region',
        results: '#shop-results-region',
        form: '#shopFilterForm'
    };

    function normalizePriceInput($input) {
        if (!$input.val()) return;
        let value = parseInt($input.val(), 10);
        if (Number.isNaN(value) || value < 1) value = 1;
        $input.val(Math.floor(value));
    }

    function buildUrlFromForm($form) {
        const action = $form.attr('action') || window.location.pathname;
        const params = new URLSearchParams($form.serialize());
        return action + '?' + params.toString();
    }

    function setLoading(isLoading) {
        $(selectors.results).toggleClass('is-loading', isLoading);
        $(selectors.form).find('button, input, select').prop('disabled', isLoading);
    }

    async function loadCatalog(url, options) {
        const settings = Object.assign({ pushState: true, scrollToResults: false }, options || {});
        setLoading(true);

        try {
            const response = await fetch(url, {
                method: 'GET',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                credentials: 'same-origin',
                cache: 'no-store'
            });

            if (!response.ok) throw new Error('Unable to load products');

            const html = await response.text();
            const parsed = new DOMParser().parseFromString(html, 'text/html');
            const freshContent = parsed.querySelector(selectors.content);

            if (!freshContent) {
                window.location.href = url;
                return;
            }

            const currentContent = document.querySelector(selectors.content);
            currentContent.replaceWith(freshContent);

            if (settings.pushState) history.pushState({ shopAjax: true }, '', url);

            if (settings.scrollToResults) {
                const target = document.querySelector(selectors.results);
                if (target) {
                    const headerOffset = 92;
                    const y = target.getBoundingClientRect().top + window.scrollY - headerOffset;
                    window.scrollTo({ top: y, behavior: 'smooth' });
                }
            }
        } catch (error) {
            window.location.href = url;
        } finally {
            setLoading(false);
        }
    }

    $(document).on('keydown', 'input[name="minPrice"], input[name="maxPrice"]', function (e) {
        if (['e', 'E', '+', '-', '.'].includes(e.key)) e.preventDefault();
    });

    $(document).on('blur', 'input[name="minPrice"], input[name="maxPrice"]', function () {
        normalizePriceInput($(this));
    });

    $(document).on('submit', selectors.form, function (e) {
        e.preventDefault();
        const $form = $(this);
        const $min = $form.find('input[name="minPrice"]');
        const $max = $form.find('input[name="maxPrice"]');
        normalizePriceInput($min);
        normalizePriceInput($max);

        let min = parseInt($min.val(), 10) || 0;
        let max = parseInt($max.val(), 10) || 0;
        if (min && max && min > max) {
            $min.val(max);
            $max.val(min);
        }

        $form.find('#pageInput').val('1');
        loadCatalog(buildUrlFromForm($form), { pushState: true, scrollToResults: true });
    });

    $(document).on('click', '.shop-sort [data-sort]', function (e) {
        e.preventDefault();
        const $form = $(selectors.form);
        $form.find('#sortInput').val($(this).data('sort'));
        $form.find('#pageInput').val('1');
        loadCatalog(buildUrlFromForm($form), { pushState: true, scrollToResults: true });
    });

    $(document).on('click', '#prescriptionFilterBtn', function (e) {
        e.preventDefault();
        const $form = $(selectors.form);
        const nextValue = $form.find('#prescriptionInput').val() === 'true' ? 'false' : 'true';
        $form.find('#prescriptionInput').val(nextValue);
        $form.find('#pageInput').val('1');
        loadCatalog(buildUrlFromForm($form), { pushState: true, scrollToResults: true });
    });

    $(document).on('click', '.shop-action.clear', function (e) {
        e.preventDefault();
        const $form = $(selectors.form);
        $form.find('input[type="text"], input[type="number"]').val('');
        $form.find('select').val('');
        $form.find('#sortInput').val('relevance');
        $form.find('#prescriptionInput').val('false');
        $form.find('#pageInput').val('1');
        loadCatalog(buildUrlFromForm($form), { pushState: true, scrollToResults: true });
    });

    $(document).on('click', '.shop-page-link', function (e) {
        e.preventDefault();
        const url = this.href;
        if (!url) return;
        loadCatalog(url, { pushState: true, scrollToResults: true });
    });

    window.addEventListener('popstate', function () {
        if (window.location.pathname.toLowerCase().includes('/frontend/shop') || window.location.pathname.toLowerCase().endsWith('/shop')) {
            loadCatalog(window.location.href, { pushState: false, scrollToResults: false });
        }
    });

    $(document).on('click', '.btn-add-to-cart', function (e) {
        e.preventDefault();
        const $button = $(this);
        if ($button.prop('disabled')) return;

        const productId = $button.data('id');
        const productName = $button.data('name');
        const productPrice = parseFloat($button.data('price'));
        const originalHtml = $button.html();

        if (!window.shopUrls || !window.shopUrls.addToCart) {
            showToast('Unable to add item', 'Please refresh the page and try again.', 'error');
            return;
        }

        $button.prop('disabled', true).html('<i class="fas fa-spinner fa-spin"></i> Adding');

        $.ajax({
            url: window.shopUrls.addToCart,
            type: 'POST',
            data: { productId: productId, quantity: 1 },
            timeout: 10000,
            success: function (response) {
                if (response.success) {
                    showToast('Added to cart', productName + ' - $' + productPrice.toFixed(2), 'success');
                    if (response.cartCount !== undefined) $('#cart-count').text(response.cartCount);
                    $button.html('<i class="fas fa-check"></i> Added');
                    setTimeout(function () {
                        $button.prop('disabled', false).html(originalHtml);
                    }, 1300);
                } else if (response.redirect) {
                    showToast('Sign in required', 'Please sign in to add products to your cart.', 'warning');
                    setTimeout(function () { window.location.href = response.redirect; }, 1200);
                } else {
                    showToast('Unable to add item', response.message || 'Please try again.', 'error');
                    $button.prop('disabled', false).html(originalHtml);
                }
            },
            error: function () {
                showToast('Connection error', 'Please check your connection and try again.', 'error');
                $button.prop('disabled', false).html(originalHtml);
            }
        });
    });

    function showToast(title, message, type) {
        const $toast = $('#toast-notification');
        $('#toast-title').text(title);
        $('#toast-message').text(message);
        $toast.removeClass('success warning error').addClass(type || 'success');
        const $icon = $toast.find('.toast-icon');
        $icon.attr('class', 'toast-icon fas ' + (type === 'error' ? 'fa-exclamation-circle' : type === 'warning' ? 'fa-exclamation-triangle' : 'fa-check-circle'));
        $toast.addClass('show');
        clearTimeout(window.shopToastTimer);
        window.shopToastTimer = setTimeout(hideToast, 3500);
    }

    window.hideToast = function () {
        $('#toast-notification').removeClass('show');
    };
});