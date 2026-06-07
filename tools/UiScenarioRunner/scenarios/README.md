# Ready UI scenarios

**Only scenarios with map status `Ready for YAML` or `YAML authored`** live here — every hook in the map §3 table is **verified** in [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md) (or **waived**).

| Location | Contents |
|----------|----------|
| **`tools/UiScenarioRunner/scenarios/`** | **Ready** — `<id>_map.md` + `<id>.yaml` pairs |
| **`.cursor/skills/visa2026-ui-scenarios/examples/`** | **Draft** — `Draft`, `Hooks pending`, templates |

**Basename rule:** `login-smoke_map.md` + `login-smoke.yaml` (same `<scenario-id>`).

**Workflow:** [visa2026-ui-scenarios](../../../.cursor/skills/visa2026-ui-scenarios/SKILL.md) — Map → Hooks → YAML → **promote here** → run (runner planned).

**Do not** add draft maps or scenarios with missing hooks to this folder.

## Inventory

| Scenario id | Map status | Notes |
|-------------|------------|--------|
| `login-smoke` | Ready for YAML | Logon only |
