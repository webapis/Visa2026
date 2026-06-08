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

**Preferred local run** (dedicated host, fresh build, step screenshots, auto stop):

```powershell
.\scripts\local\Invoke-UiScenarioRun.ps1 -Scenario person-employee-create -Headed
```

Uses launch profile **`Visa2026 - UI Scenarios (LocalDB)`** on **`http://localhost:5052`** (`VISA2026_UI_SCENARIOS=true` — no MDI tab restore). Each run uses a fresh Playwright browser context (incognito, cookies cleared). See [reference-run-lifecycle.md](../../.cursor/skills/visa2026-ui-scenarios/reference-run-lifecycle.md).

Manual runner only (host already running on :5052):

```powershell
dotnet run --project tools/UiScenarioRunner -- --scenario login-smoke --base-url http://localhost:5052
```

Options: `--base-url`, `--user`, `--password`, `--headed` (maximized full-width window), `--slow-mo`, `--timeout`, `--manifest`, `--screenshot-dir`, `--screenshot-steps`, `--pause-after-save`.

**YAML step kinds:** `goto`, `login`, `fill`, `click`, `select-tab`, `wait-for`, `assert-visible`, `select-listbox-item` (DevExpress `.dxbl-listbox-item` by display text; supports `${envKey}`).

Headed runs use the primary screen size (default 1920×1080). Override with env `VISA2026_SCENARIO_SCREEN=2560x1440`.

Screenshots: `--screenshot-dir` + `--screenshot-steps` → `{id}-step-{NN}-{kind}-before.png` / `-after.png` per YAML step; Save also writes `-before-save.png` / `-after-save.png`.
