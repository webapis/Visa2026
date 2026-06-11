# visa2026-easytest-e2e — user prompts

Invoke with **`@visa2026-easytest-e2e`** (or this skill path).

**Not this skill:** Playwright YAML → [visa2026-ui-scenarios](../visa2026-ui-scenarios/SKILL.md); CSS hooks → [visa2026-ui-test-hooks](../visa2026-ui-test-hooks/SKILL.md); unit tests → [visa2026-unit-tests](../visa2026-unit-tests/SKILL.md).

---

## Quick start

| You want… | Example prompt |
|-----------|----------------|
| New E2E test | `@visa2026-easytest-e2e Add EasyTest **EmployeeTests**-style test for {journey}. Extend E2ETestBase; run -c EasyTest.` |
| Run headed | `@visa2026-easytest-e2e Run **EmployeeTests** headed (Edge on :5050).` |
| Fix navigation | `@visa2026-easytest-e2e EasyTest opens **Family Members** instead of **Employees** — fix navigation per learnings.md.` |
| Driver setup | `@visa2026-easytest-e2e Install/configure **msedgedriver** for Visa2026.E2E.Tests.` |
| Mirror UiScenario | `@visa2026-easytest-e2e Port **person-employee-create** UiScenario to native EasyTest (captions, E2ETestDataSeed constants).` |
| CI E2E | `@visa2026-easytest-e2e Debug **e2e-tests.yml** / EasyTest build on Windows.` |

---

## Canonical commands

```powershell
dotnet build Visa2026.slnx -c EasyTest
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest --filter "FullyQualifiedName~EmployeeTests"
```

---

## Wrong skill routing

| Request | Skill |
|---------|--------|
| `data-testid`, `UI_TEST_HOOKS.md` | visa2026-ui-test-hooks |
| YAML scenario, `Invoke-UiScenarioRun.ps1`, `:5052` | visa2026-ui-scenarios |
| Evaluator unit test, `Visa2026.Module.Tests` | visa2026-unit-tests |
| EasyTest E2E class, `E2ETestBase`, Edge `:5050` | **visa2026-easytest-e2e** |
