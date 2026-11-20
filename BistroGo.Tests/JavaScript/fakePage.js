// ════════════════════════════════════════════════════════════════════════════════
// Test helpers for the JavaScript tests (OC-21)
// ════════════════════════════════════════════════════════════════════════════════
//
// Runs a real script from BistroGo.Web/wwwroot/js inside an isolated sandbox with:
//   - a small fake page (only the parts the script looks at), and
//   - fake Intro.js and Bootstrap objects that record what the script asks them to do.
//
// Uses only Node's built-in modules, so the tests need no npm packages.
// ════════════════════════════════════════════════════════════════════════════════

'use strict';

const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

// Repo root, worked out from this file's location (BistroGo.Tests/JavaScript).
const repoRoot = path.resolve(__dirname, '..', '..');

/**
 * A pretend HTML element: just enough for the scripts under test.
 * Supports ids, data-tour markers, child elements, click listeners and
 * querySelector('[data-tour="..."]').
 */
class FakeElement {
    constructor(options) {
        const settings = options || {};

        this.id = settings.id || '';
        this.tour = settings.tour || '';
        this.label = settings.label || '';
        this.children = [];
        this.parent = null;
        this.listeners = {};
    }

    /** Adds a child element and returns it, so pages can be built in one expression. */
    add(child) {
        child.parent = this;
        this.children.push(child);

        return child;
    }

    addEventListener(type, handler) {
        if (!this.listeners[type]) {
            this.listeners[type] = [];
        }

        this.listeners[type].push(handler);
    }

    /** Runs every listener for the event, like a real click would. */
    dispatch(type) {
        const handlers = this.listeners[type] || [];

        handlers.forEach(function (handler) {
            handler({ type: type, preventDefault: function () { } });
        });
    }

    /** Every element below this one, depth first, in page order. */
    descendants() {
        const found = [];

        this.children.forEach(function (child) {
            found.push(child);
            found.push(...child.descendants());
        });

        return found;
    }

    /** Supports the one selector shape the scripts use: [data-tour="name"]. */
    querySelector(selector) {
        const match = /^\[data-tour="([^"]+)"\]$/.exec(selector);
        let result = null;

        if (match) {
            result = this.descendants().find(function (element) {
                return element.tour === match[1];
            }) || null;
        }

        return result;
    }
}

/**
 * Builds a fake Cook Dashboard.
 * @param {object} [options]
 * @param {object[]} [options.cards] - One entry per order card, newest first.
 *   Each can set { complete: false } to leave out the ✓ Complete button, as the
 *   real page does for picked-up and cancelled orders. Leave empty for an empty queue.
 * @param {boolean} [options.withButton=true] - Include the Kitchen Walkthrough button.
 * @param {boolean} [options.withToast=true] - Include the completion toast.
 * @returns {object} { document, body, cards, elements } for the tests to inspect.
 */
function buildCookDashboard(options) {
    const settings = options || {};
    const cards = settings.cards || [];
    const body = new FakeElement({ label: 'body' });
    const elements = {};

    if (settings.withButton !== false) {
        elements.button = body.add(new FakeElement({
            id: 'startKitchenWalkthroughBtn',
            tour: 'walkthrough-button',
            label: 'Kitchen Walkthrough button'
        }));
    }

    elements.filters = body.add(new FakeElement({ tour: 'status-filters', label: 'filter tabs' }));

    if (cards.length === 0) {
        elements.empty = body.add(new FakeElement({ tour: 'order-empty', label: 'empty message' }));
    }

    const cardElements = cards.map(function (card, index) {
        const cardElement = body.add(new FakeElement({ tour: 'order-card', label: 'card ' + index }));

        cardElement.add(new FakeElement({ tour: 'order-header', label: 'card ' + index + ' header' }));
        cardElement.add(new FakeElement({ tour: 'order-items', label: 'card ' + index + ' items' }));
        cardElement.add(new FakeElement({ tour: 'order-status', label: 'card ' + index + ' status' }));

        if (card.complete !== false) {
            cardElement.add(new FakeElement({ tour: 'order-complete', label: 'card ' + index + ' complete' }));
        }

        return cardElement;
    });

    if (settings.withToast !== false) {
        elements.toast = body.add(new FakeElement({
            id: 'kitchenWalkthroughCompleteToast',
            label: 'completion toast'
        }));
    }

    const document = new FakeElement({ label: 'document' });
    document.add(body);
    document.getElementById = function (id) {
        return document.descendants().find(function (element) {
            return element.id === id;
        }) || null;
    };

    return { document: document, body: body, cards: cardElements, elements: elements };
}

/**
 * Creates a fake Intro.js. Each call to introJs() records one tour: the options it
 * was given, the callbacks it registered, and whether it was started.
 * @returns {object} { introJs, tours } — pass introJs to the sandbox, read tours in tests.
 */
function createFakeIntroJs() {
    const tours = [];

    function introJs() {
        const tour = {
            options: null,
            callbacks: {},
            started: false
        };
        tours.push(tour);

        const api = {
            setOptions: function (options) {
                tour.options = options;

                return api;
            },
            oncomplete: function (handler) {
                tour.callbacks.complete = handler;

                return api;
            },
            onexit: function (handler) {
                tour.callbacks.exit = handler;

                return api;
            },
            start: function () {
                tour.started = true;

                return api;
            }
        };

        return api;
    }

    return { introJs: introJs, tours: tours };
}

/**
 * Creates a fake Bootstrap that records which toast elements were shown.
 * @returns {object} { bootstrap, shownToasts }
 */
function createFakeBootstrap() {
    const shownToasts = [];
    const bootstrap = {
        Toast: {
            getOrCreateInstance: function (element) {
                return {
                    show: function () {
                        shownToasts.push(element);
                    }
                };
            }
        }
    };

    return { bootstrap: bootstrap, shownToasts: shownToasts };
}

/**
 * Loads a script from BistroGo.Web/wwwroot/js into a fresh sandbox, then fires
 * DOMContentLoaded the way a browser does once the page has loaded.
 * @param {string} scriptName - File name, e.g. "kitchen-walkthrough.js".
 * @param {object} globals - What the sandbox can see: document, introJs, bootstrap, console.
 */
function loadScript(scriptName, globals) {
    const scriptPath = path.join(repoRoot, 'BistroGo.Web', 'wwwroot', 'js', scriptName);
    const code = fs.readFileSync(scriptPath, 'utf8');
    const sandbox = vm.createContext(Object.assign({}, globals));

    vm.runInContext(code, sandbox, { filename: scriptPath });
    globals.document.dispatch('DOMContentLoaded');
}

/** Reads any repo file as text, by path from the repo root. */
function readRepoFile(relativePath) {
    return fs.readFileSync(path.join(repoRoot, relativePath), 'utf8');
}

module.exports = {
    buildCookDashboard,
    createFakeIntroJs,
    createFakeBootstrap,
    loadScript,
    readRepoFile
};
