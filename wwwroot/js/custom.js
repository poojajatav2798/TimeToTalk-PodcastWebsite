(function ($) {
  "use strict";

  // MENU
  $('.navbar-collapse a').on('click', function () {
    $(".navbar-collapse").collapse('hide');
  });

  // CUSTOM LINK
  $('.smoothscroll').click(function (e) {
    var el = $(this).attr('href');
    if (!el || !el.startsWith('#')) return;

    var elWrapped = $(el);
    if (!elWrapped.length || !elWrapped.offset()) return;

    e.preventDefault();
    var header_height = $('.navbar').height() || 0;
    var offsetTop = elWrapped.offset().top;

    $('body,html').animate({
      scrollTop: offsetTop - header_height
    }, 300);
  });

  if ($('.owl-carousel').length && $.fn.owlCarousel) {
    $('.owl-carousel').owlCarousel({
      center: true,
      loop: true,
      margin: 30,
      autoplay: true,
      responsiveClass: true,
      responsive: {
        0: { items: 2 },
        767: { items: 3 },
        1200: { items: 4 }
      }
    });
  }
})(window.jQuery);
