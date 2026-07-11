document.addEventListener('DOMContentLoaded', function () {
    const table = document.getElementById('orders-table');
    if (!table) return;

    const rows = Array.from(table.querySelectorAll('.table-row'));
    const itemsPerPage = 10;
    const totalItems = rows.length;
    const totalPages = Math.ceil(totalItems / itemsPerPage);
    let currentPage = 1;

    // Only show pagination if we have more than itemsPerPage rows
    if (totalItems <= itemsPerPage) {
        document.getElementById('pagination-controls').style.display = 'none';
        return;
    }

    function showPage(page) {
        const start = (page - 1) * itemsPerPage;
        const end = start + itemsPerPage;

        // Hide all rows first
        rows.forEach(row => row.style.display = 'none');

        // Show rows for current page
        for (let i = start; i < end && i < totalItems; i++) {
            if (rows[i]) {
                rows[i].style.display = '';
            }
        }

        // Update pagination info
        const showingStart = start + 1;
        const showingEnd = Math.min(end, totalItems);
        document.getElementById('showing-start').textContent = showingStart;
        document.getElementById('showing-end').textContent = showingEnd;

        currentPage = page;
        updatePaginationButtons();
    }

    function updatePaginationButtons() {
        const paginationList = document.getElementById('pagination-list');
        paginationList.innerHTML = '';

        // Previous button
        const prevLi = document.createElement('li');
        prevLi.className = `page-item ${currentPage === 1 ? 'disabled' : ''}`;
        prevLi.innerHTML = '<a class="page-link" href="#" data-page="prev">Previous</a>';
        paginationList.appendChild(prevLi);

        // Page numbers
        for (let i = 1; i <= totalPages; i++) {
            const li = document.createElement('li');
            li.className = `page-item ${i === currentPage ? 'active' : ''}`;
            li.innerHTML = `<a class="page-link" href="#" data-page="${i}">${i}</a>`;
            paginationList.appendChild(li);
        }

        // Next button
        const nextLi = document.createElement('li');
        nextLi.className = `page-item ${currentPage === totalPages ? 'disabled' : ''}`;
        nextLi.innerHTML = '<a class="page-link" href="#" data-page="next">Next</a>';
        paginationList.appendChild(nextLi);

        // Add click handlers
        paginationList.addEventListener('click', function (e) {
            e.preventDefault();
            if (e.target.tagName === 'A') {
                const page = e.target.getAttribute('data-page');

                if (page === 'prev' && currentPage > 1) {
                    showPage(currentPage - 1);
                } else if (page === 'next' && currentPage < totalPages) {
                    showPage(currentPage + 1);
                } else if (page !== 'prev' && page !== 'next') {
                    showPage(parseInt(page));
                }
            }
        });
    }

    // Initialize
    showPage(1);
});