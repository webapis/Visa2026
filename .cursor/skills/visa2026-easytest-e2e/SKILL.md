---
name: visa2026-easytest-e2e
description: >-
  Creates and runs Visa2026 native XAF EasyTest E2E tests (Visa2026.E2E.Tests,
  C# API, Edge/Selenium, EasyTest config). Covers E2ETestBase, Blazor host on
  :5050, Visa2026EasyTest DB, msedgedriver, caption-based FillForm/Navigate,
  URL navigation for typed Person lists, seed constants, and dotnet test -c EasyTest.
  Use when adding EasyTest, E2E test class, EmployeeTests, E2ETestBase helper,
  headed Edge run, or CI e2e-tests.yml — not Playwright ui-scenarios, ui-test-hooks,
  or Module unit tests. See user-prompts.md.
disable-model-invocation: false
---

# Visa2026: XAF EasyTest E2E (native)

## Purpose

**Author and run** officer-journey tests in **`Visa2026.E2E.Tests`** using DevExpress **EasyTest Blazor adapter** + **xUnit** + **Microsoft Edge** (Selenium). Tests use the **C# API** (`IApplicationContext`, `EasyTestParameter`) — not `.ets` scripts in this repo.

| Layer | Project / tool | Skill |
|-------|----------------|-------|
| **EasyTest E2E** | `Visa2026.E2E.Tests` | **This skill** |
| **Playwright scenarios** | `tools/UiScenarioRunner` (`:5052`, hook ids) | [visa2026-ui-scenarios](../visa2026-ui-scenarios/SKILL.md) |
| **CSS hook prep** | `data-testid` / `UI_TEST_HOOKS.md` | [visa2026-ui-test-hooks](../visa2026-ui-test-hooks/SKILL.md) |
| **Unit / integration** | `Visa2026.Module.Tests` | [visa2026-unit-tests](../visa2026-unit-tests/SKILL.md) |

**Strategy context:** [`docs/TESTING_PLAN.md`](../../../docs/TESTING_PLAN.md). **Experience log:** [learnings.md](./learnings.md) — read before, append after verified runs.

---

## User prompts

Copy-paste catalog: [user-prompts.md](./user-prompts.md). Invoke with **`@visa2026-easytest-e2e`**.

---

## Process (new E2E test)

```text
1. MAP       — scenarios/examples/<id>_map.md (caption inventory §3) + E2E-xxx id
2. YAML      — scenarios/examples/<id>.yaml when map Ready for YAML (Option A: spec only)
3. C#        — *Tests.cs [Fact] mirroring yaml steps; extend E2ETestBase helpers
4. BUILD     — dotnet build Visa2026.slnx -c EasyTest
5. RUN       — dotnet test Visa2026.E2E.Tests -c EasyTest --filter "FullyQualifiedName~YourTests"
6. PROMOTE   — move map + yaml to scenarios/ready/ when CI-stable
7. RECORD    — append learnings.md on non-obvious fixes (nav, captions, driver, host)
```

**Scenario metadata (Option A):** YAML documents steps; C# executes them. Map contract: [reference-map-contract.md](./reference-map-contract.md). Inventory: [`Visa2026.E2E.Tests/scenarios/README.md`](../../../Visa2026.E2E.Tests/scenarios/README.md).

---

## Host isolation (mandatory)

EasyTest must **not** share the IDE dev host (`:5000` / `:5001`).

| Setting | Value |
|---------|--------|
| Launch profile | **`Visa2026 - EasyTest (LocalDB)`** |
| URL | **`http://localhost:5050`** |
| DB | **`Visa2026EasyTest`** on `(localdb)\mssqllocaldb` |
| Build config | **`EasyTest`** |
| Browser | **Edge** (`runHeadless: false` in `E2ETestBase` — headed by default) |

**TabbedMDI / saved tabs:** EasyTest host sets ephemeral user model differences when **`Visa2026EasyTest`** is the connection string (see `EasyTestHostMode` in Blazor.Server). Without this, **`standarduser`** can reopen **Family Members** instead of Employees.

Full host + driver setup: [reference.md § Host and driver](./reference.md#host-and-driver).

---

## Writing tests

### Base class

- Inherit **`E2ETestBase`** (`IAsyncLifetime` — app starts in `InitializeAsync`, DB dropped once per run).
- **`[SupportedOSPlatform("windows")]`** on test class/method (Edge E2E is Windows-only today).
- Use **`Login(userName, password)`** — officer flows: **`E2ETestLoginValues.StandardUserName`** + empty password.

### Selectors: captions, not hooks

EasyTest fills fields by **English model caption** (`EasyTestParameter("First Name", value)`), not `data-testid`. Keep captions aligned with embedded model / en-US. Custom Blazor editors may need **`InputId`** / aria for EasyTest to find controls (see unit-tests learnings cross-link — fix in Module/Blazor, not in hook skill).

### Navigation (critical)

| Target | Do | Do not |
|--------|-----|--------|
| **Employees list** | Selenium URL **`/Person_ListView_Employees`** via **`NavigateEmployeesList()`** / **`EasyTestBlazorNavigationHelper`** | Rely on **`Navigate("Employees")`** or **`People.Employees`** alone — TabbedMDI may stay on **Family Members** while **New** still exists |
| **After New employee** | **`AssertEmployeeDetailViewActive()`** — URL must contain **`Person_DetailView_Employee`** | Assume list context from sidebar highlight |
| **Organization / Lookup** | **`AppContext.Navigate("Organization.Company")`**, **`Lookup/Geography.Country`** — dot/slash paths | Mix with bare leaf ids under **People** |

Constants: **`E2ETestLoginValues`** in `Visa2026.Module/DatabaseUpdate/E2ETestDataSeed.cs`.

### Helpers already on `E2ETestBase`

| Helper | Use |
|--------|-----|
| `Login` | Logon form |
| `NavigateEmployeesList` | URL → employees list |
| `CreateEmployeeWithRequiredFields` | E2E-010 pattern |
| `OpenEmployeeInListByPersonalNumber` | Grid `ProcessRow` |
| `CreateApplicationWithTypeCode` / `AddApplicationItemWithPerson` | Application smoke |
| `FillFormWithRetry` | One field at a time + retry |
| `ExecuteActionWithRetry` | Toolbar actions after Blazor load |

### Seed data

- **`E2ETestDataSeedUpdater`** — seeds applicant + passport on **`Visa2026EasyTest`** only.
- **`E2ETestEmployeeCreateValues`** / **`E2ETestLoginValues`** — stable strings for create/login tests.
- Use **unique** `PersonalNumber` per test when creating records to avoid collisions across runs.

---

## Run commands (repo root)

```powershell
dotnet build Visa2026.slnx -c EasyTest
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest --filter "FullyQualifiedName~EmployeeTests"
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest
```

**Prerequisites:** Windows, LocalDB, **`msedgedriver.exe`** matching Edge (see [reference.md](./reference.md)). Optional: `Visa2026.E2E.Tests\.webdrivers\` (copied to output on build).

---

## Current inventory (extend, do not duplicate)

| Test class | Focus |
|------------|--------|
| `SmokeTests` | E2E-001 / E2E-001-nav (`scenarios/ready/`) |
| `EmployeeTests` | E2E-010 employee create (`scenarios/ready/person-employee-create`) |
| `ApplicationTests` | Application + application item |
| `CountryTests` | Lookup CRUD |
| `OrganizationSettingsTests` | Organization singletons |

Config: **`Config.xml`**, **`e2e-tests.yml`** (CI). Docs: [`Visa2026.E2E.Tests/README.md`](../../../Visa2026.E2E.Tests/README.md), [`EasyTestFixtureContext.md`](../../../Visa2026.E2E.Tests/EasyTestFixtureContext.md).

---

## Agent workflow

When the user asks for **EasyTest**, **E2E test**, **headed Edge test**, or **`Visa2026.E2E.Tests`**:

1. **Read** [learnings.md](./learnings.md) for navigation, login, driver, caption pitfalls.
2. **Read** target production test + **`E2ETestBase`** for existing helpers.
3. **Implement** minimal test class; prefer extending base helpers over duplicating steps.
4. **Build** `-c EasyTest`; **run** filtered `dotnet test`.
5. **Append** learnings.md after verified fixes (not for trivial typos).
6. **Do not** implement Playwright YAML, `data-testid` hooks, or Module unit tests in the same task unless explicitly asked.

---

## Known pitfalls

| Pitfall | Do instead |
|---------|------------|
| Run E2E on `:5000` IDE host | **EasyTest** profile → **`:5050`**; explicit `BlazorApplicationOptions` URL in `E2ETestBase` |
| **`Navigate("Employees")`** → Family Members detail | **`NavigateEmployeesList()`** (URL **`Person_ListView_Employees`**) + **`AssertEmployeeDetailViewActive()`** |
| **`GetAction("New")` null** | Wait/retry; ensure correct list URL first |
| **`msedgedriver` not found** | Install to **`.webdrivers/`** or `%USERPROFILE%\.local\bin`; CDN **`msedgedriver.microsoft.com`** |
| Use **`Visa2026EasyTest`** in Module.Tests | E2E owns EasyTest DB; unit/integration uses **`Visa2026Test`** |
| Duplicate evaluator matrix in E2E | Unit-test evaluators; E2E = one officer path |
| Confuse with **UiScenarioRunner** | Playwright + hooks + **`:5052`** — different skill |
| Add hooks for EasyTest only | EasyTest uses **captions**; hooks are for Playwright unless caption lookup fails |

---

## Additional resources

- [user-prompts.md](./user-prompts.md) — invoke messages
- [reference.md](./reference.md) — host, driver, API patterns, CI, three-stack table
- [reference-map-contract.md](./reference-map-contract.md) — `*_map.md` + yaml + C# (Option A)
- [learnings.md](./learnings.md) — append-only verified experience
- [`Visa2026.E2E.Tests/scenarios/`](../../../Visa2026.E2E.Tests/scenarios/README.md) — scenario maps and yaml specs
- [`docs/TESTING_PLAN.md`](../../../docs/TESTING_PLAN.md) — pyramid, backlog E2E-xxx
- [visa2026-ui-scenarios](../visa2026-ui-scenarios/SKILL.md) — hook-based Playwright journeys
- [visa2026-ui-test-hooks](../visa2026-ui-test-hooks/SKILL.md) — selector prep (not EasyTest)
- [visa2026-unit-tests](../visa2026-unit-tests/SKILL.md) — Module tests (not E2E)
