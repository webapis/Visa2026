# Visa2026 — Testing Plan

Status: **Active**  
Last updated: 2026-06-11

---

## 1. Purpose

This document defines **how Visa2026 is tested** with **native XAF EasyTest E2E**: project layout, current inventory, backlog, and CI notes.

**Agent skill:** [`.cursor/skills/visa2026-easytest-e2e/SKILL.md`](../.cursor/skills/visa2026-easytest-e2e/SKILL.md)

**API reference:** [`Visa2026.E2E.Tests/README.md`](../Visa2026.E2E.Tests/README.md), [`EasyTestFixtureContext.md`](../Visa2026.E2E.Tests/EasyTestFixtureContext.md)

**Product context:** officers must trust application/process tracking, document validity states, and dashboard → list consistency. See [`BUSINESS_LOGIC_BASELINE.md`](BUSINESS_LOGIC_BASELINE.md).

---

## 2. Related documents

| Document | Role |
|----------|------|
| [`BUSINESS_LOGIC_BASELINE.md`](BUSINESS_LOGIC_BASELINE.md) | Business rules (BR-xxx); E2E smoke targets |
| [`STATE_SPECIFICATIONS.md`](STATE_SPECIFICATIONS.md) | Dashboard tiles; E2E drill-down parity |
| [`LOOKUP_SEEDING.md`](LOOKUP_SEEDING.md) | Lookup seeding (not Blazor E2E scope) |
| [`LOCALIZATION_PLAN.md`](LOCALIZATION_PLAN.md) | E2E scripts stay **English** |
| Feature plans | Per-feature E2E notes when shipped |

---

## 3. EasyTest E2E stack

| Item | Value |
|------|--------|
| Project | [`Visa2026.E2E.Tests`](../Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj) |
| Runner | xUnit |
| UI driver | DevExpress **EasyTest** Blazor adapter + **Selenium** (Edge) |
| App under test | [`Visa2026.Blazor.Server`](../Visa2026.Blazor.Server/) |
| Launch profile | **`Visa2026 - EasyTest (LocalDB)`** |
| URL | **`http://localhost:5050`** |
| DB | **`Visa2026EasyTest`** on `(localdb)\mssqllocaldb` |
| Build config | **EasyTest** |
| Platform | **Windows** (`[SupportedOSPlatform("windows")]`) |
| Selectors | **English model captions** + EasyTest actions (not Playwright hook ids) |
| Style | **C# EasyTest API** (yaml in `scenarios/` is metadata only — Option A) |

**Base fixture:** [`E2ETestBase.cs`](../Visa2026.E2E.Tests/E2ETestBase.cs) — drops DB once per run, launches app, `Login()`, shared helpers.

**Scenario maps:** [`Visa2026.E2E.Tests/scenarios/README.md`](../Visa2026.E2E.Tests/scenarios/README.md)

---

## 4. Current E2E inventory

### Ready (promoted scenarios)

| Scenario id | E2E id | C# test |
|-------------|--------|---------|
| `login-smoke` | E2E-001 | `SmokeTests.LoginSmoke_AuthenticatedShellLoads` |
| `login-nav-employees` | E2E-001-nav | `SmokeTests.LoginNavEmployees_ListOpensWithNewAction` |
| `person-employee-create` | E2E-010 | `EmployeeTests.Employee_Create_RequiredFields_SavesAndAppearsInList` |
| `person-passport-add-seeded-employee` | E2E-020 | `PassportTests.Passport_AddOnSeededEmployee_SavesAndShowsPassportNumber` |

### All implemented `[Fact]` tests

| Test class | Tests | Backlog |
|------------|-------|---------|
| `SmokeTests` | 2 | E2E-001, E2E-001-nav |
| `EmployeeTests` | 1 | E2E-010 |
| `PassportTests` | 1 | E2E-020 |

**Count:** 4 facts across 3 classes.

**Full suite:**

```powershell
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest
```

---

## 5. E2E backlog

Target: **~12–20** stable E2E tests, **&lt; ~10 min** on CI. One **ApplicationType** per scenario class.

### Tier 0 — CI gate

| ID | Scenario | Status |
|----|----------|--------|
| E2E-001 | Login + app shell | Done |
| E2E-001-nav | Login + Employees list nav | Done |

### Tier 1 — Foundation data

| ID | Scenario | Status |
|----|----------|--------|
| E2E-010 | Create Employee (minimal required fields) | Done |
| E2E-020 | Add passport on seeded employee (nested Passports tab) | Done |
| E2E-011 | Create/link Person for employee | Planned |
| E2E-012 | ApplicationType selection changes visible tabs | Planned |

### Tier 2 — Core operational value

| ID | Scenario | Status |
|----|----------|--------|
| E2E-030 | Create Application (canonical type `App_Inv`) | Planned |
| E2E-021 | Add ApplicationItem with person | Planned |
| E2E-022 | Add ApplicationProgress milestone | Planned |
| E2E-023 | State Dashboard tile → filtered list | Planned |

### Tier 3+ — Compliance, output, security, features

See prior backlog IDs **E2E-030** through **E2E-062** in git history or feature plans (`STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md`, `OFFICER_TASK_CHAT_IMPLEMENTATION_PLAN.md`).

---

## 6. CI and local commands

```powershell
dotnet build Visa2026.slnx -c EasyTest
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest
```

**Prerequisites:** Windows, SQL Server LocalDB, `msedgedriver.exe` matching Edge ([E2E README](../Visa2026.E2E.Tests/README.md)).

**Browser:** headed Edge locally (default); headless on CI via `EasyTestBrowserMode` (`CI=true` or `VISA2026_E2E_HEADLESS=true`). Override locally: `$env:VISA2026_E2E_HEADLESS='true'` before `dotnet test`.

**CI:** GitHub Actions workflow **`.github/workflows/e2e-tests.yml`** — `windows-latest`, LocalDB, Edge WebDriver, full EasyTest suite on push/PR (`CI=true`, headless).

**CI policy (recommended):**

- **PR to `main`:** Tier 0 E2E + `dotnet build -c EasyTest` (workflow runs all current facts)
- **Nightly / pre-release:** Full E2E suite (tier 0–2)

---

## 7. Conventions for new E2E tests

1. Inherit `E2ETestBase`; do not start the app twice per class.
2. Write `scenarios/examples/<id>_map.md` first (caption inventory §3).
3. Mirror steps in C# `[Fact]`; optional yaml spec when map is **Ready for YAML**.
4. Promote map + yaml to `scenarios/ready/` when CI-stable.
5. Use **`NavigateEmployeesList()`** for Employees — not bare `Navigate("Employees")`.
6. Use unique `PersonalNumber` / codes per test to avoid collisions.
7. One scenario per `[Fact]`; shared arrange via `E2ETestBase` helpers.
8. Update §4 inventory when adding or removing tests.

---

## 8. Out of scope for automated E2E

- Every **ApplicationType** and ministry letter variant
- Full **visa state machine** matrix (all BR-050+ paths)
- **Lookup catalog** content correctness
- **Word/Excel template layout** QA
- **DataImporter** CLI bulk import
- **Production** Docker/IIS deploy verification

---

## 9. Maintenance

- Update **§4** when adding/removing E2E facts.
- Update **§5** backlog status (`Done` / `Planned` / `Cancelled`).
- Append verified fixes to [`.cursor/skills/visa2026-easytest-e2e/learnings.md`](../.cursor/skills/visa2026-easytest-e2e/learnings.md).
