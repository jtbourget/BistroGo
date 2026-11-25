// ════════════════════════════════════════════════════════════════════════════════
// Tests for the first-visit walkthrough prompt (OC-11)
// ════════════════════════════════════════════════════════════════════════════════
//
// Runs the real BistroGo.Web/wwwroot/js/first-visit.js against a fake page, browser
// storage and Bootstrap (see fakePage.js) and checks it against the story's acceptance
// criteria. A "page load" is one call to loadPage(); passing the same storage to a second
// call acts like coming back later in the same browser.
//
// Run from the repo root (Node 18 or later, no npm packages needed):
//     node --test "BistroGo.Tests/JavaScript/*.test.js"
// ════════════════════════════════════════════════════════════════════════════════

'use strict';

const { describe, it } = require('node:test');
const assert = require('node:assert/strict');
const {
    FakeElement,
    buildDocument,
    createFakeIntroJs,
    createFakeBootstrap,
    createFakeStorage,
    createFakeConsole,
    loadScripts,
    readRepoFile
} = require('./fakePage');

const CUSTOMER_KEY = 'bistroGo_hasVisited';
const KITCHEN_KEY = 'bistroGo_kitchenTourPrompted';

/** The customer prompt as _Layout.cshtml declares it. */
function customerPrompt(overrides) {
    return Object.assign({
        id: 'walkthroughPromptModal',
        storageKey: CUSTOMER_KEY,
        startButton: 'startWalkthroughBtn'
    }, overrides);
}

/** The kitchen prompt as Orders/Index.cshtml declares it. */
function kitchenPrompt(userId, overrides) {
    return Object.assign({
        id: 'kitchenWalkthroughPromptModal',
        storageKey: KITCHEN_KEY,
        userId: userId,
        startButton: 'startKitchenWalkthroughBtn'
    }, overrides);
}

/**
 * Loads a fake page that holds the given prompts, runs first-visit.js, and returns
 * everything a test might inspect.
 * @param {object} [options]
 * @param {object[]} [options.prompts] - Prompts on the page. Default: the customer prompt.
 * @param {object} [options.storage] - From createFakeStorage; new and empty by default.
 * @param {string} [options.search] - The address's query string.
 * @param {boolean} [options.withBootstrap=true]
 * @param {boolean} [options.withStartButtons=true]
 */
function loadPage(options) {
    const settings = options || {};
    const prompts = settings.prompts || [customerPrompt()];
    const storage = settings.storage || createFakeStorage();
    const { document, body } = buildDocument();
    const log = createFakeConsole();
    const storedWhenShown = [];
    const boot = createFakeBootstrap({
        onModalShow: function () {
            storedWhenShown.push(Array.from(storage.entries.keys()));
        }
    });
    const startClicks = {};

    const built = prompts.map(function (prompt) {
        const dataset = { firstVisitPrompt: '' };

        if (prompt.storageKey !== undefined) {
            dataset.storageKey = prompt.storageKey;
        }

        if (prompt.userId !== undefined) {
            dataset.userId = prompt.userId;
        }

        dataset.startButton = prompt.startButton;

        const modal = body.add(new FakeElement({ id: prompt.id, dataset: dataset }));
        let accept = null;

        if (prompt.withAccept !== false) {
            accept = modal.add(new FakeElement({ dataset: { firstVisitAccept: '' } }));
        }

        return { modal: modal, accept: accept };
    });

    const startButtons = {};

    if (settings.withStartButtons !== false) {
        prompts.forEach(function (prompt) {
            const button = body.add(new FakeElement({ id: prompt.startButton }));

            startClicks[prompt.startButton] = 0;
            button.addEventListener('click', function () {
                startClicks[prompt.startButton] += 1;
            });
            startButtons[prompt.startButton] = button;
        });
    }

    const globals = {
        document: document,
        location: { search: settings.search || '' },
        localStorage: storage.storage,
        console: log.console
    };

    if (settings.withBootstrap !== false) {
        globals.bootstrap = boot.bootstrap;
    }

    loadScripts(['first-visit.js'], globals);

    return {
        storage: storage,
        messages: log.messages,
        shown: boot.shownModals,
        hidden: boot.hiddenModals,
        storedWhenShown: storedWhenShown,
        modal: built[0] ? built[0].modal : null,
        accept: built[0] ? built[0].accept : null,
        prompts: built,
        startClicks: startClicks,
        startButtons: startButtons
    };
}

/** Accept: click "Start the tour", then let the modal finish closing. */
function acceptPrompt(page) {
    page.accept.click();
    page.modal.dispatch('hidden.bs.modal');
}

/** Decline: "No thanks" or × close the modal without clicking accept. */
function declinePrompt(page) {
    page.modal.dispatch('hidden.bs.modal');
}

/** Every logged message, whatever its level. */
function allMessages(page) {
    return [...page.messages.debug, ...page.messages.warn, ...page.messages.error];
}

describe('First visit and returning visit', function () {
    it('shows the prompt when no earlier visit is recorded', function () {
        const page = loadPage();

        assert.equal(page.shown.length, 1);
        assert.equal(page.shown[0], page.modal);
    });

    it('records the visit in browser storage', function () {
        const page = loadPage();

        assert.ok(page.storage.entries.has(CUSTOMER_KEY));
        assert.ok(!Number.isNaN(Date.parse(page.storage.entries.get(CUSTOMER_KEY))));
    });

    it('records the visit before the prompt appears', function () {
        const page = loadPage();

        assert.deepEqual(page.storedWhenShown, [[CUSTOMER_KEY]]);
    });

    it('does not show the prompt on a later visit', function () {
        const storage = createFakeStorage();

        loadPage({ storage: storage });
        const second = loadPage({ storage: storage });

        assert.equal(second.shown.length, 0);
        assert.match(second.messages.debug.join(' '), /returning visit/);
    });

    it('stays hidden on later visits even if the first prompt was never answered', function () {
        const storage = createFakeStorage();

        loadPage({ storage: storage });   // the user navigates away without choosing
        const second = loadPage({ storage: storage });

        assert.equal(second.shown.length, 0);
    });

    it('does nothing on a page without a prompt', function () {
        const page = loadPage({ prompts: [] });

        assert.equal(page.shown.length, 0);
        assert.equal(page.storage.entries.size, 0);
        assert.deepEqual(allMessages(page), []);
    });
});

describe('Accepting and declining', function () {
    it('never starts the walkthrough on its own', function () {
        const page = loadPage();

        assert.equal(page.startClicks.startWalkthroughBtn, 0);
    });

    it('closes the prompt when accepted', function () {
        const page = loadPage();

        page.accept.click();

        assert.deepEqual(page.hidden, [page.modal]);
    });

    it('waits for the prompt to finish closing before starting the walkthrough', function () {
        const page = loadPage();

        page.accept.click();
        assert.equal(page.startClicks.startWalkthroughBtn, 0);

        page.modal.dispatch('hidden.bs.modal');
        assert.equal(page.startClicks.startWalkthroughBtn, 1);
    });

    it('starts the walkthrough only once, even if the prompt closes again', function () {
        const page = loadPage();

        acceptPrompt(page);
        page.modal.dispatch('hidden.bs.modal');

        assert.equal(page.startClicks.startWalkthroughBtn, 1);
    });

    it('does not start the walkthrough when declined', function () {
        const page = loadPage();

        declinePrompt(page);

        assert.equal(page.startClicks.startWalkthroughBtn, 0);
        assert.match(page.messages.debug.join(' '), /declined or dismissed/);
    });

    it('leaves the Walkthrough button working after declining', function () {
        const page = loadPage();

        declinePrompt(page);
        page.startButtons.startWalkthroughBtn.click();

        assert.equal(page.startClicks.startWalkthroughBtn, 1);
    });

    it('logs an error if the walkthrough button cannot be found', function () {
        const page = loadPage({ withStartButtons: false });

        assert.doesNotThrow(function () {
            acceptPrompt(page);
        });
        assert.match(page.messages.error.join(' '), /startWalkthroughBtn.*was not found/);
    });
});

describe('Kitchen prompt: remembered per cook on each device', function () {
    it('remembers the cook by adding their id to the storage entry', function () {
        const page = loadPage({ prompts: [kitchenPrompt('cook-1')] });

        assert.deepEqual(Array.from(page.storage.entries.keys()), [KITCHEN_KEY + '_cook-1']);
    });

    it('offers the tour to a second cook on the same device', function () {
        const storage = createFakeStorage();

        loadPage({ storage: storage, prompts: [kitchenPrompt('cook-1')] });
        const second = loadPage({ storage: storage, prompts: [kitchenPrompt('cook-2')] });

        assert.equal(second.shown.length, 1);
    });

    it('does not ask the same cook twice', function () {
        const storage = createFakeStorage();

        loadPage({ storage: storage, prompts: [kitchenPrompt('cook-1')] });
        const again = loadPage({ storage: storage, prompts: [kitchenPrompt('cook-1')] });

        assert.equal(again.shown.length, 0);
    });

    it('falls back to one entry per browser when the user id is empty', function () {
        const page = loadPage({ prompts: [kitchenPrompt('')] });

        assert.deepEqual(Array.from(page.storage.entries.keys()), [KITCHEN_KEY]);
    });

    it('is independent of the customer prompt', function () {
        const storage = createFakeStorage();

        loadPage({ storage: storage });   // customer prompt already seen in this browser
        const kitchen = loadPage({ storage: storage, prompts: [kitchenPrompt('cook-1')] });

        assert.equal(kitchen.shown.length, 1);
    });

    it('starts the kitchen tour, not the customer tour, when accepted', function () {
        const page = loadPage({ prompts: [kitchenPrompt('cook-1')] });

        acceptPrompt(page);

        assert.equal(page.startClicks.startKitchenWalkthroughBtn, 1);
        assert.equal(page.startClicks.startWalkthroughBtn, undefined);
    });
});

describe('When the prompt must be skipped', function () {
    it('shows nothing and warns when storage cannot be read', function () {
        const page = loadPage({ storage: createFakeStorage({ failRead: true }) });

        assert.equal(page.shown.length, 0);
        assert.match(page.messages.warn.join(' '), /Could not read browser storage \(SecurityError\)/);
        assert.deepEqual(page.messages.error, []);
    });

    it('shows nothing and warns when the visit cannot be saved', function () {
        const page = loadPage({ storage: createFakeStorage({ failWrite: true }) });

        assert.equal(page.shown.length, 0);
        assert.match(page.messages.warn.join(' '), /QuotaExceededError/);
    });

    it('does not record the visit when Bootstrap is missing, so the prompt can appear later', function () {
        const page = loadPage({ withBootstrap: false });

        assert.equal(page.storage.entries.size, 0);
        assert.match(page.messages.error.join(' '), /Bootstrap is not loaded/);
    });

    it('skips the prompt but records the visit when a link is already starting the tour', function () {
        const page = loadPage({ search: '?walkthrough=true' });

        assert.equal(page.shown.length, 0);
        assert.ok(page.storage.entries.has(CUSTOMER_KEY));
    });

    it('skips a prompt that has no storage key, and says so', function () {
        const page = loadPage({ prompts: [customerPrompt({ storageKey: undefined })] });

        assert.equal(page.shown.length, 0);
        assert.equal(page.storage.entries.size, 0);
        assert.match(page.messages.error.join(' '), /no data-storage-key/);
    });

    it('still shows the prompt, but logs an error, if the accept button is missing', function () {
        const page = loadPage({ prompts: [customerPrompt({ withAccept: false })] });

        assert.equal(page.shown.length, 1);
        assert.match(page.messages.error.join(' '), /no accept button/);
    });

    it('keeps working when one prompt on the page is broken', function () {
        const { document, body } = buildDocument();
        const storage = createFakeStorage();
        const boot = createFakeBootstrap();
        const log = createFakeConsole();
        const broken = body.add(new FakeElement({ id: 'broken', dataset: { firstVisitPrompt: '' } }));
        const working = body.add(new FakeElement({
            id: 'working',
            dataset: { firstVisitPrompt: '', storageKey: CUSTOMER_KEY, startButton: 'startWalkthroughBtn' }
        }));

        // Reading this prompt's settings blows up, standing in for any unexpected fault.
        Object.defineProperty(broken.dataset, 'storageKey', {
            get: function () {
                throw new TypeError('broken prompt');
            }
        });

        assert.doesNotThrow(function () {
            loadScripts(['first-visit.js'], {
                document: document,
                location: { search: '' },
                localStorage: storage.storage,
                bootstrap: boot.bootstrap,
                console: log.console
            });
        });
        assert.deepEqual(boot.shownModals, [working]);
        assert.match(log.messages.error.join(' '), /Could not set up a first-visit prompt \(TypeError/);
    });
});

describe('Logging', function () {
    it('prefixes every message so it can be filtered in the console', function () {
        const page = loadPage();

        assert.ok(allMessages(page).length > 0);
        allMessages(page).forEach(function (message) {
            assert.ok(message.startsWith('[BistroGo:first-visit] '), message);
        });
    });

    it('never logs a user id or storage entry name', function () {
        const userId = 'secret-user-4821';
        const storage = createFakeStorage();
        const runs = [
            loadPage({ storage: storage, prompts: [kitchenPrompt(userId)] }),
            loadPage({ storage: storage, prompts: [kitchenPrompt(userId)] }),
            loadPage({ storage: createFakeStorage({ failRead: true }), prompts: [kitchenPrompt(userId)] }),
            loadPage({ storage: createFakeStorage({ failWrite: true }), prompts: [kitchenPrompt(userId)] })
        ];

        acceptPrompt(runs[0]);

        runs.forEach(function (page) {
            allMessages(page).forEach(function (message) {
                assert.ok(!message.includes(userId), 'logged the user id: ' + message);
                assert.ok(!message.includes(KITCHEN_KEY), 'logged the storage key: ' + message);
            });
        });
    });
});

describe('Working with the existing customer walkthrough', function () {
    /** Loads the real walkthrough.js and first-visit.js together, then accepts the prompt. */
    function acceptOn(pathname) {
        const { document, body } = buildDocument();
        const intro = createFakeIntroJs();
        const boot = createFakeBootstrap();
        const location = { pathname: pathname, search: '', href: pathname };

        body.add(new FakeElement({ id: 'startWalkthroughBtn' }));
        body.add(new FakeElement({ selectors: ['#category'] }));
        body.add(new FakeElement({ selectors: ['.btn-add-to-order'] }));
        body.add(new FakeElement({ selectors: ['a[data-bs-target="#currentOrderModal"]'] }));

        const modal = body.add(new FakeElement({
            id: 'walkthroughPromptModal',
            dataset: { firstVisitPrompt: '', storageKey: CUSTOMER_KEY, startButton: 'startWalkthroughBtn' }
        }));
        const accept = modal.add(new FakeElement({ dataset: { firstVisitAccept: '' } }));

        loadScripts(['walkthrough.js', 'first-visit.js'], {
            document: document,
            window: { location: location, history: { replaceState: function () { } } },
            location: location,
            localStorage: createFakeStorage().storage,
            bootstrap: boot.bootstrap,
            introJs: intro.introJs,
            console: createFakeConsole().console
        });
        accept.click();
        modal.dispatch('hidden.bs.modal');

        return { location: location, tours: intro.tours };
    }

    it('goes to the Menu page, where the walkthrough starts, when accepted elsewhere', function () {
        const result = acceptOn('/');

        assert.equal(result.location.href, '/Menu?walkthrough=true');
        assert.equal(result.tours.length, 0);
    });

    it('starts the existing walkthrough from its first step when accepted on the Menu page', function () {
        const result = acceptOn('/Menu');

        assert.equal(result.tours.length, 1);
        assert.equal(result.tours[0].started, true);
        assert.equal(result.tours[0].options.steps.length, 3);
        assert.match(result.tours[0].options.steps[0].intro, /^Welcome to BistroGo/);
    });
});

describe('The pages declare the prompts correctly', function () {
    const layout = readRepoFile('BistroGo.Web/Pages/Shared/_Layout.cshtml');
    const orders = readRepoFile('BistroGo.Web/Pages/Orders/Index.cshtml');

    it('declares the customer prompt, wired to the sidebar Walkthrough button', function () {
        assert.match(layout, /data-first-visit-prompt/);
        assert.ok(layout.includes('data-storage-key="' + CUSTOMER_KEY + '"'));
        assert.match(layout, /data-start-button="startWalkthroughBtn"/);
        assert.match(layout, /id="startWalkthroughBtn"/);
        assert.match(layout, /data-first-visit-accept/);
    });

    it('leaves the customer prompt out for kitchen staff', function () {
        assert.match(layout, /!User\.IsInRole\(Roles\.KitchenStaff\)\)\s*\{\s*<div class="modal fade" id="walkthroughPromptModal"/);
    });

    it('declares the kitchen prompt for kitchen staff, with their user id', function () {
        assert.match(orders, /@if \(User\.IsInRole\(Roles\.KitchenStaff\)\)\s*\{\s*<div class="modal fade" id="kitchenWalkthroughPromptModal"/);
        assert.ok(orders.includes('data-storage-key="' + KITCHEN_KEY + '"'));
        assert.match(orders, /data-user-id="@currentUserId"/);
        assert.match(orders, /data-start-button="startKitchenWalkthroughBtn"/);
        assert.match(orders, /data-first-visit-accept/);
    });

    it('offers clear accept and decline choices on both prompts', function () {
        [layout, orders].forEach(function (page) {
            assert.match(page, /data-bs-dismiss="modal">No thanks</);
            assert.match(page, /data-first-visit-accept>Start the tour</);
        });
    });

    it('loads first-visit.js after Bootstrap', function () {
        const bootstrapAt = layout.indexOf('bootstrap.bundle.min.js');
        const scriptAt = layout.indexOf('~/js/first-visit.js');

        assert.ok(bootstrapAt > -1);
        assert.ok(scriptAt > bootstrapAt);
    });
});
