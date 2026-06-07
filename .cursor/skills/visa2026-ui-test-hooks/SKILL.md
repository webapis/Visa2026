---
name: visa2026-ui-test-hooks
description: >-
  Prepares Visa2026 Blazor UI element accessibility: stable CSS selector access
  (data-testid, InputId, e2e-*) on BO properties and controls from the live UI —
  implement hooks, verify in DevTools (or optional tools/VerifyUiTestHooks),
  record verified access in docs/UI_TEST_HOOKS.md. Not E2E, EasyTest, or scrapers.
disable-model-invocation: false
---

# Visa2026: UI test hooks — prepare selector accessibility

## Process

```text
1. DESCRIBE  — user names BO properties / UI elements to make accessible (view, member, tab, action)
2. CONFIGURE + TEST  — skill implements hooks; verify access + behavior in DevTools console
3. RECORD  — after tests pass, update docs/UI_TEST_HOOKS.md (+ registry.md)
```

| Step | Who | Outcome |
|------|-----|---------|
| **1. Describe** | User | Target list: e.g. `Person.FirstName`, logon `UserName`, tab `Passports`, action `Save` |
| **2. Configure + test** | Skill (+ user restarts app) | Hooks in code; DevTools `querySelector` non-null; behavior OK (focus, value, click) |
| **3. Record** | Skill | New rows in [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md); status **verified** in [registry.md](./registry.md) |

Optional: step 2 may use [`tools/VerifyUiTestHooks`](../../../tools/VerifyUiTestHooks/README.md) instead of manual console paste — same access + behavior bar; step 3 stays manual (verified-only inventory).

**Out of scope:** E2E suites, EasyTest, scrapers, CI. This skill only **prepares, tests, and documents** selector access from the UI.

---

## Purpose

Make business object properties, layout tabs, actions, and other controls reachable with **stable CSS selectors** (`#InputId`, `[data-testid]`, `.e2e-*`) from the **live Blazor UI** — not DevExpress `.dxbl-*` classes or localized captions.

**Where hooks live:** **`Visa2026.Blazor.Server`** (controllers, `Model.xafml`); optional **`[ModelDefault("CustomCSSClassName", …)]`** on **`Visa2026.Module`** BO properties.

---

## Scope

| Layer | Role | This skill? |
|-------|------|-------------|
| **UI accessibility prep** | Stable CSS selectors on real controls | **Yes** — implement + verify + record |
| **Verified access catalog** | [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md) | **Yes** — output of prep work (verified only) |
| **EasyTest E2E (dev/CI)** | `Visa2026.E2E.Tests` + Selenium | **No** — see [`docs/TESTING_PLAN.md`](../../../docs/TESTING_PLAN.md) |
| **Playwright / scrapers / E2E runners** | Separate tooling | **No** — may read `docs/UI_TEST_HOOKS.md`; not maintained by this skill |

**Term:** **UI test hook** = stable attribute/class on a real control.

| Doc | Role |
|-----|------|
| [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md) | **Verified-only** access inventory — DevTools-confirmed queries and behavior only |
| [registry.md](./registry.md) | Implementation — controller, mechanism, verify status |
| [reference.md](./reference.md) | Element type → hook mechanism |
| [learnings.md](./learnings.md) | Append-only verified lessons |
| [selectors.md](./selectors.md) | Redirect to `docs/UI_TEST_HOOKS.md` (do not duplicate tables here) |

**Related:** [on-prem-deploy/MATURITY.md](../on-prem-deploy/MATURITY.md) (READ → TRY → TEST → RECORD → PROMOTE).

---

## Continuous improvement (agent loop)

Maps to **Process** steps 2–3:

```text
READ learnings.md → CONFIGURE hooks → RESTART app → TEST in DevTools (access + behavior)
→ RECORD in docs/UI_TEST_HOOKS.md + registry.md → APPEND learnings.md if non-obvious
```

**Do not** append speculation — only verified DOM access or confirmed dead ends.

**`docs/UI_TEST_HOOKS.md` rule:** add or keep a selector row **only after** DevTools console confirms **access** (non-null query on the documented DOM target) **and** **behavior** (documented interaction works). Code in the repo without console verify stays in [registry.md](./registry.md) as **implemented**, not in `docs/UI_TEST_HOOKS.md`.

### Known pitfalls

| Pitfall | Do instead |
|---------|------------|
| Document selectors without DevTools verify | No row in `docs/UI_TEST_HOOKS.md` until access + behavior pass in console |
| Document selectors without implementing model/controller | Hook must exist in running app before verify |
| Logon `ViewController` only (no `CreateLogonWindowControllers`) | Register logon controller in `Visa2026BlazorApplication.CreateLogonWindowControllers` |
| Tab selectors via caption text (`"Passports"`) | Layout `LayoutGroup` **Id** + `BlazorLayoutManager.ItemCreated` → `HeaderCssClass` |
| `ModelDefault` / layout class only for tab **click** | `HeaderCssClass` on `DxFormLayoutTabPageModel`; verify **clickable** node in DevTools |
| Rely on `.dxbl-*` or `:nth-child` | Use `data-testid`, `#InputId`, or `.e2e-*` from this skill |
| Tag every BO field at once | Incremental hooks; verify each in console before next |
| Mark **verified** without behavior check | Both access and behavior must pass before row in `docs/UI_TEST_HOOKS.md` |

---

## Naming contract

| Artifact | Pattern | Example |
|----------|---------|---------|
| `data-testid` | `{area}-{target}` kebab-case | `login-user-name`, `person-employee-tab-passports` |
| `InputId` (text boxes) | same slug as test id | `#login-user-name` |
| CSS class | `e2e-{same-slug}` | `.e2e-person-first-name` |
| Tab strip | `{view-context}-tabs` | `person-employee-tabs` |
| Tab page | `{view-context}-tab-{layoutGroupId-kebab}` | `person-employee-tab-passports` |

**Stable ids:** XAF **ViewId**, layout **TabbedGroup** / **LayoutGroup** Id, **Action** Id, BO **member name** — not localized captions or MDI tab titles.

---

## Workflow: step 2 detail (configure + test one hook)

After the user **describes** the target (step 1):

1. **Read** [learnings.md](./learnings.md), [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md), [reference.md](./reference.md).
2. **Choose mechanism** (decision tree in reference).
3. **Implement** — reuse `E2eTextEditorSelectorApplicator` for string fields when possible.
4. **Build** Blazor.Server: `dotnet build Visa2026.Blazor.Server/Visa2026.Blazor.Server.csproj -c Debug`
5. **Restart** the app (hooks apply at runtime; hot reload may miss layout events).
6. **Test in DevTools** — access: query non-null; behavior: focus / value / click as expected.
7. **Record (step 3)** — add row to [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md) and set [registry.md](./registry.md) to **verified** only if step 6 passes.
8. **Append** [learnings.md](./learnings.md) if non-obvious.

---

## DevTools verify checklist (mandatory before done)

Open the target view → Chrome DevTools → **Console**.

### Step 1 — Access

```javascript
const el = document.querySelector('#person-first-name')
  ?? document.querySelector('[data-testid="person-first-name"]')
  ?? document.querySelector('.e2e-person-first-name');
console.log('access', el);
```

- [ ] Returns **non-null** for the intended node (input, tab header, or button).
- [ ] Node matches **DOM target** documented in [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md) (not a wrapper-only hit).

### Step 2 — Behavior

Run the **Expected behavior** snippet from [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md) for that row, e.g.:

```javascript
// Text input: focus + read/write
const input = document.querySelector('#login-user-name');
input.focus();
input.value = 'test';
console.log(input.value);

// Tab: click switches visible panel
document.querySelector('[data-testid="person-employee-tab-passports"]')?.click();

// Button: click triggers action (observe UI)
document.querySelector('[data-testid="login-submit"]')?.click();
```

- [ ] **Inputs:** `focus()` works; `value` readable (and writable if applicable).
- [ ] **Tabs:** click activates the correct tab content.
- [ ] **Actions:** click reaches the control (full logon side effects optional in local dev).

Hooks avoid localized captions ([`docs/LOCALIZATION_PLAN.md`](../../../docs/LOCALIZATION_PLAN.md)); console checks use English-stable ids only.

---

## Build when `bin/` is locked

```powershell
dotnet build Visa2026.Blazor.Server/Visa2026.Blazor.Server.csproj -c Debug -o _build_out/
```

See `.gitignore` (`_build_out/`, `_agent_build_out/`).

---

## Agent workflow

When the user **describes** BO properties or UI elements to make accessible (Process step 1):

1. Confirm targets: view Id, BO type, member / layout Id / action Id.
2. **Configure + test** (step 2): implement hooks; user **restarts app**; run DevTools checklist (or optional `VerifyUiTestHooks`).
3. **Record** (step 3): update **`docs/UI_TEST_HOOKS.md`** + **registry.md** only after tests pass.
4. Append **learnings.md** for non-obvious fixes.

Do **not** update the inventory before DevTools (or Playwright tool) confirms access + behavior.

---

## Phase 2 — optional Playwright verify (`tools/VerifyUiTestHooks`)

Automates the same **access + behavior** checks as DevTools (headless Chromium). **Does not** write to `docs/UI_TEST_HOOKS.md` — you still promote rows manually after a green run (verified-only policy).

| Step | Action |
|------|--------|
| 1 | App running locally (`Visa2026.Blazor.Server`, e.g. `https://localhost:5001`) |
| 2 | One-time: `dotnet build tools/VerifyUiTestHooks` + `playwright.ps1 install chromium` |
| 3 | Run verifier (see [tools/VerifyUiTestHooks/README.md](../../../tools/VerifyUiTestHooks/README.md)) |
| 4 | All **PASS** → add/update row in **`docs/UI_TEST_HOOKS.md`** + **registry.md** |

```powershell
# Login hooks only
dotnet run --project tools/VerifyUiTestHooks -- --scenario login

# Person tabs (copy detail URL from browser)
dotnet run --project tools/VerifyUiTestHooks -- --scenario person-employee-tabs `
  --start-url "/Person_DetailView_Employee/{guid}"
```

**Manifest:** `tools/VerifyUiTestHooks/hooks-manifest.json` — add checks when implementing new hooks (keep aligned with **registry.md**).

**When to use:** batch verify after several hooks, or to avoid manual console paste. Manual DevTools verify remains valid.

**Not:** CI E2E, EasyTest replacement, or auto-update of `docs/UI_TEST_HOOKS.md`.

**Next layer:** multi-step journeys → [visa2026-ui-scenarios](../visa2026-ui-scenarios/SKILL.md) — start with `<scenario-id>_map.md`; return here for missing hooks listed in map §3.

---

## Additional resources

- [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md) — **verified-only** element access (DevTools-confirmed)
- [reference.md](./reference.md) — element type → mechanism matrix
- [registry.md](./registry.md) — implementation registry
- [learnings.md](./learnings.md) — append-only experience
- [visa2026-ui-scenarios](../visa2026-ui-scenarios/SKILL.md) — YAML UI journeys using verified hook ids
- [`tools/VerifyUiTestHooks/README.md`](../../../tools/VerifyUiTestHooks/README.md) — optional Playwright batch verify (Phase 2)
