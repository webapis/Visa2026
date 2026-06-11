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
                'button[data-x-action-name="' + name + '"]',
                '[data-action-name="' + name + '"]',
                '[data-dx-action-name="' + name + '"]',
                '[data-x-action-name="' + name + '"]',
            );
        }

        if (options.extraSelectors) {
            for (const selector of options.extraSelectors) {
                selectors.push(selector);
            }
        }

        return {
            actionName: actionName,
            testIdsByViewId: testIdsByViewId,
            allTestIds: Object.values(testIdsByViewId),
            selectors: selectors,
            applyToAllMatches: !!options.applyToAllMatches,
            requirePathContains: options.requirePathContains || null,
            requireActiveTabTestId: options.requireActiveTabTestId || null,
            skipWhenPassportDetailActive: !!options.skipWhenPassportDetailActive,
            usePassportsNestedNewFinder: !!options.usePassportsNestedNewFinder,
            fallbackTestId: null,
        };
    }

    const toolbarActionGroups = {
        personList: [
            createActionConfig('New', {
                Person_ListView_Employees: 'person-list-employees-new',
                Person_ListView_FamilyMembers: 'person-list-family-members-new',
                Person_ListView_TemporaryVisitors: 'person-list-temporary-visitors-new',
            }, {
                aliases: ['Täze'],
            }),
            createActionConfig('Delete', {
                Person_ListView_Employees: 'person-list-employees-delete',
                Person_ListView_FamilyMembers: 'person-list-family-members-delete',
                Person_ListView_TemporaryVisitors: 'person-list-temporary-visitors-delete',
            }, {
                aliases: ['Poz'],
            }),
        ],
        passportDetail: [
            createActionConfig('Save', {
                Passport_DetailView: 'passport-detail-save',
            }, {
                aliases: ['Sakla'],
            }),
        ],
        personDetailNestedPassports: [
            createActionConfig('New', {}, {
                aliases: ['Täze'],
                requirePathContains: 'Person_DetailView',
                usePassportsNestedNewFinder: true,
            }),
        ],
        personDetail: [
            createActionConfig('Save', {
                Person_DetailView_Employee: 'person-detail-employee-save',
                Person_DetailView_FamilyMember: 'person-detail-family-member-save',
                Person_DetailView_TemporaryVisitor: 'person-detail-temporary-visitor-save',
            }, {
                aliases: ['Sakla'],
                requirePathContains: 'Person_DetailView',
                skipWhenPassportDetailActive: true,
            }),
            createActionConfig('SaveAndClose', {
                Person_DetailView_Employee: 'person-detail-employee-save-and-close',
                Person_DetailView_FamilyMember: 'person-detail-family-member-save-and-close',
                Person_DetailView_TemporaryVisitor: 'person-detail-temporary-visitor-save-and-close',
            }, {
                aliases: ['Save and Close'],
                applyToAllMatches: true,
                requirePathContains: 'Person_DetailView',
                skipWhenPassportDetailActive: true,
            }),
            createActionConfig('SaveAndNew', {
                Person_DetailView_Employee: 'person-detail-employee-save-and-new',
                Person_DetailView_FamilyMember: 'person-detail-family-member-save-and-new',
                Person_DetailView_TemporaryVisitor: 'person-detail-temporary-visitor-save-and-new',
            }, {
                aliases: ['Save and New'],
                applyToAllMatches: true,
                requirePathContains: 'Person_DetailView',
                skipWhenPassportDetailActive: true,
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
    let passportsNestedInterval = null;
    let passportsNestedApplyInFlight = false;

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

    function isPersonListNewHookButton(button) {
        const testId = button.getAttribute('data-testid') || '';
        return testId.indexOf('person-list-') === 0 && testId.lastIndexOf('-new') === testId.length - 4;
    }

    function isPassportsTabHeader(node) {
        return !!(node && node.getAttribute && node.getAttribute('role') === 'tab');
    }

    function isPassportsTabContentNode(node) {
        if (!node || isPassportsTabHeader(node)) {
            return false;
        }

        if (node.classList && node.classList.contains('e2e-person-employee-tab-passports-content')) {
            return true;
        }

        return !!(node.getAttribute
            && node.getAttribute('data-testid') === 'person-employee-tab-passports');
    }

    function isPassportsTabLabel(text) {
        const normalized = (text || '').trim().toLowerCase();
        return normalized.indexOf('passport') >= 0 || normalized.indexOf('pasport') >= 0;
    }

    function findElementByIdDeep(id) {
        if (!id) {
            return null;
        }

        const matches = [];
        queryDeepAll(document, '[id="' + id.replace(/\\/g, '\\\\').replace(/"/g, '\\"') + '"]', matches);
        for (const node of dedupeElements(matches)) {
            if (isVisible(node)) {
                return node;
            }
        }

        return null;
    }

    function getPassportsTabPanelByActiveHeader() {
        const tabHeaders = [];
        queryDeepAll(document, '.e2e-person-employee-tab-passports[role="tab"]', tabHeaders);
        queryDeepAll(document, '[role="tab"]', tabHeaders);
        for (const tab of dedupeElements(tabHeaders)) {
            if (tab.getAttribute('aria-selected') !== 'true') {
                continue;
            }

            const label = tab.innerText || tab.textContent || '';
            const hasPassportsHook = tab.classList
                && tab.classList.contains('e2e-person-employee-tab-passports');
            if (!hasPassportsHook && !isPassportsTabLabel(label)) {
                continue;
            }

            const panelId = tab.getAttribute('aria-controls');
            const panel = findElementByIdDeep(panelId);
            if (panel) {
                return panel;
            }
        }

        const tabPanels = [];
        queryDeepAll(document, '[role="tabpanel"]', tabPanels);
        for (const panel of dedupeElements(tabPanels)) {
            if (!isVisible(panel)) {
                continue;
            }

            const text = (panel.innerText || panel.textContent || '').toLowerCase();
            if (text.indexOf('passport number') >= 0
                || text.indexOf('pasaport numar') >= 0
                || text.indexOf('pasport belgisi') >= 0
                || text.indexOf('номер паспорта') >= 0) {
                return panel;
            }
        }

        return null;
    }

    function getVisiblePersonDetailPassportsTabRoot() {
        const matches = [];
        queryDeepAll(document, '.e2e-person-employee-tab-passports-list', matches);
        for (const node of dedupeElements(matches)) {
            if (isVisible(node)) {
                return node;
            }
        }

        queryDeepAll(document, '.e2e-person-employee-tab-passports-content', matches);
        for (const node of dedupeElements(matches)) {
            if (isVisible(node)) {
                return node;
            }
        }

        queryDeepAll(document, '[data-testid="person-employee-tab-passports"]', matches);
        for (const node of dedupeElements(matches)) {
            if (isPassportsTabHeader(node)) {
                continue;
            }

            if (isVisible(node)) {
                return node;
            }
        }

        return getPassportsTabPanelByActiveHeader();
    }

    function getPassportsNestedToolbarSearchRoots() {
        const roots = [];
        const seen = new Set();

        function addRoot(node) {
            if (!node || seen.has(node)) {
                return;
            }

            seen.add(node);
            roots.push(node);
        }

        addRoot(getPassportsTabPanelByActiveHeader());

        const matches = [];
        queryDeepAll(document, '.e2e-person-employee-tab-passports-content', matches);
        for (const node of dedupeElements(matches)) {
            if (isVisible(node)) {
                addRoot(node);
            }
        }

        queryDeepAll(document, '.e2e-person-employee-tab-passports-list', matches);
        for (const node of dedupeElements(matches)) {
            if (isVisible(node)) {
                addRoot(node);
            }
        }

        queryDeepAll(document, '.e2e-person-employee-tab-passports', matches);
        for (const node of dedupeElements(matches)) {
            if (isPassportsTabHeader(node) || !isVisible(node)) {
                continue;
            }

            addRoot(node);
        }

        queryDeepAll(document, '[data-testid="person-employee-tab-passports"]', matches);
        for (const node of dedupeElements(matches)) {
            if (isPassportsTabHeader(node) || !isVisible(node)) {
                continue;
            }

            addRoot(node);
        }

        addRoot(getVisiblePersonDetailPassportsTabRoot());

        if (roots.length === 0 && document.body) {
            addRoot(document.body);
        }

        return roots;
    }

    function isNodeInsideRoot(node, root) {
        if (!node || !root) {
            return false;
        }

        let current = node;
        for (let depth = 0; depth < 40 && current; depth++) {
            if (current === root) {
                return true;
            }

            current = current.parentElement || current.host;
        }

        return false;
    }

    function tabRootHasPassportsGridMarkers(tabRoot) {
        const text = (tabRoot.innerText || tabRoot.textContent || '').toLowerCase();
        return text.indexOf('passport number') >= 0
            || text.indexOf('pasaport numar') >= 0
            || text.indexOf('pasport belgisi') >= 0
            || text.indexOf('номер паспорта') >= 0
            || text.indexOf('passport type') >= 0
            || text.indexOf('issue date') >= 0;
    }

    function isPassportsNestedListContext(element) {
        const tabRoot = getVisiblePersonDetailPassportsTabRoot();
        if (!tabRoot || !isNodeInsideRoot(element, tabRoot)) {
            return false;
        }

        if (tabRootHasPassportsGridMarkers(tabRoot)) {
            return true;
        }

        return isPassportsTabContentNode(tabRoot);
    }

    function isPassportsNestedNewCandidate(element) {
        const testId = element.getAttribute('data-testid') || '';
        return testId.indexOf('person-list-') !== 0 && testId.indexOf('person-detail-') !== 0;
    }

    function getPassportsNewActionId(element) {
        if (!element || !element.getAttribute) {
            return '';
        }

        return element.getAttribute('data-action-name')
            || element.getAttribute('data-x-action-name')
            || element.getAttribute('data-dx-action-name')
            || element.getAttribute('data-dx-toolbar-item-id')
            || '';
    }

    function looksLikePassportsNewControl(element) {
        if (getPassportsNewActionId(element) === 'New') {
            return true;
        }

        const label = (element.innerText || element.textContent || '').trim();
        if (/^\+?\s*(new|täze)\s*$/i.test(label) || label === '+') {
            return true;
        }

        return !!(element.classList && element.classList.contains('dxbl-toolbar-adaptive-item-new'));
    }

    function isPassportsToolbarItemElement(element) {
        const tagName = (element.tagName || '').toLowerCase();
        return tagName === 'dxbl-toolbar-item'
            || !!(element.classList && element.classList.contains('dxbl-toolbar-item'));
    }

    function elementHasPassportsNewAction(element) {
        return getPassportsNewActionId(element) === 'New';
    }

    function isPassportsTabContextReady(tabRoot) {
        if (!tabRoot) {
            return false;
        }

        return isPassportsCollectionTabActive() || tabRootHasPassportsGridMarkers(tabRoot);
    }

    function findPassportsNestedListNewButtonsInRoot(tabRoot) {
        const toolbarItems = [];
        const toolbarItemSelectors = [
            '[data-dx-toolbar-item-id="New"]',
            '.dxbl-adaptive-group[data-dx-toolbar-item-id="New"]',
            'dxbl-toolbar-item[data-dx-toolbar-item-id="New"]',
            'dxbl-toolbar-item[data-action-name="New"]',
            'dxbl-toolbar-item[data-x-action-name="New"]',
            '.dxbl-toolbar-item[data-dx-toolbar-item-id="New"]',
            '.dxbl-toolbar-item[data-action-name="New"]',
            '.dxbl-toolbar-item[data-x-action-name="New"]',
            '.dxbl-toolbar-adaptive-item-new',
        ];
        for (const selector of toolbarItemSelectors) {
            queryDeepAll(tabRoot, selector, toolbarItems);
        }

        let candidates = dedupeElements(toolbarItems).filter(function (item) {
            return isVisible(item) && isPassportsNestedNewCandidate(item);
        });
        if (candidates.length > 0) {
            return [candidates[0]];
        }

        const actionButtons = [];
        queryDeepAll(tabRoot, 'button[data-dx-toolbar-item-id="New"]', actionButtons);
        queryDeepAll(tabRoot, 'button[data-action-name="New"]', actionButtons);
        queryDeepAll(tabRoot, 'button[data-x-action-name="New"]', actionButtons);
        candidates = dedupeElements(actionButtons).filter(function (button) {
            return isVisible(button) && isPassportsNestedNewCandidate(button);
        });
        if (candidates.length > 0) {
            return [candidates[0]];
        }

        const adaptiveNew = [];
        queryDeepAll(tabRoot, '.dxbl-toolbar-adaptive-item-new', adaptiveNew);
        candidates = dedupeElements(adaptiveNew).filter(function (group) {
            return isVisible(group) && isPassportsNestedNewCandidate(group);
        });
        if (candidates.length > 0) {
            return [candidates[0]];
        }

        const adaptiveGroups = [];
        queryDeepAll(tabRoot, '.dxbl-adaptive-group', adaptiveGroups);
        for (const group of dedupeElements(adaptiveGroups)) {
            if (!isVisible(group) || !isPassportsNestedNewCandidate(group)) {
                continue;
            }

            if (getPassportsNewActionId(group) === 'New') {
                return [group];
            }

            const nestedNew = [];
            queryDeepAll(group, '[data-dx-toolbar-item-id="New"]', nestedNew);
            queryDeepAll(group, 'dxbl-toolbar-item[data-action-name="New"]', nestedNew);
            queryDeepAll(group, '.dxbl-toolbar-item[data-action-name="New"]', nestedNew);
            queryDeepAll(group, 'button[data-action-name="New"]', nestedNew);
            if (nestedNew.some(isVisible)) {
                return [group];
            }

            if (looksLikePassportsNewControl(group)) {
                return [group];
            }
        }

        const toolbarGroups = [];
        queryDeepAll(tabRoot, '.dxbl-toolbar-group, .dxbl-btn-group.dxbl-toolbar-group', toolbarGroups);
        for (const group of dedupeElements(toolbarGroups)) {
            if (!isVisible(group) || !isPassportsNestedNewCandidate(group)) {
                continue;
            }

            const nestedButtons = [];
            queryDeepAll(group, 'button', nestedButtons);
            for (const button of dedupeElements(nestedButtons)) {
                if (!isVisible(button) || !isPassportsNestedNewCandidate(button)) {
                    continue;
                }

                if (getPassportsNewActionId(button) === 'New' || looksLikePassportsNewControl(button)) {
                    return [button];
                }
            }

            if (looksLikePassportsNewControl(group)) {
                return [group];
            }
        }

        return [];
    }

    function findPassportsNestedListNewButtons() {
        const path = window.location.pathname || '';
        if (path.indexOf('Person_DetailView') < 0) {
            return [];
        }

        const searchRoots = getPassportsNestedToolbarSearchRoots();
        if (searchRoots.length === 0) {
            return [];
        }

        const tabReady = isPassportsCollectionTabActive()
            || searchRoots.some(function (root) {
                return isPassportsTabContextReady(root);
            });
        if (!tabReady) {
            return [];
        }

        for (const tabRoot of searchRoots) {
            const found = findPassportsNestedListNewButtonsInRoot(tabRoot);
            if (found.length > 0) {
                return found;
            }
        }

        if (isPassportsCollectionTabActive() && document.body) {
            const bodyButtons = [];
            queryDeepAll(document.body, '[data-dx-toolbar-item-id="New"]', bodyButtons);
            queryDeepAll(document.body, 'button[data-action-name="New"]', bodyButtons);
            queryDeepAll(document.body, 'button[data-x-action-name="New"]', bodyButtons);
            queryDeepAll(document.body, '.dxbl-toolbar-adaptive-item-new', bodyButtons);
            for (const button of dedupeElements(bodyButtons)) {
                if (!isVisible(button) || !isPassportsNestedNewCandidate(button)) {
                    continue;
                }

                if (isPersonListNewHookButton(button)) {
                    continue;
                }

                const testId = button.getAttribute('data-testid') || '';
                if (testId.indexOf('person-detail-') === 0) {
                    continue;
                }

                return [button];
            }
        }

        return [];
    }

    function setPassportsHookAttributes(element, testId) {
        const e2eClass = 'e2e-' + testId;
        if (element.getAttribute('data-testid') !== testId) {
            element.setAttribute('data-testid', testId);
        }

        if (element.classList && !element.classList.contains(e2eClass)) {
            element.classList.add(e2eClass);
        }
    }

    function tagPassportsNestedNewButton(target, testId) {
        const tagged = new Set();

        function tagOnce(element) {
            if (!element || tagged.has(element)) {
                return;
            }

            tagged.add(element);
            setPassportsHookAttributes(element, testId);
        }

        tagOnce(target);

        let current = target;
        for (let depth = 0; depth < 12 && current; depth++) {
            const tagName = (current.tagName || '').toLowerCase();
            if (current.classList
                && (current.classList.contains('dxbl-adaptive-group')
                    || current.classList.contains('dxbl-toolbar-adaptive-item-new'))) {
                tagOnce(current);
            }

            if (isPassportsToolbarItemElement(current) || elementHasPassportsNewAction(current)) {
                tagOnce(current);
            }

            current = current.parentElement || current.host;
        }

        const descendants = [];
        queryDeepAll(target, '[data-dx-toolbar-item-id="New"]', descendants);
        queryDeepAll(target, 'dxbl-toolbar-item[data-action-name="New"]', descendants);
        queryDeepAll(target, 'dxbl-toolbar-item[data-x-action-name="New"]', descendants);
        queryDeepAll(target, '.dxbl-toolbar-item[data-dx-toolbar-item-id="New"]', descendants);
        queryDeepAll(target, '.dxbl-toolbar-item[data-action-name="New"]', descendants);
        queryDeepAll(target, '.dxbl-toolbar-item[data-x-action-name="New"]', descendants);
        queryDeepAll(target, 'button[data-dx-toolbar-item-id="New"]', descendants);
        queryDeepAll(target, 'button[data-action-name="New"]', descendants);
        queryDeepAll(target, 'button[data-x-action-name="New"]', descendants);
        for (const descendant of dedupeElements(descendants)) {
            if (elementHasPassportsNewAction(descendant)) {
                tagOnce(descendant);
            }
        }
    }

    function isPassportsNewHookTarget(element) {
        if (!element || !isVisible(element)) {
            return false;
        }

        if (elementHasPassportsNewAction(element)) {
            return true;
        }

        if (isPassportsToolbarItemElement(element) && elementHasPassportsNewAction(element)) {
            return true;
        }

        if (element.classList
            && (element.classList.contains('dxbl-toolbar-adaptive-item-new')
                || element.classList.contains('dxbl-adaptive-group'))) {
            if (getPassportsNewActionId(element) === 'New') {
                return true;
            }

            const nestedNew = [];
            queryDeepAll(element, '[data-dx-toolbar-item-id="New"]', nestedNew);
            queryDeepAll(element, 'dxbl-toolbar-item[data-action-name="New"]', nestedNew);
            queryDeepAll(element, '.dxbl-toolbar-item[data-action-name="New"]', nestedNew);
            queryDeepAll(element, 'button[data-action-name="New"]', nestedNew);
            if (nestedNew.some(isVisible)) {
                return true;
            }

            return looksLikePassportsNewControl(element);
        }

        const tagName = (element.tagName || '').toLowerCase();
        return tagName === 'button' || element.getAttribute?.('role') === 'button';
    }

    function findPassportsNestedNewHookedElement(testId) {
        for (const tabRoot of getPassportsNestedToolbarSearchRoots()) {
            const matches = [];
            queryDeepAll(tabRoot, '[data-testid="' + testId + '"]', matches);
            queryDeepAll(tabRoot, '.e2e-' + testId, matches);
            for (const element of dedupeElements(matches)) {
                if (!isVisible(element)) {
                    continue;
                }

                if (!isNodeInsideRoot(element, tabRoot)) {
                    continue;
                }

                if (isPassportsNewHookTarget(element)) {
                    return element;
                }
            }
        }

        return null;
    }

    function resolvePassportsNestedNewClickTarget(testId) {
        const hooked = findPassportsNestedNewHookedElement(testId);
        if (hooked) {
            return resolvePassportsNestedNewClickableElement(hooked);
        }

        const buttons = findPassportsNestedListNewButtons();
        if (buttons.length === 0) {
            return null;
        }

        tagPassportsNestedNewButton(buttons[0], testId);
        const afterHook = findPassportsNestedNewHookedElement(testId);
        return resolvePassportsNestedNewClickableElement(afterHook || buttons[0]);
    }

    function resolvePassportsNestedNewClickableElement(element) {
        if (!element || !isVisible(element)) {
            return null;
        }

        const tagName = (element.tagName || '').toLowerCase();
        if (tagName === 'button') {
            return element;
        }

        const innerButtons = [];
        queryDeepAll(element, 'button', innerButtons);
        for (const button of dedupeElements(innerButtons)) {
            if (isVisible(button)) {
                return button;
            }
        }

        return element;
    }

    function isPassportsNestedNewHookApplied(testId) {
        const tabRoot = getVisiblePersonDetailPassportsTabRoot();
        if (!isPassportsTabContextReady(tabRoot)) {
            return false;
        }

        return !!findPassportsNestedNewHookedElement(testId);
    }

    function stripPassportsNestedNewHooks(testId) {
        const e2eClass = 'e2e-' + testId;
        const matches = [];
        const tabRoot = getVisiblePersonDetailPassportsTabRoot();
        const searchRoots = tabRoot ? [tabRoot] : [document];
        for (const root of searchRoots) {
            queryDeepAll(root, '[data-testid="' + testId + '"]', matches);
            queryDeepAll(root, '.' + e2eClass, matches);
        }

        for (const element of dedupeElements(matches)) {
            if (element.getAttribute('data-testid') === testId) {
                element.removeAttribute('data-testid');
            }

            element.classList.remove(e2eClass);
        }
    }

    function findTargetActionButtons(config) {
        if (config.usePassportsNestedNewFinder) {
            return findPassportsNestedListNewButtons();
        }

        let candidates = findAllActionButtons(config);
        if (config.requirePathContains === 'Person_DetailView') {
            candidates = candidates.filter(function (button) {
                return !isPersonListNewHookButton(button);
            });
        }

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

    function applyPassportsNestedNewTestId(config, resolvedTestId) {
        if (passportsNestedApplyInFlight) {
            return isPassportsNestedNewHookApplied(resolvedTestId);
        }

        if (isPassportsNestedNewHookApplied(resolvedTestId)) {
            return true;
        }

        const buttons = findTargetActionButtons(config);
        if (buttons.length === 0) {
            return false;
        }

        passportsNestedApplyInFlight = true;
        try {
            stripPassportsNestedNewHooks(resolvedTestId);
            tagPassportsNestedNewButton(buttons[0], resolvedTestId);
            return isPassportsNestedNewHookApplied(resolvedTestId);
        } finally {
            passportsNestedApplyInFlight = false;
        }
    }

    function applyActionTestId(config, explicitTestId) {
        const resolvedTestId = resolveTestId(config, explicitTestId);
        if (!resolvedTestId) {
            return false;
        }

        if (config.usePassportsNestedNewFinder) {
            return applyPassportsNestedNewTestId(config, resolvedTestId);
        }

        const buttons = findTargetActionButtons(config);
        if (buttons.length === 0) {
            return false;
        }

        stripAllActionHooks(config);

        const e2eClass = 'e2e-' + resolvedTestId;
        for (const button of buttons) {
            button.setAttribute('data-testid', resolvedTestId);
            if (!button.classList.contains(e2eClass)) {
                button.classList.add(e2eClass);
            }
        }

        return buttons[0].getAttribute('data-testid') === resolvedTestId;
    }

    function isPassportDetailActive() {
        const path = window.location.pathname || '';
        if (path.indexOf('Passport_DetailView') >= 0) {
            return true;
        }

        return !!(
            document.querySelector('[data-testid="passport-passport-number"]')
            || document.querySelector('#passport-passport-number')
            || document.querySelector('.e2e-passport-passport-number')
        );
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
        if (config.requirePathContains) {
            const path = window.location.pathname || '';
            if (path.indexOf(config.requirePathContains) < 0) {
                return false;
            }
        }

        if (config.skipWhenPassportDetailActive && isPassportDetailActive()) {
            return false;
        }

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

    function isPassportsCollectionTabActive() {
        const tabHeaders = [];
        queryDeepAll(document, '.e2e-person-employee-tab-passports[role="tab"]', tabHeaders);
        queryDeepAll(document, '[role="tab"]', tabHeaders);
        for (const tab of dedupeElements(tabHeaders)) {
            if (tab.getAttribute('aria-selected') !== 'true') {
                continue;
            }

            const label = tab.innerText || tab.textContent || '';
            if (tab.classList && tab.classList.contains('e2e-person-employee-tab-passports')) {
                return true;
            }

            if (isPassportsTabLabel(label)) {
                return true;
            }
        }

        return !!getVisiblePersonDetailPassportsTabRoot();
    }

    function reapplyPassportsNestedNewWhenTabActive() {
        const path = window.location.pathname || '';
        if (path.indexOf('Person_DetailView') < 0) {
            return false;
        }

        const config = actionConfigsByGroupAndName.personDetailNestedPassports?.New;
        if (!config || !config.fallbackTestId) {
            return false;
        }

        const tabRoot = getVisiblePersonDetailPassportsTabRoot();
        if (!isPassportsTabContextReady(tabRoot)) {
            return false;
        }

        return applyActionTestId(config, config.fallbackTestId);
    }

    function reapplyPassportsNestedNew() {
        const path = window.location.pathname || '';
        if (path.indexOf('Person_DetailView') < 0) {
            return false;
        }

        const config = actionConfigsByGroupAndName.personDetailNestedPassports?.New;
        if (!config || !config.fallbackTestId) {
            return false;
        }

        return applyActionTestId(config, config.fallbackTestId);
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

        const passportsConfig = actionConfigsByGroupAndName.personDetailNestedPassports?.New;
        const passportsTabRoot = getVisiblePersonDetailPassportsTabRoot();
        if (passportsConfig?.fallbackTestId && isPassportsTabContextReady(passportsTabRoot)) {
            if (!reapplyPassportsNestedNewWhenTabActive()) {
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
        }, 120);
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
            attributeFilter: ['class', 'data-action-name', 'data-dx-action-name', 'data-x-action-name', 'aria-selected'],
        });
    }

    function startPassportsNestedPersistentWatch() {
        if (passportsNestedInterval) {
            return;
        }

        passportsNestedInterval = window.setInterval(function () {
            const config = actionConfigsByGroupAndName.personDetailNestedPassports?.New;
            if (!config || !config.fallbackTestId) {
                stopPassportsNestedInterval();
                return;
            }

            const path = window.location.pathname || '';
            if (path.indexOf('Person_DetailView') < 0) {
                return;
            }

            const tabRoot = getVisiblePersonDetailPassportsTabRoot();
            if (!isPassportsTabContextReady(tabRoot)) {
                return;
            }

            if (!isPassportsNestedNewHookApplied(config.fallbackTestId)) {
                reapplyPassportsNestedNew();
            }
        }, 600);
    }

    function stopPassportsNestedInterval() {
        if (!passportsNestedInterval) {
            return;
        }

        window.clearInterval(passportsNestedInterval);
        passportsNestedInterval = null;
    }

    function disconnectToolbarWatchIfIdle() {
        if (hasAnyActiveFallback()) {
            return;
        }

        stopPassportsNestedInterval();

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

        const isPassportsNested = groupName === 'personDetailNestedPassports';
        if (isPassportsNested) {
            startPassportsNestedPersistentWatch();
        }

        const applied = applyActionTestId(config, testId);
        if (applied) {
            return true;
        }

        if (isPassportsNested) {
            reapplyPassportsNestedNewWhenTabActive();
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

        if (groupName === 'personDetailNestedPassports') {
            stopPassportsNestedInterval();
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

    window.visa2026E2eHooks.ensurePassportDetailActionTestId = function (actionName, testId) {
        return ensureGroupActionTestId('passportDetail', actionName, testId);
    };

    window.visa2026E2eHooks.applyPassportDetailActionTestId = function (actionName, testId) {
        const config = actionConfigsByGroupAndName.passportDetail?.[actionName];
        if (!config) {
            return false;
        }

        return applyActionTestId(config, testId);
    };

    window.visa2026E2eHooks.stopPassportDetailToolbarWatch = function () {
        stopToolbarWatchGroup('passportDetail');
    };

    window.visa2026E2eHooks.ensurePersonDetailPassportsListNewActionTestId = function (testId) {
        return ensureGroupActionTestId('personDetailNestedPassports', 'New', testId);
    };

    window.visa2026E2eHooks.applyPersonDetailPassportsListNewActionTestId = function (testId) {
        const config = actionConfigsByGroupAndName.personDetailNestedPassports?.New;
        if (!config) {
            return false;
        }

        return applyActionTestId(config, testId);
    };

    window.visa2026E2eHooks.isPersonDetailPassportsListNewHookVisible = function (testId) {
        return isPassportsNestedNewHookApplied(testId || 'person-employee-tab-passports-new');
    };

    window.visa2026E2eHooks.isPersonDetailPassportsListNewClickable = function (testId) {
        testId = testId || 'person-employee-tab-passports-new';
        window.visa2026E2eHooks.applyPersonDetailPassportsListNewActionTestId(testId);
        return !!resolvePassportsNestedNewClickTarget(testId);
    };

    window.visa2026E2eHooks.clickPersonDetailPassportsListNew = function (testId) {
        testId = testId || 'person-employee-tab-passports-new';
        window.visa2026E2eHooks.ensurePersonDetailPassportsListNewActionTestId(testId);
        window.visa2026E2eHooks.applyPersonDetailPassportsListNewActionTestId(testId);
        const target = resolvePassportsNestedNewClickTarget(testId);
        if (!target) {
            return false;
        }

        target.click();
        return true;
    };

    window.visa2026E2eHooks.stopPersonDetailNestedPassportsToolbarWatch = function () {
        stopToolbarWatchGroup('personDetailNestedPassports');
    };

    // Back-compat alias (removed tab-content scoping controller).
    window.visa2026E2eHooks.ensurePersonNestedCollectionNewActionTestId = function (_tabContentClass, testId) {
        return window.visa2026E2eHooks.ensurePersonDetailPassportsListNewActionTestId(testId);
    };

    window.visa2026E2eHooks.stopPersonNestedCollectionToolbarWatch =
        window.visa2026E2eHooks.stopPersonDetailNestedPassportsToolbarWatch;

    window.visa2026E2eHooks.stopNewActionWatch = window.visa2026E2eHooks.stopPersonListToolbarWatch;
})();
