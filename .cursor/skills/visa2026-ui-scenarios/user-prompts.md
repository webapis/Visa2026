# UI scenarios — user prompts

Copy-paste messages to invoke [visa2026-ui-scenarios](./SKILL.md) in Cursor. Prefer **`@visa2026-ui-scenarios`** (or `@.cursor/skills/visa2026-ui-scenarios`) so the agent loads this skill.

**Not this skill:** implementing CSS hooks → [visa2026-ui-test-hooks](../visa2026-ui-test-hooks/SKILL.md); EasyTest / Selenium E2E → [`docs/TESTING_PLAN.md`](../../../docs/TESTING_PLAN.md).

**Process reminder:** **Map → Hooks → YAML → Promote → Run** — never skip the map; never put hook-pending drafts in `tools/UiScenarioRunner/scenarios/`.

**Hook catalog (read-only):** [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md) — scenario YAML uses **hook ids** from there, resolved via `tools/VerifyUiTestHooks/hooks-manifest.json`.

---

## Quick start

| You want… | Copy this |
|-----------|-----------|
| **First scenario ever** | `@visa2026-ui-scenarios Plan UI scenario **login-smoke** is already promoted. Walk me through **Map → Hooks → YAML → Promote → Run** using **login-smoke** as the ready example and **person-employee-minimal** as the draft.` |
| **New journey (safe default)** | `@visa2026-ui-scenarios Plan UI scenario **{scenario-id}**: {step 1} → {step 2} → …. Copy **_map_TEMPLATE.md** to **examples/{scenario-id}_map.md**, fill §1–§4, check every hook id against **UI_TEST_HOOKS.md**. Do **not** write YAML until map status is **Ready for YAML**.` |
| **Run what exists today** | `@visa2026-ui-scenarios Run **login-smoke** or **login-language-switch** via **Invoke-UiScenarioRun.ps1** on **:5052**.` |

---

## Verified hooks you can use in maps today

Use these **hook ids** in map §3 / YAML (not raw CSS). Status as of **2026-06-07** — re-check [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md) before promoting.

| Area | Hook ids (examples) | Typical step |
|------|---------------------|--------------|
| **Logon** | `login-user-name`, `login-password`, `login-submit`, `login-language-switcher` | `login:` / `click:` + `select-listbox-item:` (language switcher on `/LoginPage`) |
| **Nav — People** | `nav-people`, `nav-people-employees`, `nav-people-family-members`, `nav-people-temporary-visitors` | `click:` |
| **Nav — Application** | `nav-application`, `nav-application-direct-migration`, `nav-application-via-ministries` | `click:` |
| **Person list — Employees** | `person-list-employees-new`, `person-list-employees-delete` | `click:` |
| **Person list — Family** | `person-list-family-members-new`, `person-list-family-members-delete` | `click:` |
| **Person list — Temporary** | `person-list-temporary-visitors-new`, `person-list-temporary-visitors-delete` | `click:` |
| **Person detail — Employee save** | `person-detail-employee-save`, `person-detail-employee-save-and-close`, `person-detail-employee-save-and-new` | `click:` |
| **Person detail — Family save** | `person-detail-family-member-save`, `person-detail-family-member-save-and-close`, `person-detail-family-member-save-and-new` | `click:` |
| **Person detail — Temporary save** | `person-detail-temporary-visitor-save`, `person-detail-temporary-visitor-save-and-close`, `person-detail-temporary-visitor-save-and-new` | `click:` |
| **Person tabs** | `person-employee-tabs`, `person-employee-tab-passports`, `person-employee-tab-educations`, … | `select-tab:` |
| **Person scalars** | `person-first-name`, `person-last-name`, … (15 members) | `fill:` — **verified** on Employee / Family / Temporary detail |

**Still missing for full journeys:** ListView **row** hooks, optional gear-hidden fields, most Application detail actions.

---

## New scenario (start with map)

| Goal | User prompt |
|------|-------------|
| **Describe a journey** | `@visa2026-ui-scenarios Plan a UI scenario: **log in as Admin → nav to Employees → New → fill FirstName/LastName on employee detail → Save**. Start with **examples/person-employee-create_map.md** from **_map_TEMPLATE.md**.` |
| **Nav-only smoke** | `@visa2026-ui-scenarios Document **people-nav-smoke_map.md**: login → expand **nav-people** → click **nav-people-employees**, **nav-people-family-members**, **nav-people-temporary-visitors**. Hook inventory §3 only — no YAML until verified.` |
| **List toolbar smoke** | `@visa2026-ui-scenarios Plan **person-list-toolbar-smoke_map.md**: login → **goto** `/Person_ListView_Employees` → assert **person-list-employees-new** and **person-list-employees-delete** visible. Use hook ids only in §4.` |
| **Detail save smoke** | `@visa2026-ui-scenarios Plan **person-detail-save-smoke_map.md**: login → open known employee detail URL → fill **person-first-name** → **click** **person-detail-employee-save**. List §3 gaps vs **UI_TEST_HOOKS.md**.` |
| **Save and Close journey** | `@visa2026-ui-scenarios Plan scenario **person-employee-save-and-close**: login → employee detail (fixture GUID) → fill scalars → **click** **person-detail-employee-save-and-close**. Note Save and Close may live in split-button dropdown in DevTools.` |
| **Application nav journey** | `@visa2026-ui-scenarios Plan **application-nav-smoke_map.md**: login → **nav-application** → **nav-application-direct-migration** and **nav-application-via-ministries**. §3 hook table from **UI_TEST_HOOKS.md** Application nav section.` |
| **Tab switch journey** | `@visa2026-ui-scenarios Plan **person-employee-tabs-smoke_map.md**: login → employee detail URL → **select-tab** **person-employee-tab-passports** then **person-employee-tab-educations**. Waive row hooks.` |
| **Smoke only (login)** | `@visa2026-ui-scenarios Document a **login-only smoke** scenario. Compare draft **examples/** vs ready **login-smoke** in **tools/UiScenarioRunner/scenarios/**.` |
| **Named scenario id** | `@visa2026-ui-scenarios Create **application-create-smoke_map.md** for: login → Application ListView → New → pick type **101** → Save. Map §3 hook inventory first — expect gaps; hand off to **@visa2026-ui-test-hooks**.` |
| **From template** | `@visa2026-ui-scenarios Copy **_map_TEMPLATE.md** to **examples/my-scenario_map.md** and fill §1–§4 for: {describe steps}. Set status **Hooks pending** until §3 is clean.` |

---

## Refresh existing draft maps

| Goal | User prompt |
|------|-------------|
| **Update person-employee-minimal** | `@visa2026-ui-scenarios Refresh **examples/person-employee-minimal_map.md** §3: **person-detail-employee-save** is now verified in **UI_TEST_HOOKS.md**. Replace legacy **toolbar-save** row; add **click** Save to §4 when scalars are verified.` |
| **Extend minimal → create flow** | `@visa2026-ui-scenarios Fork **person-employee-minimal** into **person-employee-create_map.md**: add **nav-people-employees** → **person-list-employees-new** before fill/save. Keep draft in **examples/**.` |
| **Sync map after hook work** | `@visa2026-ui-scenarios I verified hooks **{hook-id-1}**, **{hook-id-2}** in DevTools. Update **{scenario-id}_map.md** §3 statuses and tell me if status can move to **Ready for YAML**.` |

---

## Hook gaps (hand off, then return)

| Goal | User prompt |
|------|-------------|
| **List gaps from map** | `@visa2026-ui-scenarios **person-employee-minimal** map §3 — which hooks are **missing**, **implemented**, or **verified**? Cross-check **UI_TEST_HOOKS.md** and **registry.md**.` |
| **Hand off to hooks skill** | `@visa2026-ui-scenarios Map **{scenario-id}** §3 shows hook gaps. Give me copy-paste **@visa2026-ui-test-hooks** prompts for each **missing** or **implemented** id.` |
| **Gap before YAML** | `@visa2026-ui-scenarios Before YAML for **{scenario-id}**: list every hook id in §4 not marked **verified** in **UI_TEST_HOOKS.md**. Do not author YAML until resolved or **waived** in §5.` |
| **Refresh map after hooks** | `@visa2026-ui-scenarios Hooks **person-list-employees-new** and **person-detail-employee-save** are verified. Update **{scenario-id}_map.md** §3 and set status when ready for YAML.` |

**Example handoff (filled):**

```text
@visa2026-ui-scenarios Map person-employee-minimal §3 still shows person-first-name
as implemented. Give me @visa2026-ui-test-hooks prompts to verify those scalars,
then I'll ask you to refresh the map.
```

---

## YAML and promote (hooks verified)

| Goal | User prompt |
|------|-------------|
| **Author YAML (draft)** | `@visa2026-ui-scenarios Map **person-employee-minimal** is **Ready for YAML**. Write **examples/person-employee-minimal.yaml** using hook ids only (no raw CSS). Match §4 exactly.` |
| **YAML with env GUID** | `@visa2026-ui-scenarios Author YAML for **{scenario-id}** with **env.personDetailPath** placeholder and document that runner needs **--start-url** or env **VISA2026_HOOK_VERIFY_PERSON_URL**.` |
| **Promote to runner folder** | `@visa2026-ui-scenarios Promote **login-smoke** (or **{id}**) — move **{id}_map.md** + **{id}.yaml** from **examples/** to **tools/UiScenarioRunner/scenarios/**.` |
| **Promote when ready** | `@visa2026-ui-scenarios **{scenario-id}** — all §3 hooks verified or waived. Promote map + yaml to **tools/UiScenarioRunner/scenarios/** and update **examples/README.md** if needed.` |
| **Pre-promote checklist** | `@visa2026-ui-scenarios Run the authoring checklist from **reference.md** on **{scenario-id}** before promote.` |

---

## Run scenarios (UiScenarioRunner)

App must be running (`Visa2026.Blazor.Server`, e.g. `http://localhost:5001`). **Restart host** after hook code changes.

| Goal | User prompt |
|------|-------------|
| **One scenario** | `@visa2026-ui-scenarios Run **login-smoke** with **UiScenarioRunner** (app on **http://localhost:5001**).` |
| **All ready scenarios** | `@visa2026-ui-scenarios Run **UiScenarioRunner --all** against **http://localhost:5001**.` |
| **Headed debug** | `@visa2026-ui-scenarios Run **login-smoke** with **--headed** so I can watch the browser.` |
| **With fixture URL** | `@visa2026-ui-scenarios Run **person-detail-save-smoke** (after promote) with **VISA2026_HOOK_VERIFY_PERSON_URL=/Person_DetailView_Employee/{guid}**.` |
| **After failed run** | `@visa2026-ui-scenarios **login-smoke** failed on step 2 — triage hook vs wait vs app not running; append **learnings.md** if fixed.` |
| **Hook verify vs scenario** | `@visa2026-ui-scenarios Explain when to use **Invoke-UiHookVerify.ps1** vs **UiScenarioRunner** for **person-list-employees-new**.` |

```powershell
dotnet run --project tools/UiScenarioRunner -- --scenario login-smoke --base-url http://localhost:5001
dotnet run --project tools/UiScenarioRunner -- --all --base-url http://localhost:5001
$env:VISA2026_HOOK_VERIFY_PERSON_URL = "/Person_DetailView_Employee/your-guid-here"
dotnet run --project tools/UiScenarioRunner -- --scenario person-detail-save-smoke
```

---

## CI / GitHub Actions

| Goal | User prompt |
|------|-------------|
| **Understand CI** | `@visa2026-ui-scenarios Which scenarios run on push? Explain **.github/workflows/ui-scenario-tests.yml**.` |
| **Add scenario to CI** | `@visa2026-ui-scenarios I promoted **{id}** to **scenarios/** — confirm **--all** will pick it up on the next push.` |
| **CI failure triage** | `@visa2026-ui-scenarios UI scenario CI failed on **{scenario-id}** — compare hook manifest vs **UI_TEST_HOOKS.md** and suggest fix.` |

---

## Maintain existing scenario

| Goal | User prompt |
|------|-------------|
| **Change steps** | `@visa2026-ui-scenarios Update **login-smoke** journey: add **wait-for** after logon. Sync **login-smoke_map.md** and **login-smoke.yaml**.` |
| **Add Save step** | `@visa2026-ui-scenarios Update **person-employee-minimal** YAML: after fill, **click** **person-detail-employee-save**. Sync map §1, §3, §4.` |
| **Rename scenario id** | `@visa2026-ui-scenarios Rename scenario **login.smoke** → **login-smoke** across map, yaml, and manifest if needed.` |
| **Retire scenario** | `@visa2026-ui-scenarios Remove draft **examples/old-scenario*** and document why in **learnings.md**.` |
| **Map/YAML drift** | `@visa2026-ui-scenarios **{scenario-id}** yaml steps don't match map §4 — reconcile both files.` |

---

## Inventory / read-only

| Goal | User prompt |
|------|-------------|
| **Draft vs ready** | `@visa2026-ui-scenarios List all scenario maps: which are in **examples/** (draft) vs **tools/UiScenarioRunner/scenarios/** (ready)?` |
| **Ready example** | `@visa2026-ui-scenarios Walk through **login-smoke_map.md** + yaml as the canonical ready scenario.` |
| **YAML step vocabulary** | `@visa2026-ui-scenarios What step types can I use in scenario yaml? (see **reference.md**)` |
| **Hooks for a journey** | `@visa2026-ui-scenarios Which verified hook ids from **UI_TEST_HOOKS.md** cover: login → Employees list → New → save employee detail?` |
| **What's blocked?** | `@visa2026-ui-scenarios What prevents **person-employee-minimal** from **Ready for YAML** right now?` |

---

## Prompts that look related but use a different skill

| User intent | Use instead |
|-------------|-------------|
| Add **data-testid** / CSS on a field or toolbar button | `@visa2026-ui-test-hooks` — see [user-prompts.md](../visa2026-ui-test-hooks/user-prompts.md) |
| DevTools verify only (no multi-step YAML) | `@visa2026-ui-test-hooks` + **Invoke-UiHookVerify.ps1** |
| Record verified hooks in **UI_TEST_HOOKS.md** | `@visa2026-ui-test-hooks` |
| **EasyTest** / Selenium E2E class | `Visa2026.E2E.Tests` — not ui-scenarios |
| Module unit tests | `@visa2026-unit-tests` |

---

## Minimal template (fill in blanks)

```text
@visa2026-ui-scenarios [Plan | YAML | Promote | Run] UI scenario **{scenario-id}**:
Journey: {step 1} → {step 2} → …
Hook ids (if known): {hook-id}, …
[Map only | hooks pending | ready for YAML | already in scenarios/]
[Optional: base URL http://localhost:5001, --headed, fixture GUID, hand off gaps to ui-test-hooks]
```

**Example — run person-employee-create (draft yaml):**

```text
@visa2026-ui-scenarios Run person-employee-create from examples/ after I set tenant lookup
env in person-employee-create.yaml (Gender, Nationality, ProjectContract, etc.).
App on http://localhost:5001. Append learnings.md with combo fill outcome.
```

**Example — promote person-employee-create:**

```text
@visa2026-ui-scenarios person-employee-create had a green local run.
Promote person-employee-create_map.md + person-employee-create.yaml to
tools/UiScenarioRunner/scenarios/.
```

**Example — refresh draft after hook work:**

```text
@visa2026-ui-scenarios person-detail-employee-save and person-list-employees-new
are verified in UI_TEST_HOOKS.md. Update person-employee-minimal_map.md §3–§5
and say whether we can mark Ready for YAML (person-first-name still pending verify).
```

**Example — run ready scenario:**

```text
@visa2026-ui-scenarios Run login-smoke with UiScenarioRunner;
Blazor Server is on http://localhost:5001.
```

**Example — promote:**

```text
@visa2026-ui-scenarios person-employee-minimal map is Ready for YAML.
Author examples/person-employee-minimal.yaml from §4, then promote both files
to tools/UiScenarioRunner/scenarios/ when I confirm green local run.
```
