// Cart modal AJAX interactions (works across all pages)

(function () {
    function initCartModal() {
        const csrfToken = document.querySelector('#csrfForm input[name="__RequestVerificationToken"]')?.value || '';
        const cartTableBody = document.getElementById('cartTableBody');
        const cartTotal = document.getElementById('cartTotal');
        const cartTable = document.getElementById('cartTable');
        const cartEmptyState = document.getElementById('cartEmptyState');

        if (!cartTableBody) return;

        cartTableBody.addEventListener('click', async function (e) {
            const row = e.target.closest('tr');
            if (!row) return;

            const menuItemId = parseInt(row.getAttribute('data-menu-item-id'), 10);
            const qtySpan = row.querySelector('.js-qty');
            const currentQty = parseInt(qtySpan ? qtySpan.textContent : '0', 10);

            let endpoint = null;
            let body = null;

            if (e.target.classList.contains('js-qty-increase')) {
                endpoint = '/Menu?handler=UpdateQuantityJson';
                body = new URLSearchParams({ menuItemId, quantity: currentQty + 1 });
            } else if (e.target.classList.contains('js-qty-decrease')) {
                endpoint = '/Menu?handler=UpdateQuantityJson';
                body = new URLSearchParams({ menuItemId, quantity: currentQty - 1 });
            } else if (e.target.classList.contains('js-remove-line')) {
                endpoint = '/Menu?handler=RemoveLineJson';
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

                if (!response.ok) {
                    console.error('[Cart] Update failed:', response.status);
                    return;
                }

                const data = await response.json();
                renderCart(data);
                updateBadges(data.itemCount);
            } catch (err) {
                console.error('[Cart] Request error:', err);
            }
        });

        function renderCart(data) {
            const hasLines = data.lines && data.lines.length > 0;
            if (cartTable) cartTable.classList.toggle('d-none', !hasLines);
            if (cartEmptyState) cartEmptyState.classList.toggle('d-none', hasLines);

            cartTableBody.innerHTML = '';
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
                    cartTableBody.appendChild(tr);
                }
            }

            if (cartTotal) {
                cartTotal.textContent = '$' + (data.total ? data.total.toFixed(2) : '0.00');
            }
        }

        function updateBadges(count) {
            // Sidebar badge
            const cartTicker = document.getElementById('cartTicker');
            if (cartTicker) {
                cartTicker.textContent = count;
                cartTicker.classList.toggle('d-none', count === 0);
            }
            // Menu page badge (only present on Menu)
            const menuBadge = document.getElementById('menuCartBadge');
            if (menuBadge) {
                menuBadge.textContent = count;
                menuBadge.classList.toggle('d-none', count === 0);
            }
        }

        function escapeHtml(str) {
            const div = document.createElement('div');
            div.textContent = str;
            return div.innerHTML;
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initCartModal);
    } else {
        initCartModal();
    }
})();
