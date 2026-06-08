# person-employee-minimal — scenario map

## 0. Header

| Field | Value |
|-------|--------|
| **Scenario id** | `person-employee-minimal` |
| **Status** | Ready for YAML |
| **Map version** | 0.1 |
| **Date** | 2026-06-07 |
| **YAML file** | [person-employee-minimal.yaml](./person-employee-minimal.yaml) *(draft — stays in **examples/** until Ready)* |

---

## 1. Journey

Log in as Admin, open an **employee** `Person` detail view, fill **FirstName** and **LastName**. Save is out of scope until `toolbar-save` hook exists.

---

## 2. Navigation

| Item | Value |
|------|--------|
| **Base URL** | `https://localhost:5001` |
| **Auth** | `login` step |
| **Detail path** | `/Person_DetailView_Employee/{guid}` — copy OID from browser |
| **List path** | `/Person_ListView_Employees` *(alternative when row hook exists)* |

---

## 3. Hook inventory

| Hook id | UI target | Step uses | Status | Notes |
|---------|-----------|-----------|--------|-------|
| `login-user-name` | Logon `UserName` | login | **verified** | [UI_TEST_HOOKS.md](../../../docs/UI_TEST_HOOKS.md) |
| `login-password` | Logon `Password` | login | **verified** | |
| `login-submit` | Action `Logon` | login | **verified** | |
| `person-first-name` | `Person.FirstName` | fill | **verified** | [UI_TEST_HOOKS.md](../../../docs/UI_TEST_HOOKS.md) |
| `person-last-name` | `Person.LastName` | fill | **verified** | |
| `person-detail-employee-save` | Action `Save` | click | **verified** | Optional for minimal scenario |

**Ready for YAML:** ☑ — `person-first-name` and `person-last-name` **verified**; add `click: person-detail-employee-save` in §4 when desired

---

## 4. Proposed YAML

```yaml
id: person-employee-minimal
description: Log in, open employee Person detail, fill FirstName and LastName
requiresAuth: true
env:
  personDetailPath: /Person_DetailView_Employee/REPLACE-WITH-GUID
steps:
  - login:
      user: Admin
      password: ""
  - goto: ${personDetailPath}
  - fill:
      person-first-name: Scenario
      person-last-name: Employee
```

---

## 5. Blockers

- None for hook access — optional: add `click: person-detail-employee-save` when save step is in scope.

---

## 6. Changelog

| Date | Change |
|------|--------|
| 2026-06-07 | Scalar hooks verified; status **Ready for YAML** |
