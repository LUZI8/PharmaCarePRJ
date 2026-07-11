
    $(document).ready(function() {
        // Enable tooltips
        $('[data-toggle="tooltip"]').tooltip();

    // Search functionality
    $("#searchBtn").on("click", function() {
                var searchTerm = $("#searchInput").val().toLowerCase();
    $(".admin-table tbody tr").each(function() {
                    var reservationNumber = $(this).find("td:nth-child(1)").text().toLowerCase();
    var customerName = $(this).find("td:nth-child(2)").text().toLowerCase();
    var productName = $(this).find("td:nth-child(4)").text().toLowerCase();

    if (reservationNumber.includes(searchTerm) || customerName.includes(searchTerm) || productName.includes(searchTerm)) {
        $(this).show();
                    } else {
        $(this).hide();
                    }
                });
            });

    // Status filter
    $("#statusFilter").on("change", function() {
                var status = $(this).val();

    if (status === "all") {
        $(".admin-table tbody tr").show();
                } else {
        $(".admin-table tbody tr").each(function () {
            var rowStatus = $(this).find("td:nth-child(8) .badge").text();
            $(this).toggle(rowStatus === status);
        });
                }
            });

    // Allow pressing Enter to search
    $("#searchInput").on("keyup", function(e) {
                if (e.key === "Enter") {
        $("#searchBtn").click();
                }
            });

    // Highlight reservations that are expiring soon
    $(".admin-table tbody tr").each(function() {
                var status = $(this).find("td:nth-child(8) .badge").text();
    var expiryDateText = $(this).find("td:nth-child(7)").text().trim();
    var hasExpiredBadge = expiryDateText.includes("Expired");

    if (status === "Reserved" && hasExpiredBadge) {
        $(this).addClass("bg-warning bg-opacity-25");
                }
            });
        });
