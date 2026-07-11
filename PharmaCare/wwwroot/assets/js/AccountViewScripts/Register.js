        // Password toggle functionality
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


    document.addEventListener('DOMContentLoaded', function() {
            const password = document.getElementById('password');
    const confirmPassword = document.getElementById('confirm-password');
    const confirmPasswordError = document.getElementById('confirm-password-error');
    const themeToggleBtn = document.getElementById('theme-toggle-btn');
    const sunIcon = document.querySelector('.sun-icon');
    const moonIcon = document.querySelector('.moon-icon');
    const body = document.body;


    const savedTheme = localStorage.getItem('theme') || 'dark';
    if (savedTheme === 'light') {
        body.classList.remove('dark-mode');
    body.classList.add('light-mode');
    sunIcon.style.display = 'none';
    moonIcon.style.display = 'inline-block';
            }

    // Theme toggle functionality
    themeToggleBtn.addEventListener('click', function() {
                if (body.classList.contains('dark-mode')) {
        body.classList.remove('dark-mode');
    body.classList.add('light-mode');
    sunIcon.style.display = 'none';
    moonIcon.style.display = 'inline-block';
    localStorage.setItem('theme', 'light');
                } else {
        body.classList.remove('light-mode');
    body.classList.add('dark-mode');
    moonIcon.style.display = 'none';
    sunIcon.style.display = 'inline-block';
    localStorage.setItem('theme', 'dark');
                }
            });

    // Required fields
    const requiredFields = [
    {field: document.getElementById('FirstName'), error: document.getElementById('firstName-error') },
    {field: document.getElementById('LastName'), error: document.getElementById('lastName-error') },
    {field: document.getElementById('Email'), error: document.getElementById('email-error') },
    {field: document.getElementById('PhoneNumber'), error: document.getElementById('phone-error') },
    {field: document.getElementById('Address'), error: document.getElementById('address-error') },
    {field: document.getElementById('City'), error: document.getElementById('city-error') }
    ];


    const specialChars = "!#$%^&*()_+-=[]{ }|;':./<>?";

        // Function to check for special characters
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
                if(password.length < 8) {
                    return false;
                }

        // Check for uppercase letter
        if(!/[A-Z]/.test(password)) {
                    return false;
                }

        // Check for special symbol using the function instead of regex
        if(!containsSpecialChar(password)) {
                    return false;
                }

        return true; // All requirements met
            }

        function checkPasswordRequirements(password) {
                const lengthReq = document.getElementById('length-req');
        const uppercaseReq = document.getElementById('uppercase-req');
        const specialReq = document.getElementById('special-req');

                // Check length
                if(password.length >= 8) {
            lengthReq.classList.add('requirement-met');
        lengthReq.classList.remove('requirement-not-met');
        lengthReq.innerHTML = "✓ Be at least 8 characters long";
                } else {
            lengthReq.classList.add('requirement-not-met');
        lengthReq.classList.remove('requirement-met');
        lengthReq.innerHTML = "Be at least 8 characters long";
                }

        // Check uppercase
        if(/[A-Z]/.test(password)) {
            uppercaseReq.classList.add('requirement-met');
        uppercaseReq.classList.remove('requirement-not-met');
        uppercaseReq.innerHTML = "✓ Contain at least one uppercase letter";
                } else {
            uppercaseReq.classList.add('requirement-not-met');
        uppercaseReq.classList.remove('requirement-met');
        uppercaseReq.innerHTML = "Contain at least one uppercase letter";
                }

        // Check special character using the function instead of regex
        if(containsSpecialChar(password)) {
            specialReq.classList.add('requirement-met');
        specialReq.classList.remove('requirement-not-met');
        specialReq.innerHTML = "✓ Contain at least one special character";
                } else {
            specialReq.classList.add('requirement-not-met');
        specialReq.classList.remove('requirement-met');
        specialReq.innerHTML = "Contain at least one special character";
                }
            }

            // Check required fields
            requiredFields.forEach(item => {
            item.field.addEventListener('blur', function () {
                if (!this.value.trim()) {
                    item.error.style.display = 'block';
                    this.classList.add('is-invalid');
                } else {
                    item.error.style.display = 'none';
                    this.classList.remove('is-invalid');
                }
            });
            });

        password.addEventListener('input', function() {
            checkPasswordRequirements(this.value);

        // Check if confirm password matches
        if(confirmPassword.value !== '' && confirmPassword.value !== this.value) {
            confirmPasswordError.style.display = 'block';
        confirmPasswordError.textContent = 'Passwords do not match';
                } else if(confirmPassword.value !== '') {
            confirmPasswordError.style.display = 'none';
                }
            });

        confirmPassword.addEventListener('input', function() {
                if(this.value !== password.value) {
            confirmPasswordError.style.display = 'block';
        confirmPasswordError.textContent = 'Passwords do not match';
                } else {
            confirmPasswordError.style.display = 'none';
                }
            });

        document.getElementById('registration-form').addEventListener('submit', function(e) {
            let isValid = true;

                // Validate all required fields
                requiredFields.forEach(item => {
                    if (!item.field.value.trim()) {
            item.error.style.display = 'block';
        item.field.classList.add('is-invalid');
        isValid = false;
                    }
                });

        // Validate password requirements
        if (!validatePassword(password.value)) {
            document.querySelector('.password-requirements').style.borderColor = 'var(--error-color)';
        isValid = false;
                }

        // Validate password match
        if(confirmPassword.value !== password.value) {
            confirmPasswordError.style.display = 'block';
        confirmPasswordError.textContent = 'Passwords do not match';
        isValid = false;
                }

        if (!isValid) {
            e.preventDefault();
        // Scroll to the first error
        const firstError = document.querySelector('.field-error[style="display: block;"], .requirement-not-met');
        if (firstError) {
            firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
                    }
        return false;
                }
            });
        });

        // Function to validate numeric input for phone number
        function isNumberKey(evt) {
            var charCode = (evt.which) ? evt.which : evt.keyCode;
            if (charCode > 31 && (charCode < 48 || charCode > 57))
        return false;
        return true;
        }
