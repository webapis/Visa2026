# UI scenario `*_map.md` contract

**Blocking:** Do **not** author **`<scenario-id>.yaml`** until the map shows **all required hooks verified** in [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md) (or user explicitly waives a step).

**Canonical workflow:** [SKILL.md](./SKILL.md) — **Map → Hook prep → YAML**.

Copy **`examples/_map_TEMPLATE.md`** when starting a new scenario.

---

## Co-located files

| File | When | Role |
|------|------|------|
| **`<scenario-id>_map.md`** | **First** | Planned YAML + hook gap analysis |
| **`<scenario-id>.yaml`** | **After** hooks verified | Executable scenario (hook ids only) |

### Folder rules

| Map status | Location |
|------------|----------|
| **Draft**, **Hooks pending** | [`.cursor/skills/visa2026-ui-scenarios/examples/`](./examples/) |
| **Ready for YAML**, **YAML authored** | **[`tools/UiScenarioRunner/scenarios/`](../../../tools/UiScenarioRunner/scenarios/)** only |

**Basename rule:** same stem in the same folder — e.g. `login-smoke_map.md` + `login-smoke.yaml`.

**Promote:** when map §3 is all **verified** or **waived**, set status **Ready for YAML**, write yaml if needed, **move both files** from `examples/` to `tools/UiScenarioRunner/scenarios/`. Do not copy drafts into `scenarios/`.

---

## Workflow (mandatory order)

```text
1. MAP     — user describes journey → write <id>_map.md (proposed YAML + hook status table)
2. HOOKS   — gaps → visa2026-ui-test-hooks (configure + DevTools verify + UI_TEST_HOOKS.md)
3. YAML    — when map hook table all ✅ verified → write <id>.yaml in examples/
4. PROMOTE  — move <id>_map.md + <id>.yaml → tools/UiScenarioRunner/scenarios/
5. RUN      — UiScenarioRunner (planned) reads scenarios/ only
```

| Stage | Skill | Deliverable |
|-------|-------|-------------|
| **1. Map** | **visa2026-ui-scenarios** | `<id>_map.md` |
| **2. Hooks** | **visa2026-ui-test-hooks** | `docs/UI_TEST_HOOKS.md` + registry |
| **3. YAML** | **visa2026-ui-scenarios** | `<id>.yaml` |

Update the map **hook status table** after each hook verify (stage 2).

---

## Required map sections

| § | Title | Content |
|---|--------|---------|
| **0** | Header | Scenario id, status (`Draft` \| `Hooks pending` \| `Ready for YAML` \| `YAML authored`), date |
| **1** | Journey | User goal in plain language (BO, views, outcome) |
| **2** | Navigation | Paths, fixture data (user, Person OID, env vars) |
| **3** | Hook inventory | Table: hook id, UI target, required for step, status vs `UI_TEST_HOOKS.md` |
| **4** | Proposed YAML | Sketch of final `.yaml` (hook ids only; may use `TODO` hooks while `Hooks pending`) |
| **5** | Blockers | Missing hooks, grid/MDI gaps, runner limitations |
| **6** | Changelog | Date + note |

---

## Hook status values (§3)

| Status | Meaning | Next action |
|--------|---------|-------------|
| **verified** | In `docs/UI_TEST_HOOKS.md` | None — ready for YAML |
| **implemented** | Code only; not in inventory | **ui-test-hooks** DevTools verify |
| **missing** | Not hooked | **ui-test-hooks** configure + verify |
| **waived** | Scenario works without hook (e.g. `goto` URL only) | Document why in §5 |

**Ready for YAML:** every row in §3 is **verified** or **waived**.

---

## Agent rules

| Situation | Action |
|-----------|--------|
| User asks for new scenario | Create **`<id>_map.md`** first — not `.yaml` |
| Map §3 has **missing** / **implemented** | Hand off list to **visa2026-ui-test-hooks** |
| All hooks **verified** | Set status **Ready for YAML**; author **`<id>.yaml`** from §4 |
| Hook added during prep | Update map §3 status + `UI_TEST_HOOKS.md` |
| YAML uses hook not in map §3 | Fix map or YAML — keep in sync |

---

## Handoff to visa2026-ui-test-hooks

From map §3, copy rows where status ≠ **verified**:

```markdown
Prepare hooks for scenario `<scenario-id>`:
- Person.FirstName → hook id `person-first-name` (missing)
- Action Save → hook id `toolbar-save` (missing)
View: Person_DetailView_Employee
```

After prep, return to this skill for YAML.

---

## Template

See [examples/_map_TEMPLATE.md](./examples/_map_TEMPLATE.md).

Example: [person-employee-minimal_map.md](./examples/person-employee-minimal_map.md).
