---
name: visa2026-easytest-e2e
description: >-
  Creates and runs Visa2026 E2E tests: native XAF EasyTest (C# API, Edge/Selenium)
  by default, plus Microsoft Playwright where EasyTest is unsupported or weak
  (custom Blazor components, preview slot, Resminamalar/Document copies UI).
  Covers E2ETestBase, :5050, Postgres visa2026_easytest, msedgedriver, caption
  FillForm/Navigate, Playwright locators (data-testid/CSS/role), Record-EasyTest.ps1,
  and dotnet test -c EasyTest. Use when adding EasyTest or Playwright E2E,
  PersonOfficerJourneyTests, custom-component UI tests, headed Edge, or e2e-tests.yml.
  Officer-manual media: docs/USER_MANUAL_E2E_MEDIA.md and visa2026-user-manual skill.
disable-model-invocation: false
---

# Visa2026: XAF EasyTest E2E (native) + Playwright fallback

## Purpose

**Author and run** officer-journey tests in **`Visa2026.E2E.Tests`**.

| Driver | When |
|--------|------|
| **EasyTest** (Blazor adapter + xUnit + Edge/Selenium) | Default for standard XAF ListView / DetailView / toolbar / caption forms |
| **Playwright** (.NET `Microsoft.Playwright`) | Where EasyTest is **not supported** or **lacks proper support** — especially **custom Blazor components** |

Tests use the **C# API** (`IApplicationContext`, `EasyTestParameter`) for EasyTest — not `.ets` scripts in this repo. Playwright tests use page locators (`data-testid`, role, CSS).

**Project:** `Visa2026.E2E.Tests` — EasyTest + (when needed) Playwright in the same E2E project unless a split is justified later.

**Strategy context:** [`docs/TESTING_PLAN.md`](../../../docs/TESTING_PLAN.md). **Experience log:** [learnings.md](./learnings.md) — read before, append after verified runs.

**User manual media (interlocked):** [`docs/USER_MANUAL_E2E_MEDIA.md`](../../../docs/USER_MANUAL_E2E_MEDIA.md) · [`docs/USER_MANUAL_PIPELINE.md`](../../../docs/USER_MANUAL_PIPELINE.md) · [visa2026-user-manual](../visa2026-user-manual/SKILL.md)

**Critical:** UserManual E2E is invoked by **`Build-UserManual.ps1`**, not a separate doc-update workflow. Tag tests `[Trait("Category", "UserManual")]`.

---

## EasyTest vs Playwright (choose deliberately)

**Default to EasyTest.** Switch to Playwright only when EasyTest cannot drive the surface reliably.

| Surface | Prefer | Why |
|---------|--------|-----|
| Logon, XAF nav, ListView/DetailView, standard PropertyEditors | **EasyTest** | Caption/`FillForm`/`GetAction` match model |
| Nested New on typed collections (Passports, Educations, …) | **EasyTest** first | Existing `E2ETestBase` helpers |
| Custom Razor editors / non-XAF DOM (Document copies, Resminamalar catalog, preview slot `#visa-preview-slot`, dossier panels, JS-heavy chrome) | **Playwright** | EasyTest captions / form API do not map cleanly |
| Drag-resize, canvas, file-input quirks, shadow/portal UI | **Playwright** | Locator + browser APIs |
| Hybrid journey (XAF create → open custom dialog) | **EasyTest for XAF steps**, then **Playwright** for the custom panel — or full Playwright if switching drivers mid-test is painful |

**Do not** replace working EasyTest officer journeys with Playwright. **Do not** force EasyTest `FillForm` on custom components when locators are the stable contract — add `data-testid` (or stable ids/classes) in Blazor and assert with Playwright.

**Selector policy:**

- EasyTest: **English model captions** (+ `FillFormWithRetry` / `InputId` fallbacks).
- Playwright: **`data-testid`**, roles, and stable CSS (`#visa-preview-slot`, …). Prefer `data-testid` on new custom UI. Avoid brittle XPath/absolute CSS.

Host isolation is the same for both: **`:5050`** + Postgres **`visa2026_easytest`**. Details: [reference.md § Playwright](./reference.md#playwright-fallback).

---

## User prompts

Copy-paste catalog: [user-prompts.md](./user-prompts.md). Invoke with **`@visa2026-easytest-e2e`**.

---

## Process (new E2E test)

```text
1. CHOOSE   — EasyTest (default) vs Playwright (custom/unsupported surface); note in map
2. MAP      — scenarios/examples/<id>_map.md (caption inventory §3) + E2E-xxx id
3. YAML     — scenarios/examples/<id>.yaml when map Ready for YAML (Option A: spec only)
4. C#       — *Tests.cs [Fact]; EasyTest via E2ETestBase, or Playwright*Tests + shared host helpers
5. BUILD    — dotnet build Visa2026.slnx -c EasyTest
6. RUN      — dotnet test Visa2026.E2E.Tests -c EasyTest --filter "FullyQualifiedName~YourTests"
7. PROMOTE  — move map + yaml to scenarios/ready/ when CI-stable
8. RECORD   — append learnings.md on non-obvious fixes (nav, captions, driver, host, Playwright locators)
9. USERMANUAL — add `[Trait("Category", "UserManual")]`; media via Record-EasyTest / Build-UserManual.ps1
```

**Scenario metadata (Option A):** YAML documents steps; C# executes them. Map contract: [reference-map-contract.md](./reference-map-contract.md). Manual media: [`docs/USER_MANUAL_E2E_MEDIA.md`](../../../docs/USER_MANUAL_E2E_MEDIA.md). Inventory: [`Visa2026.E2E.Tests/scenarios/README.md`](../../../Visa2026.E2E.Tests/scenarios/README.md).

In `*_map.md` §0 or frontmatter, set **`driver: easytest`** or **`driver: playwright`** (or `hybrid`) so agents do not guess.

---

## Host isolation (mandatory)

EasyTest **and** Playwright must **not** share the IDE dev host (`:5000` / `:5001`).

| Setting | Value |
|---------|--------|
| URL | **`http://localhost:5050`** |
| DB | **`visa2026_easytest`** on local **PostgreSQL** (`PG_*` / default `Visa2026Local`) |
| Build config | **`EasyTest`** |
| Browser | **Edge** — **headed** locally; on Windows CI keep headed (`EasyTestBrowserMode`) |

**TabbedMDI / saved tabs:** EasyTest host sets ephemeral user model differences when the EasyTest DB is detected (see `EasyTestHostMode` in Blazor.Server). Without this, **`StandardUser`** can reopen **Family Members** instead of Employees.

Full host + driver setup: [reference.md § Host and driver](./reference.md#host-and-driver).

**Preflight (session):** `EasyTestPreflight` checks **`:5050`** is free, drops/recreates Postgres **`visa2026_easytest`**, runs **`--updateDatabase --silent`**, then `RunApplication` on the built **`.exe`** with **`--urls http://localhost:5050 --environment Development`**. Teardown closes host in `EasyTestSessionFixture.DisposeAsync`. Playwright tests should reuse the same preflight/host pattern (or attach to an already-launched EasyTest host) — do not invent a second DB/port.

---

## Writing tests

### Base class (EasyTest)

- Inherit **`E2ETestBase`** (collection fixture — one host/browser session; DB dropped once per run).
- **`[SupportedOSPlatform("windows")]`** on test class/method (Edge E2E is Windows-only today).
- Use **`Login(userName, password)`** — officer flows: **`E2ETestLoginValues.StandardUserName`** (`StandardUser`, seeded in `Updater`) + empty password. Not `standarduser`.

### Selectors: captions (EasyTest), hooks (Playwright)

EasyTest fills fields by **English model caption** (`EasyTestParameter("First Name", value)`). Keep captions aligned with embedded model / en-US.

For **custom Blazor** surfaces, prefer **`data-testid`** (or stable element id/class) in Module/Blazor host markup, then drive with **Playwright**. Do not stretch EasyTest caption fill for non-PropertyEditor UI. `E2ETestBase.FillFormWithRetry` may fall back to `data-testid` via `EasyTestBlazorNavigationHelper` when a standard field still fails — that is not a substitute for Playwright on custom panels.

### Navigation (critical — EasyTest)

| Target | Do | Do not |
|--------|-----|--------|
| **After login (shell)** | **`AssertAuthenticatedAppShell()`** -> URL **`/Person_ListView_Employees`** | **`Navigate("Application")`** — Users role denies Application list; officers land on Report Dashboard |
| **Employees list** | Selenium URL **`/Person_ListView_Employees`** via **`NavigateEmployeesList()`** | Rely on **`Navigate("Employees")`** alone — TabbedMDI may stay on **Family Members** |
| **Passport DetailView** | Nested: employee detail → **Passports** tab → **New Passport** | Lookup/Passport sidebar (denied for Users) |
| **After New employee** | **`AssertEmployeeDetailViewActive()`** | Assume list context from sidebar highlight |
| **Organization / Lookup** | **`AppContext.Navigate("Organization.Company")`**, etc. | Mix with bare leaf ids under **People** |

Constants: **`E2ETestLoginValues`**, **`E2ETestPassportCreateOnlyJourneyValues`**, etc. in `Visa2026.Module/DatabaseUpdate/E2ETestDataSeed.cs`.

### Helpers already on `E2ETestBase` / companions

| Helper | Use |
|--------|-----|
| `Login` | Logon form (`StandardUser`) |
| `AssertAuthenticatedAppShell` | Employees URL probe (not Application) |
| `NavigateEmployeesList` | URL → employees list |
| `CreateEmployeeWithRequiredFields` | Employee create |
| `ExecutePersonPassportsNestedNew` / `FillPassportRequiredFields` | Nested passport create |
| `EasyTestScreenshotCapture.Capture` | Milestone PNGs (**on by default**; opt out `VISA2026_E2E_SCREENSHOTS=false`) |
| `FillFormWithRetry` | One field at a time + retry |
| `ExecuteActionWithRetry` | Toolbar actions after Blazor load |

### Playwright tests (custom / unsupported)

- Package: **`Microsoft.Playwright`** (+ `Microsoft.Playwright.Xunit` or plain xUnit + `Playwright` fixture).
- Prefer folder **`Visa2026.E2E.Tests/Playwright/`** and trait **`[Trait("Driver", "Playwright")]`**.
- Reuse EasyTest host URL/DB; share login seed constants (`E2ETestLoginValues`).
- Install browsers once: `pwsh bin/.../playwright.ps1 install` (or `dotnet tool` flow documented in [reference.md](./reference.md#playwright-fallback)).
- Tag UserManual media the same way when a Playwright journey produces guide assets.

### Test data

- Officer journey creates employee + passport via **UI** — no DB person/passport seed updater.
- Short passport Fact uses **`E2ETestPassportCreateOnlyJourneyValues`** (`E2E-EMP-021` / `E2E-PASS-021`) so it can share a session DB with the full CRUD Fact.
- Lookup catalogs still come from normal **`ModuleUpdater`** sync on **`visa2026_easytest`**.

---

## Run commands (repo root)

```powershell
dotnet build Visa2026.slnx -c EasyTest
# Preferred for UserManual media (video + screenshots ON by default):
.\scripts\local\Record-EasyTest.ps1
# Screenshots also ON for bare dotnet test (opt out: $env:VISA2026_E2E_SCREENSHOTS='false')
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest --filter "FullyQualifiedName~PersonOfficerJourney_LoginCreateEmployeeAddPassport"
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest --filter "Driver=Playwright"
# Opt out of media: .\scripts\local\Record-EasyTest.ps1 -NoRecord -NoScreenshots
```

**Prerequisites:** Windows, local **PostgreSQL**, **`msedgedriver.exe`** matching Edge for EasyTest (copy project `.webdrivers` → `bin/EasyTest/.../.webdrivers/` — `Record-EasyTest.ps1` does this). Portable ffmpeg: `Visa2026.E2E.Tests\.tools\ffmpeg\` (gitignored) or PATH. Playwright: install browser binaries per [reference.md](./reference.md#playwright-fallback).

### Browser mode (headed vs headless)

| Environment | Edge window | How |
|-------------|-------------|-----|
| **Local dev** (default) | Visible (headed) | No env vars — `dotnet test -c EasyTest` |
| **Windows CI** | Headed | `CI=true` keeps headed on Windows (`EasyTestBrowserMode`) |
| **Force headed** | Visible | `VISA2026_E2E_HEADED=true` |
| **Force headless locally** | Hidden | `VISA2026_E2E_HEADLESS=true` |

Implemented in **`EasyTestBrowserMode.RunHeadless`**. Mirror the same env intent for Playwright (`headless: false` locally / Windows CI).

---

## Current inventory (extend, do not duplicate)

| Test class | Focus |
|------------|--------|
| `PersonOfficerJourneyTests` | Short passport create + full master-data CRUD (`scenarios/ready/person-officer-journey`) — **EasyTest** |

Config: **`Config.xml`**, **`.github/workflows/e2e-tests.yml`** (CI). Docs: [`Visa2026.E2E.Tests/README.md`](../../../Visa2026.E2E.Tests/README.md).

Playwright inventory starts empty until the first custom-component suite lands under `Playwright/`.

---

## Media defaults (user manual)

Video recording and milestone screenshots are **enabled by default** — they feed [`docs/USER_MANUAL_E2E_MEDIA.md`](../../../docs/USER_MANUAL_E2E_MEDIA.md) / `Build-UserManual.ps1`.

| Media | Default | Opt out |
|-------|---------|---------|
| Screenshots | ON (`EasyTestScreenshotCapture`) | `VISA2026_E2E_SCREENSHOTS=false` or `Record-EasyTest.ps1 -NoScreenshots` |
| Desktop video | ON in `Record-EasyTest.ps1` | `-NoRecord` (requires ffmpeg otherwise) |

Agents running UserManual or media-producing journeys should prefer **`Record-EasyTest.ps1`**, not bare `dotnet test` alone (bare test still writes PNGs but not ffmpeg MP4).

## User manual media (visa2026-user-manual)

E2E is the **producer**; the manual site is the **consumer**.

| Media | How (today / planned) | Consumer |
|-------|----------------------|----------|
| **Video** | **ON by default** via `Record-EasyTest.ps1` (ffmpeg); CI long-run ffmpeg → `recordings/*.mp4`. Opt out: `-NoRecord` | Guide `video` frontmatter — **storage TBD** Phase 3 |
| **Screenshots** | **ON by default** (`EasyTestScreenshotCapture` / `Record-EasyTest.ps1`) → `recordings/screenshots/{run}/`. Opt out: `-NoScreenshots` or `VISA2026_E2E_SCREENSHOTS=false` | `user-manual/assets/screenshots/` |
| **Step truth** | `scenarios/ready/*_map.md` §3 captions | Guide prose must match |

When adding a journey that will become a guide:

1. Set **`e2eScenarioId`** folder name = `scenarios/ready/<id>/`.
2. Notify / update [user-manual tracking.md](../visa2026-user-manual/tracking.md) guide row.
3. After CI green, run `Record-EasyTest.ps1 -Filter <Fact> -OutputName <slug>.mp4` (video + screenshots default ON).
4. Copy selected PNGs into the manual assets pipeline when ready.

Full contract: [`docs/USER_MANUAL_E2E_MEDIA.md`](../../../docs/USER_MANUAL_E2E_MEDIA.md).

---

## Agent workflow

When the user asks for **EasyTest**, **E2E test**, **Playwright E2E**, **headed Edge test**, **custom component UI test**, or **`Visa2026.E2E.Tests`**:

1. **Read** [learnings.md](./learnings.md) for navigation, login, driver, caption, and Playwright pitfalls.
2. **Choose driver** — EasyTest unless the surface is custom / unsupported (table above).
3. **Read** target production test + **`E2ETestBase`** (and any `Playwright/` fixtures) for existing helpers.
4. **Implement** minimal test; prefer extending helpers over duplicating steps. For Playwright, add stable `data-testid` in product markup when missing.
5. **Build** `-c EasyTest`; **run** filtered `dotnet test` (sync msedgedriver for EasyTest; ensure Playwright browsers installed).
6. **Append** learnings.md after verified fixes (not for trivial typos).
7. **Scenario yaml** under `Visa2026.E2E.Tests/scenarios/` is metadata only (Option A) — mark `driver:`.
8. **Manual media** — video + screenshots are **required and ON by default** (`Record-EasyTest.ps1`; bare test still captures PNGs). Playwright journeys: screenshot/video APIs ON by default when implemented. Coordinate guides with [visa2026-user-manual](../visa2026-user-manual/SKILL.md).

---

## Known pitfalls

| Pitfall | Do instead |
|---------|------------|
| Run E2E on `:5000` IDE host | **`:5050`**; explicit `BlazorApplicationOptions` URL |
| Stuck after login on Report Dashboard | **`AssertAuthenticatedAppShell()`** uses Employees URL — never **`Navigate("Application")`** for `StandardUser` |
| Login `standarduser` fails | Use **`StandardUser`** (`E2ETestLoginValues`) |
| **`Navigate("Employees")`** → Family Members | **`NavigateEmployeesList()`** + **`AssertEmployeeDetailViewActive()`** |
| Passport via Lookup sidebar | Nested **Passports** → **New Passport** on employee detail |
| **`msedgedriver` version skew / old bin copy** | Match Edge major; copy `.webdrivers` → `bin/EasyTest/.../.webdrivers/`; CDN **`msedgedriver.microsoft.com`** |
| Empty-DB `--updateDatabase` fails on schema heal | Host-start SQL must `to_regclass` / no-op when tables missing |
| Headless on local / wrong CI mode | Local headed; Windows CI headed; `VISA2026_E2E_HEADED=true` to force |
| Force EasyTest on custom Razor / preview slot | **Playwright** + `data-testid` / `#visa-preview-slot` |
| Parallel EasyTest + Playwright hosts on same port | One host on **`:5050`**; serialize or share fixture — never two DB resets racing |

---

## Additional resources

- [user-prompts.md](./user-prompts.md) — invoke messages
- [reference.md](./reference.md) — host, driver, EasyTest API, Playwright fallback, CI
- [reference-map-contract.md](./reference-map-contract.md) — `*_map.md` + yaml + C# (Option A)
- [learnings.md](./learnings.md) — append-only verified experience
- [`scripts/local/Record-EasyTest.ps1`](../../../scripts/local/Record-EasyTest.ps1) — local video + screenshots (**default ON**; `-NoRecord` / `-NoScreenshots` to opt out)
- [`docs/USER_MANUAL_E2E_MEDIA.md`](../../../docs/USER_MANUAL_E2E_MEDIA.md) — screenshot/video contract with user manual
- [visa2026-user-manual](../visa2026-user-manual/SKILL.md) — guide authoring consumer skill
- [`Visa2026.E2E.Tests/scenarios/`](../../../Visa2026.E2E.Tests/scenarios/README.md) — scenario maps and yaml specs
- [`docs/TESTING_PLAN.md`](../../../docs/TESTING_PLAN.md) — E2E inventory, backlog E2E-xxx