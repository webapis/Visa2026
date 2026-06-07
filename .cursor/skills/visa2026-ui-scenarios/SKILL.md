---
name: visa2026-ui-scenarios
description: >-
  Plans and runs multi-step Blazor UI journeys: first <scenario-id>_map.md (proposed YAML +
  hook gap vs UI_TEST_HOOKS.md), then visa2026-ui-test-hooks for missing selectors, then
  <scenario-id>.yaml for Playwright. Not hook prep or EasyTest CI. Use for UI scenarios,
  stage smoke, Person fill flows, or scenario map authoring.
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
| **Execution** | `tools/UiScenarioRunner` *(planned)* | Playwright runs YAML |

**Out of scope:** implementing hooks ( **ui-test-hooks** ); EasyTest CI; scraping.

---

## Process (Map → Hooks → YAML)

```text
1. MAP    — user describes journey → <scenario-id>_map.md (proposed YAML + hook status table)
2. HOOKS  — gaps → visa2026-ui-test-hooks (configure + DevTools verify + UI_TEST_HOOKS.md)
3. YAML   — map status Ready for YAML → <scenario-id>.yaml from map § Proposed YAML
4. RUN    — UiScenarioRunner (planned) or manual Playwright
```

| Stage | Skill | Who starts | Output |
|-------|-------|------------|--------|
| **1. Map** | **visa2026-ui-scenarios** | User describes scenario | `<id>_map.md` |
| **2. Hooks** | **visa2026-ui-test-hooks** | Map §3 shows missing / implemented | Verified rows in `UI_TEST_HOOKS.md` |
| **3. YAML** | **visa2026-ui-scenarios** | All §3 hooks **verified** or **waived** | `<id>.yaml` |
| **4. Run** | **visa2026-ui-scenarios** | YAML exists | Test run + `learnings.md` |

**Do not** create `.yaml` before the map shows **Ready for YAML** ([reference-map-contract.md](./reference-map-contract.md)).

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
2. **Copy** [examples/_map_TEMPLATE.md](./examples/_map_TEMPLATE.md) → `<scenario-id>_map.md`.
3. **Fill §1–§4** — journey, navigation, **hook inventory** (check each id against `UI_TEST_HOOKS.md`), proposed YAML sketch.
4. **Set status** `Hooks pending` if any hook is **missing** or **implemented**.
5. **Hand off** §3 gaps to [visa2026-ui-test-hooks](../visa2026-ui-test-hooks/SKILL.md); update map §3 as hooks become **verified**.
6. When **Ready for YAML** → write `<scenario-id>.yaml` matching map §4 (hook ids only).
7. **Run** when runner exists; append [learnings.md](./learnings.md).

---

## Agent workflow

When the user asks for a **UI scenario** or **login + fill Person**:

1. **Do not** jump to `.yaml` — create **`<scenario-id>_map.md`** first.
2. Scan [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md) for each UI element in the journey; fill §3 status: **verified** | **implemented** | **missing** | **waived**.
3. Include **§4 Proposed YAML** sketch in the map.
4. If §3 has gaps → tell user to run **visa2026-ui-test-hooks** with the gap list from the map.
5. After hooks verified → update map to **Ready for YAML** → author **`.yaml`**.
6. Append **learnings.md** after a successful run.

---

## Continuous improvement

```text
MAP (_map.md) → HOOKS (ui-test-hooks) → YAML → RUN → APPEND learnings.md
```

### Known pitfalls

| Pitfall | Do instead |
|---------|------------|
| YAML before map | Write `_map.md` first |
| YAML before hooks verified | Wait for map **Ready for YAML** |
| Hook gaps only in chat | Table in map §3 + handoff to **ui-test-hooks** |
| Duplicate CSS in YAML | Hook ids only; see map §4 |
| Map and YAML out of sync | Update both when steps change |
| Confuse with EasyTest | EasyTest = CI; this skill = hook-based smoke |

---

## Planned tooling

| Path | Status |
|------|--------|
| `examples/<id>_map.md` + `<id>.yaml` | **Examples** + template |
| `tools/UiScenarioRunner/scenarios/` | **Planned** — authoritative map + yaml pairs |
| `tools/VerifyUiTestHooks/hooks-manifest.json` | **Exists** — shared hook resolution |

---

## Additional resources

- [reference-map-contract.md](./reference-map-contract.md) — **`*_map.md` contract (mandatory first step)**
- [reference.md](./reference.md) — YAML step vocabulary, URLs, runner layout
- [examples/_map_TEMPLATE.md](./examples/_map_TEMPLATE.md)
- [examples/person-employee-minimal_map.md](./examples/person-employee-minimal_map.md) — sample map
- [examples/person-employee-minimal.yaml](./examples/person-employee-minimal.yaml) — sample yaml (after hooks)
- [examples/login.smoke.yaml](./examples/login.smoke.yaml)
- [learnings.md](./learnings.md)
- [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md)
- [visa2026-ui-test-hooks](../visa2026-ui-test-hooks/SKILL.md)
- [`docs/TESTING_PLAN.md`](../../../docs/TESTING_PLAN.md)
