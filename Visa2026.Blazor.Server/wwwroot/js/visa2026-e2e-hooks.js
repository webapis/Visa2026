// UI test hooks that must pierce DevExpress Blazor open shadow roots (view toolbar / ribbon items).
window.visa2026E2eHooks = window.visa2026E2eHooks || {};

(function () {
    function createActionConfig(actionName, testIdsByViewId, options) {
        options = options || {};
        const actionNames = [actionName].concat(options.aliases || []);
        const selectors = [];

        for (const name of actionNames) {
            selectors.push(
                'button[data-action-name="' + name + '"]',
                'button[data-dx-action-name="' + name + '"]',
                '[data-action-name="' + name + '"]',
                '[data-dx-action-name="' + name + '"]',
            );
        }

        return {
            actionName: actionName,
            testIdsByViewId: testIdsByViewId,
            allTestIds: Object.values(testIdsByViewId),
            selectors: selectors,
            applyToAllMatches: !!options.applyToAllMatches,
            fallbackTestId: null,
        };
    }

    const toolbarActionGroups = {
        personList: [
            createActionConfig('New', {
                Person_ListView_Employees: 'person-list-employees-new',
                Person_ListView_FamilyMembers: 'person-list-family-members-new',
                Person_ListView_TemporaryVisitors: 'person-list-temporary-visitors-new',
            }),
            createActionConfig('Delete', {
                Person_ListView_Employees: 'person-list-employees-delete',
                Person_ListView_FamilyMembers: 'person-list-family-members-delete',
                Person_ListView_TemporaryVisitors: 'person-list-temporary-visitors-delete',
            }),
        ],
        personDetail: [
            createActionConfig('Save', {
                Person_DetailView_Employee: 'person-detail-employee-save',
                Person_DetailView_FamilyMember: 'person-detail-family-member-save',
                Person_DetailView_TemporaryVisitor: 'person-detail-temporary-visitor-save',
            }),
            createActionConfig('SaveAndClose', {
                Person_DetailView_Employee: 'person-detail-employee-save-and-close',
                Person_DetailView_FamilyMember: 'person-detail-family-member-save-and-close',
                Person_DetailView_TemporaryVisitor: 'person-detail-temporary-visitor-save-and-close',
            }, {
                aliases: ['Save and Close'],
                applyToAllMatches: true,
            }),
            createActionConfig('SaveAndNew', {
                Person_DetailView_Employee: 'person-detail-employee-save-and-new',
                Person_DetailView_FamilyMember: 'person-detail-family-member-save-and-new',
                Person_DetailView_TemporaryVisitor: 'person-detail-temporary-visitor-save-and-new',
            }, {
                aliases: ['Save and New'],
                applyToAllMatches: true,
            }),
        ],
    };

    const actionConfigsByGroupAndName = {};
    for (const [groupName, configs] of Object.entries(toolbarActionGroups)) {
        actionConfigsByGroupAndName[groupName] = {};
        for (const config of configs) {
            actionConfigsByGroupAndName[groupName][config.actionName] = config;
        }
    }

    let toolbarObserver = null;
    let reapplyTimer = null;
    let historyHooked = false;

    function queryDeepAll(root, selector, results) {
        if (!root) {
            return;
        }

        if (root.querySelectorAll) {
            for (const match of root.querySelectorAll(selector)) {
                results.push(match);
            }
        }

        const elements = root.querySelectorAll ? root.querySelectorAll('*') : [];
        for (const element of elements) {
            if (!element.shadowRoot) {
                continue;
            }

            queryDeepAll(element.shadowRoot, selector, results);
        }
    }

    function dedupeElements(elements) {
        const seen = new Set();
        const unique = [];

        for (const element of elements) {
            if (seen.has(element)) {
                continue;
            }

            seen.add(element);
            unique.push(element);
        }

        return unique;
    }

    function findAllActionButtons(config) {
        const candidates = [];
        for (const selector of config.selectors) {
            queryDeepAll(document, selector, candidates);
        }

        return dedupeElements(candidates);
    }

    function isVisible(element) {
        if (!element) {
            return false;
        }

        if (element.getClientRects().length > 0) {
            return true;
        }

        return element.offsetParent !== null;
    }

    function findTargetActionButtons(config) {
        const candidates = findAllActionButtons(config);
        if (candidates.length === 0) {
            return [];
        }

        if (config.applyToAllMatches) {
            return candidates;
        }

        for (const button of candidates) {
            if (isVisible(button)) {
                return [button];
            }
        }

        return [candidates[0]];
    }

    function applyActionTestId(config, explicitTestId) {
        const resolvedTestId = resolveTestId(config, explicitTestId);
        if (!resolvedTestId) {
            return false;
        }

        stripAllActionHooks(config);

        const buttons = findTargetActionButtons(config);
        if (buttons.length === 0) {
            return false;
        }

        const e2eClass = 'e2e-' + resolvedTestId;
        for (const button of buttons) {
            button.setAttribute('data-testid', resolvedTestId);
            if (!button.classList.contains(e2eClass)) {
                button.classList.add(e2eClass);
            }
        }

        return buttons[0].getAttribute('data-testid') === resolvedTestId;
    }

    function getActiveTestId(config) {
        const path = window.location.pathname || '';
        for (const [viewId, testId] of Object.entries(config.testIdsByViewId)) {
            if (path.indexOf(viewId) >= 0) {
                return testId;
            }
        }

        return null;
    }

    function resolveTestId(config, explicitTestId) {
        return getActiveTestId(config) || explicitTestId || config.fallbackTestId;
    }

    function shouldApplyConfig(config) {
        return !!(config.fallbackTestId || getActiveTestId(config));
    }

    function stripActionHooks(button, config) {
        for (const testId of config.allTestIds) {
            if (button.getAttribute('data-testid') === testId) {
                button.removeAttribute('data-testid');
            }

            button.classList.remove('e2e-' + testId);
        }
    }

    function stripAllActionHooks(config) {
        for (const button of findAllActionButtons(config)) {
            stripActionHooks(button, config);
        }
    }

    function enumerateAllActionConfigs() {
        const configs = [];
        for (const group of Object.values(toolbarActionGroups)) {
            for (const config of group) {
                configs.push(config);
            }
        }

        return configs;
    }

    function hasAnyActiveFallback() {
        for (const config of enumerateAllActionConfigs()) {
            if (config.fallbackTestId) {
                return true;
            }
        }

        return false;
    }

    function reapplyAllToolbarHooks() {
        let allApplied = true;
        for (const config of enumerateAllActionConfigs()) {
            if (!shouldApplyConfig(config)) {
                continue;
            }

            if (!applyActionTestId(config, config.fallbackTestId)) {
                allApplied = false;
            }
        }

        return allApplied;
    }

    function scheduleReapply() {
        if (reapplyTimer) {
            clearTimeout(reapplyTimer);
        }

        reapplyTimer = setTimeout(function () {
            reapplyTimer = null;
            reapplyAllToolbarHooks();
        }, 50);
    }

    function hookHistoryForToolbarActions() {
        if (historyHooked) {
            return;
        }

        historyHooked = true;

        const originalPushState = history.pushState.bind(history);
        history.pushState = function () {
            originalPushState.apply(history, arguments);
            scheduleReapply();
        };

        const originalReplaceState = history.replaceState.bind(history);
        history.replaceState = function () {
            originalReplaceState.apply(history, arguments);
            scheduleReapply();
        };

        window.addEventListener('popstate', scheduleReapply);
    }

    function startToolbarWatch() {
        if (toolbarObserver) {
            return;
        }

        toolbarObserver = new MutationObserver(function () {
            scheduleReapply();
        });

        toolbarObserver.observe(document.body, {
            childList: true,
            subtree: true,
            attributes: true,
            attributeFilter: ['class', 'data-action-name', 'data-dx-action-name', 'data-testid'],
        });
    }

    function disconnectToolbarWatchIfIdle() {
        if (hasAnyActiveFallback()) {
            return;
        }

        if (toolbarObserver) {
            toolbarObserver.disconnect();
            toolbarObserver = null;
        }

        if (reapplyTimer) {
            clearTimeout(reapplyTimer);
            reapplyTimer = null;
        }
    }

    function ensureGroupActionTestId(groupName, actionName, testId) {
        const config = actionConfigsByGroupAndName[groupName]?.[actionName];
        if (!config || !testId) {
            return false;
        }

        config.fallbackTestId = testId;
        hookHistoryForToolbarActions();
        startToolbarWatch();

        if (applyActionTestId(config, testId)) {
            return true;
        }

        scheduleReapply();
        return false;
    }

    function stopToolbarWatchGroup(groupName) {
        const configs = toolbarActionGroups[groupName];
        if (!configs) {
            return;
        }

        for (const config of configs) {
            config.fallbackTestId = null;
        }

        disconnectToolbarWatchIfIdle();
    }

    window.visa2026E2eHooks.applyNewActionTestId = function (testId) {
        return applyActionTestId(actionConfigsByGroupAndName.personList.New, testId);
    };

    window.visa2026E2eHooks.ensureNewActionTestId = function (testId) {
        return ensureGroupActionTestId('personList', 'New', testId);
    };

    window.visa2026E2eHooks.applyDeleteActionTestId = function (testId) {
        return applyActionTestId(actionConfigsByGroupAndName.personList.Delete, testId);
    };

    window.visa2026E2eHooks.ensureDeleteActionTestId = function (testId) {
        return ensureGroupActionTestId('personList', 'Delete', testId);
    };

    window.visa2026E2eHooks.applyPersonDetailSaveActionTestId = function (testId) {
        return applyActionTestId(actionConfigsByGroupAndName.personDetail.Save, testId);
    };

    window.visa2026E2eHooks.ensurePersonDetailSaveActionTestId = function (testId) {
        return ensureGroupActionTestId('personDetail', 'Save', testId);
    };

    window.visa2026E2eHooks.ensurePersonDetailActionTestId = function (actionName, testId) {
        return ensureGroupActionTestId('personDetail', actionName, testId);
    };

    window.visa2026E2eHooks.applyPersonDetailActionTestId = function (actionName, testId) {
        const config = actionConfigsByGroupAndName.personDetail?.[actionName];
        if (!config) {
            return false;
        }

        return applyActionTestId(config, testId);
    };

    window.visa2026E2eHooks.stopPersonListToolbarWatch = function () {
        stopToolbarWatchGroup('personList');
    };

    window.visa2026E2eHooks.stopPersonDetailToolbarWatch = function () {
        stopToolbarWatchGroup('personDetail');
    };

    window.visa2026E2eHooks.stopNewActionWatch = window.visa2026E2eHooks.stopPersonListToolbarWatch;
})();
