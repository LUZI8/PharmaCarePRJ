// Guard the cart-count observer against self-triggered mutation loops.
// The shared layout observes #cart-count and also writes back to #cart-count.
// A native MutationObserver would therefore keep triggering itself. This wrapper
// only suppresses duplicate callbacks for that one element and leaves every
// other MutationObserver on the site completely untouched.
(function () {
    const NativeMutationObserver = window.MutationObserver;
    if (!NativeMutationObserver || window.__pcCartObserverGuardInstalled) return;

    window.__pcCartObserverGuardInstalled = true;

    function PharmaCareMutationObserver(callback) {
        let observedCartCount = false;
        let lastCartValue = null;

        const observer = new NativeMutationObserver(function (mutations, nativeObserver) {
            if (!observedCartCount) {
                callback(mutations, nativeObserver);
                return;
            }

            const target = mutations && mutations.length ? mutations[0].target : null;
            const cartElement = target && target.nodeType === 3 ? target.parentElement : target;
            const currentValue = cartElement ? cartElement.textContent.trim() : '';

            if (currentValue === lastCartValue) {
                return;
            }

            lastCartValue = currentValue;
            callback(mutations, nativeObserver);
        });

        const nativeObserve = observer.observe.bind(observer);
        observer.observe = function (target, options) {
            if (target && target.id === 'cart-count') {
                observedCartCount = true;
                lastCartValue = target.textContent.trim();
            }

            return nativeObserve(target, options);
        };

        return observer;
    }

    PharmaCareMutationObserver.prototype = NativeMutationObserver.prototype;
    window.MutationObserver = PharmaCareMutationObserver;
})();

// Search functionality for PharmaCare
$(document).ready(function () {
    let searchTimeout;
    const searchInput = $('#navbar-search-input');
    const searchBtn = $('#navbar-search-btn');
    const searchResults = $('#navbar-search-results');

    function performSearch(query) {
        if (query.length < 2) {
            hideSearchResults();
            return;
        }

        showSearchResults();
        searchResults.html('<div class="navbar-search-results-wrapper"><div class="navbar-search-results-header">Searching...</div></div>');

        $.ajax({
            url: '/FrontEnd/SearchProducts',
            type: 'GET',
            data: {
                query: query,
                sort: 'relevance'
            },
            success: function (data) {
                displaySearchResults(data, query);
            },
            error: function (xhr, status, error) {
                console.error('Search error:', error);
                searchResults.html('<div class="navbar-search-results-wrapper"><div class="navbar-search-no-results"><p>Search error occurred. Please try again.</p></div></div>');
            }
        });
    }

    function displaySearchResults(products, query) {
        if (!products || products.length === 0) {
            searchResults.html(`
                <div class="navbar-search-results-wrapper">
                    <div class="navbar-search-no-results">
                        <p>No medicines found for "${query}"</p>
                        <p>Try adjusting your search terms</p>
                    </div>
                </div>
            `);
            return;
        }

        let resultsHtml = `
            <div class="navbar-search-results-wrapper">
                <div class="navbar-search-results-header">
                    Found ${products.length} medicine${products.length > 1 ? 's' : ''}
                </div>
                <div class="navbar-search-results-list">
        `;

        const limitedProducts = products.slice(0, 8);

        limitedProducts.forEach(function (product) {
            resultsHtml += `
                <a href="/FrontEnd/ShopSingle/${product.id}" class="navbar-search-result-item">
                    <div class="navbar-search-result-content">
                        <img src="${product.image}" alt="${product.name}" class="navbar-search-result-image" onerror="this.src='/assets/images/product_01.png'">
                        <div class="navbar-search-result-info">
                            <div class="navbar-search-result-name">${product.name}</div>
                            <div class="navbar-search-result-category">${product.category}</div>
                        </div>
                        <div class="navbar-search-result-price">$${product.price.toFixed(2)}</div>
                    </div>
                </a>
            `;
        });

        if (products.length > 8) {
            resultsHtml += `
                <div class="view-all-container">
                    <a href="/FrontEnd/Shop?search=${encodeURIComponent(query)}" class="view-all-link">
                        View all ${products.length} results
                    </a>
                </div>
            `;
        }

        resultsHtml += `
                </div>
            </div>
        `;

        searchResults.html(resultsHtml);
    }

    function showSearchResults() {
        searchResults.show();
    }

    function hideSearchResults() {
        searchResults.hide();
    }

    searchInput.on('input', function () {
        const query = $(this).val().trim();
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(function () {
            performSearch(query);
        }, 300);
    });

    searchBtn.on('click', function (e) {
        e.preventDefault();
        const query = searchInput.val().trim();

        if (query.length >= 2) {
            performSearch(query);
        } else if (query.length === 0) {
            hideSearchResults();
        }
    });

    searchInput.on('keypress', function (e) {
        if (e.which === 13) {
            e.preventDefault();
            const query = $(this).val().trim();

            if (query.length >= 2) {
                window.location.href = `/FrontEnd/Shop?search=${encodeURIComponent(query)}`;
            }
        }
    });

    $(document).on('click', function (e) {
        if (!$(e.target).closest('.navbar-search-container').length) {
            hideSearchResults();
        }
    });

    searchResults.on('click', function (e) {
        e.stopPropagation();
    });

    $(document).on('keydown', function (e) {
        if ((e.ctrlKey || e.metaKey) && e.which === 75) {
            e.preventDefault();
            searchInput.focus();
        }
    });
});

function handleShopSearch() {
    const searchForm = $('#search-form');
    if (searchForm.length) {
        searchForm.on('submit', function (e) {
            e.preventDefault();
            const query = $('#search-input').val().trim();
            if (query) {
                window.location.href = `/FrontEnd/Shop?search=${encodeURIComponent(query)}`;
            }
        });
    }
}