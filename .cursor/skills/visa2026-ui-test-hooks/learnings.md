# Learnings (append-only): Visa2026 UI test hooks

Purpose: this skill **gets smarter over time**. Capture **verified** outcomes from implementing hooks and **DevTools console** access/behavior checks. Agents **read before** similar work; **append after** a lesson is confirmed.

This skill **prepares UI element accessibility** with CSS selectors from the live UI — it does **not** run E2E suites or scrapers. Native EasyTest E2E → [visa2026-easytest-e2e](../visa2026-easytest-e2e/SKILL.md).

Keep **`SKILL.md`** stable; **promote** repeated lessons into **Known pitfalls** or [reference.md](./reference.md).

**Do not** delete or rewrite old entries — **append only**.

---

## How to use

**Before** adding hooks or debugging null `querySelector`:

1. **Classify** target → family **A–F** ([SKILL.md § Classify](./SKILL.md#2-classify-family--stable-id)).
2. **Match** prior art in registry → `UI_TEST_HOOKS.md` → reference → learnings → `*E2e*` controllers ([reference.md § Match checklist](./reference.md#match-checklist-search-order)).
3. **Reuse** matched pattern **or** enter **Discover** trial loop ([SKILL.md § 4b](./SKILL.md#4b-discover-path-new-family-or-first-of-its-kind)).
4. Skim **## Entries** for same-family pitfalls.

**After** verified DOM discovery or confirmed dead end:

1. Append one entry using the template below.
2. Tag **Outcome**: `positive`, `negative`, or `anti-pattern`.
3. Update [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md) (only after DevTools verify) and [registry.md](./registry.md).
4. On second occurrence of same root cause → **Promote** to `SKILL.md` Known pitfalls.

---

## Entry template

```markdown
### YYYY-MM-DD — [+/−] <short title> (<view or hook id>)

- **Outcome**: positive | negative | anti-pattern
- **Element**: login field | layout tab | action | grid | …
- **Context**: (view id, browser, local/stage URL)
- **Symptom** / **Goal**:
- **Try**:
- **Verify** (DevTools access + behavior):
- **Result**:
- **Reuse** / **Avoid**:
- **Promote**: none | pending | done YYYY-MM-DD → SKILL/reference
```

---

## Promotion ladder

| Signal | Action |
|--------|--------|
| First verified fix | Append **learnings.md** only |
| Same lesson twice | Add row to **SKILL.md** Known pitfalls or **reference.md** |
| Stable family (3+ hooks) | Keep **`docs/UI_TEST_HOOKS.md`** + **registry.md** sections |

---

## Entries

### 2026-06-06 — [+] Login hooks need CreateLogonWindowControllers (logon frame)

- **Outcome**: positive
- **Element**: login field, logon action
- **Context**: `AuthenticationStandardLogonParameters_Blazor_DetailView`, local Debug
- **Symptom**: `querySelector('[data-testid="login-user-name"]')` returned null; controller never ran
- **Try**: `LogonViewE2eSelectorsController` with `TargetViewId` only
- **Verify**: Restart app; `#login-user-name`, `#login-password`, `[data-testid="login-submit"]` non-null
- **Result**: Register controller in `Visa2026BlazorApplication.CreateLogonWindowControllers`
- **Reuse**: Any logon-only hook must use `CreateLogonWindowControllers`
- **Promote**: done → SKILL.md Known pitfalls

### 2026-06-06 — [+] Text inputs: InputId beats data-testid alone (DxTextBox)

- **Outcome**: positive
- **Element**: login field, person string field
- **Context**: DevExpress `StringPropertyEditor` → `DxTextBoxModel`
- **Goal**: Reliable DevTools console locators on the `<input>`
- **Try**: `SetAttribute("data-testid")` only vs also `InputId = testId`
- **Verify**: `#login-user-name` on `<input>`; `focus()` and `value` read/write
- **Result**: Set **both** `InputId` and `data-testid` in `E2eTextEditorSelectorApplicator`
- **Reuse**: Prefer `#slug` for text fields in console checks
- **Promote**: done → reference.md string field row

### 2026-06-06 — [−] Tab hooks planned in chat but not in DOM (Passports)

- **Outcome**: anti-pattern
- **Element**: layout tab
- **Context**: `Person_DetailView_Employee`, TabbedGroup `Tabs`, layout id `Passports`
- **Symptom**: `.e2e-person-employee-tabs .e2e-tab-passports` null — selectors never implemented
- **Try**: Document-only selector names without model/controller
- **Verify**: DevTools `querySelector` null
- **Avoid**: Suggesting selectors before code + registry entry
- **Fix**: `PersonDetailViewE2eTabSelectorsController` + `BlazorLayoutManager.ItemCreated` + `Model.xafml`
- **Promote**: done → SKILL.md Known pitfalls

### 2026-06-06 — [+] Layout tabs: HeaderCssClass for clickable tab header

- **Outcome**: positive
- **Element**: layout tab page
- **Context**: `DxFormLayoutTabPageModel`, DevExpress form layout tabs
- **Symptom**: `CustomCSSClassName` on layout group did not yield clickable header selector
- **Try**: `BlazorLayoutManager.ItemCreated`; set `HeaderCssClass` + `SetAttribute("data-testid")`
- **Verify**: `.e2e-person-employee-tab-passports` or `[data-testid="person-employee-tab-passports"]` click activates tab
- **Reuse**: DevExpress [tab layout example](https://github.com/DevExpress-Examples/xaf-how-to-access-a-tab-control-in-a-detail-view-layout)
- **Promote**: done → reference.md layout tab row

### 2026-06-06 — [+] Sidebar nav: DxAccordion ItemHeaderTextTemplate wrapper (People group)

- **Outcome**: positive (code + DevTools verify 2026-06-06)
- **Element**: accordion navigation item header
- **Context**: `ShowNavigationItemAction.CustomizeControl`, `DxAccordionAdapter`, internal `NavMenuItem`
- **Symptom**: no `IModelNavigationItemBlazor.CustomCSSClassName`; `NavMenuItem.CssClass` is computed read-only
- **Try**: wrap default `ItemHeaderTextTemplate`; resolve test id by walking `ChoiceActionItem` tree in parallel with menu items (reference equality); match navigation item **Id**, not caption
- **Verify**: `[data-testid="nav-people-employees"]` non-null after expanding People; `.click()` opens `Person_ListView_Employees`
- **Reuse**: `NavigationE2eSelectorsController`, `NavigationE2eHooks`, `NavigationE2eSelectorSupport`
- **Promote**: done → reference.md sidebar nav row

### 2026-06-07 — [-] Sidebar nav hooks null in DevTools (:5001) — CustomizeControl timing

- **Outcome**: negative → fix applied
- **Symptom**: `[data-testid="nav-people"]` and `.e2e-nav-people` both `null` on logged-in main window (Person tab hooks OK on same host)
- **Cause**: `CustomizeControl` can fire before `WindowController.OnActivated` subscribes; template closure also captured `ShowNavigationItemAction.Items` too early
- **Fix** (parallel to Person tabs’ reliable `ItemCreated` timing): on activate read `((IMainFormTemplate)Frame.Template).ShowNavigationItemActionControl` and apply immediately; subscribe `ItemsChanged`; read fresh `Items` + menu `Data` on each header render; resolve test id via menu index path → `ChoiceActionItem.Id`
- **Verify**: rebuild/restart host; DevTools queries from `docs/UI_TEST_HOOKS.md` People section (after verify passes)
- **Reuse**: same `data-testid` + `.e2e-*` contract as layout tabs

### 2026-06-07 — [+] Sidebar nav: DevExpress accordion open shadow DOM

- **Outcome**: positive (diagnosis)
- **Symptom**: `document.querySelector('[data-testid="nav-people"]')` → `null` on logged-in main window; Elements shows `#shadow-root (open)` under `.dxbl-accordion-group-body` with nav items inside
- **Cause**: `document.querySelector` does **not** pierce open shadow roots; DxAccordion group bodies encapsulate item trees
- **Try**: deep query walking `element.shadowRoot`, or Playwright `page.Locator(...)` (pierces shadow); Person detail tabs are **not** in the same shadow tree — their DevTools snippets still work with top-level `document.querySelector`
- **Verify**: `deepQuery('[data-testid="nav-people-employees"]')` non-null **inside** shadow when hooks are applied; Playwright `VerifyUiTestHooks` uses locators (not `QuerySelectorAsync`)
- **Promote**: note in reference.md sidebar nav row when verified

### 2026-06-07 — [-] Sidebar nav hooks still null — wrong adapter property + test-id lookup

- **Outcome**: negative → fix applied → **verified 2026-06-06** (DevTools `:5001`)
- **Symptom**: `deepQueryAll('[data-testid^="nav-people"]')` → `[]`; HTML shows only default `<div class="xaf-nav-link"><span>People</span></div>` (no wrapper div)
- **Cause 1**: code used non-existent `ShowNavigationItemActionControl.AccordionAdapter`; DevExpress exposes **`NavigationComponentAdapter`** (cast to `DxAccordionAdapter`) — template hook never ran
- **Cause 2**: `ReferenceEquals` path lookup could miss; added parallel index `PopulateTestIdMap` + **`NavigateUrl` fallback** for leaf items (`Person_ListView_Employees`, …)
- **Fix**: `NavigationComponentAdapter as DxAccordionAdapter`; preserve XAF default template once; wrap on match; subscribe `ComponentCaptured` for late adapter init
- **Verify**: stop VS host → rebuild → restart; `document.querySelector('[data-testid="nav-people-employees"]')` non-null (light DOM under `.xaf-navmenu`, no deep query required for leaves)
- **Verified**: `.e2e-nav-people-employees`, `.e2e-nav-people-family-members` non-null on `:5001` after rebuild

### 2026-06-06 — [+] Isolated hook verify: Invoke-UiHookVerify.ps1 + _agent_build_out :5051

- **Outcome**: positive
- **Context**: agents should not depend on developer IDE host on :5000/:5001
- **Try**: `scripts/local/Invoke-UiHookVerify.ps1` — build → LocalDB `Visa2026HookVerify` → `ASPNETCORE_URLS=http://localhost:5051` → `VerifyUiTestHooks` → stop
- **Verify**: `.\scripts\local\Invoke-UiHookVerify.ps1 -Scenario login` all PASS
- **Reuse**: default step 2 in SKILL.md; `-KeepServer` for DevTools; `-StopOnly` cleanup
- **Promote**: done → SKILL.md § Isolated verify server, scripts/README.md

### 2026-06-06 — [+] launchSettings: Visa2026 - Hook Verify (LocalDB)

- **Outcome**: positive
- **Context**: single source for hook-verify port + LocalDB connection
- **Profile**: `Visa2026 - Hook Verify (LocalDB)` — `http://localhost:5051`, database `Visa2026HookVerify`, `FORCE_XAF_DB_UPDATE=true`
- **Reuse**: `Invoke-UiHookVerify.ps1` uses `--launch-profile`; IDE F5 for manual DevTools without touching dev `:5000` stack
- **Promote**: done → SKILL.md, reference.md files checklist

### 2026-06-06 — [+] Mechanism families A–F (skill categorization)

- **Outcome**: positive (skill/docs)
- **Problem**: People nav hooks took ~1h — mostly wrong DevExpress API + lookup, not four separate nav items
- **Try**: categorize by **hook mechanism** (scalar / tab / action / logon / sidebar nav / grid), not business module
- **Reuse**: family **E** = shared controller + `NavigationE2eHooks` map; next nav group ≈ append Id + `NavigateUrl` rows + verify (~15 min)
- **Promote**: SKILL.md § UI element categories, reference.md § Sidebar navigation checklist, user-prompts “another nav group”

### 2026-06-06 — [+] Consolidated People nav → shared Navigation* files

- **Outcome**: positive (refactor; build OK)
- **Change**: `PeopleNavigationE2eHooks` + `PeopleNavigationE2eSelectorsController` → `NavigationE2eHooks` + `NavigationE2eSelectorsController`; People rows stay in hook map
- **Reuse**: next nav group = append to `NavigationE2eHooks` only

### 2026-06-06 — [+] Classify → match → reuse | discover (skill workflow)

- **Outcome**: positive (skill/docs)
- **Problem**: agents re-spiked DevExpress per target; People nav ~1h despite same family
- **Try**: mandatory before code — **classify** A–F, **match** registry/reference/learnings/code, **reuse** or **discover** trial loop until DevTools OK
- **Reuse**: SKILL.md § Before writing code; reference.md § Classify and match
- **Promote**: agent states family + reuse anchor (or declares Discover) before editing controllers

### 2026-06-07 — [-] ListView New action hook null — Ribbon vs Toolbar control type

- **Outcome**: negative → fixed
- **Element**: toolbar action (family **C**)
- **Context**: `Person_ListView_Employees`, `Model.xafml` `FormStyle="Ribbon"`, `NewObjectAction` (`SingleChoiceAction`)
- **Symptom**: `[data-testid="person-list-employees-new"]` → `null`; Elements shows `<button data-x-action-name="New">` inside open shadow root, no wrapper
- **Cause**: handler matched only `DxToolbarItemSimpleActionControl`; Ribbon UI uses **`DxRibbonItemSingleChoiceActionControl`** + `RibbonItemModel` (not toolbar types). Logon submit still uses toolbar simple control.
- **Cause 2**: `ViewController.OnActivated` subscribes **after** action control is created — DevExpress docs: use **`OnFrameAssigned`** on a frame controller; `_newActionControl` stays null.
- **Fix**: `WindowController` (main) + subscribe in **`OnFrameAssigned`**; `TryRaiseCustomizeControl`; **`AppendCssClass`** (do not replace `CssClass`); re-apply on `Frame.ViewChanged`
- **Verify**: rebuild + **restart** host; check host `dxbl-toolbar-item[data-testid="…"]` or button `.e2e-*` **inside** open shadow (`deepQuery` below). Playwright locators pierce shadow; top-level `document.querySelector` does not.
- **Reuse**: any Ribbon action hook → check `FormStyle`; prefer `E2eActionControlSelectorSupport`; call `UpdateNewObjectAction()` on view change; keep `CustomizeControl` subscribed on main window

### 2026-06-07 — [-] Person ListView New — CustomizeControl ineffective; JS shadow pierce

- **Outcome**: negative → fixed (discover)
- **Element**: view toolbar `New` button (family **C** target)
- **Context**: `FormStyle="Ribbon"`, `UIType="TabbedMDI"`, view toolbar in `dxbl-adaptive-container` → nested `#shadow-root (open)` → `button[data-action-name="New"]`
- **Symptom**: `CustomizeControl` + `RibbonItemModel.SetAttribute` / `Attributes` — no `data-testid` or `.e2e-*` in Elements; top-level and `deepQuery('[data-testid=…]')` null; built-in `data-action-name="New"` visible inside shadow
- **Try**: `WindowController` + `OnFrameAssigned`, `UpdateNewObjectAction`, `RaiseCustomizeControl`, `DxRibbonItemSingleChoiceActionControl` — all failed on `:5001`
- **Fix**: `ViewController<ListView>` (typed Person lists) → `OnViewControlsCreated` → `wwwroot/js/visa2026-e2e-hooks.js` sets `data-testid` + `.e2e-*` on the **button inside shadow** (retries until toolbar renders)
- **Verify**: rebuild + restart; `visa2026E2eHooks.applyNewActionTestId('person-list-employees-new')` → `true`; then `deepQuery('[data-testid="person-list-employees-new"]')` non-null
- **Re-nav (2026-06-07)**: hooks present on first visit only — `OnViewControlsCreated` does not re-run when sidebar/tab returns to the same ListView; toolbar DOM is recreated without hooks. **Fix**: also call `ensureNewActionTestId` from `OnActivated`; JS `MutationObserver` on `document.body` re-applies while view is active; `OnDeactivated` → `stopNewActionWatch`. Prefer visible `New` button when multiple tabs exist.
- **TabbedMDI sync (2026-06-07)**: after Employees ↔ Family Members switch, visible **New** kept wrong `data-testid` (e.g. `person-list-family-members-new` on Employees URL) — multiple Person list tabs stay alive; `watchedTestId` from last-active controller overwrote the visible toolbar. **Fix**: JS resolves test id from **`window.location.pathname`** first (URL is source of truth); strip all `person-list-*-new` hooks from every **New** button before re-applying; hook `history.pushState` / `replaceState` + `popstate` to re-sync on tab switch.
- **Reuse**: view-toolbar / nested shadow actions → JS pierce helper before spending time on `CustomizeControl`; Logon submit stays family **C** model + toolbar (no shadow)
- **Promote**: registry.md Person ListView New row; reference.md family **C** note

### 2026-06-07 — [+] Person ListView Delete — same JS toolbar pipeline as New

- **Outcome**: positive (verified DevTools)
- **Element**: view toolbar `Delete` button on typed Person lists (Employees / Family Members / Temporary visitor)
- **Fix**: extend `PersonListViewE2eActionHooks` + `PersonListViewE2eActionSelectorsController`; refactor `visa2026-e2e-hooks.js` to shared `personListToolbarActions` (New + Delete) with one observer / URL sync
- **Selectors**: `person-list-employees-delete`, `person-list-family-members-delete`, `person-list-temporary-visitors-delete` (+ `.e2e-*`)
- **Verify**: restart host; `document.querySelector('[data-testid="person-list-employees-delete"]')` on each list; re-nav between tabs like New

### 2026-06-07 — [+] Person DetailView Save — same JS toolbar pipeline as ListView

- **Outcome**: positive (verified DevTools)
- **Element**: view toolbar `Save` button on typed Person detail views (Employee / Family Member / Temporary visitor)
- **Fix**: `PersonDetailViewE2eActionHooks` + `PersonDetailViewE2eActionSelectorsController`; JS `personDetail` toolbar group (`ensurePersonDetailSaveActionTestId`); grouped watchers (`stopPersonListToolbarWatch` / `stopPersonDetailToolbarWatch`) so list + detail tabs do not clobber each other
- **Selectors**: `person-detail-employee-save`, `person-detail-family-member-save`, `person-detail-temporary-visitor-save` (+ `.e2e-*`)
- **Verify**: restart; open `/Person_DetailView_Employee/{key}` → `document.querySelector('[data-testid="person-detail-employee-save"]')`; switch detail tabs / list ↔ detail like New

### 2026-06-07 — [+] Person DetailView SaveAndClose / SaveAndNew — extend Save toolbar pipeline

- **Outcome**: positive (verified DevTools)
- **Element**: view toolbar `SaveAndClose` / `SaveAndNew` on typed Person detail views
- **Fix**: extend `PersonDetailViewE2eActionHooks.ToolbarActions`; controller loops all actions; JS `personDetail` group adds `SaveAndClose` + `SaveAndNew` (`data-action-name` matches XAF Action Id)
- **Selectors**: `person-detail-{employee|family-member|temporary-visitor}-save-and-close|save-and-new` (+ `.e2e-*`)
- **Verify**: restart; employee detail → all three queries non-null; re-nav between typed detail tabs

### 2026-06-07 — [-] Person DetailView SaveAndClose — caption `data-action-name` + hidden dropdown

- **Outcome**: negative → fixed (verified DevTools)
- **Symptom**: `Save` hook OK; `SaveAndClose` / `SaveAndNew` — no `data-testid`; Elements shows dropdown item `data-action-name="Save and Close"` (caption, not `SaveAndClose`); item hidden until split-button menu opens
- **Fix**: JS action aliases (`Save and Close`, `Save and New`); `applyToAllMatches` tags hidden dropdown `<button>`s, not only visible toolbar control

### 2026-06-07 — [+] Person typed detail scalar hooks — per-view member sets

- **Outcome**: positive (verified DevTools — Employee, Family member, Temporary visitor)
- **Family**: **A** — reuse `PersonDetailViewE2eSelectorsController` + `E2ePropertySelectorApplicator`
- **Fix**: `PersonE2eMemberHooks.GetScalarMembersForDetailView` — Employee (15), Family member (13), Temporary visitor (12); `TargetViewId` on typed views; `OnActivated` re-applies hooks
- **Verify**: restart; `document.querySelector('#person-first-name')` on each typed detail URL; `Invoke-UiHookVerify.ps1 -Scenario person-{employee|family-member|temporary-visitor}-scalar-fields`

### 2026-06-08 — [-] Nested Passports **New** — dedicated JS finder + tag adaptive group

- **Symptom**: `div.dxbl-adaptive-group` for `+ New` has no `data-testid`; generic JS finder + `isLayoutTabActive` gate returned no candidates
- **Fix**: `findPassportsNestedListNewButtons()` — visible Passports tab root OR `Passport Number` grid context; `tagPassportsNestedNewButton` sets hook on **button + adaptive group**; tab `ItemCreated` triggers ensure; dropped ineffective `CustomizeControl` path (same as Person ListView New learnings 2026-06-07)
- **Verify**: restart host → Passports tab → `visa2026E2eHooks.applyPersonDetailPassportsListNewActionTestId('person-employee-tab-passports-new')` → `true`; inspected `div.dxbl-adaptive-group` shows `data-testid="person-employee-tab-passports-new"`

### 2026-06-08 — [-] Nested Passports **New** — JS-only hook never stuck on DOM; switched to CustomizeControl

- **Symptom**: Passports tab `+ New` visible; outer `div.dxbl-adaptive-group` has no `data-testid`; scenario stuck
- **Cause**: JS `personDetailNestedPassports` could not reliably tag adaptive toolbar inside shadow DOM; inspecting wrapper div misses hook on inner `<button>`
- **Fix**: `PassportListViewE2eActionSelectorsController` — `NewObjectAction.CustomizeControl` + `E2eActionControlSelectorSupport` when `PropertyCollectionSource.MasterObject is Person` (same as login Submit); JS remains fallback
- **Verify**: restart host → Passports tab → Elements search `person-employee-tab-passports-new` (expand `#shadow-root` under toolbar) OR `deepQuery('[data-testid="person-employee-tab-passports-new"]')[0]`

### 2026-06-08 — [-] Nested Passports **New** — hook missing in DOM (`isLayoutTabActive` false)

- **Symptom**: Passports tab open, `+ New` visible, no `data-testid` / `.e2e-person-employee-tab-passports-new` on button; Playwright stuck on click/wait
- **Cause**: `isLayoutTabActive('person-employee-tab-passports')` only checked `dxbl-active` on tab node with `data-testid`; active class is on tab **header**; content uses `.e2e-*-content` without `dxbl-active`
- **Fix**: `isLayoutTabActive` — deep-query tab header + treat visible `.e2e-{id}-content` as active; add `.dxbl-adaptive-group` selectors; exclude person-list/person-detail test ids from passport New candidates
- **Verify**: Passports tab → `visa2026E2eHooks.applyPersonDetailPassportsListNewActionTestId('person-employee-tab-passports-new')` → `true`; `deepQuery('[data-testid="person-employee-tab-passports-new"]')[0]` on inner `<button>`, not the outer `dxbl-adaptive-group` div

### 2026-06-08 — [+] Passport detail Save — nested new passport; Person Save hook collision fixed

- **Outcome**: positive (`.e2e-passport-detail-save` on nested **New** passport; URL still `Person_DetailView`)
- **Symptom**: Same toolbar Save had `data-testid="person-detail-employee-save"` plus `e2e-passport-detail-save` class — person hooks overwrote passport `data-testid` because path still matched Person detail
- **Fix**: `personDetail` toolbar configs: `requirePathContains: 'Person_DetailView'` + `skipWhenPassportDetailActive` when passport form (`passport-passport-number`) or `Passport_DetailView` path is active
- **Verify**: `deepQuery('.e2e-passport-detail-save')` or `[data-testid="passport-detail-save"]` after restart + apply JS

### 2026-06-08 — [+] Passport detail scalars — all six required fields verified (incl. IssuedCountry)

- **Outcome**: positive (batch verify — all **OK** on `Passport_DetailView` after layout fix)
- **Fix**: `IssuedCountry` restored in `Passport_col1` (`Model.xafml`); hook `passport-issued-country` on lookup combo
- **Verify**: batch loop in SKILL.md § Batch verify; includes `passport-issued-country`

### 2026-06-08 — [+] Passport detail scalars — batch verify; IssuedCountry waived (layout)

- **Outcome**: positive (5/5 visible scalars verified); `passport-issued-country` **MISSING** is expected
- **Cause**: `Passport_DetailView` → `LayoutItem Id="IssuedCountry" Removed="True"` in `Visa2026.Blazor.Server/Model.xafml`; `Passport.OnCreated` sets default `IssuedCountry`
- **Action**: removed `IssuedCountry` from `PassportE2eMemberHooks`; scenario map **waives** `passport-issued-country` fill
- **Reuse**: batch console loop (SKILL.md § Batch verify) before promoting hook rows

### 2026-06-08 — [+] Person Passports nested **New** — verified on employee detail (DevTools)

- **Outcome**: positive (verified)
- **DOM**: `<button data-action-name="New" data-testid="person-employee-tab-passports-new" class="… e2e-person-employee-tab-passports-new">` inside `dxbl-toolbar-item` on Passports tab (`Person_DetailView_Employee`)
- **Note**: button is under `dxbl-toolbar-item` shadow/slot — if `document.querySelector` is null, expand **dxbl-toolbar-item → #shadow-root** in Elements or use `deepQuery`; hook attributes are on the `<button>` itself
- **Verify**: Passports tab active → `visa2026E2eHooks.applyPersonDetailPassportsListNewActionTestId('person-employee-tab-passports-new')` → `true`

### 2026-06-08 — [-] Person Passports nested **New** — nested grid toolbar uses adaptive-item class, not data-action-name

- **Outcome**: negative → fixed (rediscover)
- **Symptom**: Elements shows `div.dxbl-toolbar-adaptive-item-new` / `dxbl-toolbar-group`; no `data-testid`; `applyPersonDetailPassportsListNewActionTestId` → `false` or tags nothing
- **Cause**: embedded collection toolbar **New** may lack `data-action-name` on `<button>`; wrapper class is `dxbl-toolbar-adaptive-item-new` (DevExpress adaptive toolbar)
- **Fix**: add `data-x-action-name` to global action selectors; `personDetailNestedPassports` `extraSelectors`: `.dxbl-toolbar-adaptive-item-new button`; `requireActiveTabTestId: person-employee-tab-passports`; prefer adaptive-new candidates when Passports tab `.dxbl-active`
- **Verify**: Passports tab active → `applyPersonDetailPassportsListNewActionTestId('person-employee-tab-passports-new')` → `true`; `deepQuery('[data-testid="person-employee-tab-passports-new"]')[0]` non-null

### 2026-06-08 — [-] Person Passports nested **New** — tab-content CSS scope failed DevTools

- **Outcome**: negative → fixed (rediscover)
- **Symptom**: `[data-testid="person-employee-tab-passports-new"]` null on `Person_DetailView_Employee` with Passports tab active; `.e2e-person-employee-tab-passports-content` scope root not wrapping nested grid toolbar
- **Try**: `PersonNestedCollectionE2eActionSelectorsController` + pierce inside `.e2e-person-employee-tab-passports-content`
- **Fix**: `PassportListViewE2eActionSelectorsController` on `Passport_ListView` (same pipeline as Person ListView **New**); JS group `personDetailNestedPassports` with `requirePathContains: 'Person_DetailView'`; skip buttons already tagged `person-list-*-new`
- **Verify**: restart host; Passports tab active → `visa2026E2eHooks.applyPersonDetailPassportsListNewActionTestId('person-employee-tab-passports-new')` → `true`; then `document.querySelector('[data-testid="person-employee-tab-passports-new"]')` non-null

### 2026-06-08 — [+] Person Passports nested ListView **New** — Passport_ListView controller (supersedes tab-content scope)

- **Outcome**: positive (implemented — verify with `person-employee-tab-passports-new` scenario)
- **Family**: **C** — reuse `PersonListViewE2eActionSelectorsController` pattern on `Passport_ListView`
- **Passport detail**: family **A** `PassportDetailViewE2eSelectorsController` + family **C** `passportDetail` JS group for `passport-detail-save`
- **Verify**: activate Passports tab first; `Invoke-UiHookVerify.ps1 -Scenario person-employee-tab-passports-new -StartUrl "/Person_DetailView_Employee/{guid}"`; passport fields need `VISA2026_HOOK_VERIFY_PASSPORT_URL`

### 2026-06-10 — `person-employee-tab-passports-new` missing on `:5052` but present on IDE `:5001`

- **Symptom**: DevTools on scenario host `:5052` shows `+ New` on Passports tab but no `data-testid="person-employee-tab-passports-new"`; same build works on IDE `:5001`.
- **Cause**: `getVisiblePersonDetailPassportsTabRoot()` returned the **tab header** (`.e2e-person-employee-tab-passports`, always visible) instead of the **tab content panel**, so shadow-pierce search never reached the nested ListView toolbar. Header vs content distinction is more visible on `VISA2026_UI_SCENARIOS=true` hosts (ephemeral layout / lazy tab panels).
- **Fix**: JS — scope to `.e2e-person-employee-tab-passports-content`, skip `role="tab"`, resolve active panel via `aria-controls`; localized grid header fallbacks in `isPassportsNestedListContext`. Runner — `WaitForPassportsNestedNewHookAsync` after `select-tab: person-employee-tab-passports`. Build — copy `wwwroot` into `_scenario_build_out` after `dotnet build -o`; `asp-append-version` on hook script.
- **Verify**: restart `:5052` host (no `-SkipBuild`), hard-refresh, Passports tab active → `visa2026E2eHooks.applyPersonDetailPassportsListNewActionTestId('person-employee-tab-passports-new')` → `true`.

### 2026-06-10 — `person-employee-tab-passports-new` lost on Person detail re-open (Passports tab restored)

- **Symptom**: Hook present on first open; missing after close + re-open when Passports tab was left selected; returns after switching Educations → Passports (same family as Person ListView **New** re-nav).
- **Cause**: `OnViewControlsCreated` / nested `Passport_ListView` activation do not re-run on layout restore; `PassportListViewE2eActionSelectorsController.OnDeactivated` stopped JS `MutationObserver` while Person detail was still open; toolbar DOM recreated without `CustomizeControl` re-fire.
- **Fix**: `PersonDetailPassportsNestedNewHookSupport` — shared `OnActivated` + `OnViewControlsCreated` ensure, delayed `apply*` passes (400ms–5s), stop watch only on Person detail `OnDeactivated`; `PersonDetailViewE2eTabSelectorsController` wires `DxFormLayoutTabPagesModel.ActiveTabIndexChanged`; JS `reapplyPassportsNestedNewWhenTabActive()` on observer cycle when Passports tab `aria-selected`.
- **Verify**: open employee → Passports tab → close detail → re-open same employee (Passports still selected) → `data-testid="person-employee-tab-passports-new"` without manual tab switch.

### 2026-06-10 — Passports New toolbar uses `data-dx-toolbar-item-id` (new employee / Person_DetailView)

- **Symptom**: Existing employee hook OK; newly saved employee on `Person_DetailView/Edit/Read?oid=…` → Passports **New** visible, no `data-testid`; Elements show `data-dx-toolbar-item-id="New"` on `div.dxbl-adaptive-group`, not `data-action-name`.
- **Fix**: Finder/verify/tag `getPassportsNewActionId()` includes `data-dx-toolbar-item-id`; priority selector `[data-dx-toolbar-item-id="New"]`; `isPassportsTabContextReady` uses grid markers when tab header hooks lag; post-save `DelayedPostSaveHookRefreshAsync`.
- **Verify**: New employee → Save → Passports → `isPersonDetailPassportsListNewHookVisible()` → `true`; hook on adaptive-group with `data-dx-toolbar-item-id="New"`.

### 2026-06-10 — `person-employee-tab-passports-new` missing on newly saved Employee (Passports tab)

- **Symptom**: Hook works opening existing employee; after **Save** on new employee → Passports tab → no `data-testid`; scenario `person-employee-passport-create` fails post-save.
- **Cause**: `ObjectSpace.Committed` + URL `NavigateTo` recreates nested `ListPropertyEditor.Frame`; retry loop exhausted before Passports tab first opened; stale `CustomizeControl` on old frame.
- **Fix**: `PersonDetailPassportsListNewE2eAnchorController` handles `ObjectSpace.Committed` → unhook/rebind + burst + retry; tab `ActiveTabIndexChanged` → rebind + retry (48×250ms); `Passport_ListView` OnActivated uses full delayed ensure.
- **Verify**: New employee → fill → Save → Passports tab → `isPersonDetailPassportsListNewHookVisible()` → `true`.

### 2026-06-10 — `person-employee-tab-passports-new` lost after tab / nav UI re-render

- **Symptom**: Hook on `dxbl-adaptive-group` initially; gone after collection tab switch or XAF nav; toolbar re-renders as `div.dxbl-toolbar-item` without hook.
- **Cause**: JS interval stopped after first success; `scheduleReapply` skipped when hook thought present; shadow toolbar mutations invisible to body observer; finder missed `div.dxbl-toolbar-item[data-action-name="New"]` (only custom element tag).
- **Fix**: `startPassportsNestedPersistentWatch` (600ms, until Person detail deactivate) re-applies when hook missing; always start watch on ensure even when apply succeeds; finder includes `.dxbl-toolbar-item[data-action-name="New"]`.
- **Verify**: Passports → Educations → Passports → hook on New control; close/reopen employee detail → hook without manual tab round-trip.

### 2026-06-10 — `applyPersonDetailPassportsListNewActionTestId` true but hook absent in Elements

- **Symptom**: Console apply → `true`; inspected `dxbl-adaptive-group` / `dxbl-toolbar-item` on Passports tab has no `data-testid`; scenario fails.
- **Cause**: Verification used document-wide `isPassportsNestedListContext` with generic `"no data to display"` (every empty collection tab); hooked wrong/stale node outside visible Passports panel.
- **Fix**: Finder + verify scoped to `getVisiblePersonDetailPassportsTabRoot()` only; require Passports tab active; verify `isPassportsNewHookTarget`; tag adaptive-group + `dxbl-toolbar-item` + inner button; `isPersonDetailPassportsListNewHookVisible()` for honest check; runner waits on visible not apply alone.
- **Verify**: Passports tab → `apply…` → `true` AND `isPersonDetailPassportsListNewHookVisible()` → `true`; `deepQuery('[data-testid="person-employee-tab-passports-new"]')[0]` inside Passports panel.

### 2026-06-10 — `person-employee-tab-passports-new` flicker + missing on first Passports visit

- **Symptom**: DevTools Styles flicker on hook attrs; hook missing on first open of Passports tab; Educations → Passports restores it.
- **Cause**: MutationObserver watched `data-testid` → strip/tag feedback loop; concurrent `ScheduleEnsure` races; nested toolbar renders after first ensure; `dxbl-toolbar-item[data-action-name="New"]` not in finder.
- **Fix**: Idempotent apply (skip when hooked); remove `data-testid` from observer filter; interval stops when hooked; debounced C# ensure; burst refresh on tab attach/change; tag `dxbl-toolbar-item` directly.
- **Verify**: open employee → Passports (first visit) → hook without tab round-trip; DevTools attrs stable.

### 2026-06-10 — `person-employee-tab-passports-new` stripped on failed reapply (re-open / toolbar re-render)

- **Symptom**: Hook present on first open; gone after close+reopen with Passports tab restored; returns after tab switch; missing on new employee Passports tab.
- **Cause**: `applyActionTestId` for `usePassportsNestedNewFinder` called `stripPassportsNestedNewHooks` **before** finding candidates; MutationObserver/interval reapply during toolbar tear-down removed `data-testid` and left it off.
- **Fix**: Strip only after candidates found; `PersonDetailPassportsListNewE2eAnchorController` on `ListPropertyEditor` nested frame; JS 1.5s interval via `reapplyPassportsNestedNew()` (no tab-active gate); dedupe tab controller `ActiveTabIndexChanged` (anchor owns ensure).
- **Verify**: re-open employee with Passports selected → hook without tab switch; new employee → Passports tab → hook; `applyPersonDetailPassportsListNewActionTestId` stays `true` across close/reopen.

### 2026-06-10 — `person-employee-tab-passports-new` regression (missing on `:5001` and `:5052`)

- **Symptom**: `+ New` visible on Passports tab; no `data-testid` on `div.dxbl-adaptive-group` or inner button; JS-only path stopped working on IDE host too.
- **Cause**: (1) nested collection **New** is often the adaptive `div.dxbl-adaptive-group` with no inner `<button>`; (2) `closest()` / `innerText` context checks fail across open shadow roots; (3) tab-panel lookup via `#id` missed panels inside shadow trees.
- **Fix**: `PassportListViewE2eActionSelectorsController` — `NewObjectAction.CustomizeControl` + `E2eActionControlSelectorSupport` (primary, same as login Submit). JS — tag adaptive groups directly; `[id="…"]` deep lookup; tabpanel / active-tab fallbacks; parent-walk instead of `closest`.
- **Verify**: restart host → Passports tab → `data-testid="person-employee-tab-passports-new"` on adaptive group or `visa2026E2eHooks.applyPersonDetailPassportsListNewActionTestId('person-employee-tab-passports-new')` → `true`.

### 2026-06-08 — `person-employee-tab-passports-new` — scenario workaround: Save and Close + reopen

- **Symptom**: Hook missing on **first** Passports visit after create/save on new employee; present after close detail and reopen same employee.
- **Cause**: Nested `Passport_ListView` toolbar hook not applied on first detail mount post-save (CustomizeControl / toolbar re-render timing — see §2026-06-10 entries).
- **Workaround (v1)**: `person-employee-passport-create` YAML — `person-detail-employee-save` → `goto: /Person_ListView_Employees` (not SaveAndClose — absent on **new** detail) → `click-text: ${employeePersonalNumber}` → Passports tab.
- **Status**: Hook **verified after detail reopen** only; not claimed for first-mount post-create until render-time fix lands.

### 2026-06-08 — [-] Login `LanguageSwitcher` — not a combo; use DevExpress action selectors

- **Outcome**: anti-pattern → fixed (verified DevTools)
- **Symptom**: JS looked for `dxbl-combo-box` on `/LoginPage`; hook never applied; `.e2e-login-language-switcher` was `null`
- **DOM**: toolbar **SingleChoice** button with `data-action-name="LanguageSwitcher"`, class `language-switcher-test-container` (DevExpress test container)
- **Fix**: drop combo JS; hook Action `LanguageSwitcher` in `LogonViewE2eSelectorsController` via `CustomizeControl` + `E2eActionControlSelectorSupport`; manifest primary `[data-action-name="LanguageSwitcher"]`
- **Verify**: `document.querySelector('[data-action-name="LanguageSwitcher"]')?.click()` on `/LoginPage` — opens culture list (`.dxbl-listbox-item`)
