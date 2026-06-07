---
name: visa2026-ui-scenarios
description: >-
  Plans and runs multi-step Blazor UI journeys: <scenario-id>_map.md first (YAML +
  hook gap vs UI_TEST_HOOKS.md), then visa2026-ui-test-hooks for missing selectors,
  then YAML, promote to tools/UiScenarioRunner/scenarios/, run with UiScenarioRunner.
  Use for UI scenarios, scenario map, login smoke, Person fill flows, promote scenario,
  run login-smoke, UiScenarioRunner --all, or GitHub Actions ui-scenario-tests.
  Not hook CSS prep (ui-test-hooks) or EasyTest CI. See user-prompts.md.
disable-model-invocation: false
---

# Visa2026: UI scenarios

## Purpose

**Plan and run** multi-step UI **scenarios** (log in → open `Person` → fill fields → Save) using **hook ids** from [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md).

| Layer | Skill | Delivers |
|-------|-------|----------|
| **Scenario plan** | **This skill** | `<scenario-id>_map.md` — proposed YAML + hook availability |
| **Selector prep** | [visa2026-ui-test-hooks](../visa2026-ui-test-hooks/SKILL.md) | Verified hooks → `UI_TEST_HOOKS.md` |
| **Scenario file** | **This skill** | `<scenario-id>.yaml` — after all hooks verified |
| **Execution** | [`tools/UiScenarioRunner`](../../../tools/UiScenarioRunner/README.md) | Playwright runs YAML from **scenarios/** |

**Out of scope:** implementing hooks (**ui-test-hooks**); EasyTest CI; scraping.

---

## User prompts (how to invoke this skill)

**Copy-paste catalog:** [user-prompts.md](./user-prompts.md) — map, YAML, promote, run, CI, and maintenance messages.

In Cursor, mention **`@visa2026-ui-scenarios`** so the agent follows **Map → Hooks → YAML → Promote → Run**. Do not ask this skill to implement CSS hooks — use [visa2026-ui-test-hooks](../visa2026-ui-test-hooks/SKILL.md).

| You want… | Example prompt |
|-----------|----------------|
| Plan a new journey | `@visa2026-ui-scenarios Plan: login → Person detail → fill FirstName/LastName → Save. Start with **examples/person-employee-minimal_map.md**.` |
| Hook gap handoff | `@visa2026-ui-scenarios List §3 hook gaps in **person-employee-minimal** map; give **@visa2026-ui-test-hooks** prompts for missing ids.` |
| Write YAML when ready | `@visa2026-ui-scenarios Map status **Ready for YAML** — author **examples/{id}.yaml** (hook ids only).` |
| Promote | `@visa2026-ui-scenarios Promote **{id}** map + yaml to **tools/UiScenarioRunner/scenarios/**.` |
| Run locally | `@visa2026-ui-scenarios Run **login-smoke** with UiScenarioRunner (app on http://localhost:5000).` |
| Run all ready | `@visa2026-ui-scenarios Run **UiScenarioRunner --all**.` |

**Wrong skill:** `data-testid` / selector prep → **visa2026-ui-test-hooks**.

---

## Process (Map → Hooks → YAML)

```text
1. MAP    — user describes journey → <scenario-id>_map.md in examples/ (draft)
2. HOOKS  — gaps → visa2026-ui-test-hooks (configure + DevTools verify + UI_TEST_HOOKS.md)
3. YAML   — map status Ready for YAML → <scenario-id>.yaml (still in examples/ until promote)
4. PROMOTE — move <id>_map.md + <id>.yaml → tools/UiScenarioRunner/scenarios/ (ready only)
5. RUN    — UiScenarioRunner reads scenarios/ only
```

| Stage | Skill | Who starts | Output |
|-------|-------|------------|--------|
| **1. Map** | **visa2026-ui-scenarios** | User describes scenario | `<id>_map.md` |
| **2. Hooks** | **visa2026-ui-test-hooks** | Map §3 shows missing / implemented | Verified rows in `UI_TEST_HOOKS.md` |
| **3. YAML** | **visa2026-ui-scenarios** | All §3 hooks **verified** or **waived** | `<id>.yaml` in **examples/** (draft) |
| **4. Promote** | **visa2026-ui-scenarios** | Map **Ready for YAML** | **`tools/UiScenarioRunner/scenarios/`** |
| **5. Run** | **visa2026-ui-scenarios** | File in **scenarios/** | Test run + `learnings.md` |

**Do not** create `.yaml` before the map shows **Ready for YAML** ([reference-map-contract.md](./reference-map-contract.md)).

### Where files live

| Map status | `_map.md` + `.yaml` location |
|------------|------------------------------|
| **Draft**, **Hooks pending** | [examples/](./examples/) only |
| **Ready for YAML**, **YAML authored** | **[`tools/UiScenarioRunner/scenarios/`](../../../tools/UiScenarioRunner/scenarios/)** only |

Never put draft or hook-pending scenarios in **`tools/UiScenarioRunner/scenarios/`**.

---

## Scope

| Artifact | This skill? |
|----------|-------------|
| **`*_map.md`** | **Yes** — first deliverable per scenario |
| **`.yaml`** | **Yes** — after hooks ready |
| **`docs/UI_TEST_HOOKS.md`** | **Read**; update via **ui-test-hooks** only |
| **`hooks-manifest.json`** | **Read** (runner resolves ids) |
| **Hook implementation** | **No** → **ui-test-hooks** |

---

## Workflow: new scenario

1. **Read** [learnings.md](./learnings.md), [reference-map-contract.md](./reference-map-contract.md), [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md).
2. **Copy** [examples/_map_TEMPLATE.md](./examples/_map_TEMPLATE.md) → **`examples/<scenario-id>_map.md`**.
3. **Fill §1–§4** — journey, navigation, **hook inventory** (check each id against `UI_TEST_HOOKS.md`), proposed YAML sketch.
4. **Set status** `Hooks pending` if any hook is **missing** or **implemented**.
5. **Hand off** §3 gaps to [visa2026-ui-test-hooks](../visa2026-ui-test-hooks/SKILL.md); update map §3 as hooks become **verified**.
6. When **Ready for YAML** → write **`examples/<scenario-id>.yaml`** matching map §4.
7. **Promote** — move both files to **`tools/UiScenarioRunner/scenarios/`** (ready inventory only).
8. **Run** when runner exists; append [learnings.md](./learnings.md).

---

## Agent workflow

When the user asks for a **UI scenario** or **login + fill Person**:

1. **Do not** jump to `.yaml` — create **`<scenario-id>_map.md`** first.
2. Scan [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md) for each UI element in the journey; fill §3 status: **verified** | **implemented** | **missing** | **waived**.
3. Include **§4 Proposed YAML** sketch in the map.
4. If §3 has gaps → tell user to run **visa2026-ui-test-hooks** with the gap list from the map.
5. After hooks verified → map **Ready for YAML** → author **`examples/<id>.yaml`** → **promote** to **`tools/UiScenarioRunner/scenarios/`**.
6. Append **learnings.md** after a successful run.

---

## Continuous improvement

```text
MAP (examples/) → HOOKS → YAML (examples/) → PROMOTE (scenarios/) → RUN → learnings.md
```

### Known pitfalls

| Pitfall | Do instead |
|---------|------------|
| YAML before map | Write `_map.md` first in **examples/** |
| YAML before hooks verified | Wait for map **Ready for YAML** |
| Draft scenario in `tools/UiScenarioRunner/scenarios/` | Keep drafts in **examples/** only |
| Ready scenario left in `examples/` | **Promote** map + yaml to **scenarios/** |
| Hook gaps only in chat | Table in map §3 + handoff to **ui-test-hooks** |
| Duplicate CSS in YAML | Hook ids only; see map §4 |
| Map and YAML out of sync | Update both when steps change |
| Confuse with EasyTest | EasyTest = CI; this skill = hook-based smoke |

---

## Tooling

| Path | Role |
|------|------|
| [examples/](./examples/) | **Draft** — `_map_TEMPLATE.md`, Hook-pending maps + yaml |
| [`tools/UiScenarioRunner/scenarios/`](../../../tools/UiScenarioRunner/scenarios/) | **Ready only** — promoted `<id>_map.md` + `<id>.yaml` |
| [`tools/UiScenarioRunner/README.md`](../../../tools/UiScenarioRunner/README.md) | Runner — reads **scenarios/**; `--scenario` / `--all` |
| `tools/VerifyUiTestHooks/hooks-manifest.json` | Hook id → selectors |

---

## Additional resources

- [user-prompts.md](./user-prompts.md) — **copy-paste messages** to invoke this skill
- [reference-map-contract.md](./reference-map-contract.md) — **`*_map.md` contract (mandatory first step)**
- [reference.md](./reference.md) — YAML step vocabulary, URLs, runner layout
- [examples/README.md](./examples/README.md) — draft vs ready folders
- [examples/_map_TEMPLATE.md](./examples/_map_TEMPLATE.md)
- [examples/person-employee-minimal_map.md](./examples/person-employee-minimal_map.md) — draft (Hooks pending)
- [examples/person-employee-minimal.yaml](./examples/person-employee-minimal.yaml) — draft
- [`tools/UiScenarioRunner/scenarios/login-smoke_map.md`](../../../tools/UiScenarioRunner/scenarios/login-smoke_map.md) — ready example
- [`tools/UiScenarioRunner/scenarios/login-smoke.yaml`](../../../tools/UiScenarioRunner/scenarios/login-smoke.yaml)
- [learnings.md](./learnings.md)
- [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md)
- [visa2026-ui-test-hooks](../visa2026-ui-test-hooks/SKILL.md)
- [`docs/TESTING_PLAN.md`](../../../docs/TESTING_PLAN.md)
