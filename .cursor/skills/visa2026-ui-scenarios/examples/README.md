# Draft UI scenarios

Work-in-progress **maps and YAML** for [visa2026-ui-scenarios](../SKILL.md). **Invoke prompts:** [user-prompts.md](../user-prompts.md).

| Status | Where files live |
|--------|------------------|
| **Draft**, **Hooks pending** | **This folder** (`examples/`) |
| **Ready for YAML**, **YAML authored** | [`tools/UiScenarioRunner/scenarios/`](../../../tools/UiScenarioRunner/scenarios/) |

**Do not** copy draft scenarios into `tools/UiScenarioRunner/scenarios/` until map §3 is all **verified** or **waived** and status is updated.

## Files here

| File | Status | Notes |
|------|--------|--------|
| `_map_TEMPLATE.md` | template | Copy for new scenarios |
| `person-employee-minimal_map.md` | Ready for YAML | Fill hooks verified; yaml still in **examples/** |
| `person-employee-minimal.yaml` | draft | Do not run until promoted |
| `person-employee-create_map.md` | Ready for YAML | Login → Employees nav → New → fill → Save |
| `person-employee-create.yaml` | draft | Set tenant lookup env before run; do not promote until green |
| `person-employee-create-staging.yaml` | staging-only | **Never promote** — unique personal number for persistent staging host; copy temporarily to `scenarios/` for `-SkipServer` runs |

**Ready example:** [`tools/UiScenarioRunner/scenarios/login-smoke*`](../../../tools/UiScenarioRunner/scenarios/).
