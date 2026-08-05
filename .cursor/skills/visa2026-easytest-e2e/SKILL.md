---
name: visa2026-easytest-e2e
description: >-
  Creates and runs Visa2026 E2E tests with Microsoft Playwright only
  (Visa2026.E2E.Tests/Playwright/, xUnit, Edge/Chromium, :5050, Postgres
  visa2026_easytest). DetailView forms filled top-to-bottom. DevExpress EasyTest
  is deprecated — do not add new EasyTest Facts. Covers Playwright fixtures,
  locators (label/data-testid/role), Record-EasyTest.ps1 media defaults, UserManual
  trait, e2e-tests.yml. Use for E2E, officer journeys, custom Blazor UI tests,
  headed runs, user-manual media. Skill folder name is historical.
disable-model-invocation: false
---

# Visa2026: Playwright E2E (EasyTest deprecated)

## Purpose

**Author and run** officer-journey tests in **`Visa2026.E2E.Tests`** using **Microsoft Playwright** only.

| Driver | Status |
|--------|--------|
| **Playwright** (`.NET` `Microsoft.Playwright` + xUnit) | **Required for all new E2E** |
| **DevExpress EasyTest** (Blazor adapter + Selenium) | **Deprecated** — do not add or extend; migrate when touching a journey |

Skill folder / MSBuild config name **`EasyTest`** and script **`Record-EasyTest.ps1`** are **historical** (host build config + media helper). They do **not** mean new tests should use the EasyTest API.

**Project layout:** new tests under **`Visa2026.E2E.Tests/Playwright/`** with `[Trait("Driver", "Playwright")]`.

**Strategy:** [`docs/TESTING_PLAN.md`](../../../docs/TESTING_PLAN.md). **Experience:** [learnings.md](./learnings.md) — read before, append after verified runs.

**User manual media:** [`docs/USER_MANUAL_E2E_MEDIA.md`](../../../docs/USER_MANUAL_E2E_MEDIA.md) · [`docs/USER_MANUAL_PIPELINE.md`](../../../docs/USER_MANUAL_PIPELINE.md) · [visa2026-user-manual](../visa2026-user-manual/SKILL.md)

**Critical:** UserManual E2E is invoked by **`Build-UserManual.ps1`**. Tag tests `[Trait("Category", "UserManual")]`.

---

## Deprecated: EasyTest

- **Do not** create new `E2ETestBase` Facts, `.ets` scripts, or EasyTest `FillForm` / `GetAction` journeys.
- **Do not** “fix” EasyTest flakiness by adding more EasyTest helpers — rewrite the journey in Playwright.
- Legacy files (`E2ETestBase`, `PersonOfficerJourneyTests`, `Config.xml`, msedgedriver sync) may remain until migrated; treat as **read-only legacy**.
- When migrating a Fact: keep the same **E2E-xxx** id / scenario folder; set map `driver: playwright`; delete or `#if false` the EasyTest Fact only after Playwright is green in CI.

---

## DetailView form fill — top to bottom (mandatory)

All **DetailView** (and Logon) field entry must simulate an officer: **top → bottom** in layout order.

| Rule | Do | Do not |
|------|-----|--------|
| Order | Fill editors in **visual layout order** (top to bottom; left to right within the same row) | Random / alphabetical / “all textboxes then lookups” |
| Tabs | Complete the **active layout group/tab** top→bottom before switching tabs | Jump between tabs mid-form |
| Lookups / dates | Fill when reached in layout order (wait for Blazor settle), then continue downward | Prefill hidden/off-screen fields out of order |
| Map / YAML | §3 inventory and `fill:` keys listed **in fill order** | Unordered caption bags |
| Helpers | Shared `Fill*TopToBottom` helpers that take an ordered field list | Parallel fills / unordered dictionaries applied in hash order |

**Why:** Matches real officer behavior for user-manual video/screenshots and catches layout/tab-order bugs EasyTest batch fill hid.

---

## User prompts

Copy-paste catalog: [user-prompts.md](./user-prompts.md). Invoke with **`@visa2026-easytest-e2e`**.

---

## Process (new E2E test)

```text
1. MAP      — scenarios/examples/<id>_map.md; driver: playwright; §3 locators in top→bottom fill order; E2E-xxx id
2. YAML     — scenarios/examples/<id>.yaml when map Ready (Option A: spec only)
3. C#       — Playwright/[Fact] under Playwright/; trait Driver=Playwright; top→bottom DetailView fill
4. HOOKS    — add data-testid / accessible names in Blazor when locators are missing
5. BUILD    — dotnet build Visa2026.slnx -c EasyTest
6. RUN      — Record-EasyTest.ps1 or dotnet test -c EasyTest --filter "Driver=Playwright"
7. PROMOTE  — move map + yaml to scenarios/ready/ when CI-stable
8. RECORD   — append learnings.md on non-obvious fixes
9. USERMANUAL — [Trait("Category", "UserManual")]; media defaults ON
```

Map contract: [reference-map-contract.md](./reference-map-contract.md). Inventory: [`Visa2026.E2E.Tests/scenarios/README.md`](../../../Visa2026.E2E.Tests/scenarios/README.md).

Maps: **`driver: playwright` only** for new work (no `easytest` / `hybrid` for new scenarios).

---

## Host isolation (mandatory)

Playwright must **not** share the IDE dev host (`:5000` / `:5001`).

| Setting | Value |
|---------|--------|
| URL | **`http://localhost:5050`** |
| DB | **`visa2026_easytest`** on local **PostgreSQL** |
| Build config | **`EasyTest`** (MSBuild name only — still required for host/test project) |
| Browser | **Edge** (or Chromium) — **headed** locally; Windows CI headed |

**TabbedMDI / saved tabs:** host sets ephemeral user model differences when the EasyTest DB is detected (`EasyTestHostMode`). Without this, **`StandardUser`** can reopen **Family Members** instead of Employees.

**Preflight:** keep using existing host launch on **`:5050`** (drop/recreate DB, `--updateDatabase`, built `.exe` with `--urls http://localhost:5050 --environment Development`). One host per session — no second port/DB. Details: [reference.md](./reference.md#host-and-browser).

---

## Writing Playwright tests

### Layout

| Item | Value |
|------|--------|
| Folder | `Visa2026.E2E.Tests/Playwright/` |
| Trait | `[Trait("Driver", "Playwright")]` |
| Package | `Microsoft.Playwright` (+ xUnit fixture) |
| Login | `E2ETestLoginValues.StandardUserName` = **`StandardUser`** (not `standarduser`) + empty password |
| OS | `[SupportedOSPlatform("windows")]` while Edge E2E stays Windows-only |

### Selectors

Prefer, in order: accessible **label** / role → **`data-testid`** → stable CSS (`#visa-preview-slot`, …). Avoid brittle absolute XPath.

Add **`data-testid`** on custom Blazor (preview slot, Resminamalar, Document copies) when missing.

### Navigation (critical)

| Target | Do | Do not |
|--------|-----|--------|
| After login | Probe shell via Employees URL **`/Person_ListView_Employees`** | Open Application list (Users role denies; home is Report Dashboard) |
| Employees list | `page.GotoAsync(.../Person_ListView_Employees)` (or shared helper) | Sidebar “Employees” alone (TabbedMDI may stay on Family Members) |
| Passport | Employee detail → **Passports** tab → **New Passport** | Lookup/Passport sidebar |
| After New employee | Assert employee DetailView (form / URL) | Assume list context from nav highlight |

Constants: `E2ETestLoginValues`, `E2ETestPassportCreateOnlyJourneyValues`, etc. in `Visa2026.Module/DatabaseUpdate/E2ETestDataSeed.cs`.

### DetailView fill pattern

```csharp
// Ordered top → bottom (match DetailView layout / map §3)
await FillDetailViewTopToBottomAsync(page, new (string Label, string Value)[]
{
    ("Personal Number", values.PersonalNumber),
    ("First Name", values.FirstName),
    ("Last Name", values.LastName),
    // … next layout fields downward …
});
await page.GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
```

Implement / extend a shared helper that fills **one field at a time in array order** (never `Dictionary` iteration order).

### Media

Screenshots and video **ON by default** for user-manual generation — see [Media defaults](#media-defaults-user-manual). Prefer Playwright `page.ScreenshotAsync` + `Record-EasyTest.ps1` (ffmpeg desktop) until a Playwright-native recorder replaces the script name.

### Test data

- Create person/passport via **UI** — no DB person seed for officer journeys.
- Distinct IDs: `E2ETestPassportCreateOnlyJourneyValues` (`E2E-EMP-021` / `E2E-PASS-021`) when sharing a session DB.
- Lookups from normal `ModuleUpdater` on `visa2026_easytest`.

---

## Run commands (repo root)

```powershell
dotnet build Visa2026.slnx -c EasyTest
# Preferred (video + screenshots ON by default):
.\scripts\local\Record-EasyTest.ps1 -Filter 'YourPlaywrightFactName'
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest --filter "Driver=Playwright"
# Opt out of media: .\scripts\local\Record-EasyTest.ps1 -NoRecord -NoScreenshots
```

**Prerequisites:** Windows, local PostgreSQL, Playwright browsers installed ([reference.md](./reference.md#install-browsers)), ffmpeg for desktop video (`Visa2026.E2E.Tests\.tools\ffmpeg\` or PATH).

### Browser mode

| Environment | Window | How |
|-------------|--------|-----|
| Local (default) | Headed | No env / `VISA2026_E2E_HEADED=true` |
| Windows CI | Headed | Prefer headed for Blazor stability |
| Force headless | Hidden | `VISA2026_E2E_HEADLESS=true` |

---

## Current inventory

| Test class | Focus | Status |
|------------|--------|--------|
| `PersonOfficerJourneyTests` | Passport + master-data CRUD | **Legacy EasyTest** — migrate to `Playwright/` when next touched |
| `Playwright/*` | All new journeys | **Canonical** |

CI: **`.github/workflows/e2e-tests.yml`**. Project README may still describe EasyTest — follow **this skill** for new work.

---

## Media defaults (user manual)

| Media | Default | Opt out |
|-------|---------|---------|
| Screenshots | ON | `VISA2026_E2E_SCREENSHOTS=false` or `-NoScreenshots` |
| Desktop video | ON in `Record-EasyTest.ps1` | `-NoRecord` |

Prefer **`Record-EasyTest.ps1`** when MP4 is required. Coordinate guides with [visa2026-user-manual](../visa2026-user-manual/SKILL.md). Full contract: [`docs/USER_MANUAL_E2E_MEDIA.md`](../../../docs/USER_MANUAL_E2E_MEDIA.md).

---

## Agent workflow

When the user asks for **E2E**, **Playwright**, **EasyTest** (redirect), or **`Visa2026.E2E.Tests`**:

1. **Read** [learnings.md](./learnings.md).
2. **Use Playwright only** — if asked for EasyTest, explain it is deprecated and implement Playwright.
3. **Map first** with `driver: playwright` and §3 in **top→bottom** fill order.
4. **Implement** under `Playwright/`; reuse host/login constants; add `data-testid` when needed.
5. **Fill DetailViews top→bottom** (mandatory).
6. **Build** `-c EasyTest`; **run** filtered Playwright tests; ensure browsers installed.
7. **Append** learnings after verified fixes.
8. **Media** ON by default for UserManual journeys.

---

## Known pitfalls

| Pitfall | Do instead |
|---------|------------|
| New EasyTest Fact / `E2ETestBase` extension | **Playwright** under `Playwright/` |
| Fill DetailView fields out of layout order | **Top → bottom** ordered helper |
| Run E2E on `:5000` | **`:5050`** + `visa2026_easytest` |
| Login `standarduser` | **`StandardUser`** |
| Sidebar Employees → Family Members | Goto **`/Person_ListView_Employees`** |
| Passport via Lookup | Nested **Passports** → New Passport |
| Second host/port for Playwright | Share **`:5050`** preflight |
| Fragile XPath | Label / `data-testid` / role |

---

## Additional resources

- [user-prompts.md](./user-prompts.md)
- [reference.md](./reference.md) — host, Playwright patterns, deprecated EasyTest notes
- [reference-map-contract.md](./reference-map-contract.md)
- [learnings.md](./learnings.md)
- [`scripts/local/Record-EasyTest.ps1`](../../../scripts/local/Record-EasyTest.ps1) — media (name historical)
- [`docs/USER_MANUAL_E2E_MEDIA.md`](../../../docs/USER_MANUAL_E2E_MEDIA.md)
- [visa2026-user-manual](../visa2026-user-manual/SKILL.md)
- [`docs/TESTING_PLAN.md`](../../../docs/TESTING_PLAN.md)