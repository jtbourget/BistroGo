// Checkout page interactions

document.addEventListener('DOMContentLoaded', function () {
    const csrfToken = document.querySelector('#csrfForm input[name="__RequestVerificationToken"]')?.value || '';
    const confirmModalEl = document.getElementById('confirmOrderModal');
    const confirmModal = confirmModalEl ? new bootstrap.Modal(confirmModalEl) : null;

    // Cart Editing (- / + / ✕)
    const tableBody = document.getElementById('checkoutTableBody');
    const table = document.getElementById('checkoutTable');
    const emptyState = document.getElementById('checkoutEmptyState');
    const subtotalEl = document.getElementById('checkoutSubtotal');
    const taxEl = document.getElementById('checkoutTax');
    const totalEl = document.getElementById('checkoutTotal');
    const itemCountEl = document.getElementById('checkoutItemCount');

    if (tableBody) {
        tableBody.addEventListener('click', async function (e) {
            const row = e.target.closest('tr');
            if (!row) return;

            const menuItemId = parseInt(row.getAttribute('data-menu-item-id'), 10);
            const qtySpan = row.querySelector('.js-qty');
            const currentQty = parseInt(qtySpan ? qtySpan.textContent : '0', 10);

            let endpoint = null;
            let body = null;

            if (e.target.classList.contains('js-qty-increase')) {
                endpoint = '?handler=UpdateQuantityJson';
                body = new URLSearchParams({ menuItemId, quantity: currentQty + 1 });
            } else if (e.target.classList.contains('js-qty-decrease')) {
                endpoint = '?handler=UpdateQuantityJson';
                body = new URLSearchParams({ menuItemId, quantity: currentQty - 1 });
            } else if (e.target.classList.contains('js-remove-line')) {
                endpoint = '?handler=RemoveLineJson';
                body = new URLSearchParams({ menuItemId });
            }

            if (!endpoint) return;

            try {
                const response = await fetch(endpoint, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/x-www-form-urlencoded',
                        'RequestVerificationToken': csrfToken
                    },
                    body: body.toString()
                });

                if (!response.ok) return;

                const data = await response.json();

                const hasLines = data.lines && data.lines.length > 0;
                if (table) table.classList.toggle('d-none', !hasLines);
                if (emptyState) emptyState.classList.toggle('d-none', hasLines);

                tableBody.innerHTML = '';
                if (data.lines) {
                    for (const line of data.lines) {
                        const tr = document.createElement('tr');
                        tr.setAttribute('data-menu-item-id', line.menuItemId);
                        tr.innerHTML = `
                            <td>${escapeHtml(line.itemName)}</td>
                            <td class="text-end">$${line.unitPrice.toFixed(2)}</td>
                            <td class="text-center">
                                <div class="d-flex justify-content-center gap-1">
                                    <button type="button" class="btn btn-sm btn-outline-primary js-qty-decrease">−</button>
                                    <span class="px-2 align-self-center js-qty">${line.quantity}</span>
                                    <button type="button" class="btn btn-sm btn-outline-primary js-qty-increase">+</button>
                                </div>
                            </td>
                            <td class="text-end js-line-total">$${line.total.toFixed(2)}</td>
                            <td class="text-end">
                                <button type="button" class="btn btn-sm btn-link text-danger p-0 js-remove-line" title="Remove">✕</button>
                            </td>`;
                        tableBody.appendChild(tr);
                    }
                }

                if (subtotalEl) subtotalEl.textContent = '$' + data.subtotal.toFixed(2);
                if (taxEl) taxEl.textContent = '$' + data.tax.toFixed(2);
                if (totalEl) totalEl.textContent = '$' + data.total.toFixed(2);
                if (itemCountEl) itemCountEl.textContent = data.itemCount + ' items';

                // Sync the sidebar and menu badges to reflect the change
                const ticker = document.getElementById('cartTicker');
                if (ticker) {
                    ticker.textContent = data.itemCount;
                    ticker.classList.toggle('d-none', data.itemCount === 0);
                }
                const menuBadge = document.getElementById('menuCartBadge');
                if (menuBadge) {
                    menuBadge.textContent = data.itemCount;
                    menuBadge.classList.toggle('d-none', data.itemCount === 0);
                }

                // Disable Place Order if cart becomes empty
                const openBtn = document.getElementById('openConfirmModalBtn');
                if (openBtn) openBtn.disabled = !hasLines;
            } catch (err) {
                console.error('[Checkout] Cart update error:', err);
            }
        });
    }

    function escapeHtml(str) {
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    const openModalBtn = document.getElementById('openConfirmModalBtn');
    const submitBtn = document.getElementById('confirmPlaceOrderBtn');
    const checkoutForm = document.getElementById('checkoutForm');

    // ── Open Modal ──────────────────────────────────────────────
    openModalBtn?.addEventListener('click', function () {
        // Basic validation before opening
        if (checkoutForm && !checkoutForm.checkValidity()) {
            checkoutForm.reportValidity();
            return;
        }
        // Set the total in the modal text
        const modalTotalDisplay = document.getElementById('modalTotalDisplay');
        const checkoutTotal = document.getElementById('checkoutTotal');
        if (modalTotalDisplay && checkoutTotal) {
            modalTotalDisplay.textContent = checkoutTotal.textContent;
        }
        confirmModal?.show();
    });

    // ── Submit Order via AJAX ───────────────────────────────────
    submitBtn?.addEventListener('click', async function () {
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Processing...';

        const formData = new FormData(checkoutForm);
        const body = new URLSearchParams(formData);

        try {
            const response = await fetch('?handler=PlaceOrderJson', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': csrfToken
                },
                body: body.toString()
            });

            const data = await response.json();

            if (data.success) {
                confirmModal?.hide();

                // Hide form, show success screen
                document.getElementById('checkoutMainContent')?.classList.add('d-none');
                document.getElementById('checkoutSuccessView')?.classList.remove('d-none');

                if (data.orderId) {
                    const successMessage = document.getElementById('successMessage');
                    if (successMessage) {
                        successMessage.textContent = `Order #${data.orderId} has been received. We'll have it ready shortly!`;
                    }
                }

                // Sync the sidebar and menu badges to 0
                const ticker = document.getElementById('cartTicker');
                if (ticker) ticker.classList.add('d-none');
                const menuBadge = document.getElementById('menuCartBadge');
                if (menuBadge) menuBadge.classList.add('d-none');
            } else {
                alert(data.message || 'Failed to place order.');
                submitBtn.disabled = false;
                submitBtn.textContent = 'Place Order';
            }
        } catch (err) {
            console.error(err);
            alert('An error occurred. Please try again.');
            submitBtn.disabled = false;
            submitBtn.textContent = 'Place Order';
        }
    });
});
