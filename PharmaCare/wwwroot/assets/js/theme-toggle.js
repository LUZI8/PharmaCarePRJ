/* Main application initialization when DOM content is fully loaded */
document.addEventListener('DOMContentLoaded', function () {
    /* Theme toggle system with animated icon transitions */
    const themeToggleBtn = document.getElementById('theme-toggle-btn');
    const moonIcon = document.querySelector('.moon-icon');
    const sunIcon = document.querySelector('.sun-icon');

    /* Validate that all theme toggle elements exist before proceeding */
    if (!themeToggleBtn || !moonIcon || !sunIcon) {
        console.error('Theme toggle elements not found. Check your HTML.');
    } else {
        /* Configure smooth CSS transitions for icon animations */
        moonIcon.style.transition = 'transform 0.3s ease, opacity 0.3s ease';
        sunIcon.style.transition = 'transform 0.3s ease, opacity 0.3s ease';

        /* Animated icon switching function with rotation and scaling effects */
        function updateIconsWithAnimation(isDarkMode) {
            if (isDarkMode) {
                /* Animate moon icon out and sun icon in for dark mode */
                moonIcon.style.opacity = '0';
                moonIcon.style.transform = 'rotate(-45deg) scale(0.5)';
                setTimeout(() => {
                    moonIcon.style.display = 'none';
                    sunIcon.style.display = 'inline';
                    setTimeout(() => {
                        sunIcon.style.opacity = '1';
                        sunIcon.style.transform = 'rotate(0) scale(1)';
                    }, 10);
                }, 150);
            } else {
                /* Animate sun icon out and moon icon in for light mode */
                sunIcon.style.opacity = '0';
                sunIcon.style.transform = 'rotate(45deg) scale(0.5)';
                setTimeout(() => {
                    sunIcon.style.display = 'none';
                    moonIcon.style.display = 'inline';
                    setTimeout(() => {
                        moonIcon.style.opacity = '1';
                        moonIcon.style.transform = 'rotate(0) scale(1)';
                    }, 10);
                }, 150);
            }
        }

        /* Smooth color transition for body background and text when theme changes */
        document.body.style.transition = 'background-color 0.3s ease, color 0.3s ease';

        /* Retrieve saved theme preference from localStorage or default to light */
        const savedTheme = localStorage.getItem('theme') || 'light';
        const isDarkMode = savedTheme === 'dark';

        /* Apply the user's saved theme preference to body class */
        document.body.classList.toggle('dark-mode', isDarkMode);

        /* Set initial icon visibility based on current theme */
        if (isDarkMode) {
            moonIcon.style.display = 'none';
            sunIcon.style.display = 'inline';
            sunIcon.style.opacity = '1';
            sunIcon.style.transform = 'rotate(0) scale(1)';
        } else {
            moonIcon.style.display = 'inline';
            sunIcon.style.display = 'none';
            moonIcon.style.opacity = '1';
            moonIcon.style.transform = 'rotate(0) scale(1)';
        }

        /* Theme toggle button click handler with persistence */
        themeToggleBtn.addEventListener('click', function () {
            const newDarkMode = !document.body.classList.contains('dark-mode');
            document.body.classList.toggle('dark-mode');

            /* Save new theme preference to localStorage for persistence */
            localStorage.setItem('theme', newDarkMode ? 'dark' : 'light');

            /* Trigger animated icon transition */
            updateIconsWithAnimation(newDarkMode);

            /* Update other UI elements that depend on theme */
            updateUIForTheme(newDarkMode);
        });

        /* Initialize UI elements based on current theme */
        updateUIForTheme(isDarkMode);
    }

    /* Update UI components that need theme-specific styling */
    function updateUIForTheme(isDarkMode) {
        /* Update jQuery UI slider colors if slider component exists */
        if (window.jQuery && $('#slider-range').length) {
            if (isDarkMode) {
                $('.ui-widget-header').css('background', '#dc3545');
                $('.ui-widget-content').css('background', '#555');
                $('#amount').css({
                    'color': '#f5f5f5',
                    'background-color': 'transparent'
                });
            } else {
                $('.ui-widget-header').css('background', '#dc3545');
                $('.ui-widget-content').css('background', '');
                $('#amount').css({
                    'color': '',
                    'background-color': 'white'
                });
            }
        }
    }

    /* Product badge hover effects for enhanced interactivity */
    const productItems = document.querySelectorAll('.item');
    if (productItems && productItems.length > 0) {
        productItems.forEach(item => {
            /* Set initial reduced opacity for product badges */
            const badges = item.querySelector('.medication-badges');
            if (badges) {
                badges.style.opacity = '0.7';
            }

            /* Increase badge opacity on mouse hover for emphasis */
            item.addEventListener('mouseenter', function () {
                const badges = this.querySelector('.medication-badges');
                if (badges) {
                    badges.style.opacity = '1';
                }
            });

            /* Restore original badge opacity when mouse leaves */
            item.addEventListener('mouseleave', function () {
                const badges = this.querySelector('.medication-badges');
                if (badges) {
                    badges.style.opacity = '0.7';
                }
            });
        });
    }

    /* Initialize Bootstrap tooltips if Bootstrap library is available */
    if (typeof bootstrap !== 'undefined' && bootstrap.Tooltip) {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    }

    /* Sticky header functionality that activates after scrolling 100px */
    const header = document.querySelector('.site-navbar');
    const body = document.body;
    const headerHeight = header ? header.offsetHeight : 0;

    window.addEventListener('scroll', function () {
        if (header) {
            if (window.scrollY > 100) {
                /* Add sticky class and body padding to prevent layout jump */
                header.classList.add('sticky-header');
                body.classList.add('has-sticky-header');
                body.style.paddingTop = headerHeight + 'px';
            } else {
                /* Remove sticky behavior and padding when near top */
                header.classList.remove('sticky-header');
                body.classList.remove('has-sticky-header');
                body.style.paddingTop = '0';
            }
        }
    });

    /* Price range slider initialization with jQuery UI */
    if (window.jQuery && $.fn.slider) {
        $("#slider-range").slider({
            range: true, /* Enable range selection */
            min: 0,
            max: 200,
            values: [0, 150], /* Default range values */
            slide: function (event, ui) {
                /* Update price display and trigger filtering on slide */
                $("#amount").val("$" + ui.values[0] + " - $" + ui.values[1]);
                filterByPrice(ui.values[0], ui.values[1]);
            }
        });

        /* Set initial price range display text */
        $("#amount").val("$" + $("#slider-range").slider("values", 0) +
            " - $" + $("#slider-range").slider("values", 1));
    }

    /* Product sorting dropdown functionality */
    if (window.jQuery) {
        $('.dropdown-item').click(function (e) {
            e.preventDefault();

            /* Update dropdown button text to show selected sort option */
            const selectedSort = $(this).text();
            $('#dropdownMenuReference').text(selectedSort);

            /* Get sort method from data attribute and apply sorting */
            const sortMethod = $(this).data('sort');
            sortProducts(sortMethod);
        });
    }

    /* Filter products by price range function */
    function filterByPrice(minPrice, maxPrice) {
        if (!window.jQuery) return;

        $('.item').each(function () {
            const priceText = $(this).find('.price').text();
            let price;

            /* Handle products with sale prices (format: "original — sale") */
            if (priceText.includes('—')) {
                price = parseFloat(priceText.split('—')[1].trim().replace('$', ''));
            } else {
                price = parseFloat(priceText.replace('$', ''));
            }

            /* Show or hide product based on price range */
            if (price >= minPrice && price <= maxPrice) {
                $(this).show();
            } else {
                $(this).hide();
            }
        });
    }

    /* Product sorting function with multiple sort criteria */
    function sortProducts(sortMethod) {
        if (!window.jQuery) return;

        const products = $('.product-container').children('.item').get();

        /* Sort products array based on selected method */
        products.sort(function (a, b) {
            switch (sortMethod) {
                case 'name-asc':
                    return $(a).data('name').localeCompare($(b).data('name'));
                case 'name-desc':
                    return $(b).data('name').localeCompare($(a).data('name'));
                case 'price-asc':
                    return $(a).data('price') - $(b).data('price');
                case 'price-desc':
                    return $(b).data('price') - $(a).data('price');
                default:
                    return 0;
            }
        });

        /* Re-append sorted products to container in new order */
        $.each(products, function (i, item) {
            $('.product-container').append(item);
        });
    }

    /* Secondary sticky navbar functionality for site navigation */
    const siteNavbar = document.querySelector('.site-navbar');
    let navbarHeight = 0;

    if (siteNavbar) {
        navbarHeight = siteNavbar.offsetHeight;

        window.addEventListener('scroll', function () {
            if (window.scrollY > 100) {
                /* Apply sticky behavior with body padding compensation */
                siteNavbar.classList.add('sticky-navbar');
                body.classList.add('has-sticky-navbar');
                body.style.paddingTop = navbarHeight + 'px';
            } else {
                /* Remove sticky behavior when near top of page */
                siteNavbar.classList.remove('sticky-navbar');
                body.classList.remove('has-sticky-navbar');
                body.style.paddingTop = '0';
            }
        });
    }

    /* Pagination click handling with smooth scrolling */
    if (window.jQuery) {
        $('.site-block-27 ul li a').click(function (e) {
            e.preventDefault();

            /* Update active pagination indicator */
            $('.site-block-27 ul li.active').removeClass('active');
            $(this).parent('li').addClass('active');

            /* Smooth scroll to product container with offset */
            $('html, body').animate({
                scrollTop: $('.product-container').offset().top - 100
            }, 500);
        });
    }

    /* Page loader with fade out animation */
    const pageLoader = document.getElementById('page-loader');
    if (pageLoader) {
        setTimeout(function () {
            /* Start fade out animation */
            pageLoader.style.opacity = '0';
            setTimeout(function () {
                /* Complete removal from DOM after animation */
                pageLoader.style.display = 'none';
            }, 500);
        }, 1000);
    }

    /* Shopping cart initialization and management */
    let cartCount = 0;
    let cartItems = JSON.parse(localStorage.getItem('cartItems')) || [];
    updateCartCount();

    /* Add to cart button functionality for all products */
    document.querySelectorAll('.add-to-cart').forEach(button => {
        button.addEventListener('click', function () {
            /* Extract product data from button attributes */
            const product = this.getAttribute('data-product');
            const price = parseFloat(this.getAttribute('data-price'));

            /* Add product to cart array and save to localStorage */
            cartItems.push({ product, price, quantity: 1 });
            localStorage.setItem('cartItems', JSON.stringify(cartItems));

            /* Update cart count and show confirmation toast */
            cartCount++;
            updateCartCount();
            showToast(`${product} added to cart!`);
        });
    });

    /* Update cart count display in UI */
    function updateCartCount() {
        const cartCountElement = document.getElementById('cart-count');
        if (cartCountElement) {
            cartCountElement.textContent = cartItems.length;
        }
    }

    /* Toast notification system for user feedback */
    function showToast(message) {
        const toast = document.getElementById('toast-notification');
        if (toast) {
            const toastMessage = document.getElementById('toast-message');
            if (toastMessage) {
                toastMessage.textContent = message;
            }
            /* Show toast with auto-hide after 3 seconds */
            toast.classList.add('show');
            setTimeout(() => {
                toast.classList.remove('show');
            }, 3000);
        }
    }

    /* Health calculator functionality with BMI calculation */
    const calculateBtn = document.getElementById('calculateBtn');
    if (calculateBtn) {
        calculateBtn.addEventListener('click', function () {
            /* Get input values from health calculator form */
            const age = document.getElementById('age').value;
            const height = document.getElementById('height').value;
            const weight = document.getElementById('weight').value;
            const gender = document.getElementById('gender').value;
            const calculatorLoader = document.getElementById('calculator-loader');
            const results = document.getElementById('results');

            /* Validate that all required fields are filled */
            if (!age || !height || !weight) {
                showToast('Please fill all fields');
                return;
            }

            /* Show loading animation */
            if (calculatorLoader) {
                calculatorLoader.style.display = 'block';
            }

            /* Simulate calculation delay with timeout */
            setTimeout(function () {
                /* Calculate BMI using metric formula */
                const heightInM = height / 100;
                const bmi = (weight / (heightInM * heightInM)).toFixed(1);

                /* Display formatted results with BMI categories */
                if (results) {
                    results.innerHTML = `
                        <h4>Your Results</h4>
                        <p>BMI: <strong>${bmi}</strong></p>
                        <p>Status: <span class="badge ${bmi < 18.5 ? 'badge-warning' : bmi < 25 ? 'badge-success' : 'badge-danger'}">
                            ${bmi < 18.5 ? 'Underweight' : bmi < 25 ? 'Normal' : 'Overweight'}
                        </span></p>
                        <p class="mt-3">Recommendations: ${getRecommendations(bmi, gender)}</p>
                    `;
                    results.style.display = 'block';
                }

                /* Hide loading animation */
                if (calculatorLoader) {
                    calculatorLoader.style.display = 'none';
                }
            }, 1000);
        });
    }

    /* Generate health recommendations based on BMI and gender */
    function getRecommendations(bmi, gender) {
        if (bmi < 18.5) return 'Consider nutritional supplements';
        if (bmi < 25) return 'Maintain your healthy lifestyle!';
        return 'Consult our weight management products';
    }

    /* Chat widget toggle functionality */
    const chatButton = document.getElementById('chat-button');
    const chatPanel = document.getElementById('chat-panel');
    const closeChat = document.getElementById('close-chat');

    if (chatButton && chatPanel) {
        /* Toggle chat panel visibility */
        chatButton.addEventListener('click', function () {
            chatPanel.style.display = chatPanel.style.display === 'none' ? 'block' : 'none';
        });

        /* Close chat panel when close button is clicked */
        if (closeChat) {
            closeChat.addEventListener('click', function () {
                chatPanel.style.display = 'none';
            });
        }
    }

    /* Back to top button with scroll threshold */
    const backToTop = document.getElementById('back-to-top');
    if (backToTop) {
        /* Show/hide back to top button based on scroll position */
        window.addEventListener('scroll', function () {
            if (window.pageYOffset > 300) {
                backToTop.style.display = 'flex';
                backToTop.classList.add('active');
            } else {
                backToTop.style.display = 'none';
                backToTop.classList.remove('active');
            }
        });

        /* Smooth scroll to top when button is clicked */
        backToTop.addEventListener('click', function (e) {
            e.preventDefault();
            window.scrollTo({ top: 0, behavior: 'smooth' });
        });
    }

    /* Fade-in animation for elements on scroll */
    function checkFadeIn() {
        document.querySelectorAll('.fade-in').forEach(element => {
            const elementTop = element.getBoundingClientRect().top;
            const windowHeight = window.innerHeight;

            /* Trigger fade-in when element is 100px from bottom of viewport */
            if (elementTop < windowHeight - 100) {
                element.classList.add('visible');
            }
        });
    }

    /* Check for fade-in elements on scroll and initial page load */
    window.addEventListener('scroll', checkFadeIn);
    checkFadeIn();
});

/* Chat messaging system initialization */
document.addEventListener('DOMContentLoaded', function () {
    const messageInput = document.getElementById('message-input');
    const sendButton = document.getElementById('send-button');
    const chatMessages = document.getElementById('chat-messages');

    /* Send message when send button is clicked */
    sendButton.addEventListener('click', sendMessage);

    /* Send message when Enter key is pressed in input field */
    messageInput.addEventListener('keypress', function (e) {
        if (e.key === 'Enter') {
            sendMessage();
        }
    });

    /* Send message function with validation and DOM manipulation */
    function sendMessage() {
        const message = messageInput.value.trim();

        if (message !== '') {
            /* Create new message element */
            const messageElement = document.createElement('div');
            messageElement.textContent = message;
            messageElement.classList.add('message');

            /* Add message to chat area */
            chatMessages.appendChild(messageElement);

            /* Clear input field */
            messageInput.value = '';

            /* Auto-scroll to show latest message */
            chatMessages.scrollTop = chatMessages.scrollHeight;
        }
    }
});

/* Enhanced chat system with timestamps */
const messageInput = document.getElementById('message-input');
const sendButton = document.getElementById('send-button');
const chatMessages = document.getElementById('chat-messages');

/* Attach event listeners for message sending */
sendButton.addEventListener('click', sendMessage);
messageInput.addEventListener('keypress', function (e) {
    if (e.key === 'Enter') {
        sendMessage();
    }
});

/* Enhanced send message function with timestamp */
function sendMessage() {
    const message = messageInput.value.trim();

    if (message !== '') {
        /* Generate current timestamp in readable format */
        const timestamp = new Date().toLocaleTimeString([], {
            hour: '2-digit',
            minute: '2-digit'
        });

        /* Create message element with message text and timestamp */
        const messageElement = document.createElement('div');
        messageElement.innerHTML = `
            <span class="message-text">${message}</span>
            <span class="message-time">${timestamp}</span>
        `;
        messageElement.classList.add('message');

        /* Add message to chat container */
        chatMessages.appendChild(messageElement);

        /* Clear input and scroll to bottom */
        messageInput.value = '';
        chatMessages.scrollTop = chatMessages.scrollHeight;
    }
}
/* Add sent message styling class */
messageElement.classList.add('message', 'sent');

/* Sidebar navigation system for mobile menu */
document.addEventListener('DOMContentLoaded', function () {
    /* Get all sidebar-related DOM elements */
    const sidebarToggle = document.getElementById('sidebar-toggle');
    const sidebar = document.querySelector('.sidebar-nav');
    const closeSidebar = document.querySelector('.close-sidebar');
    const overlay = document.querySelector('.sidebar-overlay');
    const sidebarItems = document.querySelectorAll('.sidebar-item.has-submenu');

    /* Open sidebar with overlay and scroll lock */
    function openSidebar() {
        sidebar.classList.add('active');
        overlay.classList.add('active');
        document.body.style.overflow = 'hidden';
    }

    /* Close sidebar and restore page scrolling */
    function closeSidebarFunc() {
        sidebar.classList.remove('active');
        overlay.classList.remove('active');
        document.body.style.overflow = '';
    }

    /* Open sidebar when toggle button is clicked */
    sidebarToggle.addEventListener('click', function (e) {
        e.preventDefault();
        openSidebar();
    });

    /* Close sidebar when close button is clicked */
    closeSidebar.addEventListener('click', function () {
        closeSidebarFunc();
    });

    /* Close sidebar when overlay backdrop is clicked */
    overlay.addEventListener('click', function () {
        closeSidebarFunc();
    });

    /* Close sidebar when Escape key is pressed */
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && sidebar.classList.contains('active')) {
            closeSidebarFunc();
        }
    });

    /* Toggle submenu visibility in sidebar */
    sidebarItems.forEach(function (item) {
        const link = item.querySelector('.sidebar-link');
        const submenu = item.querySelector('.submenu');

        link.addEventListener('click', function (e) {
            e.preventDefault();
            /* Toggle submenu open state */
            item.classList.toggle('open');
            submenu.classList.toggle('active');
        });
    });

    /* Reset all submenus when sidebar is completely closed */
    sidebar.addEventListener('transitionend', function () {
        if (!sidebar.classList.contains('active')) {
            sidebarItems.forEach(function (item) {
                item.classList.remove('open');
                item.querySelector('.submenu').classList.remove('active');
            });
        }
    });
});

/* Alternative sidebar implementation with error checking */
document.addEventListener('DOMContentLoaded', function () {
    /* Get sidebar elements with existence validation */
    const sidebarToggle = document.getElementById('sidebar-toggle');
    const sidebar = document.querySelector('.sidebar-nav');
    const closeSidebar = document.querySelector('.close-sidebar');
    const overlay = document.querySelector('.sidebar-overlay');
    const sidebarItems = document.querySelectorAll('.sidebar-item.has-submenu');

    /* Validate that required elements exist */
    if (!sidebarToggle || !sidebar || !closeSidebar || !overlay) {
        console.error('Sidebar elements not found');
        return;
    }

    console.log('Sidebar initialized');

    /* Open sidebar with logging */
    function openSidebar() {
        console.log('Opening sidebar');
        sidebar.classList.add('active');
        overlay.classList.add('active');
        document.body.style.overflow = 'hidden';
    }

    /* Close sidebar with logging */
    function closeSidebarFunc() {
        console.log('Closing sidebar');
        sidebar.classList.remove('active');
        overlay.classList.remove('active');
        document.body.style.overflow = '';
    }

    /* Use direct onclick assignment for better compatibility */
    sidebarToggle.onclick = function (e) {
        e.preventDefault();
        console.log('Toggle button clicked');
        openSidebar();
    };

    /* Direct onclick for close button */
    closeSidebar.onclick = function () {
        closeSidebarFunc();
    };

    /* Direct onclick for overlay */
    overlay.onclick = function () {
        closeSidebarFunc();
    };

    /* Handle submenu toggling with direct onclick */
    sidebarItems.forEach(function (item) {
        const link = item.querySelector('.sidebar-link');
        const submenu = item.querySelector('.submenu');

        if (link && submenu) {
            link.onclick = function (e) {
                e.preventDefault();
                item.classList.toggle('open');
                submenu.classList.toggle('active');
            };
        }
    });
});

/* Active menu indicator color system based on user's color scheme preference */
function setActiveIndicatorColor() {
    /* Detect user's color scheme preference */
    const prefersDarkMode = window.matchMedia('(prefers-color-scheme: dark)').matches;

    /* Select all active menu indicator pseudo-elements */
    const activeIndicators = document.querySelectorAll('.site-navbar .site-navigation .site-menu .active > a:before');

    /* Set appropriate color based on color scheme */
    const color = prefersDarkMode ? '#ffffff' : '#000000';

    /* Inject CSS with high specificity to override existing styles */
    document.head.insertAdjacentHTML('beforeend', `
        <style>
            .site-navbar .site-navigation .site-menu .active > a:before {
                background-color: ${color} !important;
            }
        </style>
    `);
}

/* Initialize color on page load */
setActiveIndicatorColor();

/* Listen for changes in user's color scheme preference */
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', setActiveIndicatorColor);