$(document).ready(function () {
    // Form validation - add better validation for required fields
    $('input[required]').on('blur', function () {
        if (!$(this).val()) {
            $(this).addClass('is-invalid');
        } else {
            $(this).removeClass('is-invalid');
        }
    });

    // Phone number validation - only allow numbers
    $('#phoneNumber').on('input', function () {
        this.value = this.value.replace(/[^0-9]/g, '');

        // Validate phone number length
        if (this.value.length < 10 || this.value.length > 15) {
            $(this).addClass('is-invalid');
        } else {
            $(this).removeClass('is-invalid');
        }
    });

    // Form validation when submitting
    $('#place-order-btn').click(function (e) {
        let isValid = true;

        // Check required fields
        $('input[required]').each(function () {
            if (!$(this).val()) {
                isValid = false;
                $(this).addClass('is-invalid');
            } else {
                $(this).removeClass('is-invalid');
            }
        });

        // Validate phone number
        var phone = $('#phoneNumber').val();
        if (phone.length < 10 || phone.length > 15) {
            isValid = false;
            $('#phoneNumber').addClass('is-invalid');
        }

        if (!isValid) {
            e.preventDefault();

            // Show toast notification
            $('#toast-message').text('Please fill in all required fields correctly');
            $('#toast-notification').addClass('show');

            setTimeout(function () {
                $('#toast-notification').removeClass('show');
            }, 3000);

            // Scroll to first invalid input
            $('html, body').animate({
                scrollTop: $('input.is-invalid:first').offset().top - 100
            }, 500);
        }
    });

    // Keep cash on delivery section expanded by default
    $('#collapsecheque').collapse('show');
});