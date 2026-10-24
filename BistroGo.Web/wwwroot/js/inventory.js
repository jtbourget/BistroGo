// Inventory management script

document.addEventListener('DOMContentLoaded', function () {
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

    const newItemModalEl = document.getElementById('newItemModal');
    if (newItemModalEl && newItemModalEl.dataset.openModal === 'true') {
        new bootstrap.Modal(newItemModalEl).show();
    }
});
