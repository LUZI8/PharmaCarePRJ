
    $(document).ready(function() {
        // Form validation
        $('form').on('submit', function (e) {
            var isValid = true;

            // Required fields
            var $firstName = $('#c_fname');
            var $lastName = $('#c_lname');
            var $email = $('#c_email');

            if (!$firstName.val().trim()) {
                $firstName.addClass('is-invalid');
                isValid = false;
            } else {
                $firstName.removeClass('is-invalid');
            }

            if (!$lastName.val().trim()) {
                $lastName.addClass('is-invalid');
                isValid = false;
            } else {
                $lastName.removeClass('is-invalid');
            }

            if (!$email.val().trim() || !isValidEmail($email.val().trim())) {
                $email.addClass('is-invalid');
                isValid = false;
            } else {
                $email.removeClass('is-invalid');
            }

            if (!isValid) {
                e.preventDefault();
                // Show error message
                if (!$('.alert-danger').length) {
                    $('form').prepend('<div class="alert alert-danger">Please fill in all required fields correctly.</div>');
                }
            }
        });

    // Email validation function
    function isValidEmail(email) {
                var emailRegex = /^[^\s]+[^\s]+\.[^\s]+$/;
    return emailRegex.test(email);
            }

    // Clear validation on input
    $('.form-control').on('input', function() {
        $(this).removeClass('is-invalid');
    $('.alert-danger').remove();
            });
        });
