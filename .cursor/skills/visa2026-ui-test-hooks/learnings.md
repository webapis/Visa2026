# Learnings (append-only): Visa2026 UI test hooks

Purpose: this skill **gets smarter over time**. Capture **verified** outcomes from implementing hooks and **DevTools console** access/behavior checks. Agents **read before** similar work; **append after** a lesson is confirmed.

This skill **prepares UI element accessibility** with CSS selectors from the live UI — it does **not** run E2E suites, EasyTest, or scrapers.

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
