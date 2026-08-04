# visa2026-easytest-e2e — user prompts

Invoke with **`@visa2026-easytest-e2e`** (or this skill path).

---

## Quick start

| You want… | Example prompt |
|-----------|----------------|
| New E2E test | `@visa2026-easytest-e2e Add EasyTest journey for {scenario}. Extend E2ETestBase; run -c EasyTest.` |
| Run headed | `@visa2026-easytest-e2e Run **PersonOfficerJourney_LoginCreateEmployeeAddPassport** headed (Edge on :5050).` |
| Record video | `@visa2026-easytest-e2e Record passport create with **Record-EasyTest.ps1**.` |
| Record video + screenshots | `@visa2026-easytest-e2e Record passport create with **Record-EasyTest.ps1 -Screenshots**.` |
| Fix navigation | `@visa2026-easytest-e2e EasyTest stuck after login on Report Dashboard — shell assert / Employees URL per learnings.md.` |
| Driver setup | `@visa2026-easytest-e2e Install/configure **msedgedriver** matching Edge (microsoft.com CDN).` |
| New scenario map | `@visa2026-easytest-e2e Start **person-employee-passport-create** map in scenarios/examples/ (caption inventory §3).` |
| CI E2E | `@visa2026-easytest-e2e Debug **e2e-tests.yml** / EasyTest build on Windows.` |
| Manual video for guide | `@visa2026-easytest-e2e Record **person-register** with Record-EasyTest.ps1 per USER_MANUAL_E2E_MEDIA.md.` |

---

## Canonical commands

```powershell
dotnet build Visa2026.slnx -c EasyTest
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest --filter "FullyQualifiedName~PersonOfficerJourney_LoginCreateEmployeeAddPassport"
.\scripts\local\Record-EasyTest.ps1 -Screenshots
```