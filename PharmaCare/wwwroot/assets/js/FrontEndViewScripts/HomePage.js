$(document).ready(function () {
    console.log('HomePage.js loaded, URLs available:', window.homeUrls);

    // BMI Calculator functionality
    $('#calculateBtn').on('click', function () {
        calculateBMI();
    });

    // 🛒 FIXED ADD TO CART FUNCTIONALITY FOR HOME PAGE
    $(document).on('click', '.btn-add-to-cart', function (e) {
        e.preventDefault();

        console.log('Add to cart button clicked on homepage');

        if ($(this).attr('disabled') || $(this).prop('disabled')) {
            console.log('Button is disabled, returning');
            return;
        }

        const $button = $(this);
        const productId = $button.data('id');
        const productName = $button.data('name');
        const productPrice = parseFloat($button.data('price'));

        console.log('Homepage add to cart:', { productId, productName, productPrice });

        // Check if URLs are available
        if (!window.homeUrls || !window.homeUrls.addToCart) {
            console.error('Home page URLs not found!');
            showToast('Configuration Error', 'Please refresh the page and try again.', 'error');
            return;
        }

        // Show loading state
        const originalText = $button.html();
        $button.html('<i class="fas fa-spinner fa-spin mr-1"></i> Adding...');
        $button.prop('disabled', true);

        console.log('Making AJAX request to:', window.homeUrls.addToCart);

        // AJAX call to add to cart
        $.ajax({
            url: window.homeUrls.addToCart,
            type: 'POST',
            data: { productId: productId, quantity: 1 },
            timeout: 10000, // 10 second timeout
            success: function (response) {
                console.log('Homepage AJAX Success:', response);

                if (response.success) {
                    // 🎉 Show beautiful success notification
                    showToast('Added to Cart!', `${productName} - $${productPrice.toFixed(2)}`, 'success');

                    // Update cart count in header if element exists
                    if ($('#cart-count').length && response.cartCount) {
                        $('#cart-count').text(response.cartCount);
                    }

                    // Brief success state for button
                    $button.html('<i class="fas fa-check mr-1"></i> Added!');
                    $button.css('background-color', '#28a745');

                    setTimeout(() => {
                        $button.html(originalText);
                        $button.css('background-color', '');
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
                console.error('Homepage AJAX Error:', { xhr, status, error });
                showToast('Connection Error', 'Please check your connection and try again.', 'error');

                // Reset button on error
                $button.html(originalText);
                $button.prop('disabled', false);
            },
            complete: function () {
                console.log('Homepage AJAX request completed');
                // Fallback: ensure button is always reset after 5 seconds
                setTimeout(() => {
                    if ($button.prop('disabled') && $button.html().includes('Adding')) {
                        console.log('Fallback: Resetting stuck button on homepage');
                        $button.html(originalText);
                        $button.prop('disabled', false);
                    }
                }, 5000);
            }
        });
    });

    // BMI Calculator function
    function calculateBMI() {
        // Get input values
        const age = parseInt($('#age').val());
        const height = parseFloat($('#height').val());
        const weight = parseFloat($('#weight').val());
        const gender = $('#gender').val();

        // Validate inputs
        if (!age || !height || !weight || age < 1 || height < 1 || weight < 1) {
            alert('Please enter valid values for age, height, and weight.');
            return;
        }

        // Show loader
        $('#calculator-loader').show();
        $('#results').empty();

        // Simulate calculation delay for better UX
        setTimeout(() => {
            // Calculate BMI
            const heightInMeters = height / 100;
            const bmi = weight / (heightInMeters * heightInMeters);

            // Determine BMI category
            let category, categoryClass, advice;
            if (bmi < 18.5) {
                category = 'Underweight';
                categoryClass = 'text-info';
                advice = 'Consider consulting a healthcare provider about healthy weight gain strategies.';
            } else if (bmi < 25) {
                category = 'Normal weight';
                categoryClass = 'text-success';
                advice = 'Great! Maintain your healthy lifestyle with balanced diet and regular exercise.';
            } else if (bmi < 30) {
                category = 'Overweight';
                categoryClass = 'text-warning';
                advice = 'Consider adopting a healthier diet and increasing physical activity.';
            } else {
                category = 'Obese';
                categoryClass = 'text-danger';
                advice = 'Consider consulting a healthcare provider for personalized weight management advice.';
            }

            // Calculate ideal weight range (using Hamwi formula)
            let idealWeightMin, idealWeightMax;
            if (gender === 'male') {
                idealWeightMin = 48 + (2.7 * ((height - 152.4) / 2.54));
                idealWeightMax = idealWeightMin + 10;
            } else {
                idealWeightMin = 45.5 + (2.2 * ((height - 152.4) / 2.54));
                idealWeightMax = idealWeightMin + 10;
            }

            // Display results
            const resultsHTML = `
                <div class="card">
                    <div class="card-header bg-danger text-white">
                        <h5 class="mb-0"><i class="fas fa-calculator mr-2"></i>Your BMI Results</h5>
                    </div>
                    <div class="card-body">
                        <div class="result-item">
                            <span class="result-label">BMI:</span>
                            <span class="result-value ${categoryClass}">${bmi.toFixed(1)}</span>
                        </div>
                        <div class="result-item">
                            <span class="result-label">Category:</span>
                            <span class="result-value ${categoryClass}">${category}</span>
                        </div>
                        <div class="result-item">
                            <span class="result-label">Ideal Weight Range:</span>
                            <span class="result-value">${idealWeightMin.toFixed(1)} - ${idealWeightMax.toFixed(1)} kg</span>
                        </div>
                        <div class="mt-3 p-3 bg-light rounded">
                            <strong>Recommendation:</strong><br>
                            ${advice}
                        </div>
                        
                        <h6 class="mt-4 mb-3">BMI Categories:</h6>
                        <ul class="list-unstyled">
                            <li><span class="text-info">• Underweight:</span> Less than 18.5</li>
                            <li><span class="text-success">• Normal weight:</span> 18.5 - 24.9</li>
                            <li><span class="text-warning">• Overweight:</span> 25 - 29.9</li>
                            <li><span class="text-danger">• Obese:</span> 30 and above</li>
                        </ul>
                    </div>
                </div>
            `;

            $('#results').html(resultsHTML).addClass('fade-in');
            $('#calculator-loader').hide();
        }, 1000);
    }

    // 🎨 BEAUTIFUL TOAST NOTIFICATION FUNCTION
    function showToast(title, message, type = 'success') {
        console.log('Homepage showing toast:', { title, message, type });

        var $toast = $('#toast-notification');

        // Create toast if it doesn't exist
        if ($toast.length === 0) {
            console.log('Creating toast notification element on homepage');
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

        // Set content
        $title.text(title);
        $message.text(message);

        // Set type-specific styling
        $toast.removeClass('error warning success').addClass(type);

        // Show toast with animation
        $toast.addClass('show');
        console.log('Homepage toast shown');

        // Auto-hide after 4 seconds
        setTimeout(function () {
            hideToast();
        }, 4000);
    }

    // Make hideToast globally available
    window.hideToast = function () {
        console.log('Hiding homepage toast');
        $('#toast-notification').removeClass('show');
    };

    // 🎉 FIXED MODAL DISPLAY LOGIC - Show registration success modal if needed
    if (window.location.search.includes('registered=true') ||
        (typeof window.registrationSuccess !== 'undefined' && window.registrationSuccess === 'true')) {
        $('#registrationSuccessModal').modal('show');

        // Clean up URL parameter to prevent modal showing on refresh
        if (window.location.search.includes('registered=true')) {
            const url = new URL(window.location);
            url.searchParams.delete('registered');
            window.history.replaceState({}, document.title, url.pathname + url.search);
        }
    }

    // Show login success modal if needed
    if (window.location.search.includes('loggedIn=true')) {
        $('#loginSuccessModal').modal('show');

        // Clean up URL parameter to prevent modal showing on refresh
        const url = new URL(window.location);
        url.searchParams.delete('loggedIn');
        window.history.replaceState({}, document.title, url.pathname + url.search);
    }
});