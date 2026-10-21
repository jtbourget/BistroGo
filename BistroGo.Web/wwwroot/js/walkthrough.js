// ════════════════════════════════════════════════════════════════════════════════
// Walkthrough Session Management
// ════════════════════════════════════════════════════════════════════════════════

const WALKTHROUGH_KEY = 'walkthroughSeen';

// Check if walkthrough has been seen before in this session
function hasSeenWalkthrough() {
    try {
        return sessionStorage.getItem(WALKTHROUGH_KEY) === 'true';
    } catch (e) {
        console.error('Error checking walkthrough status:', e);
        return false;
    }
}

// Mark walkthrough as seen in this session
function markSeen() {
    try {
        sessionStorage.setItem(WALKTHROUGH_KEY, 'true');
    } catch (e) {
        console.error('Error marking walkthrough as seen:', e);
    }
}

// Show the initial walkthrough prompt using Bootstrap modal
function showWalkthroughPrompt() {
    const modal = document.getElementById('walkthroughPromptModal');
    if (!modal) {
        console.error('Walkthrough prompt modal not found');
        return;
    }

    const bsModal = new bootstrap.Modal(modal);
    bsModal.show();

    // Handle the Start Walkthrough button click
    const confirmBtn = document.getElementById('confirmWalkthroughBtn');
    const handleConfirm = () => {
        confirmBtn.removeEventListener('click', handleConfirm);
        bsModal.hide();
        setTimeout(() => {
            startWalkthrough();
            // After walkthrough completes, show the button highlight
            // The intro.js 'oncomplete' callback will handle showing the highlight
        }, 300);
    };

    confirmBtn.addEventListener('click', handleConfirm);

    // Handle the modal being dismissed
    const handleDismiss = () => {
        modal.removeEventListener('hidden.bs.modal', handleDismiss);
        confirmBtn.removeEventListener('click', handleConfirm);
        // User clicked "Maybe Later" or closed the modal - show button highlight
        showWalkthroughButtonHighlight();
    };

    modal.addEventListener('hidden.bs.modal', handleDismiss, { once: true });
}

// Show the button highlight after walkthrough ends or when user declines
function showWalkthroughButtonHighlight() {
    const btn = document.getElementById('startWalkthroughBtn');
    if (!btn) return;

    // Create a simple intro.js highlight just for the button
    const steps = [{
        element: btn,
        intro: "💡 You can access the walkthrough anytime by clicking this button!",
        position: 'left'
    }];

    introJs().setOptions({
        steps: steps,
        showProgress: false,
        showBullets: false,
        exitOnOverlayClick: true
    }).start();
}

// Show the completion modal and highlight the button
function showWalkthroughCompleteWithHighlight() {
    const modal = document.getElementById('walkthroughCompleteModal');
    if (!modal) {
        console.error('Walkthrough complete modal not found');
        // Fallback to just showing the highlight
        showWalkthroughButtonHighlight();
        return;
    }

    const bsModal = new bootstrap.Modal(modal);
    bsModal.show();

    // When the modal is dismissed, show the button highlight
    const handleDismiss = () => {
        modal.removeEventListener('hidden.bs.modal', handleDismiss);
        showWalkthroughButtonHighlight();
    };

    modal.addEventListener('hidden.bs.modal', handleDismiss, { once: true });
}

// Initialize walkthrough on page load
window.addEventListener('load', function () {
    // Check if walkthrough=true is in the URL (continue existing flow)
    if (window.location.search.includes('walkthrough=true')) {
        setTimeout(startWalkthrough, 500);
        window.history.replaceState({}, document.title, window.location.pathname);
    } 
    // Otherwise, check if this is the first visit
    else if (!hasSeenWalkthrough()) {
        // First visit - show the prompt
        setTimeout(() => {
            showWalkthroughPrompt();
        }, 500);
    }
    // If walkthrough has been seen before, do nothing
});
