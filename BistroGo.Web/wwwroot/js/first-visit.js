// ════════════════════════════════════════════════════════════════════════════════
// First-visit walkthrough prompt (OC-11)
// ════════════════════════════════════════════════════════════════════════════════
//
// Offers a guided tour to someone using BistroGo for the first time, so they don't
// have to find the Walkthrough button themselves.
//
// How it works:
//   - A page opts in by including a Bootstrap modal marked with data-first-visit-prompt
//     (the customer prompt lives in _Layout.cshtml, the kitchen prompt on the Cook Dashboard).
//   - The modal's data attributes say how to remember the visit and what to start:
//       data-storage-key   the browser storage entry that records the visit
//       data-user-id       (optional) added to the entry's name so each signed-in user is
//                          remembered separately on a shared device
//       data-start-button  the id of the existing walkthrough button to click on accept
//   - The first time the entry is missing, the visit is recorded and the prompt is shown.
//     Every later visit finds the entry and shows nothing.
//   - Accepting clicks the existing walkthrough button, so the current tours are used as
//     they are: this script never builds or changes a tour.
//   - Declining just closes the prompt. Staff can still start a tour from its usual button.
//
// Everything is kept in the browser (localStorage). There is no account and no database.
//
// Logging: messages go to the browser console, prefixed "[BistroGo:first-visit]".
//   debug = a routine decision, warn = something degraded but harmless, error = a real
//   fault. Messages never include user ids, storage keys or stored values.
// ════════════════════════════════════════════════════════════════════════════════

(function () {
    'use strict';

    const LOG_PREFIX = '[BistroGo:first-visit] ';

    // What browser storage says about a prompt.
    const VISIT_FIRST = 'first';          // no entry yet: show the prompt
    const VISIT_RETURNING = 'returning';  // entry found: show nothing
    const VISIT_UNKNOWN = 'unknown';      // storage can't be used: show nothing

    // walkthrough.js starts the customer tour on its own when the address contains
    // walkthrough=true (it is how the sidebar button reaches the Menu page), and then
    // removes that text from the address as soon as the page loads. So this is read now,
    // while the script loads, before it can disappear.
    const tourStartedFromLink = location.search.includes('walkthrough=true');

    // ────────────────────────────────────────────────────────────────────────────
    // Logging
    // ────────────────────────────────────────────────────────────────────────────

    /**
     * Writes one message to the browser console.
     * @param {string} level - "debug", "warn" or "error".
     * @param {string} message - What happened. Must not contain personal data.
     */
    function log(level, message) {
        console[level](LOG_PREFIX + message);
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Remembering the visit (browser storage)
    // ────────────────────────────────────────────────────────────────────────────

    /**
     * Works out the storage entry name for a prompt: the data-storage-key, plus the user
     * id when the prompt has one, so each signed-in user is remembered separately.
     * @param {Element} promptElement - The prompt's modal.
     * @returns {string} The storage entry name.
     */
    function buildStorageKey(promptElement) {
        const baseKey = promptElement.dataset.storageKey;
        const userId = promptElement.dataset.userId;
        let storageKey = baseKey;

        if (userId) {
            storageKey = baseKey + '_' + userId;
        }

        return storageKey;
    }

    /**
     * Checks browser storage for an earlier visit.
     *
     * Reading storage can throw: some browsers block it (strict privacy settings, some
     * private windows), and then even touching localStorage raises an error. That is
     * caught and reported as VISIT_UNKNOWN rather than breaking the page.
     *
     * @param {string} storageKey - The storage entry name.
     * @returns {string} VISIT_FIRST, VISIT_RETURNING or VISIT_UNKNOWN.
     */
    function readVisitState(storageKey) {
        let visitState = VISIT_UNKNOWN;

        try {
            if (localStorage.getItem(storageKey) === null) {
                visitState = VISIT_FIRST;
            } else {
                visitState = VISIT_RETURNING;
            }
        } catch (error) {
            log('warn', 'Could not read browser storage (' + error.name + ').');
        }

        return visitState;
    }

    /**
     * Records that this browser has now visited, by saving the current time. The time
     * isn't used for anything yet; it just makes the entry meaningful if someone looks.
     *
     * Saving can throw for the same reasons as reading, and also when storage is full.
     *
     * @param {string} storageKey - The storage entry name.
     * @returns {boolean} True if the visit was saved. False means the prompt would come
     *   back on every page, so the caller must not show it.
     */
    function recordVisit(storageKey) {
        let recorded = false;

        try {
            localStorage.setItem(storageKey, new Date().toISOString());
            recorded = true;
        } catch (error) {
            log('warn', 'Could not save to browser storage (' + error.name + ').');
        }

        return recorded;
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Showing the prompt and acting on the answer
    // ────────────────────────────────────────────────────────────────────────────

    /**
     * Starts the tour by clicking the existing walkthrough button, exactly as if the user
     * had clicked it. This keeps each tour's own start point (the customer tour moves to
     * the Menu page first) and leaves the tours themselves untouched.
     *
     * @param {string} startButtonId - The id of the walkthrough button.
     * @param {string} promptName - The prompt's id, for the log message.
     */
    function startTour(startButtonId, promptName) {
        const startButton = document.getElementById(startButtonId);

        if (startButton) {
            log('debug', promptName + ': accepted, starting the walkthrough.');
            startButton.click();
        } else {
            log('error', promptName + ': the start button "' + startButtonId + '" was not found, so the walkthrough could not start.');
        }
    }

    /**
     * Connects the prompt's buttons.
     *   - "Start the tour" (data-first-visit-accept) closes the prompt.
     *   - Once the prompt has finished closing, the tour starts if it was accepted.
     *     Waiting matters: the tour draws a dark overlay, and starting it while the
     *     prompt's own backdrop is still fading would layer the two awkwardly.
     *   - "No thanks" and the × close the prompt through Bootstrap's own data-bs-dismiss,
     *     so declining needs no code here beyond the log message.
     *
     * @param {Element} promptElement - The prompt's modal.
     * @param {string} promptName - The prompt's id, for log messages.
     */
    function connectButtons(promptElement, promptName) {
        const acceptButton = promptElement.querySelector('[data-first-visit-accept]');
        const startButtonId = promptElement.dataset.startButton;
        let accepted = false;

        if (acceptButton) {
            acceptButton.addEventListener('click', function () {
                accepted = true;
                bootstrap.Modal.getOrCreateInstance(promptElement).hide();
            });
        } else {
            log('error', promptName + ': the prompt has no accept button (data-first-visit-accept).');
        }

        promptElement.addEventListener('hidden.bs.modal', function () {
            if (accepted) {
                accepted = false;   // so closing the prompt again later can't start the tour twice
                startTour(startButtonId, promptName);
            } else {
                log('debug', promptName + ': declined or dismissed.');
            }
        });
    }

    /**
     * Shows the prompt for a first visit. The visit is recorded BEFORE the prompt appears,
     * so someone who ignores it and navigates away is not asked again on the next page.
     *
     * The prompt is skipped (and the reason logged) when:
     *   - Bootstrap isn't loaded, because the prompt can't be displayed. The visit is not
     *     recorded in this case, so the prompt still appears once the problem is fixed.
     *   - The tour is already starting from a link, because a prompt on top of it would get
     *     in the way. The visit is still recorded: they clearly know about the tour.
     *   - The visit can't be saved, because the prompt would reappear on every page.
     *
     * @param {Element} promptElement - The prompt's modal.
     * @param {string} promptName - The prompt's id, for log messages.
     * @param {string} storageKey - The storage entry name.
     */
    function showFirstVisitPrompt(promptElement, promptName, storageKey) {
        if (typeof bootstrap === 'undefined') {
            log('error', promptName + ': Bootstrap is not loaded, so the prompt cannot be shown.');
        } else if (tourStartedFromLink) {
            recordVisit(storageKey);
            log('debug', promptName + ': a tour is already starting from a link, so the prompt is skipped.');
        } else if (recordVisit(storageKey)) {
            connectButtons(promptElement, promptName);
            bootstrap.Modal.getOrCreateInstance(promptElement).show();
            log('debug', promptName + ': first visit recorded, prompt shown.');
        } else {
            log('warn', promptName + ': the visit could not be recorded, so the prompt is skipped.');
        }
    }

    /**
     * Decides what to do for one prompt on the page: show it on a first visit, otherwise
     * do nothing.
     * @param {Element} promptElement - A modal marked with data-first-visit-prompt.
     */
    function setUpPrompt(promptElement) {
        const promptName = promptElement.id || 'unnamed prompt';

        if (promptElement.dataset.storageKey) {
            const storageKey = buildStorageKey(promptElement);
            const visitState = readVisitState(storageKey);

            if (visitState === VISIT_FIRST) {
                showFirstVisitPrompt(promptElement, promptName, storageKey);
            } else if (visitState === VISIT_RETURNING) {
                log('debug', promptName + ': returning visit, prompt not shown.');
            } else {
                log('debug', promptName + ': browser storage is unavailable, so the prompt is skipped rather than shown on every page.');
            }
        } else {
            log('error', promptName + ': the prompt has no data-storage-key, so it was skipped.');
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // Starting up
    // ────────────────────────────────────────────────────────────────────────────

    // Once the page has loaded, look for prompts and set each one up. Each prompt gets its
    // own try/catch: this feature is a convenience, so a fault in one prompt must never
    // stop the other prompts or any other script on the page from working.
    document.addEventListener('DOMContentLoaded', function () {
        const prompts = document.querySelectorAll('[data-first-visit-prompt]');

        prompts.forEach(function (promptElement) {
            try {
                setUpPrompt(promptElement);
            } catch (error) {
                log('error', 'Could not set up a first-visit prompt (' + error.name + ': ' + error.message + ').');
            }
        });
    });
})();
