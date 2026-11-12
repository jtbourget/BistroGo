/**
 * Tax Rate Update Manager
 * Handles the tax rate update form submission and API communication.
 * Works both as a standalone page and modal dialog.
 */

const TaxRateUpdateManager = (() => {
    const API_ENDPOINT = '?handler=UpdateTaxRate';
    const CURRENT_RATE_ENDPOINT = '/api/taxrates/current';

    // DOM Elements
    let form, newRateInput, submitBtn, currentRateDisplay;
    let successAlert, successMessage;
    let errorAlert, errorMessage;
    let validationErrorAlert, validationErrorMessage;
    let loadingIndicator;

    /**
     * Initializes the form and event handlers when DOM is ready.
     */
    function init() {
        // Cache DOM elements (works for both page and modal)
        form = document.getElementById('updateTaxRateForm');
        newRateInput = document.getElementById('newRateInput');
        submitBtn = document.getElementById('submitBtn');
        currentRateDisplay = document.getElementById('currentRateDisplay');
        successAlert = document.getElementById('successAlert');
        successMessage = document.getElementById('successMessage');
        errorAlert = document.getElementById('errorAlert');
        errorMessage = document.getElementById('errorMessage');
        validationErrorAlert = document.getElementById('validationErrorAlert');
        validationErrorMessage = document.getElementById('validationErrorMessage');
        loadingIndicator = document.getElementById('loadingIndicator');

        if (!form) return; // Page/modal might not be loaded if user is not manager


        // Attach form submission handler
        form.addEventListener('submit', handleFormSubmit);

        // Real-time input validation: immediately clear classes and validate on any input
        newRateInput.addEventListener('input', onInputChange);
        newRateInput.addEventListener('change', validateInput);
        newRateInput.addEventListener('blur', validateInput);

        // Reset form when modal is hidden
        const modal = document.getElementById('manageTaxRateModal');
        if (modal) {
            modal.addEventListener('hidden.bs.modal', resetModal);
        }
    }

    /**
     * Handles input event - clears validation state when user starts typing.
     */
    function onInputChange() {
        // Clear validation classes immediately when user types
        newRateInput.classList.remove('is-valid', 'is-invalid');
        hideValidationError();
    }

    /**
     * Resets the modal form to initial state when closed.
     */
    function resetModal() {
        if (form) {
            form.reset();
            hideAllAlerts();
            newRateInput.classList.remove('is-valid', 'is-invalid');
        }
    }

    /**
     * Validates the input field in real-time.
     * This is called on change/blur events.
     */
    function validateInput() {
        const value = newRateInput.value;

        // Clear alerts but keep validation messages
        successAlert.classList.add('d-none');
        errorAlert.classList.add('d-none');

        if (value === '') {
            // Empty field - no validation yet
            newRateInput.classList.remove('is-valid', 'is-invalid');
            validationErrorAlert.classList.add('d-none');
            return;
        }

        const numValue = parseFloat(value);

        // Validation checks
        if (isNaN(numValue)) {
            showValidationError('Please enter a valid number.');
            return;
        }

        if (numValue < 0 || numValue > 20) {
            showValidationError('Tax rate must be between 0% and 20%.');
            return;
        }

        // All validation passed
        validationErrorAlert.classList.add('d-none');
        newRateInput.classList.remove('is-invalid');
        newRateInput.classList.add('is-valid');
    }

    /**
     * Handles form submission.
     */
    async function handleFormSubmit(e) {
        e.preventDefault();
        hideAllAlerts();

        const rateValue = newRateInput.value.trim();

        // Validation
        if (!rateValue) {
            showValidationError('Please enter a tax rate.');
            newRateInput.focus();
            return;
        }

        const rate = parseFloat(rateValue);

        if (isNaN(rate)) {
            showValidationError('Please enter a valid number.');
            return;
        }

        if (rate < 0 || rate > 20) {
            showValidationError('Tax rate must be between 0% and 20%.');
            return;
        }

        // Convert percentage to decimal (e.g., 8.5 → 0.085)
        const rateDecimal = rate / 100;

        // Submit to API
        await submitRateUpdate(rateDecimal, rate);
    }

    /**
     * Submits the rate update to the API.
     * @param {number} rateDecimal - The rate as a decimal (e.g., 0.085 for 8.5%)
     * @param {number} ratePercent - The rate as a percentage (e.g., 8.5)
     */
    async function submitRateUpdate(rateDecimal, ratePercent) {
        submitBtn.disabled = true;
        showLoadingIndicator();

        try {
            console.log(`Submitting tax rate update: ${ratePercent}% (${rateDecimal} decimal)`);

            const csrfToken = document.querySelector('#csrfForm input[name="__RequestVerificationToken"]')?.value || '';
            const response = await fetch(API_ENDPOINT, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': csrfToken
                },
                body: JSON.stringify({ rate: rateDecimal })
            });

            console.log(`API response status: ${response.status}`);

            if (response.status === 401) {
                showError('Unauthorized: Please log in again.');
                return;
            }

            if (response.status === 403) {
                showError('Forbidden: Only managers can update the tax rate.');
                return;
            }

            if (!response.ok) {
                const errorData = await response.json().catch(() => ({}));
                const errorMsg = errorData.error || `Server error: ${response.status}`;
                console.error('API error:', errorMsg);
                showError(errorMsg);
                return;
            }

            const data = await response.json();
            console.log('Tax rate updated successfully:', data);

            // Success: Show message and refresh cache
            showSuccess(`Tax rate successfully updated to ${ratePercent.toFixed(2)}%`);

            // Update the displayed current rate
            currentRateDisplay.value = ratePercent.toFixed(2);

            // Clear the form
            form.reset();
            newRateInput.classList.remove('is-valid', 'is-invalid');

            // Close modal after 2 seconds on success
            setTimeout(() => {
                const modal = bootstrap.Modal.getInstance(document.getElementById('manageTaxRateModal'));
                if (modal) {
                    modal.hide();
                }
            }, 2000);
        } catch (error) {
            console.error('Error updating tax rate:', error);
            showError(`Network error: ${error.message}`);
        } finally {
            submitBtn.disabled = false;
            hideLoadingIndicator();
        }
    }

    /**
     * Displays a success alert.
     * @param {string} message - The success message
     */
    function showSuccess(message) {
        hideAllAlerts();
        successMessage.textContent = message;
        successAlert.classList.remove('d-none');
    }

    /**
     * Displays an error alert.
     * @param {string} message - The error message
     */
    function showError(message) {
        hideAllAlerts();
        errorMessage.textContent = message;
        errorAlert.classList.remove('d-none');
    }

    /**
     * Displays a validation error.
     * @param {string} message - The validation error message
     */
    function showValidationError(message) {
        hideAllAlerts();
        validationErrorMessage.textContent = message;
        validationErrorAlert.classList.remove('d-none');
        newRateInput.classList.add('is-invalid');
        newRateInput.classList.remove('is-valid');
    }

    /**
     * Hides the validation error message.
     */
    function hideValidationError() {
        validationErrorAlert.classList.add('d-none');
    }

    /**
     * Hides all alert messages.
     */
    function hideAllAlerts() {
        successAlert.classList.add('d-none');
        errorAlert.classList.add('d-none');
        validationErrorAlert.classList.add('d-none');
    }

    /**
     * Shows the loading indicator.
     */
    function showLoadingIndicator() {
        loadingIndicator.classList.remove('d-none');
    }

    /**
     * Hides the loading indicator.
     */
    function hideLoadingIndicator() {
        loadingIndicator.classList.add('d-none');
    }

    // Public API
    return {
        init
    };
})();

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    TaxRateUpdateManager.init();
});
