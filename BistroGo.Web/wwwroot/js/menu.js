// Menu page interactions

document.addEventListener('DOMContentLoaded', function () {

    // 1. Filter by category: automatically submit form on change
    const categorySelect = document.getElementById('category');
    if (categorySelect) {
        categorySelect.addEventListener('change', function () {
            this.form?.submit();
        });
    }

    // 2. Quantity Ticker Logic
    document.querySelectorAll('.quantity-ticker').forEach(ticker => {
        const minusBtn = ticker.querySelector('.js-qty-minus');
        const plusBtn = ticker.querySelector('.js-qty-plus');
        const display = ticker.querySelector('.qty-display');

        // Find the hidden input inside the form
        const form = ticker.closest('.menu-card-actions').querySelector('form');
        const hiddenInput = form.querySelector('.js-qty-input');

        if (minusBtn && plusBtn && display && hiddenInput) {
            minusBtn.addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                let currentVal = parseInt(display.textContent);
                if (currentVal > 1) {
                    currentVal--;
                    display.textContent = currentVal;
                    hiddenInput.value = currentVal;
                }
            });

            plusBtn.addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                let currentVal = parseInt(display.textContent);
                if (currentVal < 99) {
                    currentVal++;
                    display.textContent = currentVal;
                    hiddenInput.value = currentVal;
                }
            });
        }
    });

    // 3. Fill the menu item modal with the clicked card's data
    const modal = document.getElementById('menuItemModal');
    if (modal) {
        modal.addEventListener('show.bs.modal', function (event) {
            const card = event.relatedTarget;
            if (!card) return;

            const name = card.getAttribute('data-item-name') || '';
            const category = card.getAttribute('data-item-category') || '';
            const price = card.getAttribute('data-item-price') || '0.00';
            const description = card.getAttribute('data-item-description') || '';
            const imageUrl = card.getAttribute('data-item-image');

            const labelEl = modal.querySelector('#menuItemModalLabel');
            const categoryEl = modal.querySelector('#menuItemModalCategory');
            const priceEl = modal.querySelector('#menuItemModalPrice');
            const descEl = modal.querySelector('#menuItemModalDescription');
            const imageContainer = modal.querySelector('#modalImageContainer');

            if (labelEl) labelEl.textContent = name;
            if (categoryEl) categoryEl.textContent = category;
            if (priceEl) priceEl.textContent = '$' + price;
            if (descEl) descEl.textContent = description;

            // Handle the dynamic image inside the modal
            if (imageContainer) {
                if (imageUrl && imageUrl.trim() !== '') {
                    imageContainer.innerHTML = `<img src="${imageUrl}" alt="${name}" style="width:100%; height:100%; object-fit:cover;" />`;
                } else {
                    imageContainer.innerHTML = `<span class="menu-modal-image img">[img]</span>`;
                }
            }
        });
    }
});