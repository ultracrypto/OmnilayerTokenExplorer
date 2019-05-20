'use strict';

// Auto update layout
if (window.layoutHelpers) {
    window.layoutHelpers.setAutoUpdate(true);
}

$(function () {
    // Initialize sidenav
    $('#layout-sidenav').each(function () {
        new SideNav(this, {
            orientation: $(this).hasClass('sidenav-horizontal') ? 'horizontal' : 'vertical'
        });
    });

    // Initialize sidenav togglers
    $('body').on('click', '.layout-sidenav-toggle', function (e) {
        e.preventDefault();
        window.layoutHelpers.toggleCollapsed();
    });

    // Swap dropdown menus in RTL mode
    if ($('html').attr('dir') === 'rtl') {
        $('#layout-navbar .dropdown-menu').toggleClass('dropdown-menu-right');
    }
    setTimeout(function () {
        DoRotate(360);
    }, 100);
});

function DoRotate(angle) {

    var $elem = $('#logo_45');
    if ($elem.length) {
        $({ deg: 0 }).animate({ deg: angle }, {
            duration: 2000,
            step: function step(now) {
                $elem.css({
                    transform: 'rotateY(' + now + 'deg)'
                });
            },
            done: function done() {
                setTimeout(function () {
                    DoRotate(360);
                }, 1500);
            }
        });
    }
}
//# sourceMappingURL=data:application/json;charset=utf8;base64,eyJ2ZXJzaW9uIjozLCJzb3VyY2VzIjpbImpzL3NpdGUuanMiXSwibmFtZXMiOlsid2luZG93IiwibGF5b3V0SGVscGVycyIsInNldEF1dG9VcGRhdGUiLCIkIiwiZWFjaCIsIlNpZGVOYXYiLCJvcmllbnRhdGlvbiIsImhhc0NsYXNzIiwib24iLCJlIiwicHJldmVudERlZmF1bHQiLCJ0b2dnbGVDb2xsYXBzZWQiLCJhdHRyIiwidG9nZ2xlQ2xhc3MiLCJzZXRUaW1lb3V0IiwiRG9Sb3RhdGUiLCJhbmdsZSIsIiRlbGVtIiwibGVuZ3RoIiwiZGVnIiwiYW5pbWF0ZSIsImR1cmF0aW9uIiwic3RlcCIsIm5vdyIsImNzcyIsInRyYW5zZm9ybSIsImRvbmUiXSwibWFwcGluZ3MiOiI7O0FBQUE7QUFDQSxJQUFJQSxPQUFPQyxhQUFYLEVBQTBCO0FBQ3RCRCxXQUFPQyxhQUFQLENBQXFCQyxhQUFyQixDQUFtQyxJQUFuQztBQUNIOztBQUVEQyxFQUFFLFlBQVk7QUFDVjtBQUNBQSxNQUFFLGlCQUFGLEVBQXFCQyxJQUFyQixDQUEwQixZQUFZO0FBQ2xDLFlBQUlDLE9BQUosQ0FBWSxJQUFaLEVBQWtCO0FBQ2RDLHlCQUFhSCxFQUFFLElBQUYsRUFBUUksUUFBUixDQUFpQixvQkFBakIsSUFBeUMsWUFBekMsR0FBd0Q7QUFEdkQsU0FBbEI7QUFHSCxLQUpEOztBQU1BO0FBQ0FKLE1BQUUsTUFBRixFQUFVSyxFQUFWLENBQWEsT0FBYixFQUFzQix3QkFBdEIsRUFBZ0QsVUFBVUMsQ0FBVixFQUFhO0FBQ3pEQSxVQUFFQyxjQUFGO0FBQ0FWLGVBQU9DLGFBQVAsQ0FBcUJVLGVBQXJCO0FBQ0gsS0FIRDs7QUFLQTtBQUNBLFFBQUlSLEVBQUUsTUFBRixFQUFVUyxJQUFWLENBQWUsS0FBZixNQUEwQixLQUE5QixFQUFxQztBQUNqQ1QsVUFBRSwrQkFBRixFQUFtQ1UsV0FBbkMsQ0FBK0MscUJBQS9DO0FBQ0g7QUFDREMsZUFBVyxZQUFZO0FBQ25CQyxpQkFBUyxHQUFUO0FBQ0gsS0FGRCxFQUVHLEdBRkg7QUFHSCxDQXJCRDs7QUF1QkEsU0FBU0EsUUFBVCxDQUFrQkMsS0FBbEIsRUFBeUI7O0FBRXJCLFFBQUlDLFFBQVFkLEVBQUUsVUFBRixDQUFaO0FBQ0EsUUFBSWMsTUFBTUMsTUFBVixFQUNBO0FBQ0lmLFVBQUUsRUFBRWdCLEtBQUssQ0FBUCxFQUFGLEVBQWNDLE9BQWQsQ0FBc0IsRUFBRUQsS0FBS0gsS0FBUCxFQUF0QixFQUFzQztBQUNsQ0ssc0JBQVUsSUFEd0I7QUFFbENDLGtCQUFNLGNBQVVDLEdBQVYsRUFBZTtBQUNqQk4sc0JBQU1PLEdBQU4sQ0FBVTtBQUNOQywrQkFBVyxhQUFhRixHQUFiLEdBQW1CO0FBRHhCLGlCQUFWO0FBR0gsYUFOaUM7QUFPbENHLGtCQUFNLGdCQUFZO0FBQ2RaLDJCQUFXLFlBQVk7QUFDbkJDLDZCQUFTLEdBQVQ7QUFDSCxpQkFGRCxFQUVHLElBRkg7QUFHSDtBQVhpQyxTQUF0QztBQWFIO0FBQ0oiLCJmaWxlIjoianMvc2l0ZS5lczUuanMiLCJzb3VyY2VzQ29udGVudCI6WyIvLyBBdXRvIHVwZGF0ZSBsYXlvdXRcclxuaWYgKHdpbmRvdy5sYXlvdXRIZWxwZXJzKSB7XHJcbiAgICB3aW5kb3cubGF5b3V0SGVscGVycy5zZXRBdXRvVXBkYXRlKHRydWUpXHJcbn1cclxuXHJcbiQoZnVuY3Rpb24gKCkge1xyXG4gICAgLy8gSW5pdGlhbGl6ZSBzaWRlbmF2XHJcbiAgICAkKCcjbGF5b3V0LXNpZGVuYXYnKS5lYWNoKGZ1bmN0aW9uICgpIHtcclxuICAgICAgICBuZXcgU2lkZU5hdih0aGlzLCB7XHJcbiAgICAgICAgICAgIG9yaWVudGF0aW9uOiAkKHRoaXMpLmhhc0NsYXNzKCdzaWRlbmF2LWhvcml6b250YWwnKSA/ICdob3Jpem9udGFsJyA6ICd2ZXJ0aWNhbCdcclxuICAgICAgICB9KVxyXG4gICAgfSk7XHJcblxyXG4gICAgLy8gSW5pdGlhbGl6ZSBzaWRlbmF2IHRvZ2dsZXJzXHJcbiAgICAkKCdib2R5Jykub24oJ2NsaWNrJywgJy5sYXlvdXQtc2lkZW5hdi10b2dnbGUnLCBmdW5jdGlvbiAoZSkge1xyXG4gICAgICAgIGUucHJldmVudERlZmF1bHQoKVxyXG4gICAgICAgIHdpbmRvdy5sYXlvdXRIZWxwZXJzLnRvZ2dsZUNvbGxhcHNlZCgpXHJcbiAgICB9KVxyXG5cclxuICAgIC8vIFN3YXAgZHJvcGRvd24gbWVudXMgaW4gUlRMIG1vZGVcclxuICAgIGlmICgkKCdodG1sJykuYXR0cignZGlyJykgPT09ICdydGwnKSB7XHJcbiAgICAgICAgJCgnI2xheW91dC1uYXZiYXIgLmRyb3Bkb3duLW1lbnUnKS50b2dnbGVDbGFzcygnZHJvcGRvd24tbWVudS1yaWdodCcpXHJcbiAgICB9XHJcbiAgICBzZXRUaW1lb3V0KGZ1bmN0aW9uICgpIHtcclxuICAgICAgICBEb1JvdGF0ZSgzNjApO1xyXG4gICAgfSwgMTAwKTtcclxufSk7XHJcblxyXG5mdW5jdGlvbiBEb1JvdGF0ZShhbmdsZSkge1xyXG5cclxuICAgIHZhciAkZWxlbSA9ICQoJyNsb2dvXzQ1Jyk7XHJcbiAgICBpZiAoJGVsZW0ubGVuZ3RoKVxyXG4gICAge1xyXG4gICAgICAgICQoeyBkZWc6IDAgfSkuYW5pbWF0ZSh7IGRlZzogYW5nbGUgfSwge1xyXG4gICAgICAgICAgICBkdXJhdGlvbjogMjAwMCxcclxuICAgICAgICAgICAgc3RlcDogZnVuY3Rpb24gKG5vdykge1xyXG4gICAgICAgICAgICAgICAgJGVsZW0uY3NzKHtcclxuICAgICAgICAgICAgICAgICAgICB0cmFuc2Zvcm06ICdyb3RhdGVZKCcgKyBub3cgKyAnZGVnKSdcclxuICAgICAgICAgICAgICAgIH0pO1xyXG4gICAgICAgICAgICB9LFxyXG4gICAgICAgICAgICBkb25lOiBmdW5jdGlvbiAoKSB7XHJcbiAgICAgICAgICAgICAgICBzZXRUaW1lb3V0KGZ1bmN0aW9uICgpIHtcclxuICAgICAgICAgICAgICAgICAgICBEb1JvdGF0ZSgzNjApO1xyXG4gICAgICAgICAgICAgICAgfSwgMTUwMCk7XHJcbiAgICAgICAgICAgIH1cclxuICAgICAgICB9KTtcclxuICAgIH1cclxufVxyXG4iXX0=
