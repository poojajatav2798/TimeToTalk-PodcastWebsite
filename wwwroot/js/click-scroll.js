// jquery-click-scroll
// Tiya Golf Club Smooth In-Page Section Scroll & Spy
(function ($) {
    "use strict";

    var sectionArray = [1, 2, 3, 4, 5];

    // ScrollSpy to update active class on scroll
    $(document).scroll(function () {
        var docScroll = $(document).scrollTop();
        var docScroll1 = docScroll + 1;

        $.each(sectionArray, function (index, value) {
            var sec = $('#section_' + value);
            if (sec.length > 0 && sec.offset()) {
                var offsetSection = sec.offset().top - 95;
                var nextSec = $('#section_' + (value + 1));
                var nextOffset = nextSec.length && nextSec.offset() ? nextSec.offset().top - 95 : 999999;

                if (docScroll1 >= offsetSection && docScroll1 < nextOffset) {
                    $('.navbar-nav .click-scroll').removeClass('active').addClass('inactive');
                    $('.navbar-nav .click-scroll[href*="section_' + value + '"]').addClass('active').removeClass('inactive');
                }
            }
        });
    });

    // Smooth scroll on click for in-page section links
    $(document).on('click', '.click-scroll', function (e) {
        var href = $(this).attr('href');
        if (!href) return;

        var hashIndex = href.indexOf('#section_');
        if (hashIndex !== -1) {
            var sectionId = href.substring(hashIndex);
            var sec = $(sectionId);
            if (sec.length > 0 && sec.offset()) {
                e.preventDefault();
                var offsetClick = sec.offset().top - 94;
                $('html, body').animate({
                    'scrollTop': offsetClick
                }, 300);

                // Update active state
                $('.navbar-nav .click-scroll').removeClass('active').addClass('inactive');
                $(this).addClass('active').removeClass('inactive');

                // If mobile menu open, collapse it
                if ($('.navbar-collapse').hasClass('show')) {
                    $('.navbar-collapse').collapse('hide');
                }
            }
        }
    });

    // If navigating to home with hash from another page (e.g. /#section_3)
    $(window).on('load', function () {
        if (window.location.hash && window.location.hash.indexOf('#section_') !== -1) {
            var sec = $(window.location.hash);
            if (sec.length > 0 && sec.offset()) {
                setTimeout(function () {
                    var offset = sec.offset().top - 94;
                    $('html, body').animate({
                        'scrollTop': offset
                    }, 350);
                }, 150);
            }
        }
    });
})(window.jQuery);
