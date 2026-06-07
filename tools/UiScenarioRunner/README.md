# UiScenarioRunner

Playwright runner for hook-based UI scenarios (YAML + [`docs/UI_TEST_HOOKS.md`](../docs/UI_TEST_HOOKS.md)).

**Status:** runner code **planned**; ready scenario files live in **[scenarios/](./scenarios/)**.

| Piece | Path |
|-------|------|
| **Ready scenarios** | [`scenarios/`](./scenarios/) — `<id>_map.md` + `<id>.yaml` only when hooks verified |
| **Hook resolution** | [`../VerifyUiTestHooks/hooks-manifest.json`](../VerifyUiTestHooks/hooks-manifest.json) |
| **Draft scenarios** | [`.cursor/skills/visa2026-ui-scenarios/examples/`](../.cursor/skills/visa2026-ui-scenarios/examples/) |

Skill: [visa2026-ui-scenarios](../.cursor/skills/visa2026-ui-scenarios/SKILL.md).

When implemented:

```powershell
dotnet run --project tools/UiScenarioRunner -- --scenario login-smoke
```
