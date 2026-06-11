---
name: visa2026-unit-tests
description: >-
  Creates and runs Visa2026 unit and integration tests (xUnit, Visa2026.Module.Tests).
  Covers state evaluators, pure helpers, ModuleUpdater/EF fixtures, dotnet test commands,
  and naming conventions. Read/append learnings.md for verified positive and negative
  test experience. Use when adding unit tests, integration tests, test project
  scaffolding, dotnet test, UT/IT backlog, or testing Module logic without Blazor E2E.
disable-model-invocation: false
---

# Visa2026: unit and integration tests

## Scope

| Layer | Project | Skill |
|-------|---------|-------|
| **Unit / integration** | `Visa2026.Module.Tests` *(add to solution when missing)* | **This skill** |
| **E2E (Blazor UI)** | `Visa2026.E2E.Tests` | [visa2026-easytest-e2e](../visa2026-easytest-e2e/SKILL.md) |

**Canonical strategy:** [`docs/TESTING_PLAN.md`](../../../docs/TESTING_PLAN.md) (pyramid, CI). **What to test:** [`docs/UNIT_TESTING_PLAN.md`](../../../docs/UNIT_TESTING_PLAN.md) (BOs, evaluators, helpers, UT-010+ backlog).

**Related:** [commit-after-verify](../commit-after-verify/SKILL.md) (build before commit; `dotnet test` optional but recommended when tests exist).

**Experience log:** [learnings.md](./learnings.md) — append-only; makes this skill smarter over time (read before, append after verified tries).

---

## Continuous improvement — read `learnings.md`, then append

This skill **accumulates experience** like on-prem deploy skills ([`on-prem-deploy/MATURITY.md`](../on-prem-deploy/MATURITY.md)).

```text
READ learnings.md → TRY test/fix → TEST dotnet test → RECORD entry → PROMOTE if repeated
```

1. **Before** scaffolding `Visa2026.Module.Tests`, writing evaluator tests, integration DB fixtures, or debugging `dotnet test` / CI: **read** [learnings.md](./learnings.md) — skim **## Entries** for the same evaluator, layer, or error text.
2. **After** a **verified** outcome (passing test, confirmed root cause, or reproducible failure the team should not repeat):
   - Append one dated entry — tag **Outcome**: `positive`, `negative`, or `anti-pattern`.
   - Use the template at the top of **`learnings.md`**.
3. **Promote** when the same lesson hits **twice** (see promotion ladder in **`learnings.md`**): add a short bullet here or a row in **Known pitfalls** below; long details stay in **`learnings.md`**.

**Record both:**
- **Positive (+):** factory pattern, filter command, fixture that saved time — **Reuse** for the next test.
- **Negative (−) / anti-pattern:** dead ends (wrong project reference, wrong DB name, testing Blazor in Module.Tests) — **Avoid** next time.

**Do not** append speculation — only what was verified on a real `dotnet test` run or CI log.

### Known pitfalls (promoted from learnings)

| Pitfall | Do instead |
|---------|------------|
| Unit-test Blazor / XAF UI controls | E2E or host testability (`InputId`); unit-test **Module** evaluators/helpers only |
| Use `Visa2026EasyTest` in Module.Tests | Use **`Visa2026Test`** for integration; E2E owns EasyTest DB — [visa2026-easytest-e2e](../visa2026-easytest-e2e/SKILL.md) |
| Reference `Visa2026.Blazor.Server` from Module.Tests | Reference **`Visa2026.Module`** only |

*(Add rows when learnings entries promote — do not duplicate full stories here.)*

---

## When to use which layer

```
Pure rule (no ObjectSpace)?     → Unit test — static evaluator, parser, calculator
DB / updater / SQL view?        → Integration test — fixture DB, then assert
Officer clicks through Blazor?  → E2E — keep minimal; do not duplicate evaluator matrix
```

**Prioritize unit tests for:** `Visa2026.Module/Services/StateEvaluation/Evaluators/*`, isolated validation, helpers without XAF.

**Defer to E2E:** Appearance, controllers, Blazor editors, navigation.

---

## Quick commands (repo root)

```powershell
# After Visa2026.Module.Tests exists and is in Visa2026.slnx:
dotnet build Visa2026.slnx -c Debug
dotnet test Visa2026.Module.Tests/Visa2026.Module.Tests.csproj -c Debug

# One class or method
dotnet test Visa2026.Module.Tests/Visa2026.Module.Tests.csproj -c Debug --filter "FullyQualifiedName~VisaStateEvaluator"

# With coverage (coverlet already typical in test csproj)
dotnet test Visa2026.Module.Tests/Visa2026.Module.Tests.csproj -c Debug --collect:"XPlat Code Coverage"
```

**E2E is separate** — use [visa2026-easytest-e2e](../visa2026-easytest-e2e/SKILL.md) (Windows, EasyTest config, Edge driver):

```powershell
dotnet build Visa2026.slnx -c EasyTest
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest
```

---

## Add the test project (first time)

1. Create `Visa2026.Module.Tests/` — `net8.0`, **project reference `Visa2026.Module` only** (no Blazor.Server, no E2E packages).
2. Add xUnit packages aligned with E2E: `Microsoft.NET.Test.Sdk` 17.11.1, `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.4, optional `coverlet.collector` 6.0.3.
3. Register in [`Visa2026.slnx`](../../../Visa2026.slnx):  
   `<Project Path="Visa2026.Module.Tests/Visa2026.Module.Tests.csproj" />`
4. Mirror folder layout under `Visa2026.Module` (e.g. `StateEvaluation/VisaStateEvaluatorTests.cs`).

Full **csproj template** and **slnx snippet**: [reference.md](./reference.md).

---

## Writing unit tests

### Conventions

1. **Namespace/folder** mirrors Module (`Services/StateEvaluation/…` → same under test project).
2. **Name:** `Method_Scenario_ExpectedOutcome` (e.g. `Evaluate_CancelledVisa_ReturnsCancelled`).
3. **Arrange** minimal in-memory BOs; avoid EF/XAF unless integration.
4. **Assert** with xUnit `Assert.*` (no new assertion library unless the repo adopts one).
5. **Golden cases** from [`docs/STATE_SPECIFICATIONS.md`](../../../docs/STATE_SPECIFICATIONS.md) and BR rows in TESTING_PLAN §8.

### Static evaluators (preferred first tests)

Many evaluators are **static** with plain BO inputs — no database:

```csharp
// Visa2026.Module.Tests/Services/StateEvaluation/VisaStateEvaluatorTests.cs
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.StateEvaluation.Evaluators;
using Xunit;

namespace Visa2026.Module.Tests.Services.StateEvaluation;

public class VisaStateEvaluatorTests
{
    [Fact]
    public void Evaluate_NullVisa_ReturnsNoVisa()
    {
        var settings = new StateEvaluationSettings();
        var result = VisaStateEvaluator.Evaluate(null!, settings);
        Assert.Equal("NoVisa", result.StateCode);
    }
}
```

Build **small factory helpers** in the test project (e.g. `TestVisaFactory.Active(expiresInDays: 30)`) — do not commit huge JSON unless the case is shared (UT-010 golden file).

### What not to unit-test here

- DevExpress UI, `ObjectSpace` controller actions, Blazor components → E2E or manual.
- Full XAF model / `Model.xafml` wiring.

---

## Integration tests (same project)

Use when logic needs **EF**, **`ModuleUpdater`**, or **SQL views** (dashboard count = list filter).

| Concern | Approach |
|---------|----------|
| Database | Dedicated DB name **`Visa2026Test`** on `(localdb)\mssqllocaldb` — **not** `Visa2026EasyTest` (E2E) |
| Migrations | Run Module updaters once per fixture/collection; see [`docs/ENVIRONMENTS.md`](../../../docs/ENVIRONMENTS.md) for `FORCE_XAF_DB_UPDATE` if schema stuck |
| Lookup sync | Assert after `LookupCatalogSyncUpdater` — [`docs/LOOKUP_SEEDING.md`](../../../docs/LOOKUP_SEEDING.md) |
| Speed | Fewer, coarser tests than unit; share DB via `IClassFixture` / collection fixture |

Integration setup is heavier than evaluators; add **reference.md** patterns when the first IT-010 lands. Prefer extracting **pure functions** from updaters when possible so unit tests cover the rule and IT only checks persistence.

---

## Agent workflow

When the user asks for a unit test:

1. **Read** [learnings.md](./learnings.md) (## Entries) for related evaluator, DB, or `dotnet test` lessons.
2. Read **target production code** in `Visa2026.Module` (evaluator, helper, validation).
3. Check **TESTING_PLAN** backlog id (UT-xxx / IT-xxx) if mentioned.
4. If `Visa2026.Module.Tests` missing → scaffold per [reference.md](./reference.md), then add test.
5. Run `dotnet test` on **Module.Tests** only; fix compile/assert failures.
6. **Append** [learnings.md](./learnings.md) if the run surfaced a reusable positive or negative lesson (not for trivial typos).
7. Update TESTING_PLAN §7 table row **Status** when closing a backlog item (if user wants doc sync).

When the user asks to **run all tests**:

```powershell
dotnet test Visa2026.slnx -c Debug
```

Note: solution may include E2E — on non-Windows or without Edge driver, filter:

```powershell
dotnet test Visa2026.Module.Tests/Visa2026.Module.Tests.csproj -c Debug
```

---

## Definition of done (feature + tests)

- [ ] New **pure rule** code has unit tests
- [ ] DB/updater/SQL surface has integration test (or documented deferral)
- [ ] E2E only for new navigation/security journey (see TESTING_PLAN §10)
- [ ] No secrets in test connection strings committed (use LocalDB + test DB names)

---

## Additional resources

- [learnings.md](./learnings.md) — append-only positive/negative experience (read before, append after verify)
- [reference.md](./reference.md) — csproj template, packages, integration notes, troubleshooting
- [visa2026-easytest-e2e](../visa2026-easytest-e2e/SKILL.md) — Blazor E2E (`Visa2026.E2E.Tests`, EasyTest config)
- [`docs/TESTING_PLAN.md`](../../../docs/TESTING_PLAN.md) — inventory, CI policy, E2E separation
- [`docs/BO_STATE_TRACKING.md`](../../../docs/BO_STATE_TRACKING.md) — state codes for evaluator cases
