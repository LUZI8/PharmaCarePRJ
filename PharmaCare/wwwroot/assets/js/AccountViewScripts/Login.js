document.addEventListener('DOMContentLoaded', function () {
    const passwordToggle = document.getElementById('password-toggle');
    const passwordField = document.getElementById('password-field');
    const themeToggleBtn = document.getElementById('theme-toggle-btn');
    const sunIcon = document.querySelector('.sun-icon');
    const moonIcon = document.querySelector('.moon-icon');
    const body = document.body;

    // Check for saved theme preference or use default (dark)
    const savedTheme = localStorage.getItem('theme') || 'dark';
    if (savedTheme === 'light') {
        body.classList.remove('dark-mode');
        body.classList.add('light-mode');
        sunIcon.style.display = 'none';
        moonIcon.style.display = 'inline-block';
    }

    // Theme toggle functionality
    themeToggleBtn.addEventListener('click', function () {
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

    // Password toggle
    passwordToggle.addEventListener('click', function () {
        const type = passwordField.getAttribute('type') === 'password' ? 'text' : 'password';
        passwordField.setAttribute('type', type);

        // Toggle icon
        const icon = this.querySelector('i');
        if (type === 'text') {
            icon.classList.remove('fa-eye');
            icon.classList.add('fa-eye-slash');
        } else {
            icon.classList.remove('fa-eye-slash');
            icon.classList.add('fa-eye');
        }
    });
});