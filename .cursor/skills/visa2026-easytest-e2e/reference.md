# visa2026-easytest-e2e — reference

## Stack summary

| Item | Value |
|------|--------|
| Skill | **visa2026-easytest-e2e** |
| Project | `Visa2026.E2E.Tests` |
| Port | **5050** |
| DB | **Visa2026EasyTest** / Postgres `visa2026_easytest` |
| Drivers | **EasyTest** (default) + **Playwright** (custom / unsupported surfaces) |
| Selectors (EasyTest) | **Captions** / EasyTest actions (`FillFormWithRetry` may fall back to `InputId` / `data-testid`) |
| Selectors (Playwright) | **`data-testid`**, role, stable CSS (e.g. `#visa-preview-slot`) |
| Config | **EasyTest** |
| CI | `.github/workflows/e2e-tests.yml` |

---

## Host and driver

### Launch profile (`Properties/launchSettings.json`)

Host: Postgres **`visa2026_easytest`** on `:5050` (not IDE `:5000`). Legacy launch-profile name may still say LocalDB — ignore for DB.

- `applicationUrl`: `http://localhost:5050`
- `ConnectionStrings__DefaultConnection`: `Database=Visa2026EasyTest`
- `VISA2026_EASYTEST`: `true` (optional; host also detects DB name)

### `E2ETestBase` registration

```csharp
// physicalPath must be the built .exe — not the project folder (dotnet run honors launch profiles; the .exe does not).
string hostExe = EasyTestHostLaunch.ResolveHostExecutable(blazorServerProjectPath);

new BlazorApplicationOptions(
    name: "Visa2026Blazor",
    physicalPath: hostExe,
    url: "http://localhost:5050",
    configuration: "EasyTest",
    arguments: EasyTestHostLaunch.HostArguments, // --urls http://localhost:5050 --environment Development
    browser: "Edge",
    runHeadless: EasyTestBrowserMode.RunHeadless,
    webDriverPath: ResolveWebDriverDirectory())
```

**Blazor.Server (EasyTest build):** reference `DevExpress.ExpressApp.EasyTest.BlazorAdapter` (conditional on `Configuration==EasyTest`, `EASYTEST` define) and ship `appsettings.EasyTest.json` (`Visa2026EasyTest` connection string).

### Headed vs headless

`EasyTestBrowserMode.RunHeadless` (see `EasyTestBrowserMode.cs`):

| Variable | Effect |
|----------|--------|
| *(none)* on dev PC | Headed |
| `CI=true` | Headless (GitHub Actions sets this automatically) |
| `VISA2026_E2E_HEADLESS=true` | Headless |
| `VISA2026_E2E_HEADED=true` | Headed (overrides CI / headless) |

On **Windows** CI (`windows-latest`), `CI=true` keeps Edge **headed** (headless breaks Blazor `WaitScriptLoading`). Use `VISA2026_E2E_HEADLESS=true` only when you explicitly want headless (e.g. future Linux agents).

### Edge WebDriver

1. Match Edge version: `msedgedriver --version` vs Edge → About.
2. Install:
   - **`Visa2026.E2E.Tests\.webdrivers\msedgedriver.exe`** (copied to test output), or
   - **`scripts/local/Install-MsEdgeDriver.ps1`** → `%USERPROFILE%\.local\bin`
3. CDN (2026+): **`https://msedgedriver.microsoft.com/{version}/edgedriver_win64.zip`**  
   Legacy **`msedgedriver.azureedge.net`** is dead.

### Blazor host test mode

- **`EasyTestHostMode`** — enabled when connection string contains **`Visa2026EasyTest`** or `VISA2026_EASYTEST=true`.
- **`UiScenarioEphemeralUserModelDifferenceStore`** — no persisted TabbedMDI tabs for test users.
- **`RestoreTabbedMdiLayout = false`** via `UiScenarioHostModelConfigurator`.

---

## C# EasyTest API patterns

### Logon

```csharp
AppContext.GetForm().FillForm(
    new EasyTestParameter("User Name", E2ETestLoginValues.StandardUserName),
    new EasyTestParameter("Password", E2ETestLoginValues.StandardUserPassword));
AppContext.GetAction("Log In").Execute();
```

### Navigation

```csharp
// Sidebar paths (non-People) — usually OK
AppContext.Navigate("Organization.Company");
AppContext.Navigate("Lookup/Geography.Country");
// Do NOT Navigate("Application") for StandardUser — use NavigateEmployeesList() / AssertAuthenticatedAppShell()

// Typed Person lists — use URL helper, not sidebar alone
NavigateEmployeesList(); // → /Person_ListView_Employees
```

**URL helper:** `EasyTestBlazorNavigationHelper.GoToRelativeUrl(AppContext, baseUrl, "Person_ListView_Employees")`.

### Form fill

```csharp
AppContext.GetForm().FillForm(new EasyTestParameter("First Name", "Ferdi"));
AppContext.GetAction("Save").Execute();
AppContext.GetGrid().ProcessRow(new EasyTestParameter("Personal Number", "E2E-EMP-010"));
```

Use **`FillFormWithRetry`** for Blazor lookup combos (one field per attempt).

### Assertions

```csharp
Assert.Equal(expected, AppContext.GetForm().GetPropertyValue("First Name"));
AssertEmployeeDetailViewActive(); // URL or employee form (TabbedMDI may keep URL at /)
```

---

## E2E constants (`E2ETestDataSeed.cs`)

| Type | Purpose |
|------|---------|
| `E2ETestEmployeeCreateValues` | Officer journey employee field values |
| `E2ETestPassportCreateValues` | Officer journey passport field values |
| `E2ETestLoginValues` | `StandardUser` (+ empty password), list/detail view paths |

---

## Build and test

```powershell
dotnet build Visa2026.slnx -c EasyTest
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest --no-build
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest --filter "FullyQualifiedName~EmployeeTests"
```

Visual Studio: configuration **EasyTest**, Test Explorer, run headed (Edge opens automatically).

---

## CI

Workflow: **`.github/workflows/e2e-tests.yml`** — build `-c EasyTest`, `dotnet test`, Windows + Edge driver steps as configured.

---

## User manual media

Contract: [`docs/USER_MANUAL_E2E_MEDIA.md`](../../../docs/USER_MANUAL_E2E_MEDIA.md).

### Video (shipped)

```powershell
.\scripts\local\Record-EasyTest.ps1 `
  -Filter 'PersonOfficerJourney_LoginCreateEmployeeAddPassport' `
  -OutputName 'person-register.mp4'
# video + screenshots ON by default → Visa2026.E2E.Tests/recordings/ (gitignored)
# opt out: -NoRecord -NoScreenshots
```

CI long runs upload **`easytest-e2e-recording`** artifact (`recordings/*.mp4`). **Publish target** (embed, static, object, Postgres/`FileData`) is TBD — see [`docs/USER_MANUAL_E2E_MEDIA.md`](../../../docs/USER_MANUAL_E2E_MEDIA.md) §5.1.

### Screenshots (default ON — user manual)

`EasyTestScreenshotCapture` writes milestone PNGs under `recordings/screenshots/{run}/` **unless** `VISA2026_E2E_SCREENSHOTS=false` (or `0` / `no` / `off`).

- Local preferred: `Record-EasyTest.ps1` (sets run id + copies into manual assets on success).
- Opt out: `Record-EasyTest.ps1 -NoScreenshots` or env false.
- Failure diagnostics still use `TryDumpDiagnostics` → `diag-{label}-{stamp}.png`.
- Guide promotion: `Copy-EasyTestManualScreenshots.ps1` / planned `UserManualMediaCapture`.

### Video (default ON for Record-EasyTest)

`Record-EasyTest.ps1` starts ffmpeg desktop capture unless `-NoRecord`. CI long runs also record. Bare `dotnet test` does **not** start ffmpeg — use the script for MP4.

---

## Scenario metadata (Option A)

YAML + map document officer journeys; **C# `[Fact]` methods execute** them (no yaml runner yet).

| Path | Role |
|------|------|
| `Visa2026.E2E.Tests/scenarios/README.md` | Workflow, Phase 0 inventory |
| `Visa2026.E2E.Tests/scenarios/examples/` | Draft `*_map.md` + `.yaml` |
| `Visa2026.E2E.Tests/scenarios/ready/` | Promoted specs (CI-stable) |
| [reference-map-contract.md](./reference-map-contract.md) | Map sections, yaml vocabulary, caption §3 |

Example yaml step → C#:

| YAML | C# |
|------|-----|
| `login:` | `Login(user, password)` |
| `assert-shell: true` | `AssertAuthenticatedAppShell()` |
| `goto: Person_ListView_Employees` | `NavigateEmployeesList()` |
| `assert-action-visible: New` | `Assert.NotNull(AppContext.GetAction("New"))` |

---

## File map

| Path | Role |
|------|------|
| `Visa2026.E2E.Tests/EasyTestBrowserMode.cs` | Headed (local) vs headless (CI) |
| `Visa2026.E2E.Tests/E2ETestBase.cs` | Fixture, helpers |
| `Visa2026.E2E.Tests/PersonOfficerJourneyTests.cs` | E2E-001 officer journey (EasyTest) |
| `Visa2026.E2E.Tests/Playwright/` | Playwright tests for custom / unsupported UI |
| `Visa2026.E2E.Tests/EasyTestBlazorNavigationHelper.cs` | URL navigation |
| `Visa2026.E2E.Tests/Config.xml` | Legacy EasyTest XML config |
| `Visa2026.Module/DatabaseUpdate/E2ETestDataSeed.cs` | Shared constants |
| `Visa2026.Blazor.Server/EasyTestHostMode.cs` | Test host detection |
| `scripts/local/Install-MsEdgeDriver.ps1` | Driver install |
---

## Playwright fallback

Use **Microsoft Playwright** when EasyTest cannot reliably drive the UI — especially **custom Blazor components** (preview slot, Resminamalar / Document copies editors, dossier chrome, JS-only controls). Keep EasyTest for standard XAF caption forms.

### Decision (short)

1. Can EasyTest `Navigate` / `FillForm` / `GetAction` / nested New express the assertion? → **EasyTest**.
2. Is the interaction inside custom Razor / `#visa-preview-slot` / non-PropertyEditor DOM? → **Playwright**.
3. Need both? Prefer **hybrid** only when cost is low; otherwise one Playwright journey that logs in via page + goes to URL (same `:5050` host).

### Project layout (when introducing Playwright)

| Path / item | Role |
|-------------|------|
| `Visa2026.E2E.Tests/Playwright/` | Playwright `[Fact]` classes |
| `[Trait("Driver", "Playwright")]` | Filter: `--filter "Driver=Playwright"` |
| `Microsoft.Playwright` (+ optional Xunit package) | PackageReference on `Visa2026.E2E.Tests` |
| Shared host | Same **`:5050`** + **`visa2026_easytest`** as EasyTest — reuse preflight / do not race a second host |

### Locators

```csharp
// Prefer stable hooks in product markup
await page.GetByTestId("resminamalar-catalog").ClickAsync();
await page.Locator("#visa-preview-slot.visa-preview-slot--open").WaitForAsync();

// Login constants match EasyTest
await page.GetByLabel("User Name").FillAsync(E2ETestLoginValues.StandardUserName);
await page.GetByRole(AriaRole.Button, new() { Name = "Log In" }).ClickAsync();
```

Add **`data-testid`** on new custom controls when writing Playwright coverage — do not rely on EasyTest caption inventory for those nodes.

### Install browsers (once per machine / CI)

```powershell
# After build that restores Playwright package — adjust path to output folder
dotnet build Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest
pwsh bin/EasyTest/net8.0/playwright.ps1 install chromium
# Or install msedge channel if tests target Edge explicitly
```

CI: install Playwright browsers in **`e2e-tests.yml`** when the first Playwright suite is added (keep EasyTest Edge/msedgedriver steps for existing facts).

### Headed / headless

Mirror **`EasyTestBrowserMode`**: headed locally and on Windows CI; allow `VISA2026_E2E_HEADLESS=true` for headless Playwright locally.

### Media

- EasyTest journeys: **`Record-EasyTest.ps1`** (video + screenshots **default ON**) / `EasyTestScreenshotCapture`.
- Playwright-only journeys: use Playwright **`page.ScreenshotAsync`** / video context options; still land artifacts under `Visa2026.E2E.Tests/recordings/` (gitignored) for the user-manual pipeline.

### Anti-patterns

| Avoid | Prefer |
|-------|--------|
| Rewriting stable EasyTest person/passport Facts in Playwright | Keep EasyTest |
| EasyTest `FillForm` against preview-slot catalog cards | Playwright + `data-testid` |
| Second Kestrel on another port for Playwright | Share **`:5050`** fixture |
| Fragile absolute XPath | `GetByTestId` / role / stable CSS |

