# visa2026-easytest-e2e — user prompts

Invoke with **`@visa2026-easytest-e2e`** (or this skill path).

**Driver:** Playwright only. EasyTest is deprecated.

---

## Quick start

| You want… | Example prompt |
|-----------|----------------|
| New E2E journey | `@visa2026-easytest-e2e Add Playwright journey for {scenario} under Playwright/. Fill DetailView top→bottom. Run -c EasyTest.` |
| Migrate legacy EasyTest | `@visa2026-easytest-e2e Migrate **PersonOfficerJourney_*** from EasyTest to Playwright; keep E2E ids; top→bottom fills.` |
| Custom Blazor / preview slot | `@visa2026-easytest-e2e Cover {feature} with Playwright + data-testid.` |
| Run headed + media | `@visa2026-easytest-e2e Run with **Record-EasyTest.ps1** (video + screenshots ON by default).` |
| Run without media | `@visa2026-easytest-e2e Run with **-NoRecord -NoScreenshots**.` |
| Run Playwright filter | `@visa2026-easytest-e2e Run `--filter "Driver=Playwright"`.` |
| New scenario map | `@visa2026-easytest-e2e Start map in scenarios/examples/ with **driver: playwright**; §3 locators in top→bottom order.` |
| Install browsers | `@visa2026-easytest-e2e Install Playwright browsers for Visa2026.E2E.Tests.` |
| CI E2E | `@visa2026-easytest-e2e Debug **e2e-tests.yml** / Playwright on Windows.` |
| Manual media | `@visa2026-easytest-e2e Record guide media via Record-EasyTest.ps1 per USER_MANUAL_E2E_MEDIA.md.` |

---

## Canonical commands

```powershell
dotnet build Visa2026.slnx -c EasyTest
.\scripts\local\Record-EasyTest.ps1 -Filter 'YourPlaywrightFact'
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest --filter "Driver=Playwright"
```