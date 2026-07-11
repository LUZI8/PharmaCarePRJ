
// Enable tooltips
$(function () {
    $('[data-toggle="tooltip"]').tooltip();
});

// Simple search functionality
$("#searchBtn").on("click", function () {
    var searchTerm = $("#searchInput").val().toLowerCase();
    $(".admin-table tbody tr").each(function () {
        var userName = $(this).find("td:nth-child(1)").text().toLowerCase();
        var userEmail = $(this).find("td:nth-child(2)").text().toLowerCase();

        if (userName.includes(searchTerm) || userEmail.includes(searchTerm)) {
            $(this).show();
        } else {
            $(this).hide();
        }
    });
});

// Enter key for search
$("#searchInput").on("keyup", function (e) {
    if (e.key === 'Enter') {
        $("#searchBtn").click();
    }
});

// Filter by role
$("#roleFilter").on("change", function () {
    var role = $(this).val();

    if (role === "all") {
        $(".admin-table tbody tr").show();
    } else {
        $(".admin-table tbody tr").each(function () {
            var userRole = $(this).find("td:nth-child(3) .badge").text();
            $(this).toggle(userRole === role);
        });
    }
});

// Apply dark mode specific styling
$(document).ready(function () {
    function updateButtonsForDarkMode() {
        if ($('body').hasClass('dark-mode')) {
            $('.btn-primary').addClass('dark-mode-btn');
        } else {
            $('.btn-primary').removeClass('dark-mode-btn');
        }
    }

    // Initial check
    updateButtonsForDarkMode();

    // Listen for theme changes
    $('#theme-toggle-btn, #mobile-theme-toggle').on('click', function () {
        setTimeout(updateButtonsForDarkMode, 50);
    });
});
