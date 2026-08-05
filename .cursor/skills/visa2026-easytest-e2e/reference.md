# visa2026-easytest-e2e — reference

## Stack summary

| Item | Value |
|------|--------|
| Skill | **visa2026-easytest-e2e** (name historical) |
| Driver | **Playwright only** (new work) |
| EasyTest | **Deprecated** — legacy Facts only until migrated |
| Project | `Visa2026.E2E.Tests` → **`Playwright/`** |
| Port | **5050** |
| DB | **Visa2026EasyTest** / Postgres `visa2026_easytest` |
| Selectors | Label / role / **`data-testid`** / stable CSS |
| DetailView fill | **Top → bottom** (layout order) |
| MSBuild config | **EasyTest** (host + test project still use this name) |
| CI | `.github/workflows/e2e-tests.yml` |

---

## Host and browser

### Launch

Host: Postgres **`visa2026_easytest`** on `:5050` (not IDE `:5000`).

- Built `Visa2026.Blazor.Server.exe` with `--urls http://localhost:5050 --environment Development`
- Connection string / DB name triggers **`EasyTestHostMode`** (ephemeral TabbedMDI — name historical)
- Do **not** invent a second port or DB for Playwright

### Headed vs headless

| Variable | Effect |
|----------|--------|
| *(none)* on dev PC | Headed |
| `VISA2026_E2E_HEADLESS=true` | Headless |
| `VISA2026_E2E_HEADED=true` | Headed |
| Windows CI | Prefer **headed** (Blazor stability) |

Reuse the same env intent as legacy `EasyTestBrowserMode` until Playwright fixture owns the flag.

### Blazor host test mode

- **`EasyTestHostMode`** — when connection string contains **`Visa2026EasyTest`** or `VISA2026_EASYTEST=true`
- Ephemeral user model differences — no sticky Family Members tabs for `StandardUser`

---

## Playwright (canonical)

### Project layout

| Path / item | Role |
|-------------|------|
| `Visa2026.E2E.Tests/Playwright/` | All new `[Fact]` classes |
| `[Trait("Driver", "Playwright")]` | `--filter "Driver=Playwright"` |
| `Microsoft.Playwright` | PackageReference |
| Shared host preflight | Same `:5050` / DB drop as legacy session fixture |

### Install browsers

```powershell
dotnet build Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest
pwsh Visa2026.E2E.Tests/bin/EasyTest/net8.0/playwright.ps1 install msedge
# chromium is acceptable if tests target Chromium explicitly
```

CI: install Playwright browsers in **`e2e-tests.yml`** for Playwright suites; msedgedriver remains only for unmigrated legacy EasyTest Facts.

### Login

```csharp
await page.GotoAsync("http://localhost:5050/");
await page.GetByLabel("User Name").FillAsync(E2ETestLoginValues.StandardUserName);
await page.GetByLabel("Password").FillAsync(E2ETestLoginValues.StandardUserPassword);
await page.GetByRole(AriaRole.Button, new() { Name = "Log In" }).ClickAsync();
```

### Navigation

```csharp
// Typed Person lists — URL, not sidebar alone
await page.GotoAsync("http://localhost:5050/Person_ListView_Employees");
// Do NOT open Application list for StandardUser
```

### DetailView fill — top to bottom

```csharp
// Field list MUST match DetailView layout order (top → bottom).
await FillDetailViewTopToBottomAsync(page, new (string Label, string Value)[]
{
    ("Personal Number", "E2E-EMP-021"),
    ("First Name", "Ferdi"),
    ("Last Name", "Test"),
    // next visible editors downward…
});
```

Rules:

1. One field at a time in **array order**.
2. Wait for Blazor/lookup settle before the next field.
3. Finish current tab/group before switching tabs.
4. Map §3 and yaml `fill:` use the **same order**.

### Custom UI

```csharp
await page.GetByTestId("resminamalar-catalog").ClickAsync();
await page.Locator("#visa-preview-slot.visa-preview-slot--open").WaitForAsync();
```

### Media

- Screenshots: ON by default (`VISA2026_E2E_SCREENSHOTS`); use `page.ScreenshotAsync` into `recordings/screenshots/{run}/`.
- Video: `Record-EasyTest.ps1` ffmpeg desktop (default ON) until Playwright video context is standardized.
- Opt out: `-NoRecord` / `-NoScreenshots` / env false.

### Anti-patterns

| Avoid | Prefer |
|-------|--------|
| New EasyTest / `E2ETestBase` Facts | Playwright under `Playwright/` |
| Unordered / alphabetical DetailView fill | Top → bottom layout order |
| Second Kestrel port | Share `:5050` |
| Fragile XPath | Label / `data-testid` / role |
| Extending EasyTest for custom Razor | Playwright + `data-testid` |

---

## E2E constants (`E2ETestDataSeed.cs`)

| Type | Purpose |
|------|---------|
| `E2ETestEmployeeCreateValues` | Employee field values |
| `E2ETestPassportCreateValues` | Passport field values |
| `E2ETestPassportCreateOnlyJourneyValues` | Short passport journey IDs |
| `E2ETestLoginValues` | `StandardUser` (+ empty password), view paths |

---

## Build and test

```powershell
dotnet build Visa2026.slnx -c EasyTest
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest --filter "Driver=Playwright"
.\scripts\local\Record-EasyTest.ps1 -Filter 'YourPlaywrightFact'
```

---

## CI

**`.github/workflows/e2e-tests.yml`** — build `-c EasyTest`, Windows runner, video on long runs, screenshots artifact. Add Playwright browser install when Playwright Facts are the gate; keep legacy EasyTest steps only until migration completes.

---

## User manual media

Contract: [`docs/USER_MANUAL_E2E_MEDIA.md`](../../../docs/USER_MANUAL_E2E_MEDIA.md).

```powershell
.\scripts\local\Record-EasyTest.ps1 `
  -Filter 'YourPlaywrightFact' `
  -OutputName 'person-register.mp4'
# video + screenshots ON by default
```

---

## Scenario metadata (Option A)

| Path | Role |
|------|------|
| `scenarios/README.md` | Workflow |
| `scenarios/examples/` | Draft maps + yaml |
| `scenarios/ready/` | Promoted specs |
| [reference-map-contract.md](./reference-map-contract.md) | Map + yaml + Playwright C# |

| YAML | Playwright C# |
|------|----------------|
| `login:` | Label fill + Log In click |
| `goto: Person_ListView_Employees` | `GotoAsync` Employees URL |
| `fill:` (ordered) | `FillDetailViewTopToBottomAsync` |
| `action: Save` | `GetByRole(Button, Save)` |
| `assert-shell:` | Employees URL / shell locator |

---

## File map

| Path | Role |
|------|------|
| `Visa2026.E2E.Tests/Playwright/` | **Canonical** Playwright tests |
| `Visa2026.Module/DatabaseUpdate/E2ETestDataSeed.cs` | Shared constants |
| `Visa2026.Blazor.Server/EasyTestHostMode.cs` | Test host detection (name historical) |
| `scripts/local/Record-EasyTest.ps1` | Media capture (name historical) |
| `E2ETestBase.cs`, `PersonOfficerJourneyTests.cs`, `Config.xml`, msedgedriver helpers | **Deprecated EasyTest legacy** — migrate, do not extend |

---

## Deprecated EasyTest (legacy only)

Kept for unmigrated Facts and historical learnings. **Do not use for new work.**

Former patterns (do not copy):

- `E2ETestBase` + `IApplicationContext` + `EasyTestParameter`
- `FillForm` / `FillFormWithRetry` / `AppContext.Navigate`
- `msedgedriver` for new suites (Playwright manages browser)

When touching a legacy Fact: **rewrite in Playwright** with top→bottom DetailView fill, then remove the EasyTest Fact.