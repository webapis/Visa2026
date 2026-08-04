---
name: visa2026-easytest-e2e
description: >-
  Creates and runs Visa2026 native XAF EasyTest E2E tests (Visa2026.E2E.Tests,
  C# API, Edge/Selenium, EasyTest config). Covers E2ETestBase, Blazor host on
  :5050, Postgres visa2026_easytest, msedgedriver, caption-based FillForm/Navigate,
  URL navigation for typed Person lists, seed constants, Record-EasyTest.ps1
  (video + -Screenshots), and dotnet test -c EasyTest. Use when adding EasyTest,
  PersonOfficerJourneyTests, E2ETestBase helper, headed Edge run, or CI e2e-tests.yml.
  Officer-manual media: docs/USER_MANUAL_E2E_MEDIA.md and visa2026-user-manual skill.
disable-model-invocation: false
---

# Visa2026: XAF EasyTest E2E (native)

## Purpose

**Author and run** officer-journey tests in **`Visa2026.E2E.Tests`** using DevExpress **EasyTest Blazor adapter** + **xUnit** + **Microsoft Edge** (Selenium). Tests use the **C# API** (`IApplicationContext`, `EasyTestParameter`) — not `.ets` scripts in this repo.

**Project:** `Visa2026.E2E.Tests` — native XAF EasyTest Blazor adapter + xUnit + Edge (Selenium).

**Strategy context:** [`docs/TESTING_PLAN.md`](../../../docs/TESTING_PLAN.md). **Experience log:** [learnings.md](./learnings.md) — read before, append after verified runs.

**User manual media (interlocked):** [`docs/USER_MANUAL_E2E_MEDIA.md`](../../../docs/USER_MANUAL_E2E_MEDIA.md) · [`docs/USER_MANUAL_PIPELINE.md`](../../../docs/USER_MANUAL_PIPELINE.md) · [visa2026-user-manual](../visa2026-user-manual/SKILL.md)

**Critical:** UserManual E2E is invoked by **`Build-UserManual.ps1`**, not a separate doc-update workflow. Tag tests `[Trait("Category", "UserManual")]`.

---

## User prompts

Copy-paste catalog: [user-prompts.md](./user-prompts.md). Invoke with **`@visa2026-easytest-e2e`**.

---

## Process (new E2E test)

```text
1. MAP       — scenarios/examples/<id>_map.md (caption inventory §3) + E2E-xxx id
2. YAML      — scenarios/examples/<id>.yaml when map Ready for YAML (Option A: spec only)
3. C#        — *Tests.cs [Fact] mirroring yaml steps; extend E2ETestBase helpers
4. BUILD     — dotnet build Visa2026.slnx -c EasyTest
5. RUN       — dotnet test Visa2026.E2E.Tests -c EasyTest --filter "FullyQualifiedName~YourTests"
6. PROMOTE   — move map + yaml to scenarios/ready/ when CI-stable
7. RECORD    — append learnings.md on non-obvious fixes (nav, captions, driver, host)
8. USERMANUAL — add `[Trait("Category", "UserManual")]`; media via Record-EasyTest / Build-UserManual.ps1
```

**Scenario metadata (Option A):** YAML documents steps; C# executes them. Map contract: [reference-map-contract.md](./reference-map-contract.md). Manual media: [`docs/USER_MANUAL_E2E_MEDIA.md`](../../../docs/USER_MANUAL_E2E_MEDIA.md). Inventory: [`Visa2026.E2E.Tests/scenarios/README.md`](../../../Visa2026.E2E.Tests/scenarios/README.md).

---

## Host isolation (mandatory)

EasyTest must **not** share the IDE dev host (`:5000` / `:5001`).

| Setting | Value |
|---------|--------|
| URL | **`http://localhost:5050`** |
| DB | **`visa2026_easytest`** on local **PostgreSQL** (`PG_*` / default `Visa2026Local`) |
| Build config | **`EasyTest`** |
| Browser | **Edge** — **headed** locally; on Windows CI keep headed (`EasyTestBrowserMode`) |

**TabbedMDI / saved tabs:** EasyTest host sets ephemeral user model differences when the EasyTest DB is detected (see `EasyTestHostMode` in Blazor.Server). Without this, **`StandardUser`** can reopen **Family Members** instead of Employees.

Full host + driver setup: [reference.md § Host and driver](./reference.md#host-and-driver).

**Preflight (session):** `EasyTestPreflight` checks **`:5050`** is free, drops/recreates Postgres **`visa2026_easytest`**, runs **`--updateDatabase --silent`**, then `RunApplication` on the built **`.exe`** with **`--urls http://localhost:5050 --environment Development`**. Teardown closes host in `EasyTestSessionFixture.DisposeAsync`.

---

## Writing tests

### Base class

- Inherit **`E2ETestBase`** (collection fixture — one host/browser session; DB dropped once per run).
- **`[SupportedOSPlatform("windows")]`** on test class/method (Edge E2E is Windows-only today).
- Use **`Login(userName, password)`** — officer flows: **`E2ETestLoginValues.StandardUserName`** (`StandardUser`, seeded in `Updater`) + empty password. Not `standarduser`.

### Selectors: captions, not hooks

EasyTest fills fields by **English model caption** (`EasyTestParameter("First Name", value)`). Keep captions aligned with embedded model / en-US. Custom Blazor editors may need **`InputId`** / aria — fix in Module/Blazor; `E2ETestBase.FillFormWithRetry` falls back to `data-testid` via `EasyTestBlazorNavigationHelper` when captions fail.

### Navigation (critical)

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
| `EasyTestScreenshotCapture.Capture` | Milestone PNGs when `VISA2026_E2E_SCREENSHOTS=true` |
| `FillFormWithRetry` | One field at a time + retry |
| `ExecuteActionWithRetry` | Toolbar actions after Blazor load |

### Test data

- Officer journey creates employee + passport via **UI** — no DB person/passport seed updater.
- Short passport Fact uses **`E2ETestPassportCreateOnlyJourneyValues`** (`E2E-EMP-021` / `E2E-PASS-021`) so it can share a session DB with the full CRUD Fact.
- Lookup catalogs still come from normal **`ModuleUpdater`** sync on **`visa2026_easytest`**.

---

## Run commands (repo root)

```powershell
dotnet build Visa2026.slnx -c EasyTest
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest --filter "FullyQualifiedName~PersonOfficerJourney_LoginCreateEmployeeAddPassport"
.\scripts\local\Record-EasyTest.ps1 -Screenshots
dotnet test Visa2026.E2E.Tests/Visa2026.E2E.Tests.csproj -c EasyTest
```

**Prerequisites:** Windows, local **PostgreSQL**, **`msedgedriver.exe`** matching Edge (copy project `.webdrivers` → `bin/EasyTest/.../.webdrivers/` — `Record-EasyTest.ps1` does this). Portable ffmpeg: `Visa2026.E2E.Tests\.tools\ffmpeg\` (gitignored) or PATH.

### Browser mode (headed vs headless)

| Environment | Edge window | How |
|-------------|-------------|-----|
| **Local dev** (default) | Visible (headed) | No env vars — `dotnet test -c EasyTest` |
| **Windows CI** | Headed | `CI=true` keeps headed on Windows (`EasyTestBrowserMode`) |
| **Force headed** | Visible | `VISA2026_E2E_HEADED=true` |
| **Force headless locally** | Hidden | `VISA2026_E2E_HEADLESS=true` |

Implemented in **`EasyTestBrowserMode.RunHeadless`**.

---

## Current inventory (extend, do not duplicate)

| Test class | Focus |
|------------|--------|
| `PersonOfficerJourneyTests` | Short passport create + full master-data CRUD (`scenarios/ready/person-officer-journey`) |

Config: **`Config.xml`**, **`.github/workflows/e2e-tests.yml`** (CI). Docs: [`Visa2026.E2E.Tests/README.md`](../../../Visa2026.E2E.Tests/README.md).

---

## User manual media (visa2026-user-manual)

E2E is the **producer**; the manual site is the **consumer**.

| Media | How (today / planned) | Consumer |
|-------|----------------------|----------|
| **Video** | `Record-EasyTest.ps1`; CI ffmpeg → `recordings/*.mp4` | Guide `video` frontmatter — **storage TBD** Phase 3 |
| **Screenshots** | `Record-EasyTest.ps1 -Screenshots` -> `EasyTestScreenshotCapture` -> `recordings/screenshots/{run}/`; diag via `TryDumpDiagnostics`; guide copy via planned `UserManualMediaCapture` | `user-manual/assets/screenshots/` |
| **Step truth** | `scenarios/ready/*_map.md` §3 captions | Guide prose must match |

When adding a journey that will become a guide:

1. Set **`e2eScenarioId`** folder name = `scenarios/ready/<id>/`.
2. Notify / update [user-manual tracking.md](../visa2026-user-manual/tracking.md) guide row.
3. After CI green, run `Record-EasyTest.ps1 -Filter <Fact> -Screenshots -OutputName <slug>.mp4`.
4. Copy selected PNGs into the manual assets pipeline when ready.

Full contract: [`docs/USER_MANUAL_E2E_MEDIA.md`](../../../docs/USER_MANUAL_E2E_MEDIA.md).

---

## Agent workflow

When the user asks for **EasyTest**, **E2E test**, **headed Edge test**, or **`Visa2026.E2E.Tests`**:

1. **Read** [learnings.md](./learnings.md) for navigation, login, driver, caption pitfalls.
2. **Read** target production test + **`E2ETestBase`** for existing helpers.
3. **Implement** minimal test class; prefer extending base helpers over duplicating steps.
4. **Build** `-c EasyTest`; **run** filtered `dotnet test` (sync msedgedriver into test output).
5. **Append** learnings.md after verified fixes (not for trivial typos).
6. **Stay in EasyTest** — scenario yaml under `Visa2026.E2E.Tests/scenarios/` is metadata only (Option A).
7. **Manual media** — video/screenshots via `Record-EasyTest.ps1`; coordinate guides with [visa2026-user-manual](../visa2026-user-manual/SKILL.md).

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

---

## Additional resources

- [user-prompts.md](./user-prompts.md) — invoke messages
- [reference.md](./reference.md) — host, driver, API patterns, CI
- [reference-map-contract.md](./reference-map-contract.md) — `*_map.md` + yaml + C# (Option A)
- [learnings.md](./learnings.md) — append-only verified experience
- [`scripts/local/Record-EasyTest.ps1`](../../../scripts/local/Record-EasyTest.ps1) — local video + `-Screenshots`
- [`docs/USER_MANUAL_E2E_MEDIA.md`](../../../docs/USER_MANUAL_E2E_MEDIA.md) — screenshot/video contract with user manual
- [visa2026-user-manual](../visa2026-user-manual/SKILL.md) — guide authoring consumer skill
- [`Visa2026.E2E.Tests/scenarios/`](../../../Visa2026.E2E.Tests/scenarios/README.md) — scenario maps and yaml specs
- [`docs/TESTING_PLAN.md`](../../../docs/TESTING_PLAN.md) — E2E inventory, backlog E2E-xxx
