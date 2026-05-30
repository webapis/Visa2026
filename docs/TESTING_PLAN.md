# Visa2026 — Testing Plan

Status: **Draft v0.1**  
Owner: Tech Lead + Visa product  
Last updated: 2026-05-30

---

## 1. Purpose

This document defines **how Visa2026 is tested**: which layers exist, what each layer is responsible for, what is implemented today, and what to add next. It is the single place to align developers, QA, and agents on test scope before implementing new tests or wiring CI.

**Product context** (why testing matters here): officers must trust **application/process tracking**, **document validity states**, and **dashboard → list** consistency for compliance workflows. See [`BUSINESS_LOGIC_BASELINE.md`](BUSINESS_LOGIC_BASELINE.md).

**Not in scope for this plan:** step-by-step EasyTest API reference (see [`Visa2026.E2E.Tests/README.md`](../Visa2026.E2E.Tests/README.md) and [`EasyTestFixtureContext.md`](../Visa2026.E2E.Tests/EasyTestFixtureContext.md)).

---

## 2. Related documents

| Document | Role in testing |
|----------|-----------------|
| [`BUSINESS_LOGIC_BASELINE.md`](BUSINESS_LOGIC_BASELINE.md) | Business rules (BR-xxx); traceability targets |
| [`BO_STATE_TRACKING.md`](BO_STATE_TRACKING.md) | State codes and conditions to unit-test |
| [`STATE_SPECIFICATIONS.md`](STATE_SPECIFICATIONS.md) | Dashboard tiles; E2E drill-down parity |
| [`STATE_TRACKING_IMPLEMENTATION_PLAN.md`](STATE_TRACKING_IMPLEMENTATION_PLAN.md) | Evaluators, snapshots; integration tests |
| [`LOOKUP_SEEDING.md`](LOOKUP_SEEDING.md) | JSON catalog sync; deploy/integration, not Blazor E2E |
| [`LOCALIZATION_PLAN.md`](LOCALIZATION_PLAN.md) | E2E scripts stay **English** |
| [`USER_REPORT_MAP_STANDARD.md`](USER_REPORT_MAP_STANDARD.md) | User Word/Excel templates; tool validation |
| [`WORD_REPORT_GENERATION_IDEA.md`](WORD_REPORT_GENERATION_IDEA.md) | Code-backed Word reports; preview tools |
| Feature plans | Per-feature E2E notes (e.g. [`STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md`](STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md), [`OFFICER_TASK_CHAT_IMPLEMENTATION_PLAN.md`](OFFICER_TASK_CHAT_IMPLEMENTATION_PLAN.md)) |

---

## 3. Testing strategy (pyramid)

Visa2026 is an **XAF Blazor** app with **complex state rules** (Level 4–5 in the baseline). Use a pyramid: many fast tests at the bottom, few slow E2E journeys at the top.

```
                    ┌─────────────────────┐
                    │  E2E (few)          │  Officer journeys, UI wiring, smoke
                    ├─────────────────────┤
                    │  Integration        │  EF, updaters, evaluators + DB, SQL views
                    ├─────────────────────┤
                    │  Unit (many)        │  Pure rule logic, parsers, helpers
                    └─────────────────────┘
```

| Layer | Primary question | Typical location | Speed |
|-------|------------------|------------------|-------|
| **Unit** | Is this rule correct for inputs X? | `Visa2026.Module.Tests` *(planned)* | Fast |
| **Integration** | Does persistence + evaluator + seed produce the right state? | Module test project + LocalDB / test DB | Medium |
| **E2E** | Can an officer complete this flow in the real UI? | [`Visa2026.E2E.Tests`](../Visa2026.E2E.Tests/) | Slow |
| **Tool / manual** | Is this template or deploy artifact correct? | `tools/*`, Docker deploy, Visual Studio | Ad hoc |

**Principle:** Do **not** encode full visa state precedence (BR-050+) only in E2E. Evaluators and SQL views get **unit/integration** coverage; E2E proves **navigation, save, and one representative path** still work.

---

## 4. Test layers for this solution

### 4.1 Unit tests *(planned — not in repo yet)*

**Project:** `Visa2026.Module.Tests` (to be added; `net8.0`, xUnit, reference `Visa2026.Module` only).

**Prioritize:**

| Area | Examples |
|------|----------|
| State evaluators | `VisaStateEvaluator`, `WorkPermitItemStateEvaluator`, registration/invitation evaluators |
| Precedence / evidence | Cancellation vs extension vs `RejectionItem` branches (BR-059–065) |
| Pure helpers | Line parsers, date/window calculators, merge helpers without `ObjectSpace` |
| Validation rules | Submission blockers vs warnings where logic is isolated |

**Defer to E2E:** XAF Appearance, controller visibility, Blazor control behavior.

### 4.2 Integration tests *(planned)*

**Same test project** (or separate `Visa2026.Module.Integration.Tests` if the suite grows).

| Area | Examples |
|------|----------|
| `ModuleUpdater` / lookup sync | JSON catalog apply, prune rules ([`LOOKUP_SEEDING.md`](LOOKUP_SEEDING.md)) |
| Organization singletons | `TryGetInstance`, tenant JSON ([`LOOKUP_ORGANIZATION_SINGLETONS.md`](LOOKUP_ORGANIZATION_SINGLETONS.md)) |
| State snapshot / SQL views | Dashboard count matches filter query for a fixture dataset |
| `ApplicationProgress` timeline | Latest entry drives process state (BR timeline rules) |

**Database:** dedicated test DB (e.g. `Visa2026Test` on LocalDB), migrated per fixture or collection fixture — separate from E2E DB `Visa2026EasyTest`.

### 4.3 End-to-end tests *(implemented)*

| Item | Value |
|------|--------|
| Project | [`Visa2026.E2E.Tests`](../Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj) |
| Runner | xUnit |
| UI driver | DevExpress **EasyTest** Blazor adapter + **Selenium** (Edge) |
| App under test | [`Visa2026.Blazor.Server`](../Visa2026.Blazor.Server/) |
| DB | `Visa2026EasyTest` on `(localdb)\mssqllocaldb` |
| Build config | **EasyTest** (also Debug for local dev) |
| Platform | **Windows** (`[SupportedOSPlatform("windows")]`) |
| Language | **English** UI strings in tests ([`LOCALIZATION_PLAN.md`](LOCALIZATION_PLAN.md)) |
| Style | **C# EasyTest API** (no `.ets` scripts in repo today) |

**Base fixture:** [`E2ETestBase.cs`](../Visa2026.E2E.Tests/E2ETestBase.cs) — drops DB once per run, launches app, `Login()`, shared helpers (`CreateCountry`, `CreateEmployee`, organization navigations).

### 4.4 Tool-based and manual testing

| Tool / practice | Use when |
|-----------------|----------|
| `tools/PreviewWordReports`, `tools/GenerateTemplates` | Code-backed Word report placeholders |
| User template **Extract/Validate** | [`USER_REPORT_MAP_STANDARD.md`](USER_REPORT_MAP_STANDARD.md) |
| `dotnet build` + `dotnet test` | Pre-commit ([`.cursor/skills/commit-after-verify/SKILL.md`](../.cursor/skills/commit-after-verify/SKILL.md)) |
| Docker compose dev stack | Deploy/DB updater smoke ([`ENVIRONMENTS.md`](ENVIRONMENTS.md)) |
| Visual Studio Test Explorer | Local E2E with visible browser |

---

## 5. Current E2E inventory

| Test class | Test | Status |
|------------|------|--------|
| `Visa2026Tests` | `TestBlazorApp_Opens` — login smoke | Done |
| `GeneralTests` | `TestBlazorApp_Opens` — login + navigate Employees | Done |
| `CountryTests` | `Country_CRUD_Lifecycle` | Done |
| `CountryTests` | `Country_Validation_RequiredFields` | Done |
| `CountryTests` | `Country_Delete_Cancellation` | Done |
| `OrganizationSettingsTests` | `CompanyProfile_UpdateName` | Done |
| `OrganizationSettingsTests` | `AuthorizedSignatory_RequiredFullName` | Done |
| `OrganizationSettingsTests` | `AuthorizedRepresentative_UpdatePhone` | Done |
| `ApplicationTests` | `Application_Create_AppInv_SavesWithNumber` | Done |

**Count:** 9 facts across 5 classes (2 overlapping smoke tests).

---

## 6. E2E backlog (target suite)

Target: **~12–20** stable E2E tests, **&lt; ~10 min** on CI. One **ApplicationType** per scenario class — no matrix of every ministry letter.

### Tier 0 — CI gate (must pass on PR)

| ID | Scenario | Class (suggested) | Status |
|----|----------|-------------------|--------|
| E2E-001 | Login as Admin, app loads | `SmokeTests` (consolidate duplicate smokes) | Partial |
| E2E-002 | Lookup Country CRUD + validation | `CountryTests` | Done |
| E2E-003 | Organization singleton save / required field | `OrganizationSettingsTests` | Done |

### Tier 1 — Foundation data

| ID | Scenario | Notes | Status |
|----|----------|-------|--------|
| E2E-010 | Create Employee (minimal required fields) | Use `CreateEmployee` helper | Planned |
| E2E-011 | Create/link Person for employee | Person detail from employee | Planned |
| E2E-012 | ApplicationType selection changes visible tabs | [`APPLICATION_BO_TYPE_SELECTION_REFACTOR.md`](APPLICATION_BO_TYPE_SELECTION_REFACTOR.md) | Planned |

### Tier 2 — Core operational value

| ID | Scenario | Notes | Status |
|----|----------|-------|--------|
| E2E-020 | Create Application (one canonical type) | `ApplicationTests` — code **101** (`App_Inv`) | Done |
| E2E-021 | Add ApplicationItem with person | One line, save | Planned |
| E2E-022 | Add ApplicationProgress milestone | Smoke only; assert UI shows new progress | Planned |
| E2E-023 | State Dashboard tile → filtered list | Same filter as count (BR-010–012 trust) | Planned |

### Tier 3 — Compliance visibility

| ID | Scenario | Notes | Status |
|----|----------|-------|--------|
| E2E-030 | Record in warning/expiring window appears on dashboard | Fixture dates or seed; light assert | Planned |
| E2E-031 | Registration check-in smoke | When UI stable | Planned |
| E2E-032 | Compliance journey (BR-003) | End-to-end “see issue → open record”; not full evaluator matrix | Planned |

### Tier 4 — Output smoke

| ID | Scenario | Notes | Status |
|----|----------|-------|--------|
| E2E-040 | PDF form fill/download (one mapping) | No binary assert; action completes | Planned |
| E2E-041 | Word report “Resminamalar” (one preset) | Generation succeeds | Planned |

### Tier 5 — Security

| ID | Scenario | Notes | Status |
|----|----------|-------|--------|
| E2E-050 | Officer role: limited nav vs Admin | Second test user in seed | Planned |
| E2E-051 | Chief role: approval/supervisor actions | If distinct from Officer | Planned |

### Tier 6 — Feature-flagged (when shipped)

| ID | Scenario | Doc | Status |
|----|----------|-----|--------|
| E2E-060 | State notifications inbox + badge | [`STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md`](STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md) | Planned |
| E2E-061 | Officer task chat (two users, one message) | [`OFFICER_TASK_CHAT_IMPLEMENTATION_PLAN.md`](OFFICER_TASK_CHAT_IMPLEMENTATION_PLAN.md) | Planned |
| E2E-062 | Optional: culture cookie / localized caption | [`LOCALIZATION_PLAN.md`](LOCALIZATION_PLAN.md) | Backlog |

---

## 7. Unit / integration backlog (Module)

| ID | Area | Priority | Status |
|----|------|----------|--------|
| UT-001 | Add `Visa2026.Module.Tests` project to solution | P0 | Not started |
| UT-010 | Visa state evaluator — golden cases from [`STATE_SPECIFICATIONS.md`](STATE_SPECIFICATIONS.md) | P0 | Not started |
| UT-011 | Work permit / invitation evaluators | P1 | Not started |
| UT-012 | `RejectionItem` person-level vs application-level (BR-063–065) | P1 | Not started |
| IT-010 | Lookup JSON sync after updater run | P1 | Not started |
| IT-011 | Dashboard SQL view count = list filter (fixture DB) | P1 | Not started |
| IT-012 | `ApplicationProgress` latest-wins process state | P1 | Not started |

---

## 8. Business rule traceability (starter)

Extend this table as tests are added. “None” means gap — prefer filling **UT/IT** before new E2E.

| BR ID | Business concern | Unit | Integration | E2E |
|-------|------------------|------|-------------|-----|
| BR-001 | Passport / visa / WP validity | UT-010 | IT-011 | E2E-030 |
| BR-002 | Work permit scheduling | UT-011 | IT-011 | — |
| BR-003 | Visa/registration compliance flow | UT-010 | IT-011 | E2E-032 |
| BR-010–012 | Dashboard ↔ list parity | — | IT-011 | E2E-023 |
| BR-013 | Data completeness / prerequisites | UT-* | IT-* | E2E-060 (with notifications) |

Full rule list: [`BUSINESS_LOGIC_BASELINE.md`](BUSINESS_LOGIC_BASELINE.md) §6–8.

---

## 9. CI and local commands

### Local (developer workstation)

```powershell
# Build
dotnet build Visa2026.slnx -c Debug

# E2E (Windows, Edge WebDriver on PATH)
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c Debug

# E2E with EasyTest configuration
dotnet build Visa2026.slnx -c EasyTest
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest --no-build
```

**Prerequisites:** SQL Server LocalDB, `msedgedriver.exe` matching Edge version ([E2E README](../Visa2026.E2E.Tests/README.md)).

### Continuous integration

| Item | Status | Action |
|------|--------|--------|
| Workflow file | Exists at [`Visa2026.E2E.Tests/e2e-tests.yml`](../Visa2026.E2E.Tests/e2e-tests.yml) | **Move or copy** to `.github/workflows/e2e-tests.yml` |
| Runner | `windows-latest` | Keep |
| DB | LocalDB in workflow | Keep |
| Browser | Headless Edge via `Config.xml` patch in workflow | Keep |
| Docker publish workflow | [`.github/workflows/publish-to-docker-hub.yml`](../.github/workflows/publish-to-docker-hub.yml) | Does not run E2E today |

**CI policy (recommended):**

- **PR to `main`:** Tier 0 E2E (required) + `dotnet build` solution
- **Nightly or pre-release:** Full E2E tier 0–2
- **Module unit tests:** required on PR once `Visa2026.Module.Tests` exists

---

## 10. Conventions for new tests

### E2E

1. Inherit `E2ETestBase`; do not start the app twice per class.
2. Prefer **Arrange → Act → Assert** with explicit cleanup (delete test rows or use unique codes like `TST`).
3. Use `AppContext.Navigate(...)` ids from the XAF model; document the navigation id in a comment if non-obvious.
4. Avoid `Thread.Sleep`; use EasyTest wait/retry patterns where needed.
5. One scenario per `[Fact]`; shared arrange via protected helpers on the base class.
6. Keep tests **independent** — collection disables parallelization (`DisableTestParallelization = true`) already.

### Unit / integration

1. Mirror namespace/folder layout under `Visa2026.Module` (e.g. `StateEvaluation/VisaStateEvaluatorTests.cs`).
2. Name tests `Method_Scenario_ExpectedOutcome`.
3. Use embedded JSON or minimal BO graphs for evaluator inputs before hitting EF where possible.

### Feature completion (definition of done)

When shipping a user-visible feature:

- [ ] Business rule referenced in PR / plan (BR-xxx or feature plan §)
- [ ] Unit tests for new **pure rule** code
- [ ] Integration test if feature touches **DB, updater, or SQL views**
- [ ] E2E smoke **only if** tier 1–2 officer journey or new navigation/security surface
- [ ] Update §5 or §6 tables in this doc (inventory / backlog status)

---

## 11. Explicitly out of scope for automated E2E

- Every **ApplicationType** and ministry letter variant
- Full **visa state machine** matrix (all BR-050+ paths)
- **Lookup catalog** content correctness (JSON + deploy sync)
- **Word/Excel template layout** QA (manual + Extract/Validate tools)
- **DataImporter** CLI bulk import
- **Production** Docker/IIS deploy verification (separate deploy runbooks/skills)

---

## 12. Phased roadmap

| Phase | Goal | Exit criteria |
|-------|------|----------------|
| **P0** | Stabilize smoke + CI | Tier 0 green on GitHub Actions; dedupe `TestBlazorApp_Opens` |
| **P1** | Module unit project + core evaluators | UT-001, UT-010; 20+ unit tests |
| **P2** | Officer foundation E2E | E2E-010–012; employee/person flows |
| **P3** | Application + dashboard E2E | E2E-020–023; IT-011 |
| **P4** | Compliance + output smoke | E2E-030–032, E2E-040–041 |
| **P5** | Security + new features | E2E-050+, feature-tier tests |

---

## 13. Maintenance

- Update **§5 inventory** when adding/removing E2E facts.
- Update **§6–7 backlog** status (`Done` / `Planned` / `Cancelled`).
- When a feature plan mentions E2E, add a row to §6 and link the plan in §2.
- Review this plan quarterly or after a major state-tracking release.

---

## 14. Revision history

| Date | Version | Change |
|------|---------|--------|
| 2026-05-30 | v0.1 | Initial testing plan (pyramid, E2E inventory, backlog, CI notes) |
