---
name: visa2026-ui-test-hooks
description: >-
  Prepares Visa2026 Blazor UI element accessibility: stable CSS selector access
  (data-testid, InputId, e2e-*) on BO direct/scalar properties (required /
  always-visible only), layout tabs, actions, and sidebar nav — not collections,
  optional gear-hidden fields, or hidden/computed members unless explicitly requested.
  Before hooking any target: classify mechanism family (A–F), match prior verified
  hooks in registry/reference/learnings, reuse that pattern; if none exists, discover
  via build/DevTools trial until success, then record. Verify in DevTools or
  VerifyUiTestHooks; record in docs/UI_TEST_HOOKS.md. Use when user asks for UI test
  hooks, CSS selectors, data-testid, e2e-*, hook Person/Application fields, verify
  selectors, or update UI_TEST_HOOKS.md. Not E2E, EasyTest, ui-scenarios YAML, or
  scrapers. See user-prompts.md.
disable-model-invocation: false
---

# Visa2026: UI test hooks — prepare selector accessibility

## Process

```text
1. DESCRIBE   — user names BO properties / UI elements (view, member, tab, action, nav Id)
2. CLASSIFY   — assign mechanism family A–F + stable id source; apply BO member scope rules
3. MATCH      — find prior hook for same family in registry / reference / learnings / code
4. CONFIGURE  — REUSE matched pattern OR DISCOVER (trial until DevTools access + behavior OK)
5. RECORD     — update docs/UI_TEST_HOOKS.md + registry.md; append learnings.md if non-obvious
```

| Step | Who | Outcome |
|------|-----|---------|
| **1. Describe** | User | Target list: scalar members, layout tab Id, action Id, nav item Id — not whole collections |
| **2. Classify** | Skill | One family **A–F** per target; stable id (member name, LayoutGroup Id, Action Id, nav Id, …) |
| **3. Match** | Skill | **Reuse candidate** found (verified example + controller/applicator) **or** **Discover** (no prior art / family F) |
| **4. Configure** | Skill | Hooks in code; **Reuse** = copy-extend only. **Discover** = hypothesize → build → DevTools → fix loop |
| **5. Record** | Skill | Verified rows in [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md); **verified** in [registry.md](./registry.md) |

Optional: step 4 uses [`scripts/local/Invoke-UiHookVerify.ps1`](../../../scripts/local/Invoke-UiHookVerify.ps1) (build → isolated host on `:5051` → `VerifyUiTestHooks` → stop) — **no developer IDE host required**. Manual DevTools against `-KeepServer` remains valid.

**Out of scope:** E2E suites, EasyTest, scrapers, CI. This skill only **prepares, tests, and documents** selector access from the UI.

---

## Before writing code: classify → match → reuse | discover

**Mandatory** after step 1 (describe), **before** any controller/model edit. Full checklist: [reference.md § Classify and match](./reference.md#classify-and-match-before-code).

### 2. Classify (family + stable id)

Answer for **each** target:

1. What is it on screen? (detail field, tab header, toolbar button, login control, sidebar nav leaf, grid cell, …)
2. Which **family A–F**? (decision tree in [reference.md](./reference.md))
3. What **stable id** hooks it? (BO member, `LayoutGroup` Id, Action Id, navigation item Id — **never** caption or `.dxbl-*`)
4. In scope per **BO member scope** below? (skip collections as fields, optional gear-hidden unless explicit, etc.)

State the classification briefly in the agent response (e.g. “`Application` nav leaf → **family E**, id `Application_ListView` → `nav-application-list`”).

### 3. Match (prior art)

Search **in order** — stop when you find a **verified** or **implemented** hook for the **same family**:

| Order | Where | Look for |
|-------|--------|----------|
| 1 | [registry.md](./registry.md) | Same family, mechanism, controller name, status **verified** |
| 2 | [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md) | Verified row + DevTools snippet for same family |
| 3 | [reference.md](./reference.md) | Element matrix row + code pattern for family |
| 4 | [learnings.md](./learnings.md) | Entries tagged same family / element (pitfalls + fixes) |
| 5 | Codebase | `Visa2026.Blazor.Server/Controllers/*E2e*` — mirror closest controller |

**Match found (same family, working pattern):** go to **Reuse** — do **not** re-research DevExpress APIs or invent a new controller type.

**No match, or family F (backlog):** go to **Discover**.

### 4a. Reuse path (default — minutes, not hours)

1. Copy the **verified** pattern from reference (files, events, applicator calls).
2. Extend only what differs: hook map row, member list, `LayoutGroup` Id, Action Id, test id slug.
3. Build → verify access + behavior (DevTools or `Invoke-UiHookVerify.ps1`).
4. Record if verify passes.

**Examples:** `Person.FirstName` → family **A** → reuse `PersonDetailViewE2eSelectorsController` + applicator. `Passports` tab → family **B** → reuse `PersonDetailViewE2eTabSelectorsController`. `Application` nav → family **E** → append `NavigationE2eHooks` only.

### 4b. Discover path (new family or first of its kind)

Use when **no** verified hook exists for that family (today: **F** grid/MDI, custom editors) or reuse failed after applying learnings.

```text
READ learnings.md (same element / family)
→ FORM hypothesis (which XAF/Blazor extension point — one at a time)
→ IMPLEMENT minimal hook for ONE target
→ BUILD ( _agent_build_out/ if bin/ locked )
→ RESTART host
→ DEVTools: access (non-null) then behavior (click/focus/value)
→ FAIL? diagnose (Elements panel, append learnings.md negative entry, adjust hypothesis)
→ REPEAT until access + behavior pass
→ PROMOTE: reference.md row + registry mechanism; next similar target uses Reuse path
```

**Rules for discover:**

- One target at a time until the pattern works.
- Do **not** document in `docs/UI_TEST_HOOKS.md` until DevTools confirms access + behavior.
- Append **learnings.md** after each confirmed fix or dead end (see learnings template).
- After **two** successful hooks of the same new pattern → add family row to reference matrix and **Known pitfalls** if applicable.

**Anti-pattern:** skipping classify/match and spiking a new WindowController when family **E** only needs a map row (see learnings: People nav ~1h API/debug, not four items).

---

## User prompts (how to invoke this skill)

**Copy-paste catalog:** [user-prompts.md](./user-prompts.md) — full table of messages for configure, verify, record, and inventory.

In Cursor, mention **`@visa2026-ui-test-hooks`** (or this skill path) so the agent loads these rules. Natural-language requests also match when they ask for **UI test hooks**, **`data-testid` / `e2e-*`**, or updates to **`docs/UI_TEST_HOOKS.md`**.

| You want… | Example prompt |
|-----------|----------------|
| Hook required fields on a BO | `@visa2026-ui-test-hooks Set up CSS selectors for all **required / always-visible** direct fields on **Person** (no collections, no optional gear-hidden).` |
| Hook one member | `@visa2026-ui-test-hooks Add hooks for **Person.FirstName** on employee detail.` |
| Hook a collection tab | `@visa2026-ui-test-hooks Hook the **Passports** tab on Person detail (`Tabs`).` |
| Optional field (opt-in) | `@visa2026-ui-test-hooks Also hook optional **Person.Email** (gear expanded for verify).` |
| Verify only | `@visa2026-ui-test-hooks Verify **nav-people** with **Invoke-UiHookVerify.ps1** (isolated server).` |
| Record after verify | `@visa2026-ui-test-hooks DevTools passed for login hooks — update **UI_TEST_HOOKS.md** and **registry.md** to verified.` |

**Wrong skill:** multi-step YAML journeys → [visa2026-ui-scenarios](../visa2026-ui-scenarios/SKILL.md).

---

## Purpose

Make **direct BO fields**, layout tabs, actions, and other controls reachable with **stable CSS selectors** (`#InputId`, `[data-testid]`, `.e2e-*`) from the **live Blazor UI** — not DevExpress `.dxbl-*` classes or localized captions.

**Where hooks live:** **`Visa2026.Blazor.Server`** (controllers, `Model.xafml`); optional **`[ModelDefault("CustomCSSClassName", …)]`** on **`Visa2026.Module`** BO **scalar** properties.

---

## BO member scope — what to hook

When the user asks to hook a business object (e.g. “all fields on `Person`”), configure UI accessibility for **direct scalar members that are required or always visible on the detail form** — not optional gear-hidden fields unless the developer **names them explicitly**.

| Hook? | BO member kind | Examples | Mechanism |
|-------|----------------|----------|-----------|
| **Yes (default)** | **Required / always-visible** direct scalar | `Person.FirstName`, `DateOfBirth`, `[ExcludeFromOptionalDetailFields]` members, required lookups | `{bo-kebab}-{member-kebab}` — controller + `E2ePropertySelectorApplicator` + optional `ModelDefault` |
| **No (default)** | **Optional detail fields** — behind gear on `[SupportsOptionalDetailFields]` types | `Person.MiddleName`, `Person.Email`, `Person.Photo`, `Person.IsArchived` | Skip unless developer **explicitly** requests that member; see [`docs/OPTIONAL_DETAIL_FIELDS.md`](../../../docs/OPTIONAL_DETAIL_FIELDS.md) |
| **No** | **Collections** — `IList<T>`, aggregated children | `Person.Passports`, `Educations`, `FamilyMembers` | Hook the **layout tab** (`LayoutGroup` Id under `TabbedGroup`), not grid rows — see tab controllers |
| **No** | **Computed / `[NotMapped]`** display | `Person.FullName`, `Person.Age` | Derived in code; no separate editor to target |
| **No** | **Hidden / validation-only** — `[Browsable(false)]`, rule helpers | `PersonRole`, `IsPersonalNumberUniqueAmongActive`, `RequiresRelationshipOnSave` | Not shown on detail UI |
| **No** | **UI chrome** (unless explicitly requested) | `ShowOptionalFields` gear toggle | Optional-detail machinery, not BO data |

**Optional vs required (how to decide):**

- Types with **`[SupportsOptionalDetailFields]`** ([`docs/OPTIONAL_DETAIL_FIELDS.md`](../../../docs/OPTIONAL_DETAIL_FIELDS.md)): hook **required** members (typically `[RuleRequiredField]` and/or `[ExcludeFromOptionalDetailFields]`). **Do not** hook optional gear-hidden scalars in a bulk “all fields” pass.
- **Explicit opt-in:** developer says e.g. “also hook `Person.Email`” or “all optional fields on Person” → then add those members only.
- **Verify optional hooks:** DOM may be absent until `ShowOptionalFields` is true — click gear (`.xaf-optional-fields-toggle`) or expand optional section before DevTools / Playwright checks.

**Rule:** one hook target per **real control** on the detail form. Collection data lives behind a **tab** → tab header hook; row/cell hooks are a separate backlog (ListView / grid).

**Bulk pattern (reference):** `{Bo}E2eMemberHooks` lists **in-scope** scalar members only; `{Bo}DetailViewE2eSelectorsController` applies `E2ePropertySelectorApplicator` to each. Example: `PersonE2eMemberHooks`, `PersonDetailViewE2eSelectorsController` — member list must respect optional-field exclusion unless developer opted in.

---

## UI element categories (mechanism families)

Categorize by **how** hooks are applied — not by business module. Same family → same controller pattern, same verify checklist, same doc section shape in [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md).

| Family | UI target | Stable id source | Controller / applicator | New hook effort |
|--------|-----------|------------------|-------------------------|-----------------|
| **A — Scalar property** | Detail text/date/lookup/bool/image | BO member name | `{Bo}DetailViewE2eSelectorsController` + `E2ePropertySelectorApplicator` + optional `{Bo}E2eMemberHooks` | Low per field (list + verify) |
| **B — Layout tab** | Collection tab header / strip | `LayoutGroup` Id under `TabbedGroup` | `{Bo}DetailViewE2eTabSelectorsController` + `BlazorLayoutManager.ItemCreated` | Low per tab (map Id → test id) |
| **C — Toolbar action** | Ribbon / toolbar button | Action Id | `ViewController` + `Action.CustomizeControl` | Low per action |
| **D — Logon** | Login fields + Log In | Member + Action Id | `LogonViewE2eSelectorsController` + **`CreateLogonWindowControllers`** | Once per app |
| **E — Sidebar nav** | Accordion nav group / leaf | Navigation item **Id** (+ `NavigateUrl` fallback) | **`NavigationE2eSelectorsController`** (one) + **`NavigationE2eHooks`** map | **Low per group** — extend map only |
| **F — Grid / MDI** | ListView row, MDI tab | TBD | Backlog | Spike first |

**Decision:** pick **one family** → open [reference.md § UI element categories](./reference.md#ui-element-categories-mechanism-families) — do **not** re-spike DevExpress APIs per business area.

### Sidebar nav (family E) — avoid repeat 1h work

People / Employees took long because of **wrong API** (`AccordionAdapter` vs `NavigationComponentAdapter`) and lookup bugs — not because each nav item needs custom code.

**Target pattern (after consolidation):**

1. **One** `NavigationE2eSelectorsController` (main window) — wires `ShowNavigationItemAction` → `NavigationComponentAdapter as DxAccordionAdapter` → `ItemHeaderTextTemplate` wrapper.
2. **One** hook registry (`NavigationE2eHooks.cs` or merged `{Area}NavigationE2eHooks`) — rows only:
   - `ChoiceActionItem.Id` → `data-testid` / `.e2e-*`
   - optional `NavigateUrl` → test id for leaves (`Person_ListView_Employees`, …)
3. **Support:** `NavigationE2eSelectorSupport` — parallel tree map + URL fallback (shared, do not duplicate).
4. **Adding Application / WorkPermit nav:** append Id + test id rows → rebuild → DevTools / `-Scenario nav-*` — **no new controller**.

**Current repo:** family **E** — `NavigationE2eSelectorsController` + `NavigationE2eHooks` + `NavigationE2eSelectorSupport` (People entries in hook map today).

**Naming:** `nav-{target-kebab}` from navigation item **Id** (`Employees` → `nav-people-employees` when grouped under People; `Application` → `nav-application`).

Full steps + pitfalls: [reference.md § Sidebar navigation](./reference.md#sidebar-navigation-family-e).

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
| [user-prompts.md](./user-prompts.md) | Copy-paste Cursor messages to invoke this skill |

**Related:** [on-prem-deploy/MATURITY.md](../on-prem-deploy/MATURITY.md) (READ → TRY → TEST → RECORD → PROMOTE).

---

## Continuous improvement (agent loop)

Maps to **Process** steps 4–5:

```text
CLASSIFY → MATCH → (Reuse | Discover loop) → TEST → RECORD → APPEND learnings.md
```

**Reuse branch:** MATCH found → extend map/controller → TEST → RECORD.

**Discover branch:**

```text
READ learnings.md → hypothesize extension point → minimal implement → BUILD → RESTART
→ DevTools access + behavior → on fail: diagnose + append learnings → retry
→ on success: RECORD + promote pattern to reference.md
```

**Do not** append speculation — only verified DOM access or confirmed dead ends.

**`docs/UI_TEST_HOOKS.md` rule:** add or keep a selector row **only after** DevTools console confirms **access** (non-null query on the documented DOM target) **and** **behavior** (documented interaction works). Code in the repo without console verify stays in [registry.md](./registry.md) as **implemented**, not in `docs/UI_TEST_HOOKS.md`.

### Known pitfalls

| Pitfall | Do instead |
|---------|------------|
| Skip **classify / match** and code immediately | State family A–F + cite reuse example **or** declare **Discover** path first |
| Re-spike DevExpress when family already verified | **Match** registry + reference; **Reuse** copy-extend only |
| Copy `PeopleNavigation*` controller for another nav group | Family **E**: extend `NavigationE2eHooks` map only |
| Document selectors without DevTools verify | No row in `docs/UI_TEST_HOOKS.md` until access + behavior pass in console |
| Document selectors without implementing model/controller | Hook must exist in running app before verify |
| Logon `ViewController` only (no `CreateLogonWindowControllers`) | Register logon controller in `Visa2026BlazorApplication.CreateLogonWindowControllers` |
| Tab selectors via caption text (`"Passports"`) | Layout `LayoutGroup` **Id** + `BlazorLayoutManager.ItemCreated` → `HeaderCssClass` |
| `ModelDefault` / layout class only for tab **click** | `HeaderCssClass` on `DxFormLayoutTabPageModel`; verify **clickable** node in DevTools |
| Rely on `.dxbl-*` or `:nth-child` | Use `data-testid`, `#InputId`, or `.e2e-*` from this skill |
| Hook **collection properties** (`IList<T>`) as BO fields | Hook **tab** `LayoutGroup` Id; grid/cell hooks are separate |
| Hook **optional gear-hidden** scalars in bulk | Skip unless developer **explicitly** names member(s) or asks for “optional fields” |
| Hook **computed / hidden** members | Only **required / always-visible** direct scalars; skip `[NotMapped]` / `[Browsable(false)]` |
| Tag every BO field at once | Incremental hooks per scalar member; verify each in console before next |
| Mark **verified** without behavior check | Both access and behavior must pass before row in `docs/UI_TEST_HOOKS.md` |
| New **sidebar nav group** → copy whole WindowController | Family **E**: extend **`NavigationE2eHooks`** map + verify; reuse shared controller/support |
| Use `AccordionAdapter` on nav control | Use **`NavigationComponentAdapter as DxAccordionAdapter`** |
| `document.querySelector` null on nav → assume shadow DOM | Hooks in **light DOM** under `.xaf-navmenu`; empty query usually means hook **not applied**, not piercing issue |

---

## Naming contract

| Artifact | Pattern | Example |
|----------|---------|---------|
| `data-testid` | `{area}-{target}` kebab-case | `login-user-name`, `person-employee-tab-passports` |
| `InputId` (text boxes) | same slug as test id | `#login-user-name` |
| CSS class | `e2e-{same-slug}` | `.e2e-person-first-name` |
| **BO scalar field** | `{bo-kebab}-{member-kebab}` | `Person.FirstName` → `person-first-name` |
| Tab strip | `{view-context}-tabs` | `person-employee-tabs` |
| Tab page | `{view-context}-tab-{layoutGroupId-kebab}` | `person-employee-tab-passports` |

**Stable ids:** XAF **ViewId**, layout **TabbedGroup** / **LayoutGroup** Id, **Action** Id, BO **member name** — not localized captions or MDI tab titles.

---

## Workflow: step 4 detail (configure + test one hook)

After steps **1–3** (describe, classify, match):

**If Reuse (matched prior art):**

1. Open the **verified** controller/applicator cited in reference or registry.
2. Extend hook map / member list / layout Id / test id slug only — no new mechanism.
3. **Build** Blazor.Server (agent default: `-o _agent_build_out/`).
4. **Verify** — `Invoke-UiHookVerify.ps1` or DevTools on `-KeepServer`.
5. **Record (step 5)** if access + behavior pass.

**If Discover (no prior art — e.g. family F):**

1. **Read** [learnings.md](./learnings.md) for related failures.
2. **Filter targets** — scope per **BO member scope** + [`docs/OPTIONAL_DETAIL_FIELDS.md`](../../../docs/OPTIONAL_DETAIL_FIELDS.md).
3. **Hypothesize** one extension point (decision tree in reference); implement **one** target.
4. **Build** → **restart** → **DevTools** access then behavior.
5. On failure: diagnose (Elements), **append learnings.md**, adjust; goto 3–4 — do not bulk-hook.
6. On success: **Record (step 5)**; add/update reference.md row so the next hook **Reuses**.
7. **Append** [learnings.md](./learnings.md) for non-obvious fixes.

**Shared verify (both paths):**

- Run [`Invoke-UiHookVerify.ps1`](../../../scripts/local/Invoke-UiHookVerify.ps1) when a manifest scenario exists, or manual DevTools.
- Do **not** update [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md) until access + behavior pass.

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

- [ ] **Optional fields:** if hooking a gear-hidden member, gear expanded (`ShowOptionalFields`) before access check.

Hooks avoid localized captions ([`docs/LOCALIZATION_PLAN.md`](../../../docs/LOCALIZATION_PLAN.md)); console checks use English-stable ids only.

---

## Build when `bin/` is locked

```powershell
dotnet build Visa2026.Blazor.Server/Visa2026.Blazor.Server.csproj -c Debug -o _agent_build_out/
```

See `.gitignore` (`_build_out/`, `_agent_build_out/`).

---

## Isolated verify server (agent default — step 2)

Do **not** rely on the developer’s IDE host (`:5000` / `:5001`). Use the script:

[`scripts/local/Invoke-UiHookVerify.ps1`](../../../scripts/local/Invoke-UiHookVerify.ps1)

| Step | Action |
|------|--------|
| 1 | One-time: `dotnet build tools/VerifyUiTestHooks` + `playwright.ps1 install chromium` |
| 2 | `.\scripts\local\Invoke-UiHookVerify.ps1 -Scenario login` (or `nav-people`, comma-separated, or omit for all) |
| 3 | All **PASS** → promote rows in **`docs/UI_TEST_HOOKS.md`** + **registry.md** |

What it does:

- Builds `Visa2026.Blazor.Server` → **`_agent_build_out/`**
- Starts host via launch profile **`Visa2026 - Hook Verify (LocalDB)`** (see `Properties/launchSettings.json`)
- **`http://localhost:5051`** — does not conflict with daily dev (`Visa2026 - LocalDB` on `:5000` / `:5001`)
- SQL: **LocalDB** database **`Visa2026HookVerify`** (separate from dev **`Visa2026`**)
- Sets **`FORCE_XAF_DB_UPDATE=true`** on first run
- Runs **`VerifyUiTestHooks`**, then **stops** the host (unless **`-KeepServer`**)

**IDE / manual DevTools:** run profile **`Visa2026 - Hook Verify (LocalDB)`** from Visual Studio or `dotnet run --launch-profile "Visa2026 - Hook Verify (LocalDB)"` (normal `bin/` output, not `_agent_build_out`).

**Keep in sync:** port `5051`, database `Visa2026HookVerify`, and `Invoke-UiHookVerify.ps1` `-LaunchProfile` must match `launchSettings.json`.

```powershell
# Login hooks
.\scripts\local\Invoke-UiHookVerify.ps1 -Scenario login

# People sidebar nav
.\scripts\local\Invoke-UiHookVerify.ps1 -Scenario nav-people

# Person detail (still needs a URL or seeded employee — see hooks-manifest)
.\scripts\local\Invoke-UiHookVerify.ps1 -Scenario person-scalar-fields `
  -StartUrl "/Person_DetailView_Employee/{guid}"

# Debug: leave server up for DevTools on :5051
.\scripts\local\Invoke-UiHookVerify.ps1 -Scenario login -KeepServer

# Stop a leftover server
.\scripts\local\Invoke-UiHookVerify.ps1 -StopOnly
```

**Prerequisites:** .NET 8, LocalDB (or Docker dev SQL for `-Profile DockerDev`), DevExpress license (same as CI build), Playwright Chromium once.

**Manual verify:** pass **`-SkipServer`** and **`-BaseUrl`** only when you intentionally target an already-running host.

---

## Agent workflow

When the user **describes** BO properties or UI elements to make accessible:

1. **Describe** — confirm view Id, BO type, member / layout Id / action Id / nav Id.
2. **Classify** — family **A–F** + stable id per target; apply **BO member scope** (skip collections-as-fields, optional gear-hidden unless explicit).
3. **Match** — search registry → `UI_TEST_HOOKS.md` → reference → learnings → `*E2e*` controllers; choose **Reuse** or **Discover**.
4. **Configure + test** — **Reuse:** copy-extend verified pattern. **Discover:** trial loop until DevTools access + behavior OK (`Invoke-UiHookVerify.ps1` or `-KeepServer`).
5. **Record** — `docs/UI_TEST_HOOKS.md` + **registry.md** **verified** only after step 4 passes.
6. **Append learnings.md** — required on Discover path when non-obvious; optional on Reuse if new pitfall found.

Do **not** update the inventory before DevTools (or Playwright) confirms access + behavior.

Do **not** start coding before steps 2–3 are explicit (family + reuse reference or discover declaration).

---

## Phase 2 — Playwright verify (`tools/VerifyUiTestHooks`)

Automates the same **access + behavior** checks as DevTools (headless Chromium). **Does not** write to `docs/UI_TEST_HOOKS.md` — promote rows manually after a green run.

**Preferred (agents):** [`scripts/local/Invoke-UiHookVerify.ps1`](../../../scripts/local/Invoke-UiHookVerify.ps1) — builds, starts isolated host, runs verifier, stops host.

**Advanced (host already running):**

```powershell
dotnet run --project tools/VerifyUiTestHooks -- --base-url http://localhost:5051 --scenario login
```

See [tools/VerifyUiTestHooks/README.md](../../../tools/VerifyUiTestHooks/README.md) for flags and manifest.

**Next layer:** multi-step journeys → [visa2026-ui-scenarios](../visa2026-ui-scenarios/SKILL.md) — start with `<scenario-id>_map.md`; return here for missing hooks listed in map §3.

---

## Additional resources

- [user-prompts.md](./user-prompts.md) — **copy-paste messages** to invoke this skill
- [`docs/OPTIONAL_DETAIL_FIELDS.md`](../../../docs/OPTIONAL_DETAIL_FIELDS.md) — required vs optional (gear) — **exclude optional from default hook scope**
- [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md) — **verified-only** element access (DevTools-confirmed)
- [reference.md](./reference.md) — element type → mechanism matrix
- [registry.md](./registry.md) — implementation registry
- [learnings.md](./learnings.md) — append-only experience
- [visa2026-ui-scenarios](../visa2026-ui-scenarios/SKILL.md) — YAML UI journeys using verified hook ids
- [`tools/VerifyUiTestHooks/README.md`](../../../tools/VerifyUiTestHooks/README.md) — optional Playwright batch verify (Phase 2)
