# UiScenarioRunner

Playwright runner for hook-based UI scenarios (YAML + [`docs/UI_TEST_HOOKS.md`](../docs/UI_TEST_HOOKS.md)).

| Piece | Path |
|-------|------|
| **Ready scenarios** | [`scenarios/`](./scenarios/) — `<id>_map.md` + `<id>.yaml` only when hooks verified |
| **Hook resolution** | [`../VerifyUiTestHooks/hooks-manifest.json`](../VerifyUiTestHooks/hooks-manifest.json) |
| **Draft scenarios** | [`.cursor/skills/visa2026-ui-scenarios/examples/`](../.cursor/skills/visa2026-ui-scenarios/examples/) |

Skill: [visa2026-ui-scenarios](../.cursor/skills/visa2026-ui-scenarios/SKILL.md).

**CI:** [`.github/workflows/ui-scenario-tests.yml`](../../.github/workflows/ui-scenario-tests.yml) runs `--all` on every push / pull request (Windows + LocalDB + Playwright).

## Local setup

```powershell
dotnet build tools/UiScenarioRunner/UiScenarioRunner.csproj -c Debug
powershell -ExecutionPolicy Bypass -File tools/UiScenarioRunner/bin/Debug/net8.0/playwright.ps1 install chromium
```

Start **Visa2026.Blazor.Server** (e.g. `http://localhost:5000`), then:

```powershell
dotnet run --project tools/UiScenarioRunner -- --scenario login-smoke
dotnet run --project tools/UiScenarioRunner -- --all --base-url http://localhost:5000
```

Options: `--base-url`, `--user`, `--password`, `--headed`, `--timeout`, `--manifest`.
