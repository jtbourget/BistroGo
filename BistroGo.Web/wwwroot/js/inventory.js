// Inventory management script

document.addEventListener('DOMContentLoaded', function () {

    // ── Delete Confirmation Modal Logic ──
    const deleteItemId = document.getElementById('deleteItemId');
    const deleteItemName = document.getElementById('deleteItemName');
    const deleteForm = document.querySelector('#deleteConfirmationModal form');

    document.querySelectorAll('[data-bs-target="#deleteConfirmationModal"]').forEach(function (button) {
        button.addEventListener('click', function () {
            if (deleteItemId) deleteItemId.value = this.dataset.itemId;
            if (deleteItemName) deleteItemName.textContent = this.dataset.itemName;

            if (deleteForm) {
                const formAction = new URL(deleteForm.action, window.location.origin);
                formAction.searchParams.set('handler', this.dataset.deleteHandler || 'DeleteItem');
                deleteForm.action = formAction.toString();
            }
        });
    });

    // ── Re-open New Item Modal on Validation Error ──
    const newItemModalEl = document.getElementById('newItemModal');
    if (newItemModalEl && newItemModalEl.dataset.openModal === 'true') {
        new bootstrap.Modal(newItemModalEl).show();
    }

    // ── Handle Edit Item Click (Populate Modal) ──
    document.querySelectorAll('.edit-item-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const id = this.getAttribute('data-id');
            const name = this.getAttribute('data-name');
            const desc = this.getAttribute('data-desc');
            const price = this.getAttribute('data-price');
            const category = this.getAttribute('data-category');
            const image = this.getAttribute('data-image');

            setTimeout(() => {
                document.getElementById('edit-id').value = id || '';
                document.getElementById('edit-name').value = name || '';
                document.getElementById('edit-desc').value = desc || '';
                document.getElementById('edit-price').value = price || '';
                document.getElementById('edit-category').value = category || '';
                document.getElementById('edit-image').value = image || '';
            }, 100);
        });
    });

    // ── Re-open Edit Item Modal on Validation Error ──
    const editModalEl = document.getElementById('editItemModal');
    if (editModalEl && editModalEl.getAttribute('data-open-modal') === 'true') {
        new bootstrap.Modal(editModalEl).show();
    }

    // ── Handle Inline Category Editing ──
    document.querySelectorAll('.edit-inline-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const id = this.getAttribute('data-id');

            // Hide display view, show edit form
            document.getElementById(`display-view-${id}`).classList.add('d-none');
            document.getElementById('edit-view-' + id).classList.remove('d-none');

            // Add highlight border
            document.getElementById(`category-li-${id}`).classList.add('editing');

            // Auto-focus the input field
            document.getElementById(`input-${id}`).focus();
        });
    });

    document.querySelectorAll('.cancel-inline-btn').forEach(btn => {
        btn.addEventListener('click', function () {
            const id = this.getAttribute('data-id');

            // Hide edit form, show display view
            document.getElementById(`edit-view-${id}`).classList.add('d-none');
            document.getElementById(`display-view-${id}`).classList.remove('d-none');

            // Remove highlight border
            document.getElementById(`category-li-${id}`).classList.remove('editing');
        });
    });
});