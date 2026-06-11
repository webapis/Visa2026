# UiScenarioRunner

Playwright runner for hook-based UI scenarios (YAML + [`docs/UI_TEST_HOOKS.md`](../docs/UI_TEST_HOOKS.md)).

| Piece | Path |
|-------|------|
| **Ready scenarios** | [`scenarios/`](./scenarios/) — `<id>_map.md` + `<id>.yaml` only when hooks verified |
| **Hook resolution** | [`../VerifyUiTestHooks/hooks-manifest.json`](../VerifyUiTestHooks/hooks-manifest.json) |
| **Draft scenarios** | [`.cursor/skills/visa2026-ui-scenarios/examples/`](../.cursor/skills/visa2026-ui-scenarios/examples/) |

Skill: [visa2026-ui-scenarios](../.cursor/skills/visa2026-ui-scenarios/SKILL.md).

**CI:** [`.github/workflows/ui-scenario-tests.yml`](../../.github/workflows/ui-scenario-tests.yml) runs `--all` on every push / pull request. Produces artifact **`ui-scenario-report`** (JUnit, HTML, JSON, screenshots). On green **`main`** push, publishes to GitHub Pages **`test-reports/latest/`** (enable Pages from branch **`gh-pages`** once).

## Local setup

```powershell
dotnet build tools/UiScenarioRunner/UiScenarioRunner.csproj -c Debug
powershell -ExecutionPolicy Bypass -File tools/UiScenarioRunner/bin/Debug/net8.0/playwright.ps1 install chromium
```

**Preferred local run** — [`Invoke-UiScenarioRun.ps1`](../../scripts/local/Invoke-UiScenarioRun.ps1) (dedicated host, fresh build, step screenshots, auto stop):

```powershell
# Single scenario, reuse existing scenario DB
.\scripts\local\Invoke-UiScenarioRun.ps1 -Scenario login-smoke

# Deterministic single run
.\scripts\local\Invoke-UiScenarioRun.ps1 -Scenario person-employee-create -FreshDatabase -Headed

# Full suite (fresh DB each time)
.\scripts\local\Invoke-UiScenarioRun.ps1 -All -FreshDatabase

# Faster suite (after New-UiScenarioBaselineSnapshot.ps1)
.\scripts\local\Invoke-UiScenarioRun.ps1 -All -UseBaselineSnapshot

# Fast iteration (no step screenshots, shorter settle delays, no headed slow-mo)
.\scripts\local\Invoke-UiScenarioRun.ps1 -Scenario person-employee-passport-create -Fast -SkipBuild -KeepServer
```

Uses launch profile **`Visa2026 - UI Scenarios (LocalDB)`** on **`http://localhost:5052`** (`VISA2026_UI_SCENARIOS=true` — no MDI tab restore). Database: LocalDB **`Visa2026UiScenario`** (lookup baseline — use `-FreshDatabase` or `-All` for a clean seed). Optional speed-up: [baseline/README.md](./baseline/README.md). Each run uses a fresh Playwright browser context (incognito, cookies cleared). See [reference-run-lifecycle.md](../../.cursor/skills/visa2026-ui-scenarios/reference-run-lifecycle.md).

Manual runner only (host already running on :5052):

```powershell
dotnet run --project tools/UiScenarioRunner -- --scenario login-smoke --base-url http://localhost:5052
```

Options: `--base-url`, `--user`, `--password`, `--headed` (maximized full-width window), `--slow-mo`, `--fast` (shorter default step timeout caps when YAML omits `timeout`), `--timeout`, `--manifest`, `--screenshot-dir`, `--screenshot-steps`.

**YAML step kinds:** `goto`, `login`, `fill`, `click`, `select-tab`, `wait-for`, `assert-visible`, `select-listbox-item` (DevExpress `.dxbl-listbox-item` by display text; supports `${envKey}`), `click-text` (exact visible text — e.g. open ListView row by `PersonalNumber`; supports `${envKey}`).

**Per-step wait timeout** — `wait-for`, `assert-visible`, `click`, `select-tab`, and `click-text` accept an optional `timeout` (ms). Default is the runner `--timeout` (90s via `Invoke-UiScenarioRun.ps1`, 30s via raw `dotnet run`). This is a **maximum condition-wait** (succeeds early when the hook/text is ready); the runner does not inject fixed sleeps. Save milestone screenshots wait for the loading overlay to hide before `-after-save.png`.

```yaml
  - wait-for: person-first-name
  - wait-for:
      hook: person-employee-tabs
      timeout: 20000
  - click: person-list-employees-new
  - click:
      hook: person-employee-tab-passports-new
      timeout: 45000
  - select-tab: person-employee-tab-passports
  - select-tab:
      hook: person-employee-tab-passports
      timeout: 15000
  - click-text: ${employeePersonalNumber}
  - click-text:
      text: ${employeePersonalNumber}
      timeout: 20000
  - assert-visible:
      hook: person-employee-tab-passports-new
      timeout: 45000
```

Headed runs use the primary screen size (default 1920×1080). Override with env `VISA2026_SCENARIO_SCREEN=2560x1440`.

Screenshots: `--screenshot-dir` + `--screenshot-steps` → `{id}-step-{NN}-{kind}-before.png` / `-after.png` per YAML step; Save also writes `-before-save.png` / `-after-save.png`.
