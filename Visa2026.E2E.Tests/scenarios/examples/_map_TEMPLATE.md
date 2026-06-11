# `<scenario-id>` — EasyTest scenario map

## 0. Header

| Field | Value |
|-------|--------|
| **Scenario id** | `<scenario-id>` |
| **E2E id** | E2E-xxx |
| **Status** | Draft \| Captions pending \| Ready for YAML \| YAML authored |
| **Map version** | 0.1 |
| **Date** | YYYY-MM-DD |
| **YAML file** | `<scenario-id>.yaml` *(not created until Ready for YAML)* |
| **C# test** | `*Tests.<MethodName>` |

---

## 1. Journey

*(Officer goal: BO, views, expected outcome.)*

---

## 2. Navigation

| Item | Value |
|------|--------|
| **Base URL** | `http://localhost:5050` (EasyTest host) |
| **User** | `standarduser` / empty password (or `Admin`) |
| **Paths** | Blazor view ids, e.g. `Person_ListView_Employees` |
| **Seed constants** | `E2ETestLoginValues`, `E2ETestEmployeeCreateValues`, … |

---

## 3. Caption inventory

| Caption / action | UI target | Step uses | Status | Notes |
|------------------|-----------|-----------|--------|-------|
| `User Name` | Logon | login fill | **verified** | |
| `Password` | Logon | login fill | **verified** | |
| `Log In` | Logon action | login | **verified** | |
| `New` | List toolbar | assert / click | **verified** | |

**Ready for YAML:** ☐ all rows **verified** or **waived**

---

## 4. Proposed YAML

```yaml
id: <scenario-id>
e2eId: E2E-xxx
description: ...
user: standarduser
password: ""
steps:
  - login: { user: standarduser, password: "" }
  - assert-shell: true
```

---

## 5. Blockers

- TabbedMDI, combo retry, nested collection New, caption drift vs model.

---

## 6. Changelog

| Date | Change |
|------|--------|
| YYYY-MM-DD | Initial map |
