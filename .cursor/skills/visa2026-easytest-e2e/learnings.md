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

### 2026-06-08 — EasyTest port 5050 isolated from IDE

- **Outcome**: positive
- **Context**: `E2ETestBase`, launch profile `Visa2026 - EasyTest (LocalDB)`
- **Symptom**: Edge opened `localhost:5000` → connection refused or wrong app.
- **Fix / reuse**: Dedicated profile on **`:5050`**; `BlazorApplicationOptions` must pass explicit `url` + `configuration` (not two-arg ctor reading IIS Express).
- **Reuse**: Never run EasyTest against IDE `:5000`.

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
