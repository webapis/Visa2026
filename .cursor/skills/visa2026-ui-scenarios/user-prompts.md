# UI scenarios — user prompts

Copy-paste messages to invoke [visa2026-ui-scenarios](./SKILL.md) in Cursor. Prefer **`@visa2026-ui-scenarios`** (or `@.cursor/skills/visa2026-ui-scenarios`) so the agent loads this skill.

**Not this skill:** implementing CSS hooks → [visa2026-ui-test-hooks](../visa2026-ui-test-hooks/SKILL.md); EasyTest / Selenium E2E → [`docs/TESTING_PLAN.md`](../../../docs/TESTING_PLAN.md).

**Process reminder:** **Map → Hooks → YAML → Promote → Run** — never skip the map; never put hook-pending drafts in `tools/UiScenarioRunner/scenarios/`.

---

## New scenario (start with map)

| Goal | User prompt |
|------|-------------|
| **Describe a journey** | `@visa2026-ui-scenarios Plan a UI scenario: **log in as Admin → open employee Person detail → fill FirstName and LastName → Save**. Start with **person-employee-minimal_map.md** in **examples/**.` |
| **Smoke only** | `@visa2026-ui-scenarios Document a **login-only smoke** scenario (hook ids only). Check **UI_TEST_HOOKS.md** for gaps before YAML.` |
| **Named scenario id** | `@visa2026-ui-scenarios Create **application-create-smoke_map.md** for: login → Application ListView → New → pick type **101** → Save. Map §3 hook inventory first.` |
| **From template** | `@visa2026-ui-scenarios Copy **_map_TEMPLATE.md** to **examples/my-scenario_map.md** and fill §1–§4 for: {describe steps}.` |

---

## Hook gaps (hand off, then return)

| Goal | User prompt |
|------|-------------|
| **List gaps from map** | `@visa2026-ui-scenarios **person-employee-minimal** map §3 — which hooks are still **missing** or **implemented** (not verified)?` |
| **Hand off to hooks skill** | `@visa2026-ui-scenarios Map **person-employee-minimal** shows hook gaps. Give me **@visa2026-ui-test-hooks** prompts for each missing id in §3.` |
| **Refresh map after hooks** | `@visa2026-ui-scenarios Hooks for **person-first-name** are now verified in **UI_TEST_HOOKS.md**. Update **person-employee-minimal_map.md** §3 and set status when ready for YAML.` |

---

## YAML and promote (hooks verified)

| Goal | User prompt |
|------|-------------|
| **Author YAML (draft)** | `@visa2026-ui-scenarios Map **person-employee-minimal** is **Ready for YAML**. Write **examples/person-employee-minimal.yaml** using hook ids only (no raw CSS).` |
| **Promote to runner folder** | `@visa2026-ui-scenarios Promote **login-smoke** (or **{id}**) — move **{id}_map.md** + **{id}.yaml** from **examples/** to **tools/UiScenarioRunner/scenarios/**.` |
| **Promote when ready** | `@visa2026-ui-scenarios **person-employee-minimal** — all §3 hooks verified. Promote map + yaml to **tools/UiScenarioRunner/scenarios/**.` |

---

## Run scenarios (UiScenarioRunner)

App must be running (`Visa2026.Blazor.Server`, e.g. `http://localhost:5000`).

| Goal | User prompt |
|------|-------------|
| **One scenario** | `@visa2026-ui-scenarios Run **login-smoke** with **UiScenarioRunner** (app on localhost:5000).` |
| **All ready scenarios** | `@visa2026-ui-scenarios Run **UiScenarioRunner --all** against **http://localhost:5000**.` |
| **Headed debug** | `@visa2026-ui-scenarios Run **login-smoke** with **--headed** so I can watch the browser.` |
| **After failed run** | `@visa2026-ui-scenarios **login-smoke** failed on step 2 — triage hook vs wait vs app not running; append **learnings.md** if fixed.` |

```powershell
dotnet run --project tools/UiScenarioRunner -- --scenario login-smoke
dotnet run --project tools/UiScenarioRunner -- --all --base-url http://localhost:5000
```

---

## CI / GitHub Actions

| Goal | User prompt |
|------|-------------|
| **Understand CI** | `@visa2026-ui-scenarios Which scenarios run on push? Explain **.github/workflows/ui-scenario-tests.yml**.` |
| **Add scenario to CI** | `@visa2026-ui-scenarios I promoted **{id}** to **scenarios/** — confirm **--all** will pick it up on the next push.` |

---

## Maintain existing scenario

| Goal | User prompt |
|------|-------------|
| **Change steps** | `@visa2026-ui-scenarios Update **login-smoke** journey: add **wait-for** after logon. Sync **login-smoke_map.md** and **login-smoke.yaml**.` |
| **Rename scenario id** | `@visa2026-ui-scenarios Rename scenario **login.smoke** → **login-smoke** across map, yaml, and manifest if needed.` |
| **Retire scenario** | `@visa2026-ui-scenarios Remove draft **examples/old-scenario*** and document why in **learnings.md**.` |

---

## Inventory / read-only

| Goal | User prompt |
|------|-------------|
| **Draft vs ready** | `@visa2026-ui-scenarios List all scenario maps: which are in **examples/** (draft) vs **tools/UiScenarioRunner/scenarios/** (ready)?` |
| **Ready example** | `@visa2026-ui-scenarios Walk through **login-smoke_map.md** + yaml as the canonical ready scenario.` |
| **YAML step vocabulary** | `@visa2026-ui-scenarios What step types can I use in scenario yaml? (see **reference.md**)` |

---

## Prompts that look related but use a different skill

| User intent | Use instead |
|-------------|-------------|
| Add **data-testid** / CSS on a field | `@visa2026-ui-test-hooks` |
| Verify hooks in DevTools only | `@visa2026-ui-test-hooks` |
| **EasyTest** / Selenium E2E class | `Visa2026.E2E.Tests` — not ui-scenarios |
| Module unit tests | `@visa2026-unit-tests` |

---

## Minimal template (fill in blanks)

```text
@visa2026-ui-scenarios [Plan | YAML | Promote | Run] UI scenario **{scenario-id}**:
Journey: {step 1} → {step 2} → …
[Map only | hooks pending | ready for YAML | already in scenarios/]
[Optional: base URL, --headed, hand off hook ids to ui-test-hooks]
```

**Example (filled — new journey):**

```text
@visa2026-ui-scenarios Plan UI scenario person-employee-minimal:
Login as Admin → navigate to employee Person detail → fill person-first-name
and person-last-name → Save.
Start with examples/person-employee-minimal_map.md; do not write yaml until
§3 hooks are verified. Hand off missing hooks to @visa2026-ui-test-hooks.
```

**Example (filled — run ready scenario):**

```text
@visa2026-ui-scenarios Run login-smoke with UiScenarioRunner;
Blazor Server is on http://localhost:5000.
```
