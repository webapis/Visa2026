# login-smoke — scenario map

## 0. Header

| Field | Value |
|-------|--------|
| **Scenario id** | `login-smoke` |
| **Status** | Ready for YAML |
| **Map version** | 1.0 |
| **Date** | 2026-06-07 |
| **YAML file** | [login-smoke.yaml](./login-smoke.yaml) |

---

## 1. Journey

Open logon page, fill **UserName** and **Password**, click **Log In** (Admin, empty password — local dev default).

---

## 2. Navigation

| Item | Value |
|------|--------|
| **Base URL** | `https://localhost:5001` |
| **Auth** | none (`requiresAuth: false`) |
| **Paths** | `/LoginPage` |

---

## 3. Hook inventory

| Hook id | UI target | Step uses | Status | Notes |
|---------|-----------|-----------|--------|-------|
| `login-user-name` | Logon `UserName` | fill | **verified** | [UI_TEST_HOOKS.md](../../../docs/UI_TEST_HOOKS.md) |
| `login-password` | Logon `Password` | fill | **verified** | |
| `login-submit` | Action `Logon` | click | **verified** | |

**Ready for YAML:** ☑ all rows verified or waived

---

## 4. Proposed YAML

```yaml
id: login-smoke
description: Fill logon fields and submit (Admin)
requiresAuth: false
steps:
  - goto: /LoginPage
  - fill:
      login-user-name: Admin
      login-password: ""
  - click: login-submit
```

---

## 5. Blockers

None.

---

## 6. Changelog

| Date | Change |
|------|--------|
| 2026-06-07 | Promoted to `tools/UiScenarioRunner/scenarios/` — ready |
