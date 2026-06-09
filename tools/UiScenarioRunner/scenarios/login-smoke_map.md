# login-smoke — scenario map

## 0. Header

| Field | Value |
|-------|--------|
| **Scenario id** | `login-smoke` |
| **Status** | Ready for YAML |
| **Map version** | 1.1 |
| **Date** | 2026-06-09 |
| **YAML file** | [login-smoke.yaml](./login-smoke.yaml) |

---

## 1. Journey

Open logon page, fill **UserName** and **Password**, click **Log In** (Admin, empty password — local dev default), then assert the **People** nav group is visible (authenticated app shell).

**Outcome shield:** logon fields + submit are not sufficient — `assert-visible: nav-people` proves post-login shell loaded.

---

## 2. Navigation

| Item | Value |
|------|--------|
| **Base URL** | `http://localhost:5052` (scenario host) or CI `:5000` |
| **Auth** | none (`requiresAuth: false`) |
| **Paths** | `/LoginPage` → app shell (nav) |

---

## 3. Hook inventory

| Hook id | UI target | Step uses | Status | Notes |
|---------|-----------|-----------|--------|-------|
| `login-user-name` | Logon `UserName` | fill | **verified** | [UI_TEST_HOOKS.md](../../../docs/UI_TEST_HOOKS.md) |
| `login-password` | Logon `Password` | fill | **verified** | |
| `login-submit` | Action `Logon` | click | **verified** | Runner waits for app shell after click |
| `nav-people` | Nav group `People` | assert-visible | **verified** | **Outcome shield** — fails if still on logon / bad credentials |

**Ready for YAML:** ☑ all rows verified or waived

---

## 4. Proposed YAML

```yaml
id: login-smoke
description: Log on as Admin and assert app shell (nav) is visible
requiresAuth: false
steps:
  - goto: /LoginPage
  - fill:
      login-user-name: Admin
      login-password: ""
  - click: login-submit
  - assert-visible: nav-people
```

---

## 5. Blockers

None.

---

## 6. Changelog

| Date | Change |
|------|--------|
| 2026-06-07 | Promoted to `tools/UiScenarioRunner/scenarios/` — ready |
| 2026-06-09 | P0 outcome shield: `assert-visible: nav-people` after logon |
