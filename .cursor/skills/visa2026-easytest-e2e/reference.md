# visa2026-easytest-e2e — reference

## Three UI test stacks

| | **EasyTest E2E** | **UiScenarioRunner** | **UI test hooks** |
|--|------------------|----------------------|-------------------|
| Skill | **visa2026-easytest-e2e** | visa2026-ui-scenarios | visa2026-ui-test-hooks |
| Project | `Visa2026.E2E.Tests` | `tools/UiScenarioRunner` | `Visa2026.Blazor.Server` controllers |
| Port | **5050** | **5052** | 5051 (verify only) |
| DB | **Visa2026EasyTest** | Visa2026UiScenario | Visa2026HookVerify |
| Selectors | **Captions** / actions | **`data-testid`** hook ids | Prepare hooks → `UI_TEST_HOOKS.md` |
| Config | **EasyTest** | Debug + `VISA2026_UI_SCENARIOS` | Hook Verify profile |
| CI | `e2e-tests.yml` | `ui-scenario-tests.yml` | optional VerifyUiTestHooks |

Same officer journey may exist in **both** EasyTest and UiScenario — keep seed values and steps aligned (`E2ETestEmployeeCreateValues` ↔ scenario `env:` block).

---

## Host and driver

### Launch profile (`Properties/launchSettings.json`)

Profile: **`Visa2026 - EasyTest (LocalDB)`**

- `applicationUrl`: `http://localhost:5050`
- `ConnectionStrings__DefaultConnection`: `Database=Visa2026EasyTest`
- `VISA2026_EASYTEST`: `true` (optional; host also detects DB name)

### `E2ETestBase` registration

```csharp
new BlazorApplicationOptions(
    name: "Visa2026Blazor",
    physicalPath: blazorServerPath,
    url: "http://localhost:5050",
    configuration: "EasyTest",
    arguments: "--launch-profile \"Visa2026 - EasyTest (LocalDB)\"",
    browser: "Edge",
    runHeadless: false,
    webDriverPath: ResolveWebDriverDirectory())
```

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

## Seed constants (`E2ETestDataSeed.cs`)

| Type | Purpose |
|------|---------|
| `E2ETestDataSeed` | Applicant + passport for ApplicationTests |
| `E2ETestEmployeeCreateValues` | E2E-010 employee field values |
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

Workflow: **`Visa2026.E2E.Tests/e2e-tests.yml`** — build `-c EasyTest`, `dotnet test`, Windows + Edge driver steps as configured.

---

## When to choose EasyTest vs UiScenario

| Prefer **EasyTest** | Prefer **UiScenarioRunner** |
|---------------------|----------------------------|
| CI E2E matrix already wired | Journey needs **hook ids** / shadow DOM toolbar |
| Caption-based smoke aligned with XAF docs | Nested collection **New** (Passports tab) |
| DevExpress-native API maintenance | Agent scenario maps + screenshot steps |
| Windows officer regression | Promoted YAML inventory under `scenarios/` |

Both can coexist; align data constants and business steps, not selector mechanism.

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
| `Visa2026.E2E.Tests/E2ETestBase.cs` | Fixture, helpers |
| `Visa2026.E2E.Tests/SmokeTests.cs` | E2E-001 / E2E-001-nav Tier 0 smokes |
| `Visa2026.E2E.Tests/EasyTestBlazorNavigationHelper.cs` | URL navigation |
| `Visa2026.E2E.Tests/Config.xml` | Legacy EasyTest XML config |
| `Visa2026.Module/DatabaseUpdate/E2ETestDataSeed.cs` | Shared constants |
| `Visa2026.Module/DatabaseUpdate/E2ETestDataSeedUpdater.cs` | DB seed on update |
| `Visa2026.Blazor.Server/EasyTestHostMode.cs` | Test host detection |
| `scripts/local/Install-MsEdgeDriver.ps1` | Driver install |
