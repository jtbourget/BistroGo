// BistroGo site-wide script

document.addEventListener('DOMContentLoaded', function () {
    // Highlight the active sidebar link based on current URL
    const current = window.location.pathname.toLowerCase();

    document.querySelectorAll('.sidebar-nav .nav-link').forEach(link => {
        const href = link.getAttribute('href')?.toLowerCase() || '';

        if (href && (current === href || current.startsWith(href + '/'))) {
            link.classList.add('active');
        }
    });

    // Sidebar toggle (mobile)
    document.getElementById('sidebarToggle')?.addEventListener('click', function () {
        document.querySelector('.app-wrapper')?.classList.toggle('sidebar-collapsed');
    });
});
