// Manage staff script: handles edit and delete modal data population and modal reopening on validation error

document.addEventListener('DOMContentLoaded', function () {
    // Populate Edit Staff modal
    document.querySelectorAll('.edit-staff-btn').forEach(function (button) {
        button.addEventListener('click', function () {
            const id = this.getAttribute('data-id');
            const firstName = this.getAttribute('data-firstname');
            const lastName = this.getAttribute('data-lastname');
            const email = this.getAttribute('data-email');
            const role = this.getAttribute('data-role');

            const idInput = document.getElementById('editStaffId');
            const firstNameInput = document.getElementById('editFirstName');
            const lastNameInput = document.getElementById('editLastName');
            const emailInput = document.getElementById('editEmail');
            const roleSelect = document.getElementById('editRole');
            const passwordInput = document.getElementById('editPassword');
            const confirmPasswordInput = document.getElementById('editConfirmPassword');

            if (idInput) idInput.value = id || '';
            if (firstNameInput) firstNameInput.value = firstName || '';
            if (lastNameInput) lastNameInput.value = lastName || '';
            if (emailInput) emailInput.value = email || '';
            if (roleSelect) roleSelect.value = role || 'KitchenStaff';
            if (passwordInput) passwordInput.value = '';
            if (confirmPasswordInput) confirmPasswordInput.value = '';
        });
    });

    // Populate Delete Staff modal
    document.querySelectorAll('.delete-staff-btn').forEach(function (button) {
        button.addEventListener('click', function () {
            const id = this.getAttribute('data-id');
            const name = this.getAttribute('data-name');
            const email = this.getAttribute('data-email');

            const idInput = document.getElementById('deleteStaffId');
            const nameSpan = document.getElementById('deleteStaffName');
            const emailSpan = document.getElementById('deleteStaffEmail');

            if (idInput) idInput.value = id || '';
            if (nameSpan) nameSpan.textContent = name || '';
            if (emailSpan) emailSpan.textContent = email || '';
        });
    });

    // Automatically reopen modals if server-side validation failed
    const createModalEl = document.getElementById('createStaffModal');
    if (createModalEl && createModalEl.dataset.openModal === 'true') {
        new bootstrap.Modal(createModalEl).show();
    }

    const editModalEl = document.getElementById('editStaffModal');
    if (editModalEl && editModalEl.dataset.openModal === 'true') {
        new bootstrap.Modal(editModalEl).show();
    }
});
