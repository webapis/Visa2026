# login-smoke — EasyTest scenario map

## 0. Header

| Field | Value |
|-------|--------|
| **Scenario id** | `login-smoke` |
| **E2E id** | E2E-001 |
| **Status** | Promoted — `scenarios/ready/` |
| **Map version** | 1.0 |
| **Date** | 2026-06-11 |
| **YAML file** | [login-smoke.yaml](./login-smoke.yaml) |
| **C# test** | `SmokeTests.LoginSmoke_AuthenticatedShellLoads` |

---

## 1. Journey

Log on as **`standarduser`** (empty password — officer default on EasyTest DB), then assert the **authenticated app shell** loaded.

**Outcome shield:** filling logon fields alone is not enough — after `Log In`, C# calls `AssertAuthenticatedAppShell()` (navigates to **Application** list and expects **New**).

---

## 2. Navigation

| Item | Value |
|------|--------|
| **Base URL** | `http://localhost:5050` |
| **User** | `E2ETestLoginValues.StandardUserName` (`standarduser`) |
| **Password** | empty |
| **Post-login check** | `AppContext.Navigate("Application")` + action `New` visible |

---

## 3. Caption inventory

| Caption / action | UI target | Step uses | Status | Notes |
|------------------|-----------|-----------|--------|-------|
| `User Name` | Logon | login fill | **verified** | EasyTestParameter |
| `Password` | Logon | login fill | **verified** | |
| `Log In` | Logon action | login | **verified** | `E2ETestBase.Login` |
| `Application` | Sidebar navigation | assert-shell | **verified** | `AppContext.Navigate("Application")` |
| `New` | Application ListView toolbar | assert-shell | **verified** | Outcome shield |

**Ready for YAML:** ☑ all rows verified or waived

---

## 4. Proposed YAML

```yaml
id: login-smoke
e2eId: E2E-001
description: Log on as standarduser and assert authenticated app shell
user: standarduser
password: ""
steps:
  - login: { user: standarduser, password: "" }
  - assert-shell: true
```

---

## 5. Blockers

None.

---

## 6. Changelog

| Date | Change |
|------|--------|
| 2026-06-11 | Phase 0 — EasyTest map + yaml + `SmokeTests` (Option A) |
| 2026-06-11 | Promoted to `scenarios/ready/` after green `SmokeTests` run |
