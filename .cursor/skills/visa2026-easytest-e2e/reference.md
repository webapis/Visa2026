# visa2026-easytest-e2e — reference

## Stack summary

| Item | Value |
|------|--------|
| Skill | **visa2026-easytest-e2e** |
| Project | `Visa2026.E2E.Tests` |
| Port | **5050** |
| DB | **Visa2026EasyTest** |
| Selectors | **Captions** / EasyTest actions (`FillFormWithRetry` may fall back to `InputId` / `data-testid`) |
| Config | **EasyTest** |
| CI | `.github/workflows/e2e-tests.yml` |

---

## Host and driver

### Launch profile (`Properties/launchSettings.json`)

Profile: **`Visa2026 - EasyTest (LocalDB)`**

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
AppContext.Navigate("Application");

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
| `E2ETestLoginValues` | `standarduser`, list/detail view paths |

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
| `Visa2026.E2E.Tests/PersonOfficerJourneyTests.cs` | E2E-001 officer journey |
| `Visa2026.E2E.Tests/EasyTestBlazorNavigationHelper.cs` | URL navigation |
| `Visa2026.E2E.Tests/Config.xml` | Legacy EasyTest XML config |
| `Visa2026.Module/DatabaseUpdate/E2ETestDataSeed.cs` | Shared constants |
| `Visa2026.Blazor.Server/EasyTestHostMode.cs` | Test host detection |
| `scripts/local/Install-MsEdgeDriver.ps1` | Driver install |
