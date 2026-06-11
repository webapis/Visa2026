# login-nav-employees — EasyTest scenario map

## 0. Header

| Field | Value |
|-------|--------|
| **Scenario id** | `login-nav-employees` |
| **E2E id** | E2E-001-nav |
| **Status** | Promoted — `scenarios/ready/` |
| **Map version** | 1.0 |
| **Date** | 2026-06-11 |
| **YAML file** | [login-nav-employees.yaml](./login-nav-employees.yaml) |
| **C# test** | `SmokeTests.LoginNavEmployees_ListOpensWithNewAction` |

---

## 1. Journey

Log on as **`standarduser`**, open the **Employees** list via URL (not sidebar alone — TabbedMDI pitfall), assert list toolbar **New** is available.

**Outcome:** URL contains `Person_ListView_Employees` and `New` action is found.

---

## 2. Navigation

| Item | Value |
|------|--------|
| **Base URL** | `http://localhost:5050` |
| **User** | `standarduser` / empty password |
| **List path** | `Person_ListView_Employees` (`E2ETestLoginValues.EmployeesListViewPath`) |
| **Helper** | `NavigateEmployeesList()` — Selenium URL via `EasyTestBlazorNavigationHelper` |

**Do not** rely on `Navigate("Employees")` or `People.Employees` alone — may land on Family Members detail while **New** still exists.

---

## 3. Caption inventory

| Caption / action | UI target | Step uses | Status | Notes |
|------------------|-----------|-----------|--------|-------|
| `User Name` | Logon | login fill | **verified** | |
| `Password` | Logon | login fill | **verified** | |
| `Log In` | Logon action | login | **verified** | |
| *(URL)* | `Person_ListView_Employees` | goto | **verified** | Prefer URL over sidebar |
| `New` | Employees ListView | assert | **verified** | Outcome shield |

**Ready for YAML:** ☑ all rows verified or waived

---

## 4. Proposed YAML

```yaml
id: login-nav-employees
e2eId: E2E-001-nav
description: Log on and open Employees list with New action
user: standarduser
password: ""
steps:
  - login: { user: standarduser, password: "" }
  - goto: Person_ListView_Employees
  - assert-action-visible: New
```

---

## 5. Blockers

None.

---

## 6. Changelog

| Date | Change |
|------|--------|
| 2026-06-11 | Phase 0 — replaces duplicate `GeneralTests.TestBlazorApp_Opens` |
| 2026-06-11 | Promoted to `scenarios/ready/` after green `SmokeTests` run |
