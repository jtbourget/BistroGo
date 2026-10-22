// Orders management page interactions

document.addEventListener('DOMContentLoaded', function () {
    const modal = document.getElementById('orderModal');
    if (!modal) return;

    modal.addEventListener('show.bs.modal', function (event) {
        const card = event.relatedTarget;
        if (!card) return;

        const id       = card.getAttribute('data-order-id') || '';
        const customer = card.getAttribute('data-order-customer') || '';
        const table    = card.getAttribute('data-order-table') || '';
        const status   = card.getAttribute('data-order-status') || '';
        const total    = card.getAttribute('data-order-total') || '0.00';
        const time     = card.getAttribute('data-order-time') || '';

        const modalId = modal.querySelector('#orderModalId');
        const modalCustomer = modal.querySelector('#orderModalCustomer');
        const modalTable = modal.querySelector('#orderModalTable');
        const modalTime = modal.querySelector('#orderModalTime');
        const modalTotal = modal.querySelector('#orderModalTotal');

        if (modalId) modalId.textContent = '#' + id;
        if (modalCustomer) modalCustomer.textContent = customer;
        if (modalTable) modalTable.textContent = 'Table ' + table;
        if (modalTime) modalTime.textContent = time;
        if (modalTotal) modalTotal.textContent = '$' + total;

        // Status badge color
        const badge = modal.querySelector('#orderModalStatusBadge');
        if (badge) {
            badge.textContent = status;
            badge.className = 'badge ' + (
                status === 'Waiting'   ? 'badge-reserved' :
                status === 'Preparing' ? 'badge-occupied' :
                status === 'Served'    ? 'badge-available' :
                'bg-secondary'
            );
        }

        // Wire the hidden inputs on both forms
        const statusIdInput = modal.querySelector('#orderStatusId');
        const completeIdInput = modal.querySelector('#orderCompleteId');
        if (statusIdInput) statusIdInput.value = id;
        if (completeIdInput) completeIdInput.value = id;

        // Pre-select the current status in the dropdown (if it's Waiting/Preparing)
        const select = modal.querySelector('select[name="status"]');
        if (select && (status === 'Waiting' || status === 'Preparing')) {
            select.value = status;
        }
    });
});
