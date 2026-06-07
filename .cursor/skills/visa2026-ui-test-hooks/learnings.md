# Learnings (append-only): Visa2026 UI test hooks

Purpose: this skill **gets smarter over time**. Capture **verified** outcomes from implementing hooks, DevTools discovery, and stage smoke attempts. Agents **read before** similar work; **append after** a lesson is confirmed.

Keep **`SKILL.md`** stable; **promote** repeated lessons into **Known pitfalls** or [reference.md](./reference.md).

**Do not** delete or rewrite old entries — **append only**.

---

## How to use

**Before** adding hooks, debugging null `querySelector`, or planning Playwright stage smoke: skim **## Entries**.

**After** verified DOM discovery or confirmed dead end:

1. Append one entry using the template below.
2. Tag **Outcome**: `positive`, `negative`, or `anti-pattern`.
3. Update [registry.md](./registry.md) status when verify completes.
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
- **Verify** (DevTools query or Playwright):
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
| Stable family (3+ hooks) | Keep **registry.md** section; optional future `docs/E2E_UI_TEST_HOOKS.md` |

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
- **Goal**: Reliable console/Playwright locators
- **Try**: `SetAttribute("data-testid")` only vs also `InputId = testId`
- **Verify**: `#login-user-name` on `<input>` element
- **Result**: Set **both** `InputId` and `data-testid` in `E2eTextEditorSelectorApplicator`
- **Reuse**: Prefer `#slug` for text fields in scripts
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
