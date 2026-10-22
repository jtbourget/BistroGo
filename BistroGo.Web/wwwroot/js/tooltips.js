// Bootstrap tooltips initialization

document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(el => {
        new bootstrap.Tooltip(el, {
            html: true,
            delay: { show: 0, hide: 0 }
        });
    });
});
