$(document).ready(function () {
    var $body = $('body');
    var $themeToggleBtn = $('#theme-toggle-btn');
    var $moonIcon = $('#moon-icon');
    var $sunIcon = $('#sun-icon');
    var $hamburgerToggle = $('#hamburger-toggle');
    var $adminSidebar = $('#admin-sidebar');
    var $sidebarOverlay = $('#sidebar-overlay');

    function applyTheme(enabled) {
        $body.toggleClass('dark-mode', enabled);
        $moonIcon.toggle(!enabled);
        $sunIcon.toggle(enabled);
    }

    applyTheme(localStorage.getItem('darkMode') === 'enabled');

    $themeToggleBtn.on('click', function () {
        var enabled = !$body.hasClass('dark-mode');
        applyTheme(enabled);
        if (enabled) localStorage.setItem('darkMode', 'enabled');
        else localStorage.removeItem('darkMode');
    });

    $hamburgerToggle.on('click', function () {
        $adminSidebar.toggleClass('active');
        $sidebarOverlay.toggleClass('active');
    });

    $sidebarOverlay.on('click', closeMobileSidebar);
    $adminSidebar.on('click', '.nav-link', function () {
        if ($(window).width() <= 991) closeMobileSidebar();
    });

    $(window).on('resize', function () {
        if ($(window).width() > 991) closeMobileSidebar();
    });

    function closeMobileSidebar() {
        $adminSidebar.removeClass('active');
        $sidebarOverlay.removeClass('active');
    }

    // Keep the new Operations Center discoverable without duplicating layout markup on every page.
    var $dashboardLink = $adminSidebar.find('a[href*="/Admin"] span').filter(function () {
        return $(this).text().trim() === 'Dashboard';
    }).closest('li');

    if ($dashboardLink.length && !$adminSidebar.find('a[href="/Operations"]').length) {
        var active = window.location.pathname.toLowerCase().startsWith('/operations') ? ' active' : '';
        $dashboardLink.after(
            '<li class="nav-item"><a class="nav-link' + active + '" href="/Operations">' +
            '<i class="fas fa-brain"></i><span>Operations Center</span></a></li>'
        );
    }

    // Do not force users to Login when they use the browser Back button. Session authorization
    // is already enforced server-side by controllers, so normal navigation should stay normal.

    function addDataLabels() {
        $('.admin-table').each(function () {
            var $table = $(this);
            var $headers = $table.find('thead th');
            $table.find('tbody tr').each(function () {
                $(this).find('td').each(function (index) {
                    var $cell = $(this);
                    var headerText = $headers.eq(index).text().trim();
                    if (headerText && !$cell.attr('data-label')) $cell.attr('data-label', headerText);
                });
            });
        });
    }

    addDataLabels();
    initializePagination('.admin-table', 10);

    // MutationObserver replaces deprecated DOMNodeInserted and handles dynamic admin tables safely.
    var observer = new MutationObserver(function () {
        addDataLabels();
    });
    if (document.querySelector('.admin-content')) {
        observer.observe(document.querySelector('.admin-content'), { childList: true, subtree: true });
    }

    function initializePagination(tableSelector, itemsPerPage) {
        $(tableSelector).each(function () {
            var table = $(this);
            var tbody = table.find('tbody');
            var rows = tbody.find('tr');
            if (rows.length <= itemsPerPage || table.data('pagination-initialized') || rows.find('td[colspan]').length > 0) return;

            table.data('pagination-initialized', true);
            var totalPages = Math.ceil(rows.length / itemsPerPage);
            var paginationContainer = $('<div class="d-flex justify-content-between align-items-center mt-3 admin-pagination-wrap"></div>');
            var infoDiv = $('<div><span class="text-muted showing-info">Showing 1-' + Math.min(itemsPerPage, rows.length) + ' of ' + rows.length + ' items</span></div>');
            var pagination = $('<nav aria-label="Page navigation"><ul class="pagination pagination-sm admin-pagination mb-0"></ul></nav>');
            var list = pagination.find('ul');

            list.append('<li class="page-item disabled prev-page"><a class="page-link" href="#">Previous</a></li>');
            for (var i = 1; i <= totalPages; i++) {
                list.append('<li class="page-item ' + (i === 1 ? 'active' : '') + '" data-page="' + i + '"><a class="page-link" href="#">' + i + '</a></li>');
            }
            list.append('<li class="page-item next-page"><a class="page-link" href="#">Next</a></li>');
            paginationContainer.append(infoDiv, pagination);
            table.closest('.card-body, .table-responsive').after(paginationContainer);

            showPage(table, 1, itemsPerPage);

            paginationContainer.on('click', '.page-item[data-page]', function (e) {
                e.preventDefault();
                switchPage(parseInt($(this).data('page')));
            });
            paginationContainer.on('click', '.prev-page', function (e) {
                e.preventDefault();
                if ($(this).hasClass('disabled')) return;
                switchPage(parseInt(paginationContainer.find('.page-item.active').data('page')) - 1);
            });
            paginationContainer.on('click', '.next-page', function (e) {
                e.preventDefault();
                if ($(this).hasClass('disabled')) return;
                switchPage(parseInt(paginationContainer.find('.page-item.active').data('page')) + 1);
            });

            function switchPage(page) {
                if (page < 1 || page > totalPages) return;
                showPage(table, page, itemsPerPage);
                paginationContainer.find('.page-item[data-page]').removeClass('active');
                paginationContainer.find('.page-item[data-page="' + page + '"]').addClass('active');
                paginationContainer.find('.prev-page').toggleClass('disabled', page === 1);
                paginationContainer.find('.next-page').toggleClass('disabled', page === totalPages);
                var start = (page - 1) * itemsPerPage + 1;
                var end = Math.min(page * itemsPerPage, rows.length);
                paginationContainer.find('.showing-info').text('Showing ' + start + '-' + end + ' of ' + rows.length + ' items');
            }
        });
    }

    function showPage(table, pageNumber, itemsPerPage) {
        var rows = table.find('tbody tr');
        var startIndex = (pageNumber - 1) * itemsPerPage;
        rows.hide().slice(startIndex, startIndex + itemsPerPage).show();
    }
});
