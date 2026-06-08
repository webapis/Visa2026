# UI scenario run lifecycle

Mandatory when an agent or developer **runs** a scenario locally (not when only authoring maps/YAML).

Canonical profile: [`Visa2026.Blazor.Server/Properties/launchSettings.json`](../../../Visa2026.Blazor.Server/Properties/launchSettings.json) → **`Visa2026 - UI Scenarios (LocalDB)`**.

---

## Dedicated host (do not reuse IDE)

| Setting | Value |
|---------|--------|
| **Launch profile** | `Visa2026 - UI Scenarios (LocalDB)` |
| **URL** | `http://localhost:5052` |
| **Database** | LocalDB `Visa2026` |
| **Env** | `VISA2026_UI_SCENARIOS=true` — disables XAF **RestoreTabbedMdiLayout** (no MDI tabs restored from prior runs) |
| **Why** | Isolated from IDE host (`:5000` / `:5001`), hook-verify host (`:5051`), and EasyTest — avoids stale DLLs, wrong model, and port locks |

**Rule:** Scenario runs use **only** `:5052` (or an explicit override documented in the map §2). Never point UiScenarioRunner at the Visual Studio debug host unless the user explicitly asks.

### Clean browser + shell state (each run)

| Layer | Mechanism |
|-------|-----------|
| **Playwright** | New browser + new context per scenario; `--incognito`; `ClearCookiesAsync()` before first step |
| **XAF TabbedMDI** | `RestoreTabbedMdiLayout = false` on scenario host — logon does **not** reopen tabs from `standarduser` model differences |
| **Interrupted headed run** | Close any leftover Chromium window manually; the next run launches a **new** process |

Without MDI restore disabled, repeated logins as the same user reopen old **Işgärler** / **Person** tabs and break `wait-for` / toolbar hooks even with a fresh browser.

---

## One run = build → start → run → stop

Each scenario execution follows this lifecycle:

```text
1. STOP   — kill any process on the scenario port (and read ui-scenario.pid if present)
2. BUILD  — fresh `dotnet build` of Blazor.Server to an isolated output folder
3. START  — launch dedicated host on :5052 with scenario connection string
4. WAIT   — HTTP ready on /LoginPage (or /)
5. RUN    — UiScenarioRunner with screenshots
6. STOP   — always stop the dedicated host when the run ends (pass or fail)
7. REVIEW — agent/developer inspects screenshot folder + console log
```

**Orchestration script (preferred):**

```powershell
.\scripts\local\Invoke-UiScenarioRun.ps1 -Scenario person-employee-create
```

| Switch | Use |
|--------|-----|
| `-KeepServer` | Leave host up for manual DevTools (exception only) |
| `-SkipBuild` | Re-run against existing `_scenario_build_out` DLL |
| `-SkipServer` | Runner only; host already on `-BaseUrl` |
| `-StopOnly` | Kill scenario host and exit |
| `-NoScreenshots` | Skip step screenshots (not recommended for agent runs) |

Append outcomes to [learnings.md](./learnings.md) after a verified run.

---

## Blazor wait discipline (critical)

XAF Blazor is **not** synchronous DOM. A hook can be **visible** while the app is still loading (spinner overlay, SignalR round-trip, MDI tab swap). Clicking too early produces **silent wrong navigation** — wrong tab, list still loading, Education instead of Employee detail, splash screen.

**Rule:** the runner must wait **before and after** navigation-changing actions, not only after.

Implementation in [`ScenarioRunner.cs`](../../../tools/UiScenarioRunner/ScenarioRunner.cs):

| Phase | Wait | Why |
|-------|------|-----|
| After `login` | `WaitForBlazorAsync` + `WaitForAppShellAsync` | Shell / nav must exist before `goto` |
| After `goto` | `WaitForBlazorAsync` + `WaitForAppShellAsync` + **`WaitForBusyOverlayAsync`** | Deep link returns before grid/detail is interactive |
| **Before every `click`** | **`WaitForBusyOverlayAsync`** | Do not click **New** / nav / **Save** under `.dxbl-loading-panel` |
| **After every `click`** | **`WaitForBusyOverlayAsync`** + `WaitForBlazorAsync` | Let Blazor process the action |
| After `*-new` clicks | extra 1.5s + **`WaitForBusyOverlayAsync` again** | List **New** opens detail asynchronously |
| After `person-detail-*-save` | `WaitForBlazorAsync` + optional `--pause-after-save` | Validation banner / persisted state |

`WaitForBusyOverlayAsync` targets `.dxbl-loading-panel` / `.dx-loadingpanel` and waits until **hidden** (up to 60s), then one Blazor beat.

### YAML authoring implications

| Pitfall | Symptom in screenshots | Do instead |
|---------|------------------------|------------|
| `wait-for` on toolbar **Save** after **New** | Wrong view (Save exists on list + many details) | `wait-for: person-first-name` (or another **detail-only** scalar) |
| `wait-for` on list **New** then immediate `click` | `step-04-click-after` still shows list + **Loading…** | Runner pre-click busy wait (built-in); increase `--timeout` if cold start |
| `goto` then `fill` without list settle | Splash or empty shell | Keep `wait-for: person-list-*-new` before **New** |
| `NetworkIdle` navigation | Multi-minute hang (SignalR) | `WaitUntilState.Load` only — see learnings |

### When authoring new runner steps

Any new step that triggers server round-trip (tab change, popup OK, lookup dropdown) should follow the same pattern:

```text
WaitForBusyOverlayAsync → action → WaitForBusyOverlayAsync → WaitForBlazorAsync
```

Document new wait requirements in the scenario map §5 **Run-time notes** and append [learnings.md](./learnings.md) when verified.

---

## Screenshot policy (tiered)

Full-page PNGs support agent self-review and developer sign-off. **Do not** screenshot every keystroke inside a `fill` step — that produces hundreds of images with little value.

| Mode | Flag | When | Files |
|------|------|------|--------|
| **Milestones** | `--screenshot-dir` only | CI / quick smoke | `{id}-before-save.png`, `{id}-after-save.png`, `{id}-failure.png` |
| **Steps** *(default for skill)* | `--screenshot-dir` + `--screenshot-steps` | Agent / local review | `{id}-step-{NN}-{kind}-before.png`, `…-after.png` per YAML step |
| **Fields** *(future)* | `--screenshot-fields` | Debugging flaky combos only | One pair per `fill` key |

**Default agent run:**

```powershell
.\scripts\local\Invoke-UiScenarioRun.ps1 -Scenario person-employee-create -Headed -SlowMo 1000
```

Screenshots land under:

```text
tools/UiScenarioRunner/screenshots/<scenario-id>/run-<yyyyMMdd-HHmmss>/
```

**Agent review checklist after run:**

1. Open the run folder; scan `step-*-before/after` in order.
2. Confirm navigation reached the intended view (not splash, wrong tab, or lookup admin page).
3. On failure, read `failure.png` and the last `step-*-after.png`.
4. Note fixes in `learnings.md` (port lock, wait timing, wrong user, layout duplicate, etc.).

---

## Runner CLI (manual)

When not using the script:

```powershell
# stop port 5052, then:
dotnet build Visa2026.slnx -c Debug
dotnet run --project Visa2026.Blazor.Server --launch-profile "Visa2026 - UI Scenarios (LocalDB)" --no-build
# in another shell:
dotnet run --project tools/UiScenarioRunner -- --scenario person-employee-create `
  --base-url http://localhost:5052 --timeout 90000 --headed --slow-mo 1000 `
  --screenshot-dir tools/UiScenarioRunner/screenshots/person-employee-create `
  --screenshot-steps --pause-after-save 5000
# stop port 5052 when done
```

---

## Map §2 fields

Every `*_map.md` should document:

| Field | Example |
|-------|---------|
| **Base URL** | `http://localhost:5052` |
| **Launch profile** | `Visa2026 - UI Scenarios (LocalDB)` |
| **Login user** | `standarduser` (empty password in dev) |
| **List path** | `/Person_ListView_Employees` |
| **Screenshot folder** | `tools/UiScenarioRunner/screenshots/<scenario-id>/` |

---

## Relation to hook verify

| Tool | Port | DB | Script |
|------|------|-----|--------|
| **VerifyUiTestHooks** | 5051 | `Visa2026HookVerify` | `Invoke-UiHookVerify.ps1` |
| **UiScenarioRunner** | 5052 | `Visa2026` | `Invoke-UiScenarioRun.ps1` |

Do not mix hosts or databases between the two flows.
