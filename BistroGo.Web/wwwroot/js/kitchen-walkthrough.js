// ════════════════════════════════════════════════════════════════════════════════
// Kitchen Walkthrough (OC-21)
// ════════════════════════════════════════════════════════════════════════════════
//
// A guided tour of the Cook Dashboard (Pages/Orders/Index.cshtml) that teaches kitchen
// staff how to read the order queue, find an order's details, and update its status.
//
// How it fits together:
//   - The page tags each part the tour talks about with a data-tour="..." attribute
//     (status-filters, order-card, order-header, order-items, order-status,
//     order-complete, order-empty, walkthrough-button).
//   - The "Kitchen Walkthrough" button (#startKitchenWalkthroughBtn) starts the tour.
//   - When staff click Finish, the #kitchenWalkthroughCompleteToast message is shown.
//
// Built on Intro.js 7.2.0, which _Layout.cshtml already loads on every page.
//
// This file is deliberately separate from walkthrough.js (the customer tour on the
// Menu page). Everything below is wrapped in a function so none of its names
// (for example startKitchenWalkthrough) can clash with names in other scripts.
// ════════════════════════════════════════════════════════════════════════════════

(function () {
    'use strict';   // turns silent mistakes (like typos creating global variables) into errors

    // ────────────────────────────────────────────────────────────────────────────
    // Finding parts of the page
    // ────────────────────────────────────────────────────────────────────────────

    /**
     * Finds the first element tagged with the given tour marker.
     * @param {string} name - The data-tour value to look for, e.g. "order-items".
     * @param {Element} [scope] - Optional element to search inside. Used to keep the
     *   order steps on one card, so the header, items and status all belong to the
     *   same order. Searches the whole page when left out.
     * @returns {Element|null} The element, or null if that part isn't on screen
     *   right now (for example, there are no order cards when the queue is empty).
     */
    function findTourElement(name, scope) {
        const root = scope || document;

        return root.querySelector('[data-tour="' + name + '"]');
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Building the steps
    // ────────────────────────────────────────────────────────────────────────────

    /**
     * Builds one Intro.js step.
     * @param {Element|null} element - What the step highlights. When null, the step
     *   has no element and Intro.js shows it as a box in the middle of the screen.
     *   That way a step still explains its part even when that part isn't showing.
     * @param {string} title - The heading at the top of the step.
     * @param {string} intro - The step's text. HTML is allowed (bold, lists).
     * @param {string} [position] - Where the box sits next to the element
     *   ("top", "bottom", "left", "right"). Defaults to "bottom". Ignored when there
     *   is no element.
     * @returns {object} A step object in the shape Intro.js expects.
     */
    function makeStep(element, title, intro, position) {
        const step = {
            title: title,
            intro: intro
        };

        // Only attach the element (and its position) when the part exists on the page.
        if (element) {
            step.element = element;
            step.position = position || 'bottom';
        }

        return step;
    }

    /**
     * Builds the four steps that explain a single order: its details, its items,
     * the status dropdown and the Complete button.
     
     * @param {Element} card - The order card to point at (the first one in the list).
     * @returns {object[]} The four steps, in order.
     */
    function buildOrderSteps(card) {
        const steps = [];

        // Order details: number, customer, pickup slot, time placed, confirmation code, total.
        steps.push(makeStep(
            findTourElement('order-header', card),
            'Order details',
            'Each card starts with the order number and the customer\'s name, then the pickup slot ' +
            'and the time the order was placed. The code in the grey box is the customer\'s ' +
            'confirmation code; use it to match the order when they collect it. The amount on the ' +
            'right is the order total.'));

        // Items, quantities and any special instructions (the red ⚠️ notes).
        steps.push(makeStep(
            findTourElement('order-items', card),
            'What to make',
            'Every line shows the quantity and the item, for example <strong>2x</strong> Margherita Pizza. ' +
            'If the customer asked for something special, a red <strong>⚠️ Note</strong> appears ' +
            'under that item. Check these notes before you start cooking.'));

        // The status dropdown and what each status means (the order lifecycle).
        // Positioned above the dropdown because the list of statuses makes this box tall.
        steps.push(makeStep(
            findTourElement('order-status', card),
            'Update the status',
            'Click the status badge to choose a new status. It saves straight away; there\'s no Save button.' +
            '<ul class="mb-0 mt-2 ps-3">' +
            '<li><strong>Received</strong>: a new order, not started yet.</li>' +
            '<li><strong>Preparing</strong>: you\'re cooking it.</li>' +
            '<li><strong>Ready</strong>: finished and waiting for the customer.</li>' +
            '<li><strong>Picked Up / Completed</strong>: the customer has it. It moves to the Done tab.</li>' +
            '<li><strong>Cancelled</strong>: it won\'t be made. It only shows under All, and its pickup spot is freed for another customer.</li>' +
            '</ul>',
            'top'));

        // The Complete button is hidden on orders that are already picked up or cancelled,
        // so the first card may not have one. Use the first card's button if it has one,
        // otherwise the first button anywhere on the page, otherwise null (a centered step).
        const completeButton = findTourElement('order-complete', card) || findTourElement('order-complete');

        steps.push(makeStep(
            completeButton,
            'Complete an order',
            'When the customer collects their order, click <strong>✓ Complete</strong>. It sets the ' +
            'status to Picked Up / Completed in one click, the same as choosing it from the status list. ' +
            'The button disappears once an order is completed or cancelled.',
            'top'));

        return steps;
    }

    /**
     * Builds the full tour from what is on the page right now.
     *
     * Called every time the tour starts, not once on page load, because the queue
     * changes during the day and the filter tabs change which orders are showing.
     * Building it fresh means the tour always matches what the cook can see.
     
     * @returns {object[]} Every step, in the order they're shown.
     */
    function buildSteps() {
        const steps = [];

        // 1. Welcome. No element, so it's centered. Explains the navigation controls.
        steps.push(makeStep(
            null,
            'Welcome to the Cook Dashboard',
            'This short tour shows you how to read incoming orders and move them through the kitchen. ' +
            'Use <strong>Next</strong> and <strong>Back</strong> to move through it, the dots to jump ' +
            'to a step, or <strong>×</strong> to leave at any time.'));

        // 2. The queue and its filter tabs, including where cancelled orders go.
        steps.push(makeStep(
            findTourElement('status-filters'),
            'The order queue',
            'Every placed order is listed below, with newer orders at the top. These tabs filter the list: ' +
            '<strong>New</strong> (Received), <strong>Prep</strong> (Preparing), <strong>Ready</strong> ' +
            'and <strong>Done</strong> (Picked Up). The number on each tab is how many orders it holds. ' +
            '<strong>All</strong> shows everything, including cancelled orders, which have no tab of their own.'));

        // 3–6. Walk through the first order card, or explain the empty queue.
        const firstCard = findTourElement('order-card');

        if (firstCard) {
            // The spread (...) adds each of the four order steps to this list.
            steps.push(...buildOrderSteps(firstCard));
        } else {
            steps.push(makeStep(
                findTourElement('order-empty'),
                'No orders right now',
                'There are no orders in this view yet. When a customer places an order, it appears here ' +
                'as a card showing the order details, the items to make, any special notes, and controls ' +
                'to update its status. Run this tour again when orders are showing to see each part.'));
        }

        // 7. Finish on the button that starts the tour, so staff know how to run it again.
        steps.push(makeStep(
            findTourElement('walkthrough-button'),
            'You\'re all set',
            'That\'s the whole tour. Click <strong>Kitchen Walkthrough</strong> anytime to run it again.',
            'bottom'));

        return steps;
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Running the tour
    // ────────────────────────────────────────────────────────────────────────────

    /**
     * Shows the "Walkthrough complete" message (#kitchenWalkthroughCompleteToast on the
     * Orders page). Runs only when staff click Finish, not when they leave early with ×.
     * Does nothing if the message or Bootstrap isn't on the page, so the tour itself
     * can never fail because of it.
     */
    function showCompletionMessage() {
        const toastElement = document.getElementById('kitchenWalkthroughCompleteToast');

        if (toastElement && typeof bootstrap !== 'undefined') {
            // getOrCreateInstance reuses the same toast if the tour is finished twice.
            bootstrap.Toast.getOrCreateInstance(toastElement).show();
        }
    }

    /**
     * Starts the tour from step 1. Clicking the button again later restarts it.
     * Logs an error instead of failing if Intro.js didn't load.
     */
    function startKitchenWalkthrough() {
        if (typeof introJs === 'function') {
            introJs()
                .setOptions({
                    steps: buildSteps(),
                    showProgress: true,         // progress bar showing how far through the tour you are
                    showBullets: true,          // the dots let staff jump to any step
                    exitOnOverlayClick: false,  // a stray tap on a busy kitchen tablet shouldn't end the tour
                    disableInteraction: true,   // highlighted controls can't be clicked, so no real order is changed by accident
                    scrollToElement: true,      // on small screens, scroll each highlighted part into view
                    nextLabel: 'Next →',
                    prevLabel: '← Back',
                    doneLabel: 'Finish'         // Intro.js shows its own × button for leaving early
                })
                .oncomplete(showCompletionMessage)  // fires on Finish only
                .start();
        } else {
            console.error('Intro.js is not loaded, so the kitchen walkthrough cannot start.');
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Wiring up the button
    // ────────────────────────────────────────────────────────────────────────────

    // Wait for the page to finish loading, then connect the Kitchen Walkthrough button.
    // The button only exists on the Orders page, so on any other page this does nothing.
    document.addEventListener('DOMContentLoaded', function () {
        const startButton = document.getElementById('startKitchenWalkthroughBtn');

        if (startButton) {
            startButton.addEventListener('click', startKitchenWalkthrough);
        }
    });
})();
