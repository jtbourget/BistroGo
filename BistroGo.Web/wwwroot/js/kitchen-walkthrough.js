// Kitchen walkthrough: a guided tour of the Cook Dashboard (Orders page), built on Intro.js.
// Kept separate from walkthrough.js (the customer tour) so the two never affect each other.
// Each step points at an element tagged with a data-tour="..." attribute in Orders/Index.cshtml.

(function () {
    'use strict';

    // Finds the first element on the page with the given tour marker, or null if that part
    // isn't on screen right now (for example, the order steps when the queue is empty).
    function findTourElement(name, scope) {
        const root = scope || document;

        return root.querySelector('[data-tour="' + name + '"]');
    }

    // Builds one Intro.js step. A step with no element floats in the middle of the screen,
    // which is how the tour still explains a part of the page that isn't showing.
    function makeStep(element, title, intro, position) {
        const step = {
            title: title,
            intro: intro
        };

        if (element) {
            step.element = element;
            step.position = position || 'bottom';
        }

        return step;
    }

    // Steps that explain a single order card, using the first card in the current list.
    function buildOrderSteps(card) {
        const steps = [];

        steps.push(makeStep(
            findTourElement('order-header', card),
            'Order details',
            'Each card starts with the order number and the customer\'s name, then the pickup slot ' +
            'and the time the order was placed. The code in the grey box is the customer\'s ' +
            'confirmation code; use it to match the order when they collect it. The amount on the ' +
            'right is the order total.'));

        steps.push(makeStep(
            findTourElement('order-items', card),
            'What to make',
            'Every line shows the quantity and the item, for example <strong>2x</strong> Margherita Pizza. ' +
            'If the customer asked for something special, a red <strong>⚠️ Note</strong> appears ' +
            'under that item. Check these notes before you start cooking.'));

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

        // The Complete button is hidden on finished or cancelled orders, so the first card may not
        // have one. Fall back to any card that does, and to a floating step if none do.
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

    // The full list of steps, built from what is on the page right now so the tour always
    // matches what the cook can see.
    function buildSteps() {
        const steps = [];

        steps.push(makeStep(
            null,
            'Welcome to the Cook Dashboard',
            'This short tour shows you how to read incoming orders and move them through the kitchen. ' +
            'Use <strong>Next</strong> and <strong>Back</strong> to move through it, the dots to jump ' +
            'to a step, or <strong>×</strong> to leave at any time.'));

        steps.push(makeStep(
            findTourElement('status-filters'),
            'The order queue',
            'Every placed order is listed below, with newer orders at the top. These tabs filter the list: ' +
            '<strong>New</strong> (Received), <strong>Prep</strong> (Preparing), <strong>Ready</strong> ' +
            'and <strong>Done</strong> (Picked Up). The number on each tab is how many orders it holds. ' +
            '<strong>All</strong> shows everything, including cancelled orders, which have no tab of their own.'));

        const firstCard = findTourElement('order-card');

        if (firstCard) {
            steps.push(...buildOrderSteps(firstCard));
        } else {
            steps.push(makeStep(
                findTourElement('order-empty'),
                'No orders right now',
                'There are no orders in this view yet. When a customer places an order, it appears here ' +
                'as a card showing the order details, the items to make, any special notes, and controls ' +
                'to update its status. Run this tour again when orders are showing to see each part.'));
        }

        steps.push(makeStep(
            findTourElement('walkthrough-button'),
            'You\'re all set',
            'That\'s the whole tour. Click <strong>Kitchen Walkthrough</strong> anytime to run it again.',
            'bottom'));

        return steps;
    }

    // Shows the "walkthrough complete" message added to the Orders page. Does nothing if the
    // message isn't on the page, so the tour itself never fails because of it.
    function showCompletionMessage() {
        const toastElement = document.getElementById('kitchenWalkthroughCompleteToast');

        if (toastElement && typeof bootstrap !== 'undefined') {
            bootstrap.Toast.getOrCreateInstance(toastElement).show();
        }
    }

    function startKitchenWalkthrough() {
        if (typeof introJs === 'function') {
            introJs()
                .setOptions({
                    steps: buildSteps(),
                    showProgress: true,
                    showBullets: true,          // the dots let staff jump to any step
                    exitOnOverlayClick: false,  // a stray tap shouldn't end the tour
                    disableInteraction: true,   // no accidental status changes mid-tour
                    scrollToElement: true,
                    nextLabel: 'Next →',
                    prevLabel: '← Back',
                    doneLabel: 'Finish'
                })
                .oncomplete(showCompletionMessage)
                .start();
        } else {
            console.error('Intro.js is not loaded, so the kitchen walkthrough cannot start.');
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        const startButton = document.getElementById('startKitchenWalkthroughBtn');

        if (startButton) {
            startButton.addEventListener('click', startKitchenWalkthrough);
        }
    });
})();
