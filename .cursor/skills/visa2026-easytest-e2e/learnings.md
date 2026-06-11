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
