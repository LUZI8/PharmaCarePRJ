
        $(document).ready(function() {
            
            var $body = $('body');
            var $themeToggleBtn = $('#theme-toggle-btn');
            var $moonIcon = $('#moon-icon');
            var $sunIcon = $('#sun-icon');
            var $hamburgerToggle = $('#hamburger-toggle');
            var $adminSidebar = $('#admin-sidebar');
            var $sidebarOverlay = $('#sidebar-overlay');

            if (localStorage.getItem('darkMode') === 'enabled') {
                $body.addClass('dark-mode');
                $moonIcon.hide();
                $sunIcon.show();
            }

            $themeToggleBtn.on('click', function() {
                if ($body.hasClass('dark-mode')) {

                    $body.removeClass('dark-mode');
                    $sunIcon.hide();
                    $moonIcon.show();
                    localStorage.setItem('darkMode', null);
                } else {
                   
                    $body.addClass('dark-mode');
                    $moonIcon.hide();
                    $sunIcon.show();
                    localStorage.setItem('darkMode', 'enabled');
                }
            });

            $hamburgerToggle.on('click', function() {
                $adminSidebar.toggleClass('active');
                $sidebarOverlay.toggleClass('active');
            });

           
            $sidebarOverlay.on('click', function() {
                $adminSidebar.removeClass('active');
                $sidebarOverlay.removeClass('active');
            });

            $adminSidebar.find('.nav-link').on('click', function() {
                if ($(window).width() <= 991) {
                    $adminSidebar.removeClass('active');
                    $sidebarOverlay.removeClass('active');
                }
            });


            $(window).on('resize', function() {
                if ($(window).width() > 991) {
                    $adminSidebar.removeClass('active');
                    $sidebarOverlay.removeClass('active');
                }
            });


            function addDataLabels() {
                $('.admin-table').each(function() {
                    var $table = $(this);
                    var $headers = $table.find('thead th');

                    $table.find('tbody tr').each(function() {
                        var $row = $(this);
                        $row.find('td').each(function(index) {
                            var $cell = $(this);
                            var headerText = $headers.eq(index).text().trim();
                            if (headerText && !$cell.attr('data-label')) {
                                $cell.attr('data-label', headerText);
                            }
                        });
                    });
                });
            }


            addDataLabels();

            $(document).on('DOMNodeInserted', '.admin-table', function() {
                setTimeout(addDataLabels, 100);
            });
        });

 
        window.addEventListener('pageshow', function(event) {
            if (event.persisted || performance.getEntriesByType("navigation")[0].type === 'back_forward') {
  
                window.location.href = '/Account/Login';
            }
        });

    
        $(document).ready(function() {
      
            initializePagination('.admin-table', 10);

            function initializePagination(tableSelector, itemsPerPage) {
                $(tableSelector).each(function() {
                    var table = $(this);
                    var tbody = table.find('tbody');
                    var rows = tbody.find('tr');

                
                    if (rows.length <= itemsPerPage || table.data('pagination-initialized') || rows.find('td[colspan]').length > 0) {
                        return;
                    }

                
                    table.data('pagination-initialized', true);

                 
                    var totalPages = Math.ceil(rows.length / itemsPerPage);

                   
                    var paginationContainer = $('<div class="d-flex justify-content-between align-items-center mt-3"></div>');
                    var infoDiv = $('<div><span class="text-muted showing-info">Showing 1-' +
                        Math.min(itemsPerPage, rows.length) + ' of ' + rows.length + ' items</span></div>');

                    var pagination = $('<nav aria-label="Page navigation"><ul class="pagination pagination-sm admin-pagination"></ul></nav>');
                    var paginationList = pagination.find('ul');

                    // Add prev button
                    paginationList.append('<li class="page-item disabled prev-page">' +
                        '<a class="page-link" href="#" tabindex="-1">Previous</a></li>');

                    // Add page numbers
                    for (var i = 1; i <= totalPages; i++) {
                        var activeClass = i === 1 ? 'active' : '';
                        paginationList.append('<li class="page-item ' + activeClass + '" data-page="' + i + '">' +
                            '<a class="page-link" href="#">' + i + '</a></li>');
                    }

                    // Add next button
                    paginationList.append('<li class="page-item next-page">' +
                        '<a class="page-link" href="#">Next</a></li>');

                    // Append pagination
                    paginationContainer.append(infoDiv);
                    paginationContainer.append(pagination);

                    // Find existing pagination or append new one
                    var existingPagination = table.closest('.card-body, .table-responsive').siblings('.d-flex.justify-content-between');
                    if (existingPagination.length) {
                        existingPagination.replaceWith(paginationContainer);
                    } else {
                        table.closest('.card-body, .table-responsive').after(paginationContainer);
                    }

                   
                    showPage(table, 1, itemsPerPage);

                    // Page click handlers
                    paginationContainer.find('.page-item[data-page]').on('click', function(e) {
                        e.preventDefault();
                        var page = parseInt($(this).data('page'));
                        showPage(table, page, itemsPerPage);
                        updatePaginationState(paginationContainer, page, totalPages);
                        updateShowingInfo(paginationContainer, page, itemsPerPage, rows.length);
                    });

                    // Prev button click handler
                    paginationContainer.find('.prev-page').on('click', function(e) {
                        e.preventDefault();
                        if ($(this).hasClass('disabled')) return;

                        var activePage = parseInt(paginationContainer.find('.page-item.active').data('page'));
                        var prevPage = activePage - 1;
                        if (prevPage >= 1) {
                            showPage(table, prevPage, itemsPerPage);
                            updatePaginationState(paginationContainer, prevPage, totalPages);
                            updateShowingInfo(paginationContainer, prevPage, itemsPerPage, rows.length);
                        }
                    });

                    // Next button click handler
                    paginationContainer.find('.next-page').on('click', function(e) {
                        e.preventDefault();
                        if ($(this).hasClass('disabled')) return;

                        var activePage = parseInt(paginationContainer.find('.page-item.active').data('page'));
                        var nextPage = activePage + 1;
                        if (nextPage <= totalPages) {
                            showPage(table, nextPage, itemsPerPage);
                            updatePaginationState(paginationContainer, nextPage, totalPages);
                            updateShowingInfo(paginationContainer, nextPage, itemsPerPage, rows.length);
                        }
                    });
                });
            }

            function showPage(table, pageNumber, itemsPerPage) {
                var rows = table.find('tbody tr');
                var startIndex = (pageNumber - 1) * itemsPerPage;
                var endIndex = startIndex + itemsPerPage;

                rows.hide();
                rows.slice(startIndex, endIndex).show();
            }

            function updatePaginationState(container, currentPage, totalPages) {
                // Update active page
                container.find('.page-item[data-page]').removeClass('active');
                container.find('.page-item[data-page="' + currentPage + '"]').addClass('active');

                // Update prev/next buttons
                container.find('.prev-page').toggleClass('disabled', currentPage === 1);
                container.find('.next-page').toggleClass('disabled', currentPage === totalPages);
            }

            function updateShowingInfo(container, currentPage, itemsPerPage, totalItems) {
                var start = (currentPage - 1) * itemsPerPage + 1;
                var end = Math.min(currentPage * itemsPerPage, totalItems);
                container.find('.showing-info').text('Showing ' + start + '-' + end + ' of ' + totalItems + ' items');
            }
        });
