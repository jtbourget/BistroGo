// Interactive walkthrough guide using Intro.js

document.addEventListener('DOMContentLoaded', function () {
    document.getElementById('startWalkthroughBtn')?.addEventListener('click', function (e) {
        e.preventDefault();
        const path = window.location.pathname.toLowerCase();
        if (!path.includes('/menu')) {
            window.location.href = '/Menu?walkthrough=true';
            return;
        }
        startWalkthrough();
    });

    function startWalkthrough() {
        const steps = [];

        const categoryEl = document.querySelector('#category');
        if (categoryEl) {
            steps.push({
                element: categoryEl,
                intro: "Welcome to BistroGo! Let's get started by selecting a category to filter the menu items.",
                position: 'right'
            });
        }

        const addBtn = document.querySelector('.btn-add-to-order');
        if (addBtn) {
            steps.push({
                element: addBtn,
                intro: "Once you find an item you like, click here to add it to your cart.",
                position: 'top'
            });
        }

        const cartEl = document.querySelector('a[data-bs-target="#currentOrderModal"]');
        if (cartEl) {
            steps.push({
                element: cartEl,
                intro: "Click here to view your cart and proceed to checkout when you're ready.",
                position: 'right'
            });
        }

        if (steps.length > 0 && typeof introJs === 'function') {
            introJs().setOptions({
                steps: steps,
                showProgress: true,
                showBullets: false
            }).start();
        } else if (typeof introJs !== 'function') {
            console.error('Intro.js is not loaded.');
        } else {
            alert("Walkthrough steps are not available on this page.");
        }
    }

    if (window.location.search.includes('walkthrough=true')) {
        setTimeout(startWalkthrough, 500);
        window.history.replaceState({}, document.title, window.location.pathname);
    }
});
