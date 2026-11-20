// ════════════════════════════════════════════════════════════════════════════════
// Tests for the kitchen walkthrough (OC-21)
// ════════════════════════════════════════════════════════════════════════════════
//
// Runs the real BistroGo.Web/wwwroot/js/kitchen-walkthrough.js against a fake Cook
// Dashboard (see fakePage.js) and checks the tour it builds against the story's
// acceptance criteria.
//
// Run from the repo root (Node 18 or later, no npm packages needed):
//     node --test "BistroGo.Tests/JavaScript/*.test.js"
// ════════════════════════════════════════════════════════════════════════════════

'use strict';

const { describe, it } = require('node:test');
const assert = require('node:assert/strict');
const {
    buildCookDashboard,
    createFakeIntroJs,
    createFakeBootstrap,
    loadScript,
    readRepoFile
} = require('./fakePage');

const SCRIPT = 'kitchen-walkthrough.js';

// Step titles in the order staff see them when there are orders on the page.
const FULL_TOUR_TITLES = [
    'Welcome to the Cook Dashboard',
    'The order queue',
    'Order details',
    'What to make',
    'Update the status',
    'What each status means',
    'Complete an order',
    'You\'re all set'
];

// Step titles when the queue is empty: the five order steps become one.
const EMPTY_TOUR_TITLES = [
    'Welcome to the Cook Dashboard',
    'The order queue',
    'No orders right now',
    'You\'re all set'
];

/**
 * Builds a fake dashboard, loads the script, and clicks Kitchen Walkthrough.
 * @param {object} [pageOptions] - Passed to buildCookDashboard.
 * @returns {object} The page, the recorded tours, the shown toasts and logged errors.
 */
function startTour(pageOptions) {
    const page = buildCookDashboard(pageOptions);
    const intro = createFakeIntroJs();
    const boot = createFakeBootstrap();
    const errors = [];

    loadScript(SCRIPT, {
        document: page.document,
        introJs: intro.introJs,
        bootstrap: boot.bootstrap,
        console: { error: function (message) { errors.push(message); } }
    });

    if (page.elements.button) {
        page.elements.button.dispatch('click');
    }

    return { page: page, tours: intro.tours, shownToasts: boot.shownToasts, errors: errors };
}

/** The tour's step titles, as a plain array (steps are built inside the sandbox). */
function titlesOf(tour) {
    return Array.from(tour.options.steps, function (step) {
        return step.title;
    });
}

/** Finds a step by its title. */
function stepTitled(tour, title) {
    return Array.from(tour.options.steps).find(function (step) {
        return step.title === title;
    });
}

describe('Kitchen walkthrough: starting and restarting', function () {
    it('starts the tour when Kitchen Walkthrough is clicked', function () {
        const result = startTour({ cards: [{}] });

        assert.equal(result.tours.length, 1);
        assert.equal(result.tours[0].started, true);
    });

    it('does not start on its own when the page loads', function () {
        const page = buildCookDashboard({ cards: [{}] });
        const intro = createFakeIntroJs();

        loadScript(SCRIPT, { document: page.document, introJs: intro.introJs, console: console });

        assert.equal(intro.tours.length, 0);
    });

    it('starts a fresh tour from step 1 each time the button is clicked again', function () {
        const result = startTour({ cards: [{}] });

        result.page.elements.button.dispatch('click');

        assert.equal(result.tours.length, 2);
        assert.equal(result.tours[1].started, true);
        assert.deepEqual(titlesOf(result.tours[1]), FULL_TOUR_TITLES);
    });

    it('does nothing on a page without the Kitchen Walkthrough button', function () {
        const result = startTour({ cards: [{}], withButton: false });

        assert.equal(result.tours.length, 0);
        assert.deepEqual(result.errors, []);
    });

    it('logs an error instead of crashing when Intro.js is missing', function () {
        const page = buildCookDashboard({ cards: [{}] });
        const errors = [];

        loadScript(SCRIPT, {
            document: page.document,
            console: { error: function (message) { errors.push(message); } }
        });

        assert.doesNotThrow(function () {
            page.elements.button.dispatch('click');
        });
        assert.equal(errors.length, 1);
        assert.match(errors[0], /Intro\.js is not loaded/);
    });
});

describe('Kitchen walkthrough: steps with orders in the queue', function () {
    it('shows all eight steps in order', function () {
        const result = startTour({ cards: [{}, {}] });

        assert.deepEqual(titlesOf(result.tours[0]), FULL_TOUR_TITLES);
    });

    it('explains the order queue on the filter tabs', function () {
        const result = startTour({ cards: [{}] });
        const step = stepTitled(result.tours[0], 'The order queue');

        assert.equal(step.element, result.page.elements.filters);
    });

    it('points every order step at the same (first) order card', function () {
        const result = startTour({ cards: [{}, {}, {}] });
        const tour = result.tours[0];
        const firstCard = result.page.cards[0];

        ['Order details', 'What to make', 'Update the status', 'Complete an order'].forEach(function (title) {
            assert.equal(stepTitled(tour, title).element.parent, firstCard, title + ' should be on the first card');
        });
    });

    it('points each order step at the right part of the card', function () {
        const result = startTour({ cards: [{}] });
        const tour = result.tours[0];

        assert.equal(stepTitled(tour, 'Order details').element.tour, 'order-header');
        assert.equal(stepTitled(tour, 'What to make').element.tour, 'order-items');
        assert.equal(stepTitled(tour, 'Update the status').element.tour, 'order-status');
        assert.equal(stepTitled(tour, 'Complete an order').element.tour, 'order-complete');
    });

    it('ends on the Kitchen Walkthrough button so staff know how to restart', function () {
        const result = startTour({ cards: [{}] });
        const step = stepTitled(result.tours[0], 'You\'re all set');

        assert.equal(step.element, result.page.elements.button);
        assert.match(step.intro, /run it again/);
    });
});

describe('Kitchen walkthrough: matching what is on screen', function () {
    it('uses another order\'s Complete button when the first order has none', function () {
        // The real page hides ✓ Complete on picked-up and cancelled orders.
        const result = startTour({ cards: [{ complete: false }, { complete: false }, {}] });
        const step = stepTitled(result.tours[0], 'Complete an order');

        assert.equal(step.element.parent, result.page.cards[2]);
    });

    it('centres the Complete step when no order has a Complete button', function () {
        const result = startTour({ cards: [{ complete: false }] });
        const step = stepTitled(result.tours[0], 'Complete an order');

        assert.equal(step.element, undefined);
        assert.equal(step.position, undefined);
    });

    it('swaps the order steps for one "No orders" step when the queue is empty', function () {
        const result = startTour({ cards: [] });
        const tour = result.tours[0];

        assert.deepEqual(titlesOf(tour), EMPTY_TOUR_TITLES);
        assert.equal(stepTitled(tour, 'No orders right now').element, result.page.elements.empty);
    });

    it('rebuilds the steps on each click, so they follow changes to the queue', function () {
        const result = startTour({ cards: [] });

        // An order arrives: the empty message goes and a card appears. The script and
        // button stay the same, just as when the real page refreshes the list.
        result.page.body.children = result.page.body.children.filter(function (child) {
            return child.tour !== 'order-empty';
        });
        const firstCard = result.page.body.add(buildCookDashboard({ cards: [{}] }).cards[0]);
        result.page.elements.button.dispatch('click');

        assert.deepEqual(titlesOf(result.tours[1]), FULL_TOUR_TITLES);
        assert.equal(stepTitled(result.tours[1], 'Order details').element.parent, firstCard);
    });
});

describe('Kitchen walkthrough: what the steps say', function () {
    it('explains all five statuses, including how cancelled orders are handled', function () {
        const result = startTour({ cards: [{}] });
        const text = stepTitled(result.tours[0], 'What each status means').intro;

        ['Received', 'Preparing', 'Ready', 'Picked Up / Completed', 'Cancelled'].forEach(function (status) {
            assert.ok(text.includes('<strong>' + status + '</strong>'), 'missing status: ' + status);
        });
        assert.match(text, /Shows under All only/);
    });

    it('centres the status list so it has room on any screen', function () {
        const result = startTour({ cards: [{}] });
        const step = stepTitled(result.tours[0], 'What each status means');

        assert.equal(step.element, undefined);
    });

    it('tells staff where to find items, quantities and special notes', function () {
        const result = startTour({ cards: [{}] });
        const text = stepTitled(result.tours[0], 'What to make').intro;

        assert.match(text, /quantity/);
        assert.match(text, /Note/);
    });

    it('explains the navigation controls in the welcome step', function () {
        const result = startTour({ cards: [{}] });
        const text = stepTitled(result.tours[0], 'Welcome to the Cook Dashboard').intro;

        assert.match(text, /Next/);
        assert.match(text, /Back/);
        assert.match(text, /dots/);
        assert.match(text, /×/);
    });

    it('never mentions manager-only features', function () {
        const result = startTour({ cards: [{}] });
        const allText = Array.from(result.tours[0].options.steps, function (step) {
            return step.title + ' ' + step.intro;
        }).join(' ');

        [/inventory/i, /manage staff/i, /tax rate/i, /expense/i, /menu item/i].forEach(function (forbidden) {
            assert.doesNotMatch(allText, forbidden);
        });
    });
});

describe('Kitchen walkthrough: navigation and completion', function () {
    it('turns on Next, Back, step dots and the progress bar', function () {
        const options = startTour({ cards: [{}] }).tours[0].options;

        assert.equal(options.showBullets, true);
        assert.equal(options.showProgress, true);
        assert.equal(options.nextLabel, 'Next →');
        assert.equal(options.prevLabel, '← Back');
        assert.equal(options.doneLabel, 'Finish');
    });

    it('protects real orders and stray taps during the tour', function () {
        const options = startTour({ cards: [{}] }).tours[0].options;

        assert.equal(options.disableInteraction, true);
        assert.equal(options.exitOnOverlayClick, false);
    });

    it('uses the kitchen tooltip class so only this tour gets the wider boxes', function () {
        const options = startTour({ cards: [{}] }).tours[0].options;

        assert.equal(options.tooltipClass, 'kitchen-walkthrough-tooltip');
    });

    it('shows the "Walkthrough complete" toast when staff click Finish', function () {
        const result = startTour({ cards: [{}] });

        result.tours[0].callbacks.complete();

        assert.equal(result.shownToasts.length, 1);
        assert.equal(result.shownToasts[0], result.page.elements.toast);
    });

    it('does not show the toast when staff leave early with ×', function () {
        const result = startTour({ cards: [{}] });

        // Only Finish (oncomplete) is hooked up; leaving with × (onexit) shows nothing.
        assert.equal(result.tours[0].callbacks.exit, undefined);
        assert.equal(result.shownToasts.length, 0);
    });

    it('still finishes cleanly if the toast is missing from the page', function () {
        const result = startTour({ cards: [{}], withToast: false });

        assert.doesNotThrow(function () {
            result.tours[0].callbacks.complete();
        });
        assert.equal(result.shownToasts.length, 0);
    });
});

describe('Kitchen walkthrough: the Orders page has everything the script needs', function () {
    const page = readRepoFile('BistroGo.Web/Pages/Orders/Index.cshtml');
    const script = readRepoFile('BistroGo.Web/wwwroot/js/kitchen-walkthrough.js');

    it('has every data-tour marker the script looks for', function () {
        const used = new Set();
        const pattern = /findTourElement\('([^']+)'/g;
        let match = pattern.exec(script);

        while (match) {
            used.add(match[1]);
            match = pattern.exec(script);
        }

        assert.ok(used.size >= 8, 'expected the script to use at least 8 markers');
        used.forEach(function (marker) {
            assert.ok(page.includes('data-tour="' + marker + '"'), 'Orders/Index.cshtml is missing data-tour="' + marker + '"');
        });
    });

    it('has the Kitchen Walkthrough button the script connects to', function () {
        assert.match(page, /id="startKitchenWalkthroughBtn"/);
        assert.match(page, /type="button"/);
    });

    it('has the completion toast the script shows', function () {
        assert.match(page, /id="kitchenWalkthroughCompleteToast"/);
    });

    it('loads the kitchen walkthrough script', function () {
        assert.match(page, /<script src="~\/js\/kitchen-walkthrough\.js"/);
    });

    it('has the CSS for the kitchen tooltip class', function () {
        const css = readRepoFile('BistroGo.Web/wwwroot/css/site.css');

        assert.match(css, /\.introjs-tooltip\.kitchen-walkthrough-tooltip\s*\{/);
    });
});
