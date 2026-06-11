# person-employee-create — EasyTest scenario map

## 0. Header

| Field | Value |
|-------|--------|
| **Scenario id** | `person-employee-create` |
| **E2E id** | E2E-010 |
| **Status** | Promoted — `scenarios/ready/` |
| **Map version** | 1.0 |
| **Date** | 2026-06-11 |
| **YAML file** | [person-employee-create.yaml](./person-employee-create.yaml) |
| **C# test** | `EmployeeTests.Employee_Create_RequiredFields_SavesAndAppearsInList` |

**UiScenario twin:** [`tools/UiScenarioRunner/scenarios/person-employee-create.yaml`](../../../tools/UiScenarioRunner/scenarios/person-employee-create.yaml) (hook ids, `:5052`). Seed display strings differ slightly; EasyTest uses [`E2ETestEmployeeCreateValues`](../../../Visa2026.Module/DatabaseUpdate/E2ETestDataSeed.cs).

---

## 1. Journey

Log on as **`standarduser`**, open **Employees** list via URL, click **New**, fill required **Person (Employee)** detail fields, **Save**, return to list, open the row by **Personal Number**, assert scalar values persisted.

**Outcome:** saved employee with `Personal Number` **E2E-EMP-010** and expected First/Last name on detail.

**Note:** `Visa Application Family Members Text` is not filled in EasyTest — `Person.OnSaving` defaults it for employees when empty (UiScenario fills explicitly via hook).

---

## 2. Navigation

| Item | Value |
|------|--------|
| **Base URL** | `http://localhost:5050` |
| **User** | `standarduser` / empty password |
| **List path** | `Person_ListView_Employees` |
| **Detail path** | `Person_DetailView_Employee` (after **New**) |
| **Seed constants** | `E2ETestEmployeeCreateValues`, `E2ETestLoginValues` |

---

## 3. Caption inventory

| Caption / action | UI target | Step uses | Status | Notes |
|------------------|-----------|-----------|--------|-------|
| `User Name` | Logon | login fill | **verified** | |
| `Password` | Logon | login fill | **verified** | |
| `Log In` | Logon action | login | **verified** | |
| *(URL)* | `Person_ListView_Employees` | goto | **verified** | `NavigateEmployeesList()` |
| `New` | Employees ListView | action | **verified** | |
| `First Name` | `Person.FirstName` | fill | **verified** | `FillFormWithRetry` |
| `Last Name` | `Person.LastName` | fill | **verified** | |
| `Personal Number` | `Person.PersonalNumber` | fill / grid | **verified** | `ProcessRow` key |
| `Date Of Birth` | `Person.DateOfBirth` | fill | **verified** | XAF title case; Selenium fallback `#person-date-of-birth` |
| `Birth Place` | `Person.BirthPlace` | fill | **verified** | |
| `Country Of Birth` | `Person.CountryOfBirth` | fill | **verified** | Lookup — `Türkiye` |
| `Gender` | `Person.Gender` | fill | **verified** | `Male` |
| `Marital Status` | `Person.MaritalStatus` | fill | **verified** | `Single` |
| `Nationality` | `Person.Nationality` | fill | **verified** | |
| `Foreign Address` | `Person.ForeignAddress` | fill | **verified** | |
| `Foreign Address Country` | `Person.ForeignAddressCountry` | fill | **verified** | |
| `Project Contract` | `Person.ProjectContract` | fill | **verified** | `GT-15` |
| `Company (Subcontractor)` | `Person.Subcontractor` | fill | **verified** | `Çalyk Enerji` |
| `Save` | Detail toolbar | action | **verified** | Not SaveAndClose on new employee |
| *(grid row)* | List by Personal Number | open | **verified** | `GetGrid().ProcessRow` |

**Ready for YAML:** ☑ all rows verified or waived

---

## 4. Proposed YAML

Authoritative: [person-employee-create.yaml](./person-employee-create.yaml).

---

## 5. Blockers

| Topic | Notes |
|-------|--------|
| **TabbedMDI URL** | After **New**, browser URL may stay `http://localhost:5050/` — assert employee detail via **Save** + **Project Contract** form field, not URL alone |
| **Blazor captions** | Use XAF title case (**Date Of Birth**, **Country Of Birth**); date editor may need hook `InputId` fallback via `FillInputByTestId` |
| **SaveAndClose** | Not on new employee detail — use **Save** then `goto` list (same as UiScenario promoted yaml) |
| **Lookup combos** | Use tenant display strings from `E2ETestEmployeeCreateValues`; `FillFormWithRetry` per field |
| **Unique PersonalNumber** | Fixed seed `E2E-EMP-010`; DB dropped once per test run |

---

## 6. Changelog

| Date | Change |
|------|--------|
| 2026-06-11 | Phase 1 — EasyTest map + yaml; C# already in `EmployeeTests` |
| 2026-06-11 | Promoted to `scenarios/ready/` after green `EmployeeTests` run (~95s) |
