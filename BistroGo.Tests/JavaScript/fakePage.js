// ════════════════════════════════════════════════════════════════════════════════
// Test helpers for the JavaScript tests (OC-21, extended for OC-11)
// ════════════════════════════════════════════════════════════════════════════════
//
// Runs real scripts from BistroGo.Web/wwwroot/js inside an isolated sandbox with:
//   - a small fake page (only the parts the scripts look at), and
//   - fake Intro.js, Bootstrap, browser storage and console objects that record what the
//     scripts ask them to do.
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
 * Supports ids, data-* attributes, child elements, event listeners, click() and
 * querySelector / querySelectorAll for [data-...] selectors (plus any extra selectors an
 * element is given).
 */
class FakeElement {
    constructor(options) {
        const settings = options || {};

        this.id = settings.id || '';
        this.tour = settings.tour || '';
        this.label = settings.label || '';
        this.dataset = settings.dataset || {};       // data-storage-key becomes dataset.storageKey
        this.selectors = settings.selectors || [];   // other selectors this element answers to
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

    /** Runs every listener for the event, like a real event would. */
    dispatch(type) {
        const handlers = this.listeners[type] || [];

        handlers.forEach(function (handler) {
            handler({ type: type, preventDefault: function () { } });
        });
    }

    /** Runs the click listeners, like element.click() in a browser. */
    click() {
        this.dispatch('click');
    }

    /**
     * True when this element answers to the selector: [data-x], [data-x="value"], or one
     * of the extra selectors it was given.
     */
    matches(selector) {
        const match = /^\[data-([a-z-]+)(?:="([^"]*)")?\]$/.exec(selector);
        let result = this.selectors.includes(selector);

        if (!result && match) {
            const name = match[1].replace(/-([a-z])/g, function (whole, letter) {
                return letter.toUpperCase();
            });
            const actual = name === 'tour'
                ? this.tour || undefined
                : this.dataset[name];

            result = actual !== undefined && (match[2] === undefined || actual === match[2]);
        }

        return result;
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

    /** Every element below this one that matches the selector, in page order. */
    querySelectorAll(selector) {
        return this.descendants().filter(function (element) {
            return element.matches(selector);
        });
    }

    /** The first element below this one that matches the selector, or null. */
    querySelector(selector) {
        return this.querySelectorAll(selector)[0] || null;
    }
}

/**
 * Builds an empty fake document with a body, ready for elements to be added.
 * @returns {object} { document, body }
 */
function buildDocument() {
    const body = new FakeElement({ label: 'body' });
    const document = new FakeElement({ label: 'document' });

    document.add(body);
    document.getElementById = function (id) {
        return document.descendants().find(function (element) {
            return element.id === id;
        }) || null;
    };

    return { document: document, body: body };
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
    const { document, body } = buildDocument();
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
 * Creates a fake Bootstrap that records which toasts were shown and which modals were
 * shown or hidden. Hiding does not fire hidden.bs.modal by itself: in a real browser that
 * happens after the closing animation, so tests dispatch it when they want it.
 * @param {object} [options]
 * @param {function} [options.onModalShow] - Called as each modal is shown.
 * @returns {object} { bootstrap, shownToasts, shownModals, hiddenModals }
 */
function createFakeBootstrap(options) {
    const settings = options || {};
    const shownToasts = [];
    const shownModals = [];
    const hiddenModals = [];
    const bootstrap = {
        Toast: {
            getOrCreateInstance: function (element) {
                return {
                    show: function () {
                        shownToasts.push(element);
                    }
                };
            }
        },
        Modal: {
            getOrCreateInstance: function (element) {
                return {
                    show: function () {
                        if (settings.onModalShow) {
                            settings.onModalShow(element);
                        }

                        shownModals.push(element);
                    },
                    hide: function () {
                        hiddenModals.push(element);
                    }
                };
            }
        }
    };

    return { bootstrap: bootstrap, shownToasts: shownToasts, shownModals: shownModals, hiddenModals: hiddenModals };
}

/**
 * Creates a fake localStorage. Reuse the same one across page loads to act like a
 * returning visit in the same browser.
 * @param {object} [options]
 * @param {boolean} [options.failRead] - getItem throws, as when storage is blocked.
 * @param {boolean} [options.failWrite] - setItem throws, as when storage is full.
 * @returns {object} { storage, entries } where entries is a Map of what was saved.
 */
function createFakeStorage(options) {
    const settings = options || {};
    const entries = new Map();

    function storageError(name) {
        const error = new Error('storage is not available');
        error.name = name;

        return error;
    }

    const storage = {
        getItem: function (key) {
            if (settings.failRead) {
                throw storageError('SecurityError');
            }

            return entries.has(key)
                ? entries.get(key)
                : null;
        },
        setItem: function (key, value) {
            if (settings.failWrite) {
                throw storageError('QuotaExceededError');
            }

            entries.set(key, String(value));
        }
    };

    return { storage: storage, entries: entries };
}

/**
 * Creates a fake console that keeps every message by level.
 * @returns {object} { console, messages } — messages.debug, messages.warn, messages.error
 */
function createFakeConsole() {
    const messages = { debug: [], warn: [], error: [] };
    const fakeConsole = {};

    Object.keys(messages).forEach(function (level) {
        fakeConsole[level] = function (message) {
            messages[level].push(message);
        };
    });

    return { console: fakeConsole, messages: messages };
}

/**
 * Loads scripts from BistroGo.Web/wwwroot/js into one fresh sandbox, in order, then fires
 * DOMContentLoaded the way a browser does once the page has loaded.
 * @param {string[]} scriptNames - File names, e.g. ["walkthrough.js", "first-visit.js"].
 * @param {object} globals - What the sandbox can see: document, introJs, bootstrap, console...
 */
function loadScripts(scriptNames, globals) {
    const sandbox = vm.createContext(Object.assign({}, globals));

    scriptNames.forEach(function (scriptName) {
        const scriptPath = path.join(repoRoot, 'BistroGo.Web', 'wwwroot', 'js', scriptName);
        const code = fs.readFileSync(scriptPath, 'utf8');

        vm.runInContext(code, sandbox, { filename: scriptPath });
    });
    globals.document.dispatch('DOMContentLoaded');
}

/** Loads a single script; see loadScripts. */
function loadScript(scriptName, globals) {
    loadScripts([scriptName], globals);
}

/** Reads any repo file as text, by path from the repo root. */
function readRepoFile(relativePath) {
    return fs.readFileSync(path.join(repoRoot, relativePath), 'utf8');
}

module.exports = {
    FakeElement,
    buildDocument,
    buildCookDashboard,
    createFakeIntroJs,
    createFakeBootstrap,
    createFakeStorage,
    createFakeConsole,
    loadScript,
    loadScripts,
    readRepoFile
};
