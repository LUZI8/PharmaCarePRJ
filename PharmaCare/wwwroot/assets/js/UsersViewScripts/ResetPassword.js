
document.addEventListener('DOMContentLoaded', function () {
    const newPassword = document.getElementById('newPassword');
    const confirmPassword = document.getElementById('confirmPassword');
    const passwordError = document.getElementById('password-error');
    const confirmPasswordError = document.getElementById('confirm-password-error');

    const specialChars = "!#$%^&*()_+-=[]{}|;':\"./<>?";


    function containsSpecialChar(str) {
        for (let i = 0; i < str.length; i++) {
            if (specialChars.includes(str[i])) {
                return true;
            }
        }
        return false;
    }

    function validatePassword(password) {
        // Check minimum length
        if (password.length < 8) {
            return "Password should be at least 8 characters";
        }

        // Check for uppercase letter
        if (!/[A-Z]/.test(password)) {
            return "Password must contain at least one uppercase letter";
        }

        // Check for special symbol using the function instead of regex
        if (!containsSpecialChar(password)) {
            return "Password must contain at least one special character";
        }

        return ""; // Empty string means no error
    }

    function checkPasswordRequirements(password) {
        const lengthReq = document.getElementById('length-req');
        const uppercaseReq = document.getElementById('uppercase-req');
        const specialReq = document.getElementById('special-req');

        // Check length
        if (password.length >= 8) {
            lengthReq.classList.add('requirement-met');
            lengthReq.classList.remove('requirement-not-met');
            lengthReq.innerHTML = "✓ Be at least 8 characters long";
        } else {
            lengthReq.classList.add('requirement-not-met');
            lengthReq.classList.remove('requirement-met');
            lengthReq.innerHTML = "Be at least 8 characters long";
        }

        // Check uppercase
        if (/[A-Z]/.test(password)) {
            uppercaseReq.classList.add('requirement-met');
            uppercaseReq.classList.remove('requirement-not-met');
            uppercaseReq.innerHTML = "✓ Contain at least one uppercase letter";
        } else {
            uppercaseReq.classList.add('requirement-not-met');
            uppercaseReq.classList.remove('requirement-met');
            uppercaseReq.innerHTML = "Contain at least one uppercase letter";
        }

        // Check special character using the function instead of regex
        if (containsSpecialChar(password)) {
            specialReq.classList.add('requirement-met');
            specialReq.classList.remove('requirement-not-met');
            specialReq.innerHTML = "✓ Contain at least one special character";
        } else {
            specialReq.classList.add('requirement-not-met');
            specialReq.classList.remove('requirement-met');
            specialReq.innerHTML = "Contain at least one special character";
        }
    }

    newPassword.addEventListener('input', function () {
        const error = validatePassword(this.value);
        if (error) {
            passwordError.textContent = error;
        } else {
            passwordError.textContent = '';
        }

        checkPasswordRequirements(this.value);

        // Check confirm password match
        if (confirmPassword.value !== '' && confirmPassword.value !== this.value) {
            confirmPasswordError.textContent = 'Passwords do not match';
        } else if (confirmPassword.value !== '') {
            confirmPasswordError.textContent = '';
        }
    });

    confirmPassword.addEventListener('input', function () {
        if (this.value !== newPassword.value) {
            confirmPasswordError.textContent = 'Passwords do not match';
        } else {
            confirmPasswordError.textContent = '';
        }
    });

    document.getElementById('reset-password-form').addEventListener('submit', function (e) {
        const error = validatePassword(newPassword.value);
        if (error) {
            passwordError.textContent = error;
            e.preventDefault();
            return false;
        }

        if (confirmPassword.value !== newPassword.value) {
            confirmPasswordError.textContent = 'Passwords do not match';
            e.preventDefault();
            return false;
        }
    });
});

// Toggle password visibility
function togglePasswordVisibility(inputId) {
    const passwordInput = document.getElementById(inputId);
    const toggleIcon = passwordInput.parentElement.querySelector('.password-toggle i');

    if (passwordInput.type === 'password') {
        passwordInput.type = 'text';
        toggleIcon.classList.remove('fa-eye');
        toggleIcon.classList.add('fa-eye-slash');
    } else {
        passwordInput.type = 'password';
        toggleIcon.classList.remove('fa-eye-slash');
        toggleIcon.classList.add('fa-eye');
    }
}

$(document).ready(function () {
    function updatePasswordFieldsForDarkMode() {
        if ($('body').hasClass('dark-mode')) {
            $('.password-toggle i').addClass('dark-mode-icon');
        } else {
            $('.password-toggle i').removeClass('dark-mode-icon');
        }
    }

    // Initial check
    updatePasswordFieldsForDarkMode();

    // Listen for theme changes
    $('#theme-toggle-btn, #mobile-theme-toggle').on('click', function () {
        setTimeout(updatePasswordFieldsForDarkMode, 50);
    });
});
