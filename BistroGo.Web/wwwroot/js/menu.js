// Menu page interactions

document.addEventListener('DOMContentLoaded', function () {
    // Filter by category: automatically submit form on change
    const categorySelect = document.getElementById('category');
    if (categorySelect) {
        categorySelect.addEventListener('change', function () {
            this.form?.submit();
        });
    }

    // Stop propagation on "Add" button forms to prevent triggering the card click (modal)
    document.querySelectorAll('.menu-card-actions form').forEach(function (form) {
        form.addEventListener('click', function (e) {
            e.stopPropagation();
        });
    });

    // Fill the menu item modal with the clicked card's data
    const modal = document.getElementById('menuItemModal');
    if (modal) {
        modal.addEventListener('show.bs.modal', function (event) {
            const card = event.relatedTarget;
            if (!card) return;

            const name = card.getAttribute('data-item-name') || '';
            const category = card.getAttribute('data-item-category') || '';
            const price = card.getAttribute('data-item-price') || '0.00';
            const description = card.getAttribute('data-item-description') || '';

            const labelEl = modal.querySelector('#menuItemModalLabel');
            const categoryEl = modal.querySelector('#menuItemModalCategory');
            const priceEl = modal.querySelector('#menuItemModalPrice');
            const descEl = modal.querySelector('#menuItemModalDescription');

            if (labelEl) labelEl.textContent = name;
            if (categoryEl) categoryEl.textContent = category;
            if (priceEl) priceEl.textContent = '$' + price;
            if (descEl) descEl.textContent = description;
        });
    }
});
