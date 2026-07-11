$(function () {
 
    $('[data-toggle="tooltip"]').tooltip();


    setTimeout(function () {
        $('.alert').alert('close');
    }, 5000);

  
    $("#searchInput").on("keyup", function () {
        var value = $(this).val().toLowerCase();
        $(".categories-table tbody tr").each(function () {
            var $row = $(this);
            var text = $row.text().toLowerCase();
            if (text.indexOf(value) > -1) {
                $row.show();
            } else {
                $row.hide();
            }
        });
    });

    function adjustForMobile() {
        if ($(window).width() <= 991) {
          
            $('.categories-table').addClass('mobile-view');
        } else {
            $('.categories-table').removeClass('mobile-view');
        }
    }


    adjustForMobile();
    $(window).resize(adjustForMobile);
});