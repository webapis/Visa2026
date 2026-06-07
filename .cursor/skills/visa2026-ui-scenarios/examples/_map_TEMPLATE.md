# `<scenario-id>` — scenario map

## 0. Header

| Field | Value |
|-------|--------|
| **Scenario id** | `<scenario-id>` |
| **Status** | Draft \| Hooks pending \| Ready for YAML \| YAML authored |
| **Map version** | 0.1 |
| **Date** | YYYY-MM-DD |
| **YAML file** | `<scenario-id>.yaml` *(not created until Ready for YAML)* |

---

## 1. Journey

*(User goal: which BO, which views, expected outcome.)*

Example: Log in as Admin, open an employee `Person` detail, fill `FirstName` and `LastName`, save.

---

## 2. Navigation

| Item | Value |
|------|--------|
| **Base URL** | `https://localhost:5001` |
| **Auth** | `login` step — user `Admin` |
| **Paths** | `/LoginPage`, `/Person_DetailView_Employee/{guid}` |
| **Fixture** | Employee OID: *(paste from browser)* |
| **Env vars** | `VISA2026_HOOK_VERIFY_PERSON_URL` optional |

---

## 3. Hook inventory

| Hook id | UI target (view / BO / action) | Step uses | Status | Notes |
|---------|--------------------------------|-----------|--------|-------|
| `login-user-name` | Logon `UserName` | login | verified | `UI_TEST_HOOKS.md` |
| `login-password` | Logon `Password` | login | verified | |
| `login-submit` | Action `Logon` | login | verified | |
| `person-first-name` | `Person.FirstName` | fill | implemented | needs DevTools verify |
| `person-last-name` | `Person.LastName` | fill | missing | |
| `toolbar-save` | Action `Save` | click | missing | |

**Ready for YAML:** ☐ all rows verified or waived

---

## 4. Proposed YAML

*(Sketch — hook ids only. Final file matches this when §3 complete.)*

```yaml
id: <scenario-id>
description: ...
requiresAuth: true
env:
  personDetailPath: /Person_DetailView_Employee/REPLACE-GUID
steps:
  - login:
      user: Admin
      password: ""
  - goto: ${personDetailPath}
  - fill:
      person-first-name: Scenario
      person-last-name: Employee
  # - click: toolbar-save   # when verified
```

---

## 5. Blockers

- List missing hooks and navigation gaps (grid row hook, MDI tabs, …).

---

## 6. Changelog

| Date | Change |
|------|--------|
| YYYY-MM-DD | Initial map |
