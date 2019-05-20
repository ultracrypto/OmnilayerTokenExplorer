// Auto update layout
if (window.layoutHelpers) {
    window.layoutHelpers.setAutoUpdate(true)
}

$(function () {
    // Initialize sidenav
    $('#layout-sidenav').each(function () {
        new SideNav(this, {
            orientation: $(this).hasClass('sidenav-horizontal') ? 'horizontal' : 'vertical'
        })
    });

    // Initialize sidenav togglers
    $('body').on('click', '.layout-sidenav-toggle', function (e) {
        e.preventDefault()
        window.layoutHelpers.toggleCollapsed()
    })

    // Swap dropdown menus in RTL mode
    if ($('html').attr('dir') === 'rtl') {
        $('#layout-navbar .dropdown-menu').toggleClass('dropdown-menu-right')
    }
    setTimeout(function () {
        DoRotate(360);
    }, 100);
});

function DoRotate(angle) {

    var $elem = $('#logo_45');
    if ($elem.length)
    {
        $({ deg: 0 }).animate({ deg: angle }, {
            duration: 2000,
            step: function (now) {
                $elem.css({
                    transform: 'rotateY(' + now + 'deg)'
                });
            },
            done: function () {
                setTimeout(function () {
                    DoRotate(360);
                }, 1500);
            }
        });
    }
}
