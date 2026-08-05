# visa2026-easytest-e2e — learnings

Append-only. Read **## Entries** before new E2E work; append after **verified** `dotnet test -c EasyTest` outcomes.

## Entry template

```markdown
### YYYY-MM-DD — Short title

- **Outcome**: positive | negative | anti-pattern
- **Context**: test class, view, OS
- **Symptom**: …
- **Fix / reuse**: …
- **Reuse**: one-line rule for next run
```

---

## Entries

### 2026-06-11 — GHA: host never binds :5050 before EasyTest 60s script wait

- **Outcome**: negative → fix
- **Context**: `EasyTestHostProcessLauncher`, `EasyTestHostReadiness`, CI logs `connection refused (localhost:5050)`
- **Symptom**: Headed Edge + `Development` env still fail — HTTP probe after timeout shows **port 5050 not listening**; DevExpress `WaitScriptLoading` times out at 60s while Kestrel is still in `Startup.Configure` (fresh DB template seed, schema gates).
- **Fix / reuse**: **Pre-launch** `Visa2026.Blazor.Server.exe` with redirected logs (`easytest-host-logs/`), **`WaitUntilHttpResponds` up to 12 min on CI**, then `RunApplication`. Skip **`UserReportTemplateSeedGate`** when **`EasyTestHostMode`**. Upload host log artifact from workflow.
- **Reuse**: EasyTest 60s script wait is not host-startup wait — start host explicitly before `RunApplication` on slow/CI agents.

### 2026-06-11 — GHA: `--environment EasyTest` breaks HTTP-only :5050 host

- **Outcome**: negative → fix
- **Context**: `EasyTestHostLaunch.HostArguments`, `Startup.cs`, `BlazorAppResponseAwaiter.WaitScriptLoading`, GHA `e2e-tests.yml`
- **Symptom**: All facts fail at `RunApplication()` — `WebDriverTimeoutException: Timed out after 60 seconds` on CI (headed or headless); local F5 **Visa2026 - EasyTest (LocalDB)** profile works.
- **Fix / reuse**: Launch built `.exe` with **`--environment Development`** (same as F5 profile), **`--urls http://localhost:5050`**. EasyTest sets **`ConnectionStrings__DefaultConnection`** for `Visa2026EasyTest`. Do **not** use `--environment EasyTest` for the running host — non-Development middleware enables **HTTPS redirect + HSTS** on HTTP-only Kestrel and Blazor EasyTest scripts never load. **`Startup`**: skip `UseHttpsRedirection` when **`EasyTestHostMode.IsEnabled`**. Also: **`EasyTestSessionFixture`**, headed Edge on Windows CI, `RunApplication` retries.
- **Reuse**: MSBuild config `EasyTest` ≠ `ASPNETCORE_ENVIRONMENT`; E2E host args mirror `launchSettings.json` **Development** + test connection string from EasyTest.

### 2026-06-11 — GHA Windows: headless Edge breaks WaitScriptLoading

- **Outcome**: negative → partial (headed still failed until Development env fix above)
- **Context**: `.github/workflows/e2e-tests.yml`, `EasyTestBrowserMode`
- **Fix / reuse**: On **`CI=true` + Windows**, run **headed** Edge; do not set `VISA2026_E2E_HEADLESS=true` on the Windows workflow.
- **Reuse**: Prefer headed on `windows-latest`; headless only when explicitly requested.

### 2026-06-11 — Headed local / headless CI via EasyTestBrowserMode

- **Outcome**: positive
- **Context**: `EasyTestBrowserMode.cs`, `E2ETestBase`, `e2e-tests.yml`
- **Fix / reuse**: `runHeadless: EasyTestBrowserMode.RunHeadless` — headed when no env; headless when `CI=true` or `VISA2026_E2E_HEADLESS=true`; `VISA2026_E2E_HEADED=true` forces headed.
- **Reuse**: Do not hardcode `runHeadless` in tests; removed obsolete CI `Config.xml` headless patch.

### 2026-06-11 — Promote Tier 0 scenarios to ready/

- **Outcome**: positive
- **Context**: `scenarios/ready/` — login-smoke, login-nav-employees, person-employee-create
- **Fix / reuse**: Move map + yaml from `examples/` after green local runs (`SmokeTests` ~22s, `EmployeeTests` ~95s).
- **Reuse**: Promote only after filtered `dotnet test` pass; keep `_map_TEMPLATE.md` in `examples/`.

### 2026-06-11 — Employees list ProcessRow vs nested Passports grid

- **Outcome**: negative → fix
- **Context**: `OpenEmployeeInListByPersonalNumber`, TabbedMDI after Save
- **Symptom**: `Personal Number column was not found` — EasyTest `GetGrid()` targeted nested **Passports** grid on detail tab, not `Person_ListView_Employees`.
- **Fix / reuse**: `IsEmployeesListActive` waits until employee detail form is gone (+ list URL or **Personal Number** column header); `ClickListRowContaining` Selenium fallback when `ProcessRow` fails.
- **Reuse**: After Save on detail, confirm list is active before grid ops; do not trust `ProcessRow` alone on Blazor TabbedMDI.

### 2026-06-11 — Date Of Birth caption + hook fallback

- **Outcome**: negative → fix
- **Context**: `EmployeeTests`, Blazor employee detail, EasyTest `FillForm`
- **Symptom**: `Cannot find the 'Date of Birth' control` while UI label shows **Date Of Birth**; First/Last name already filled.
- **Fix / reuse**: `E2ETestPersonFieldCaptions` (XAF title case); `FillSingleFieldWithRetry` tries caption aliases then `EasyTestBlazorNavigationHelper.FillInputByTestId` (`person-date-of-birth`, …).
- **Reuse**: Person scalars → shared captions in `E2ETestDataSeed.cs`; hook ids for Selenium fallback when EasyTest cannot bind Blazor date/combo editors.

### 2026-06-11 — TabbedMDI detail URL stays at /

- **Outcome**: negative → fix
- **Context**: `EmployeeTests`, `AssertEmployeeDetailViewActive`, Blazor TabbedMDI on `:5050`
- **Symptom**: After **New** on employees list, Employee detail is visible but URL is `http://localhost:5050/` → URL-only assert fails while form is open.
- **Fix / reuse**: Retry **`AssertEmployeeDetailViewActive()`** — accept URL **or** **`Save` + `First Name` + `Project Contract`** form read (employee vs family member shield).
- **Reuse**: Do not rely on detail view id in browser URL for TabbedMDI **New** flows; use caption/form outcome shield.

### 2026-06-11 — person-employee-create map + yaml (E2E-010)

- **Outcome**: positive (pattern)
- **Context**: `EmployeeTests`, `scenarios/examples/person-employee-create.yaml`
- **Symptom**: C# test existed without co-located EasyTest scenario spec.
- **Fix / reuse**: Map §3 lists English captions from `CreateEmployeeWithRequiredFields`; yaml mirrors steps including `open-grid-row` + `assert-property`; constants from `E2ETestEmployeeCreateValues`.
- **Reuse**: UiScenario twin uses hook ids — keep business steps aligned, not selector mechanism; `VisaApplicationFamilyMembersText` waived in EasyTest (OnSaving default).

### 2026-06-11 — Phase 0 scenario metadata (Option A)

- **Outcome**: positive
- **Context**: `SmokeTests`, `scenarios/examples/login-smoke.yaml`, `login-nav-employees.yaml`
- **Symptom**: Duplicate smokes in `Visa2026Tests` / `GeneralTests`; no shared journey spec with ui-scenarios.
- **Fix / reuse**: Map + yaml in `Visa2026.E2E.Tests/scenarios/examples/`; C# `[Fact]` mirrors yaml; `AssertAuthenticatedAppShell()` = UiScenario `nav-people` shield (Navigate Application + `New`).
- **Reuse**: New EasyTest journeys → map first, yaml when captions verified, then C#; promote to `scenarios/ready/` after CI pass.

### 2026-06-08 — EasyTest port 5050 isolated from IDE

- **Outcome**: positive
- **Context**: `E2ETestBase`, launch profile `Visa2026 - EasyTest (LocalDB)`
- **Symptom**: Edge opened `localhost:5000` → connection refused or wrong app.
- **Fix / reuse**: Dedicated profile on **`:5050`**; `BlazorApplicationOptions` must pass explicit `url` + `configuration` (not two-arg ctor reading IIS Express).
- **Reuse**: Never run EasyTest against IDE `:5000`.

### 2026-06-11 — `--launch-profile` ignored by built `.exe` (ERR_CONNECTION_REFUSED on :5050)

- **Outcome**: negative → positive
- **Context**: `E2ETestBase` `arguments: --launch-profile "Visa2026 - EasyTest (LocalDB)"`, preflight `DropDB` + DB provision
- **Symptom**: Edge on **`localhost:5050`** → `ERR_CONNECTION_REFUSED`; host process listening on **`:5000`** instead; DB missing after drop until `--updateDatabase`.
- **Fix / reuse**: EasyTest launches **`bin/EasyTest/net8.0/Visa2026.Blazor.Server.exe`** — **`--launch-profile` only works with `dotnet run`**. Use **`EasyTestHostLaunch.HostArguments`**: `--urls http://localhost:5050 --environment Development` (EasyTest sets **`ConnectionStrings__DefaultConnection`** for `Visa2026EasyTest`). After `DropDB`, run **`--updateDatabase --silent`** via **`EasyTestDatabaseProvisioner`** (create empty catalog first).
- **Reuse**: IDE F5 may still use launch profile; E2E must use explicit `--urls` + **Development** on the exe (MSBuild config `EasyTest` is separate).

### 2026-06-08 — msedgedriver CDN

- **Outcome**: negative
- **Context**: Windows, Edge 149.x
- **Symptom**: `msedgedriver.azureedge.net` DNS failure.
- **Fix / reuse**: Download from **`https://msedgedriver.microsoft.com/{version}/edgedriver_win64.zip`** into **`Visa2026.E2E.Tests\.webdrivers\`**.
- **Reuse**: Use Microsoft CDN URL; optional `scripts/local/Install-MsEdgeDriver.ps1` (update script if CDN URL changes).

### 2026-06-08 — Employees vs Family Members navigation

- **Outcome**: negative
- **Context**: `EmployeeTests`, login `standarduser`
- **Symptom**: Sidebar **Family Members** selected; detail title **Family member** after **New**; data filled on wrong role.
- **Fix / reuse**: (1) **`EasyTestHostMode`** + ephemeral user model store for **`Visa2026EasyTest`**. (2) Navigate via URL **`/Person_ListView_Employees`** (`NavigateEmployeesList`), not **`Navigate("People.Employees")`** alone. (3) **`AssertEmployeeDetailViewActive()`** after **New**.
- **Reuse**: Typed Person lists → URL navigation + URL assert; do not trust **New** on wrong TabbedMDI tab.

### 2026-06-08 — Officer login for employee create

- **Outcome**: positive
- **Context**: E2E-010, mirrors `person-employee-create` UiScenario
- **Fix / reuse**: **`E2ETestLoginValues.StandardUserName`** (`standarduser`) + empty password; fill both **User Name** and **Password** on logon form.
- **Reuse**: Officer flows use `standarduser`; Admin reserved for org/settings tests unless specified.

### 2026-06-08 — FillForm retry for lookups

- **Outcome**: positive
- **Context**: `CreateEmployeeWithRequiredFields`, Blazor combos
- **Fix / reuse**: **`FillSingleFieldWithRetry`** — one `EasyTestParameter` per attempt, not bulk `FillForm(all fields)`.
- **Reuse**: Lookup fields one-at-a-time with retry.

### 2026-06-11 — E2E seed PersonRole vs IsEmployee

- **Outcome**: negative → fix
- **Context**: `E2ETestDataSeedUpdater`, `Person_ListView_Employees`, `PassportTests`
- **Symptom**: Seeded `E2E-TEST-001` missing from Employees list; person exists as **Family Member** (`PersonRole` default). Setting `IsEmployee = true` alone is undone by `Person.OnSaving` → `PersonRoleHelper.SyncIsEmployee`.
- **Fix / reuse**: Use **`PersonRoleHelper.ApplyRole(person, PersonRecordRole.Employee)`** on create; on existing seed row correct role before return. Employees list filters **`PersonRole`**, not `IsEmployee`. EF seed queries: avoid `string.Contains(..., StringComparison)` — not SQL-translatable.
- **Reuse**: E2E parent Person seed → always `ApplyRole(Employee)`; idempotent role correction for `PersonPersonalNumber`.

### 2026-07-30 — Person master-data CRUD journey (E2E-001…008) for GHA

- **Outcome**: positive (compiled; GHA run validates captions)
- **Context**: `PersonOfficerJourneyTests.PersonOfficerJourney_LoginCreateEmployeeMasterDataCrud`, `E2ETestBase.PersonMasterData`, `E2ETestDataSeed`
- **Symptom**: Only passport covered; need officer-like create of Person record children on GitHub Actions.
- **Fix / reuse**: One long Fact: employee → Passport → **Visa under Passport** → Education → Address (Private house) → Medical (update + best-effort delete) → PositionHistory → WorkDuty → Salary → External Arrival. Generic `ExecutePersonNestedNew` + tab/New title aliases. **ActualPosition** seeded only when connection string contains `Visa2026EasyTest` (`Updater.EnsureEasyTestActualPositionSeed`). PersonDocument file upload deferred. Issued tabs excluded.
- **Reuse**: Visa is never a Person tab; Address Type must switch to Private house before Full Address; WorkDuty field/tab captions may be TM (`Gelmeginiň Maksady`); promote `person-master-data-crud` map to `ready/` after GHA green.

### 2026-07-30 — GHA: employee Save empty Project Contract → empty Employees list

- **Outcome**: negative → fix
- **Context**: `CreateEmployeeWithRequiredFields`, host-out.log ValidationException
- **Symptom**: `"Project Contract" must not be empty` on Save; `OpenEmployeeInListByPersonalNumber` then sees Empty table.
- **Fix / reuse**: `EnsureEmployeeRequiredLookupsBound` before Save; `SaveEmployeeDetailAndConfirm` retries on validation banner and verifies list row via Selenium; `OpenEmployee…` always falls through to `ClickListRowContaining`.
- **Reuse**: Combo FillForm success ≠ bound lookup — re-read property values before Save; confirm persistence via Employees list row, not detail form alone.

### 2026-07-30 — E2E ProjectContractDisplay GT-15 removed from catalog

- **Outcome**: negative → fix
- **Context**: `E2ETestEmployeeCreateValues.ProjectContractDisplay`, tenant `project-contract.json`
- **Symptom**: EasyTest FillForm("Project Contract", "GT-15") appears to run but Save fails `"Project Contract" must not be empty` — **GT-15 is not in the seeded catalog** (file is Çalik 73-row set; README still mentions greenfield GT-15 demo).
- **Fix / reuse**: Use **`14306 Mary`** (Code/NameTm present in embedded `project-contract.json`). Subcontractor `Çalyk Enerji` still valid.
- **Reuse**: Before relying on E2E lookup display strings, confirm the value exists in the **currently embedded** LookupCatalogs JSON — not historical docs.

### 2026-07-30 — Save confirm must not New again with same Personal Number

- **Outcome**: negative → fix
- **Context**: `SaveEmployeeDetailAndConfirm` after ProjectContract fix
- **Symptom**: First Save succeeded; list-row confirm failed; retry created New with same `E2E-EMP-010` → uniqueness validation; list still looked empty to the helper.
- **Fix / reuse**: On success stay on detail with PN (no forced New retry). On "already uses this personal number", treat as saved and locate list row — never recreate the same PN.
- **Reuse**: Employee create confirm = detail PN present without validation banner; uniqueness error means prior Save worked.

### 2026-07-30 — Return via captured employee detail URL after Passport/Visa

- **Outcome**: negative → fix
- **Context**: after Visa Save, `OpenEmployeeInListByPersonalNumber` opened employee URL but PN assert failed
- **Symptom**: URL `Person_DetailView_Employee/{oid}` yet Personal Number not detected; list ProcessRow unreliable after nested Passport/Visa.
- **Fix / reuse**: Capture `SavedEmployeeDetailUrl` after create/open; `ReturnToSavedEmployeeDetail()` uses Selenium `GoToAbsoluteUrl` before falling back to list.
- **Reuse**: After deep nested child detail (Visa), reopen parent via oid URL — not Employees list ProcessRow.

### 2026-07-30 — Address E2E: prefer Lodging over Private house

- **Outcome**: negative → fix
- **Context**: `FillAddressPrivateHouseRequiredFields` on GHA after Education passed
- **Symptom**: Could not fill `Full Address` (Type=Private house ImmediatePostData hide/show flaky; URL stayed `/`).
- **Fix / reuse**: Keep OnCreated **Lodging**; fill Region → City → Lodging (`1932 (A.Garlyýew) köç. 70/1 UÝJ` in catalog).
- **Reuse**: Avoid Private-house Full Address in EasyTest unless Type change + field visibility is proven stable.

### 2026-07-30 — Address Lodging cascade deferred on EasyTest CI

- **Outcome**: negative → deferred
- **Context**: Region → City → Lodging FillForm / FillLookupUntilBound / dropdown commit
- **Symptom**: Host ValidationException Region/City/Lodging empty after Save; GetPropertyValue can show typed filter text without FK bind. Also briefly broke build by overwriting `GoToAbsoluteUrl`.
- **Fix / reuse**: Keep helpers; skip Address step in journey until a proven DOM lookup-select helper exists. Rest of CRUD (medical string fields, work duty, salary) continues.
- **Reuse**: Cascading lookups need real dropdown item selection, not FillForm alone; never delete `GoToAbsoluteUrl` when editing navigation helpers.

### 2026-07-31 — Travel New is dxbl-btn-split (no data-action-name on button)

- **Outcome**: negative → fix (GHA green: push `30603693018`, PR `30603694743`)
- **Context**: `ExecutePersonTravelExternalArrivalNestedNew`, diag screenshot TravelHistories tab with visible **New**
- **Symptom**: `Could not execute nested New [New External Arrival | External Arrival]` while HTML had `title="New External Arrival"` on a **split** control.
- **Fix / reuse**: Polymorphic New puts `data-action-name` + `title` on `div.dxbl-btn-split`; inner `button` has `title` only. Extend `TryClickToolbarActionByTitle` / `HasToolbarActionByTitle` to click split primary / titled buttons without requiring `@data-action-name`. Re-enabled External Arrival after TravelHistory↔ApplicationItem decoupling.
- **Reuse**: Nested polymorphic New → match split toolbar, not only `button[@data-action-name]`.

### 2026-08-04 — User manual media interlock (visa2026-user-manual)

- **Outcome**: positive (planning)
- **Context**: Officer manual roadmap; `Record-EasyTest.ps1`; CI `easytest-e2e-recording` artifact
- **Symptom**: N/A — new cross-skill contract
- **Fix / reuse**: EasyTest is **media producer**: video via `Record-EasyTest.ps1` / CI ffmpeg; screenshots today via `TryDumpDiagnostics`; planned `UserManualMediaCapture` → `user-manual/assets/`. Guides link via `e2eScenarioId` + `scenarios/ready/<id>/`. See `docs/USER_MANUAL_E2E_MEDIA.md` and `docs/USER_MANUAL_ROADMAP.md`.
- **Reuse**: When adding a UserManual guide, tag E2E `[Trait("Category", "UserManual")]`; pipeline runs via `Build-UserManual.ps1` — see `docs/USER_MANUAL_PIPELINE.md`.

### 2026-08-04 — Unified doc pipeline (E2E embedded)

- **Outcome**: positive (architecture)
- **Context**: User priority — manual is shipment gate; doc generation is orchestrator
- **Fix / reuse**: `USER_MANUAL_PIPELINE.md` — single `Build-UserManual.ps1`; fail closed; manifest-driven UserManual filter; `e2e-tests.yml` stays for full regression only.
- **Reuse**: Never publish manual without running UserManual E2E inside doc build.
### 2026-08-04 — Shell assert must not Navigate(Application) for StandardUser

- **Outcome**: positive
- **Context**: `PersonOfficerJourney_LoginCreateEmployeeAddPassport`, Users role, Report Dashboard home
- **Symptom**: After successful login, Edge shows Report Dashboard; test hangs ~30s× timeouts on `AssertAuthenticatedAppShell` / `Navigate("Application")`. Never reaches Employees or nested Passport DetailView.
- **Fix / reuse**: Users role **denies** Application list nav. Probe authenticated shell via URL **`/Person_ListView_Employees`**. Passport DetailView is **nested** (`Passports` tab → `New Passport`), not Lookup/Passport sidebar (also denied).
- **Reuse**: Officer EasyTest shell check = Employees URL + New; never `Navigate("Application")` for StandardUser.
### 2026-08-04 — Login user is StandardUser not standarduser

- **Outcome**: positive
- **Context**: `E2ETestLoginValues`, `Updater` seed, host log `Login failed for 'standarduser'`
- **Symptom**: Stay on `/LoginPage` after FillForm + Log In.
- **Fix / reuse**: Seeded officer is **`StandardUser`** (empty password). Keep `E2ETestLoginValues.StandardUserName = "StandardUser"`.
- **Reuse**: Match `Updater.CreateUser` names exactly; do not assume case-insensitive Identity login.

### 2026-08-04 — Record-EasyTest -Screenshots milestone PNGs

- **Outcome**: positive
- **Context**: `EasyTestScreenshotCapture`, `Record-EasyTest.ps1 -Screenshots`, passport journey
- **Symptom**: Needed still frames plus desktop MP4 for review.
- **Fix / reuse**: Set `VISA2026_E2E_SCREENSHOTS=true` (+ optional `VISA2026_E2E_SCREENSHOT_RUN`); call `EasyTestScreenshotCapture.Capture` at journey steps. Video remains ffmpeg gdigrab via `Record-EasyTest.ps1`. Portable ffmpeg under `Visa2026.E2E.Tests\.tools\ffmpeg\`.
- **Reuse**: `-Screenshots` for local media; CI already records desktop video separately.
### 2026-08-04 — Playwright for EasyTest gaps (custom components)

- **Outcome**: positive (policy)
- **Context**: skill `visa2026-easytest-e2e`; custom Blazor (preview slot, Resminamalar, Document copies)
- **Symptom**: EasyTest caption/`FillForm` API does not map cleanly to non-PropertyEditor Razor UI
- **Fix / reuse**: Default remains **EasyTest** for XAF List/Detail/toolbar. Use **Microsoft Playwright** when EasyTest is unsupported or weak; prefer `data-testid` + `#visa-preview-slot`; same `:5050` / `visa2026_easytest` host; trait `Driver=Playwright`; do not rewrite stable EasyTest journeys
- **Reuse**: Custom component E2E → Playwright; standard XAF forms → EasyTest

### 2026-08-04 — Media default ON for user-manual generation

- **Outcome**: positive (policy + code)
- **Context**: `EasyTestScreenshotCapture`, `Record-EasyTest.ps1`, `e2e-tests.yml`
- **Symptom**: Screenshots required `-Screenshots`; agents skipped media needed for manual docs
- **Fix / reuse**: Screenshots **ON by default** (opt out `VISA2026_E2E_SCREENSHOTS=false` / `-NoScreenshots`). Video **ON by default** in `Record-EasyTest.ps1` (`-NoRecord` to skip). Prefer script over bare `dotnet test` when MP4 is required. CI uploads `easytest-e2e-screenshots` artifact.
- **Reuse**: User-manual E2E always assume media unless explicitly opted out

### 2026-08-05 — Mandatory failure capture before browser exit

- **Outcome**: positive (policy + code)
- **Context**: User asked for PNG + HTML on every Playwright E2E failure before exit
- **Fix / reuse**: `PlaywrightFailureCapture` writes PNG + HTML + `.txt` (logs all three paths). `PlaywrightE2eTestRunner` wraps Facts; `PlaywrightE2eStepRunner` wraps journey steps; `PlaywrightE2eFixture.DisposeAsync` calls `CaptureBeforeExitAsync` as safety net before `Page.CloseAsync`. Independent of milestone screenshot setting.
- **Reuse**: New Playwright Facts → `PlaywrightE2eTestRunner.RunAsync`; new journey phases → `PlaywrightE2eStepRunner.RunAsync`; triage `recordings/screenshots/{runId}/failures/`

### 2026-08-05 — Playwright failure capture + passport field locators

- **Outcome**: positive (investigation) — superseded by **Person add-passport RCA** below for full root-cause stack
- **Context**: UserManual E2E stuck on new Passport detail (defaults set, Passport Number empty)
- **Symptom**: Test exited without showing which step/field failed; `FillTextFieldAsync` had no caption fallback for passport fields; milestone screenshots only on success path
- **Fix / reuse**: `PlaywrightFailureCapture` → `recordings/screenshots/{runId}/failures/` (PNG + HTML + `.txt` note, always on). `PlaywrightE2eStepRunner` wraps journey steps (`add-passport-open-form`, `add-passport-fill-fields`, `add-passport-save`). Passport fills use `E2ETestPassportFieldCaptions` + `PassportFieldInput`; `FillTextFieldAsync` retries with `PressSequentially`. Pinpoint/lookup timeouts are non-fatal.
- **Reuse**: On red Playwright run, open `failures/` under the screenshot run id first

### 2026-08-05 — Close all MDI tabs at Playwright journey start — **reverted**

- **Outcome**: reverted (do not use)
- **Context**: Post-login `CloseAllMdiTabsAndRestoreShellAsync` added during passport E2E debugging
- **Decision**: **Do not close MDI tabs** in Playwright E2E — rely on `ActivateMdi*TabAsync` + visible-field locators instead; closing also dismisses Report Dashboard and adds an extra officer step not in real journeys
- **Reuse**: Never call **Close all tabs** / `VisaCloseAllTabs` in automated E2E

### 2026-08-05 — Close all MDI tabs at Playwright journey start

- **Outcome**: positive (policy) — part of **Person add-passport RCA** below — **superseded by revert above**
- **Context**: TabbedMDI keeps inactive views in DOM; passport step matched hidden duplicate fields / wrong toolbar
- **Symptom**: Stale Employees / Passport tabs from prior attempts or headed debugging caused invisible `xaf-item-passportnumber` matches
- **Fix / reuse**: After login + first `WaitForApplicationShellAsync`, call `CloseAllMdiTabsAndRestoreShellAsync` (toolbar **Close all tabs** / `VisaCloseAllTabs`, then re-open **Report Dashboard** nav — closing also dismisses the dashboard tab). **Do not** close all before nested Passport New — that would close the employee detail tab.
- **Reuse**: Post-login MDI reset only; passport flow still needs `ActivateMdiPassportTabAsync` + visible-field locators

### 2026-08-05 — Playwright-only; EasyTest deprecated; DetailView top→bottom fill

- **Outcome**: positive (policy)
- **Context**: skill `visa2026-easytest-e2e`; UserManual media; officer simulation
- **Symptom**: Dual-driver guidance and EasyTest batch fill did not match custom UI or real officer top→bottom entry
- **Fix / reuse**: **Playwright only** for new E2E under `Playwright/`. **EasyTest deprecated** (do not add/extend; migrate when touching). All DetailView fills **top → bottom** in layout order (map §3 + yaml `fill:` + helper arrays). MSBuild config / `Record-EasyTest.ps1` names remain historical.
- **Reuse**: New journey → Playwright + ordered fill; never new EasyTest Facts

### 2026-08-05 — Person add-passport Playwright failure RCA (TabbedMDI + locators) — **canonical**

- **Outcome**: positive (verified green — run `20260805-160740`, `PersonOfficerJourney_LoginCreateEmployeeAddPassport_Local`, ~1m 8s)
- **Context**: UserManual Playwright `PlaywrightPersonOfficerJourney.RunLoginCreateEmployeeAddPassportAsync`; media keys `person-add-passport-step-*`; red on runs `20260805-154058`, `20260805-155226` at step `add-passport-open-form`
- **Symptoms**:
  - Timeout waiting for `e2e-passport-passport-number` — DOM had `xaf-item-passportnumber` but field **not visible**
  - Headed: Passport detail opened with defaults (**P — National passport**, Türkiye) yet test never filled Passport Number
  - Wrong toolbar **New** (e.g. Education) when multiple MDI tabs were open
  - `WaitForURL` after **New Passport** never fires — URL stays on `Person_DetailView_Employee` (TabbedMDI)
- **Root causes** (stacked — all contributed):
  1. **TabbedMDI DOM duplication** — inactive MDI tabs keep full form HTML; Playwright `.First` / non-visible locators match **hidden** duplicates
  2. **`e2e-passport-*` not in rendered HTML** — `ModelDefault(CustomCSSClassName)` on `Passport` BO does not appear in Blazor output; real markers are `xaf-item-passportnumber`, `xaf-item-passporttype`, etc.
  3. **MDI tab name collision** — `:has-text('Passport')` / substring tab click hit nested layout tab **Passports**, not MDI tab **Passport**
  4. **Stale MDI tabs** — headed retries left Employees / Passport tabs open → wrong Save/New targets
  5. **Unscoped nested New** — generic `button[title='New']` on employee detail can hit the wrong nested collection
- **Fixes** (`PlaywrightPageInteractions.cs`, `PlaywrightPersonOfficerJourney.cs`):
  - `FindFirstVisibleLocatorAsync` + `TryGetVisibleXafItemLocatorAsync` — map `e2e-passport-*` → `xaf-item-{suffix}`; always pick **first visible**
  - `ActivateMdiPassportTabAsync` — `GetByRole(Tab, Name: "Passport", Exact: true)` after nested New
  - `PassportsNestedNewButton` — `title^='New Passport'` + `data-action-name='New'`, scoped to Passports nested list
  - `ClickPassportsNestedNewAsync` — layout **Passports** tab → wait list → New Passport → activate MDI **Passport** tab → `WaitForPassportNumberFieldAsync`
  - `PlaywrightFailureCapture` + `PlaywrightE2eStepRunner` — step-scoped artifacts (`add-passport-open-form`, `add-passport-fill-fields`, `add-passport-save`) under `recordings/screenshots/{runId}/failures/`
  - `EnsureLookupBoundAsync` for Passport Type; product default **P — National passport** (`Passport.DefaultPassportTypeCode = "P"`, `E2ETestPassportCreateValues.PassportTypeDisplay`)
  - ~~`CloseAllMdiTabsAndRestoreShellAsync`~~ — **reverted**; do not close MDI tabs in E2E
- **Reuse checklist** (any TabbedMDI nested DetailView):
  1. Do not close all MDI tabs in E2E
  2. Do not rely on URL after nested New — assert visible fields / toolbar on **active** tab
  3. Activate MDI tab by **exact** title before fill/assert
  4. Locators: visible-first; prefer `xaf-item-*` or caption/`GetByLabel` over bare `e2e-*` unless wired in `Model.xafml` (like login fields)
  5. Scope nested **New** by full action title (`New Passport`, not `New`)
  6. On red: open `failures/` under screenshot run id first
- **Files**: `PlaywrightPageInteractions.cs`, `PlaywrightPersonOfficerJourney.cs`, `PlaywrightFailureCapture.cs`, `PlaywrightE2eStepRunner.cs`, `Passport.cs`, `E2ETestDataSeed.cs`, `CloseTabsToolbarController.cs` (`VisaCloseAllTabs`)
- **See also**: earlier same-day bullets (failure capture, close-all-tabs) — this entry is the **single RCA** for person-add-passport

### 2026-08-05 — Visa fill: Issue Date caption collision + failure capture verified

- **Context**: `add-visa-fill-fields` red after passport steps green; failure artifacts under `20260805-165402/failures/`
- **Symptoms**: `Could not find visible field '' (Issue Date)` — `FillDateFieldAsync` caption/`GetByLabel` path finds nothing visible
- **Root cause**: TabbedMDI keeps passport **and** visa forms in DOM; both have `xaf-item-issuedate` / caption **Issue Date**; caption-only locators cannot distinguish active visa tab from hidden passport tab
- **Fix**: Add `ModelDefault(CustomCSSClassName)` on `Visa` BO (`e2e-visa-visa-number`, `e2e-visa-issue-date`, `e2e-visa-start-date`, `e2e-visa-expiration-date`); journey fills use these classes → `TryGetVisibleXafItemLocatorAsync` picks **first visible** `xaf-item-{suffix}` on active MDI tab
- **Failure capture**: `PlaywrightFailureCapture` wrote PNG + HTML + `.txt` at step failure before rethrow; fixture/test runners also captured — open `failures/add-visa-fill-fields-*.html` to confirm field markup
- **Green**: full journey pass after visa e2e classes (run after `20260805-165402`)
