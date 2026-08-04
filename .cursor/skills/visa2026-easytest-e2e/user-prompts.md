# visa2026-easytest-e2e — user prompts

Invoke with **`@visa2026-easytest-e2e`** (or this skill path).

---

## Quick start

| You want… | Example prompt |
|-----------|----------------|
| New EasyTest journey | `@visa2026-easytest-e2e Add EasyTest journey for {scenario}. Extend E2ETestBase; run -c EasyTest.` |
| Custom-component / Playwright | `@visa2026-easytest-e2e EasyTest cannot drive {custom UI}. Add Playwright test under Playwright/; reuse :5050 host; add data-testid if needed.` |
| Preview slot / Resminamalar / Document copies UI | `@visa2026-easytest-e2e Cover {feature} with Playwright (custom Blazor) — not EasyTest FillForm.` |
| Run headed + media (default) | `@visa2026-easytest-e2e Run passport create with **Record-EasyTest.ps1** (video + screenshots ON by default).` |
| Run headed EasyTest only | `@visa2026-easytest-e2e Run **PersonOfficerJourney_LoginCreateEmployeeAddPassport** headed (Edge on :5050).` |
| Run without media | `@visa2026-easytest-e2e Run with Record-EasyTest **-NoRecord -NoScreenshots**.` |
| Run Playwright filter | `@visa2026-easytest-e2e Run Playwright E2E: --filter "Driver=Playwright".` |
| Fix navigation | `@visa2026-easytest-e2e EasyTest stuck after login on Report Dashboard — shell assert / Employees URL per learnings.md.` |
| Driver setup | `@visa2026-easytest-e2e Install/configure **msedgedriver** matching Edge (microsoft.com CDN).` |
| Playwright browsers | `@visa2026-easytest-e2e Install Playwright browsers for Visa2026.E2E.Tests.` |
| New scenario map | `@visa2026-easytest-e2e Start **person-employee-passport-create** map in scenarios/examples/ (caption inventory §3; set driver: easytest\|playwright).` |
| CI E2E | `@visa2026-easytest-e2e Debug **e2e-tests.yml** / EasyTest build on Windows.` |
| Manual media for guide | `@visa2026-easytest-e2e Record **person-register** with Record-EasyTest.ps1 per USER_MANUAL_E2E_MEDIA.md (defaults ON).` |

---

## Canonical commands

```powershell
dotnet build Visa2026.slnx -c EasyTest
.\scripts\local\Record-EasyTest.ps1
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest --filter "FullyQualifiedName~PersonOfficerJourney_LoginCreateEmployeeAddPassport"
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest --filter "Driver=Playwright"
```