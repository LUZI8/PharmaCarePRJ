// Search functionality for PharmaCare
$(document).ready(function () {
    let searchTimeout;
    const searchInput = $('#navbar-search-input');
    const searchBtn = $('#navbar-search-btn');
    const searchResults = $('#navbar-search-results');

    // Function to perform search
    function performSearch(query) {
        if (query.length < 2) {
            hideSearchResults();
            return;
        }

        // Show loading state
        showSearchResults();
        searchResults.html('<div class="navbar-search-results-wrapper"><div class="navbar-search-results-header">Searching...</div></div>');

        // Make AJAX call to search endpoint
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

    // Function to display search results
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

        // Limit to first 8 results for dropdown
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

    // Function to show search results
    function showSearchResults() {
        searchResults.show();
    }

    // Function to hide search results
    function hideSearchResults() {
        searchResults.hide();
    }

    // Search input event handlers
    searchInput.on('input', function () {
        const query = $(this).val().trim();

        // Clear previous timeout
        clearTimeout(searchTimeout);

        // Set new timeout for debounced search
        searchTimeout = setTimeout(function () {
            performSearch(query);
        }, 300); // Wait 300ms after user stops typing
    });

    // Search button click handler
    searchBtn.on('click', function (e) {
        e.preventDefault();
        const query = searchInput.val().trim();

        if (query.length >= 2) {
            performSearch(query);
        } else if (query.length === 0) {
            hideSearchResults();
        }
    });

    // Handle Enter key press
    searchInput.on('keypress', function (e) {
        if (e.which === 13) { // Enter key
            e.preventDefault();
            const query = $(this).val().trim();

            if (query.length >= 2) {
                // If there are results, go to full search page
                window.location.href = `/FrontEnd/Shop?search=${encodeURIComponent(query)}`;
            }
        }
    });

    // Hide search results when clicking outside
    $(document).on('click', function (e) {
        if (!$(e.target).closest('.navbar-search-container').length) {
            hideSearchResults();
        }
    });

    // Prevent search results from closing when clicking inside them
    searchResults.on('click', function (e) {
        e.stopPropagation();
    });

    // Focus search input with Ctrl+K or Cmd+K
    $(document).on('keydown', function (e) {
        if ((e.ctrlKey || e.metaKey) && e.which === 75) { // Ctrl/Cmd + K
            e.preventDefault();
            searchInput.focus();
        }
    });
});

// Additional helper function to handle search form submission on shop page
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