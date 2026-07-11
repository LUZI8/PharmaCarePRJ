/* jQuery document ready initialization for main application functionality */
$(document).ready(function () {
    /* Global variable cleanup to prevent conflicts between scripts */
    if (typeof window.products !== 'undefined') {
        delete window.products;
    }

    if (typeof window.medications !== 'undefined') {
        delete window.medications;
    }

    if (typeof window.medicationData !== 'undefined') {
        delete window.medicationData;
    }

    /* Disable legacy search functions to prevent interference */
    if (typeof window.performNavbarSearch === 'function') {
        window.performNavbarSearch = function () {
            console.log('Old search function disabled');
            return false;
        };
    }

    /* Back to top button with smooth scroll animation */
    $(window).scroll(function () {
        if ($(this).scrollTop() > 300) {
            $('#back-to-top').fadeIn(); /* Show button after scrolling 300px */
        } else {
            $('#back-to-top').fadeOut(); /* Hide button when near top */
        }
    });

    /* Back to top button click handler with smooth animation */
    $('#back-to-top').click(function (e) {
        e.preventDefault();
        $('html, body').animate({ scrollTop: 0 }, 500); /* Smooth scroll to top */
        return false;
    });

    /* Initialize shopping cart count display */
    updateCartCount();

    /* AJAX function to fetch current cart count from server */
    function updateCartCount() {
        $.ajax({
            url: '/Cart/GetCartCount',
            type: 'GET',
            success: function (response) {
                if (response.success) {
                    $('#cart-count').text(response.count); /* Update cart badge */
                }
            },
            error: function () {
                console.log('Error fetching cart count');
            }
        });
    }

    /* Mobile menu toggle functionality with body class management */
    $('.js-menu-toggle').on('click', function (e) {
        e.preventDefault();

        if ($('body').hasClass('offcanvas-menu')) {
            /* Close mobile menu */
            $('body').removeClass('offcanvas-menu');
            $('.site-mobile-menu').removeClass('active');
            $(this).removeClass('active');
        } else {
            /* Open mobile menu */
            $('body').addClass('offcanvas-menu');
            $('.site-mobile-menu').addClass('active');
            $(this).addClass('active');
        }
    });

    /* Close mobile menu when clicking outside the menu area */
    $(document).mouseup(function (e) {
        var container = $(".site-mobile-menu");

        /* Check if click target is outside menu container */
        if (!container.is(e.target) && container.has(e.target).length === 0) {
            if ($('body').hasClass('offcanvas-menu')) {
                $('body').removeClass('offcanvas-menu');
                $('.site-mobile-menu').removeClass('active');
                $('.js-menu-toggle').removeClass('active');
            }
        }
    });

    /* Generate mobile menu structure from desktop navigation */
    function generateMobileMenu() {
        var mobileMenuBody = $('.site-mobile-menu-body');

        /* Clear any existing mobile menu content */
        mobileMenuBody.empty();

        /* Add mobile search functionality at top of menu */
        var mobileSearchHtml = `
            <div class="mobile-search-container" style="padding: 15px 0; border-bottom: 1px solid #eee; margin-bottom: 15px;">
                <div class="mobile-search-wrapper" style="position: relative;">
                    <input type="text" id="mobile-search-input" class="form-control" placeholder="Search products..." style="padding-right: 40px;">
                    <button id="mobile-search-btn" style="position: absolute; right: 5px; top: 50%; transform: translateY(-50%); border: none; background: none; color: #666;">
                        <span class="icon-search"></span>
                    </button>
                </div>
                <div id="mobile-search-results" class="mobile-search-results" style="display: none; margin-top: 10px;"></div>
            </div>
        `;

        mobileMenuBody.append(mobileSearchHtml);

        /* Clone desktop navigation and adapt for mobile */
        var cloneSiteMenu = $('.site-navigation .site-menu').clone();

        /* Remove desktop-specific classes and add mobile classes */
        cloneSiteMenu.removeClass('d-flex').addClass('mobile-menu-list');

        /* Build mobile menu structure with expandable dropdowns */
        var mobileMenuHtml = '<ul class="mobile-menu-nav">';

        cloneSiteMenu.find('> li').each(function () {
            var $this = $(this);
            var $link = $this.find('> a').first();
            var linkText = $link.text();
            var linkHref = $link.attr('href');
            var hasDropdown = $this.hasClass('has-children');

            mobileMenuHtml += '<li class="mobile-menu-item' + ($this.hasClass('active') ? ' active' : '') + '">';

            if (hasDropdown) {
                /* Create expandable menu item with toggle button */
                mobileMenuHtml += '<div class="mobile-menu-item-wrapper">';
                mobileMenuHtml += '<a href="' + linkHref + '" class="mobile-menu-link">' + linkText + '</a>';
                mobileMenuHtml += '<span class="mobile-menu-toggle" data-toggle="collapse"><i class="fas fa-chevron-down"></i></span>';
                mobileMenuHtml += '</div>';

                /* Add collapsible submenu items */
                var dropdownItems = $this.find('.dropdown li');
                if (dropdownItems.length > 0) {
                    mobileMenuHtml += '<ul class="mobile-submenu" style="display: none;">';
                    dropdownItems.each(function () {
                        var $dropdownItem = $(this);
                        var $dropdownLink = $dropdownItem.find('a');
                        mobileMenuHtml += '<li><a href="' + $dropdownLink.attr('href') + '" class="mobile-submenu-link">' + $dropdownLink.text() + '</a></li>';
                    });
                    mobileMenuHtml += '</ul>';
                }
            } else {
                /* Regular menu item without dropdown */
                mobileMenuHtml += '<a href="' + linkHref + '" class="mobile-menu-link">' + linkText + '</a>';
            }

            mobileMenuHtml += '</li>';
        });

        mobileMenuHtml += '</ul>';

        /* Append completed mobile menu to container */
        mobileMenuBody.append(mobileMenuHtml);
    }

    /* Initialize mobile menu generation */
    generateMobileMenu();

    /* Handle mobile dropdown menu toggles with animation */
    $(document).on('click', '.mobile-menu-toggle', function (e) {
        e.preventDefault();
        e.stopPropagation();

        var $toggle = $(this);
        var $submenu = $toggle.closest('.mobile-menu-item').find('.mobile-submenu');
        var $icon = $toggle.find('i');

        if ($submenu.is(':visible')) {
            /* Close currently open submenu */
            $submenu.slideUp(300);
            $icon.removeClass('fa-chevron-up').addClass('fa-chevron-down');
        } else {
            /* Close all other open submenus first */
            $('.mobile-submenu:visible').slideUp(300);
            $('.mobile-menu-toggle i').removeClass('fa-chevron-up').addClass('fa-chevron-down');

            /* Open clicked submenu with slide animation */
            $submenu.slideDown(300);
            $icon.removeClass('fa-chevron-down').addClass('fa-chevron-up');
        }
    });

    /* Mobile search functionality variables */
    const mobileSearchInput = document.getElementById('mobile-search-input');
    const mobileSearchBtn = document.getElementById('mobile-search-btn');
    const mobileSearchResults = document.getElementById('mobile-search-results');
    let mobileSearchTimeout = null;

    /* Mobile search function with debouncing for performance */
    function performMobileSearch(query) {
        if (mobileSearchTimeout) {
            clearTimeout(mobileSearchTimeout);
        }

        if (!query || query.trim() === '') {
            if (mobileSearchResults) {
                mobileSearchResults.innerHTML = '';
                mobileSearchResults.style.display = 'none';
            }
            return;
        }

        /* Debounce search requests to reduce server load */
        mobileSearchTimeout = setTimeout(function () {
            if (mobileSearchResults) {
                mobileSearchResults.innerHTML = '<div class="p-2 text-center"><small>Searching...</small></div>';
                mobileSearchResults.style.display = 'block';
            }

            /* AJAX request to search products */
            $.ajax({
                url: '/FrontEnd/SearchProducts',
                type: 'GET',
                data: { query: query },
                success: function (data) {
                    displayMobileSearchResults(data, query);
                },
                error: function () {
                    if (mobileSearchResults) {
                        mobileSearchResults.innerHTML = '<div class="p-2"><small>Error occurred while searching.</small></div>';
                    }
                }
            });
        }, 300);
    }

    /* Display mobile search results with limited items for performance */
    function displayMobileSearchResults(results, query) {
        if (!mobileSearchResults) return;

        if (!results || results.length === 0) {
            mobileSearchResults.innerHTML = '<div class="p-2"><small>No results found.</small></div>';
            return;
        }

        let html = '<div class="mobile-search-list">';

        /* Show only first 3 results on mobile to save space */
        const displayResults = results.slice(0, 3);

        displayResults.forEach(function (item) {
            /* Handle image path with fallback to default image */
            let imagePath = item.image;
            if (imagePath && !imagePath.startsWith('/') && !imagePath.startsWith('http')) {
                imagePath = '/uploads/' + imagePath;
            }
            if (!imagePath || imagePath === '') {
                imagePath = '/assets/images/product_01.png';
            }

            /* Create mobile-optimized search result item */
            html += `
                <a href="/FrontEnd/ShopSingle/${item.id}" class="mobile-search-item d-flex align-items-center p-2 border-bottom" style="text-decoration: none; color: inherit;">
                    <img src="${imagePath}" alt="${item.name}" style="width: 40px; height: 40px; object-fit: cover; margin-right: 10px;" onerror="this.src='/assets/images/product_01.png'">
                    <div class="flex-grow-1">
                        <div style="font-size: 0.9rem; font-weight: 500;">${item.name}</div>
                        <div style="font-size: 0.8rem; color: #666;">$${parseFloat(item.price).toFixed(2)}</div>
                    </div>
                </a>
            `;
        });

        html += '</div>';
        /* Add link to view all search results */
        html += `<div class="p-2 text-center"><a href="/FrontEnd/Shop?search=${encodeURIComponent(query)}" class="btn btn-outline-danger btn-sm">View All Results</a></div>`;

        mobileSearchResults.innerHTML = html;
        mobileSearchResults.style.display = 'block';
    }

    /* Mobile search input event listeners */
    if (mobileSearchInput) {
        mobileSearchInput.addEventListener('input', function () {
            const query = this.value.trim();
            if (query.length >= 1) {
                performMobileSearch(query);
            } else {
                if (mobileSearchResults) {
                    mobileSearchResults.innerHTML = '';
                    mobileSearchResults.style.display = 'none';
                }
            }
        });

        /* Handle Enter key press to navigate to full search results */
        mobileSearchInput.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                const query = this.value.trim();
                if (query) {
                    window.location.href = `/FrontEnd/Shop?search=${encodeURIComponent(query)}`;
                }
            }
        });
    }

    /* Mobile search button click handler */
    if (mobileSearchBtn) {
        mobileSearchBtn.addEventListener('click', function (e) {
            e.preventDefault();
            const query = mobileSearchInput.value.trim();
            if (query) {
                window.location.href = `/FrontEnd/Shop?search=${encodeURIComponent(query)}`;
            }
        });
    }

    /* Desktop search functionality variables */
    const searchInput = document.getElementById('navbar-search-input');
    const searchBtn = document.getElementById('navbar-search-btn');
    const searchResults = document.getElementById('navbar-search-results');
    let searchTimeout = null;
    let resultsVisible = false;

    /* Advanced search filters object for complex queries */
    let activeFilters = {
        query: '',
        sort: 'relevance',
        minPrice: null,
        maxPrice: null,
        categoryId: null
    };

    /* Enhanced search function with category matching and filters */
    function performSearch(query) {
        /* Clear existing search timeout to prevent multiple requests */
        if (searchTimeout) {
            clearTimeout(searchTimeout);
        }

        /* Update active search query */
        activeFilters.query = query;

        /* Hide results if query is empty */
        if (!query || query.trim() === '') {
            searchResults.innerHTML = '';
            searchResults.style.display = 'none';
            resultsVisible = false;
            return;
        }

        /* Debounce search to avoid excessive API calls */
        searchTimeout = setTimeout(function () {
            /* Show loading spinner during search */
            searchResults.innerHTML = '<div class="p-3 text-center"><div class="spinner-border spinner-border-sm text-danger" role="status"></div> <span>Searching...</span></div>';
            searchResults.style.display = 'block';
            resultsVisible = true;

            /* First fetch categories to check for category name matches */
            $.ajax({
                url: '/FrontEnd/GetCategories',
                type: 'GET',
                success: function (categories) {
                    /* Find categories that match the search query */
                    let matchingCategories = [];
                    if (categories && categories.length) {
                        matchingCategories = categories.filter(function (category) {
                            return category.categoryName.toLowerCase().includes(query.toLowerCase());
                        });
                    }

                    /* Build search parameters object */
                    const searchParams = {
                        query: query,
                        sort: activeFilters.sort
                    };

                    /* Add optional filter parameters if they exist */
                    if (activeFilters.minPrice !== null) {
                        searchParams.minPrice = activeFilters.minPrice;
                    }

                    if (activeFilters.maxPrice !== null) {
                        searchParams.maxPrice = activeFilters.maxPrice;
                    }

                    if (activeFilters.categoryId !== null) {
                        searchParams.categoryId = activeFilters.categoryId;
                    }

                    /* Include matching category names in search */
                    if (matchingCategories.length > 0) {
                        searchParams.categoryNames = matchingCategories.map(c => c.categoryName).join(',');
                    }

                    /* Perform main product search with all parameters */
                    $.ajax({
                        url: '/FrontEnd/SearchProducts',
                        type: 'GET',
                        data: searchParams,
                        success: function (data) {
                            displaySearchResults(data, query, searchParams, matchingCategories);
                        },
                        error: function () {
                            searchResults.innerHTML = '<div class="navbar-search-no-results"><p>Error occurred while searching.</p></div>';
                        }
                    });
                },
                error: function () {
                    /* Fallback to regular search if category fetch fails */
                    const searchParams = {
                        query: query,
                        sort: activeFilters.sort
                    };

                    if (activeFilters.minPrice !== null) {
                        searchParams.minPrice = activeFilters.minPrice;
                    }

                    if (activeFilters.maxPrice !== null) {
                        searchParams.maxPrice = activeFilters.maxPrice;
                    }

                    if (activeFilters.categoryId !== null) {
                        searchParams.categoryId = activeFilters.categoryId;
                    }

                    $.ajax({
                        url: '/FrontEnd/SearchProducts',
                        type: 'GET',
                        data: searchParams,
                        success: function (data) {
                            displaySearchResults(data, query, searchParams, []);
                        },
                        error: function () {
                            searchResults.innerHTML = '<div class="navbar-search-no-results"><p>Error occurred while searching.</p></div>';
                        }
                    });
                }
            });
        }, 300);
    }

    /* Display search results with category highlighting and filter information */
    function displaySearchResults(results, query, searchParams, matchingCategories) {
        /* Clear previous search results */
        searchResults.innerHTML = '';

        if (!results || results.length === 0) {
            searchResults.innerHTML = '<div class="navbar-search-no-results"><p>No results found.</p></div>';
            return;
        }

        /* Build results header with count and filter information */
        let html = '<div class="navbar-search-results-header">';

        /* Display applied filters for user feedback */
        let filterInfo = '';
        if (searchParams) {
            filterInfo += '<div class="applied-filters mt-1 mb-1" style="font-size: 0.8rem; color: #6c757d;">';

            /* Show matching category filters */
            if (matchingCategories && matchingCategories.length > 0) {
                filterInfo += '<span class="mr-2"><i class="fas fa-tag"></i> Categories: ';
                let categoryNames = matchingCategories.slice(0, 2).map(c => c.categoryName).join(', ');
                if (matchingCategories.length > 2) {
                    categoryNames += ` +${matchingCategories.length - 2} more`;
                }
                filterInfo += `${categoryNames}</span>`;
            }

            if (searchParams.categoryId) {
                const categoryName = $('#category-filter option[value="' + searchParams.categoryId + '"]').text();
                filterInfo += `<span class="mr-2"><i class="fas fa-tag"></i> ${categoryName}</span>`;
            }

            if (searchParams.minPrice) {
                filterInfo += `<span class="mr-2"><i class="fas fa-dollar-sign"></i> Min: $${searchParams.minPrice}</span>`;
            }

            if (searchParams.maxPrice) {
                filterInfo += `<span><i class="fas fa-dollar-sign"></i> Max: $${searchParams.maxPrice}</span>`;
            }

            filterInfo += '</div>';
        }

        html += `Search Results (${results.length})${filterInfo}</div>`;
        html += '<div class="navbar-search-results-list">';

        /* Display only first 5 results for better performance */
        const displayResults = results.slice(0, 5);

        displayResults.forEach(function (item) {
            /* Handle image path with proper URL construction */
            let imagePath = item.image;

            if (imagePath && !imagePath.startsWith('/') && !imagePath.startsWith('http')) {
                imagePath = '/uploads/' + imagePath;
            }

            /* Use default image as fallback */
            if (!imagePath || imagePath === '') {
                imagePath = '/assets/images/product_01.png';
            }

            /* Highlight category name if it matches search query */
            let categoryDisplay = item.category || 'Uncategorized';
            if (matchingCategories && matchingCategories.length > 0 &&
                matchingCategories.some(c => c.categoryName === item.category)) {
                categoryDisplay = `<strong style="color: #dc3545;">${item.category}</strong>`;
            }

            /* Create search result item with image, name, category, and price */
            html += `
                <a href="/FrontEnd/ShopSingle/${item.id}" class="navbar-search-result-item">
                    <div class="navbar-search-result-content">
                        <img src="${imagePath}" alt="${item.name}" class="navbar-search-result-image" onerror="this.src='/assets/images/product_01.png'">
                        <div class="navbar-search-result-info">
                            <div class="navbar-search-result-name">${item.name}</div>
                            <div class="navbar-search-result-category">${categoryDisplay}</div>
                        </div>
                        <div class="navbar-search-result-price">$${parseFloat(item.price).toFixed(2)}</div>
                    </div>
                </a>
            `;
        });

        html += '</div>';

        /* Build "View All Results" URL with all applied filters */
        let viewAllUrl = `/FrontEnd/Shop?search=${encodeURIComponent(query)}`;

        /* Append filter parameters to URL for consistency */
        if (searchParams) {
            if (searchParams.minPrice) viewAllUrl += `&minPrice=${searchParams.minPrice}`;
            if (searchParams.maxPrice) viewAllUrl += `&maxPrice=${searchParams.maxPrice}`;
            if (searchParams.categoryId) viewAllUrl += `&category=${searchParams.categoryId}`;
            if (searchParams.sort && searchParams.sort !== 'relevance') viewAllUrl += `&sort=${searchParams.sort}`;

            /* Add category names if they match search */
            if (matchingCategories && matchingCategories.length > 0) {
                viewAllUrl += `&categoryNames=${encodeURIComponent(matchingCategories.map(c => c.categoryName).join(','))}`;
            }
        }

        /* Add "View All Results" button */
        html += `
            <div class="p-3 border-top">
                <a href="${viewAllUrl}" class="btn btn-outline-danger btn-sm w-100">
                    VIEW ALL RESULTS
                </a>
            </div>
        `;

        /* Update DOM with completed search results */
        searchResults.innerHTML = html;
        searchResults.style.display = 'block';
        resultsVisible = true;
    }

    /* Desktop search input event handlers */
    if (searchInput) {
        /* Trigger search on input with minimum character requirement */
        searchInput.addEventListener('input', function () {
            const query = this.value.trim();

            if (query.length >= 1) {
                performSearch(query);
            } else {
                searchResults.innerHTML = '';
                searchResults.style.display = 'none';
                resultsVisible = false;
            }
        });

        /* Handle Enter key to navigate to full search page */
        searchInput.addEventListener('keypress', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                const query = this.value.trim();

                if (query) {
                    window.location.href = `/FrontEnd/Shop?search=${encodeURIComponent(query)}`;
                }
            }
        });

        /* Show existing results when input gains focus */
        searchInput.addEventListener('focus', function () {
            const query = this.value.trim();
            if (query.length >= 1 && searchResults.innerHTML !== '') {
                searchResults.style.display = 'block';
                resultsVisible = true;
            }
        });
    }

    /* Desktop search button click handler */
    if (searchBtn) {
        searchBtn.addEventListener('click', function (e) {
            e.preventDefault();
            const query = searchInput.value.trim();

            if (query) {
                window.location.href = `/FrontEnd/Shop?search=${encodeURIComponent(query)}`;
            }
        });
    }

    /* Close search results when clicking outside search area */
    document.addEventListener('click', function (e) {
        if (resultsVisible &&
            !searchInput.contains(e.target) &&
            !searchBtn.contains(e.target) &&
            !searchResults.contains(e.target)) {

            searchResults.style.display = 'none';
            resultsVisible = false;
        }
    });

    /* Expose search function globally for use by other scripts */
    window.performNavbarSearch = performSearch;

    /* Global function to update search filters from external sources */
    window.updateSearchFilters = function (filters) {
        if (filters.minPrice !== undefined) activeFilters.minPrice = filters.minPrice;
        if (filters.maxPrice !== undefined) activeFilters.maxPrice = filters.maxPrice;
        if (filters.categoryId !== undefined) activeFilters.categoryId = filters.categoryId;
        if (filters.sort !== undefined) activeFilters.sort = filters.sort;
    };

    /* Initialize search with URL parameter if present */
    const urlParams = new URLSearchParams(window.location.search);
    const searchQuery = urlParams.get('search');
    if (searchQuery && searchInput) {
        searchInput.value = searchQuery;
        /* Don't auto-trigger search to avoid conflicts with page load */
    }
});
