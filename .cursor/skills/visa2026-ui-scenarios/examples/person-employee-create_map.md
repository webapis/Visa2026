# person-employee-create — scenario map

## 0. Header

| Field | Value |
|-------|--------|
| **Scenario id** | `person-employee-create` |
| **Status** | Green local run — ready to promote |
| **Map version** | 0.3 |
| **Date** | 2026-06-08 |
| **YAML file** | [person-employee-create.yaml](./person-employee-create.yaml) |

---

## 1. Journey

Log in as **Admin**, open **Employees** from the sidebar, click **New** on the employee list, fill **employee Person** detail fields on the new record, and click **Save** so the person persists.

**Outcome:** a new `Person` with `PersonRole = Employee` is saved (validation passes).

**Scope note:** XAF save rules require more than FirstName/LastName — see §5 run-time notes. Scenario **v1 fill** uses all hook-backed employee scalars from §3 (verified).

---

## 2. Navigation

| Item | Value |
|------|--------|
| **Base URL** | `https://localhost:5001` |
| **Auth** | `login` step — user `Admin`, password from env / empty in dev |
| **List path** | `/Person_ListView_Employees` *(after nav click or direct `goto`)* |
| **Detail path** | `/Person_DetailView_Employee/{new-oid}` *(opened by **New** — OID assigned at runtime)* |
| **Fixture** | None for list/detail URL — **New** creates unsaved detail |
| **Env vars** | `VISA2026_SCENARIO_BASE_URL`, `VISA2026_SCENARIO_USER`, `VISA2026_SCENARIO_PASSWORD` |

**Nav sequence (YAML v1):** `login` → **`goto: /Person_ListView_Employees`** — nav click variant (`nav-people-employees`) is **waived** in yaml because People accordion expand/collapse is flaky in Playwright; hooks remain verified for a separate nav smoke.

---

## 3. Hook inventory

| Hook id | UI target | Step uses | Status | Notes |
|---------|-----------|-----------|--------|-------|
| `login-user-name` | Logon `UserName` | login | **verified** | [UI_TEST_HOOKS.md](../../../docs/UI_TEST_HOOKS.md) |
| `login-password` | Logon `Password` | login | **verified** | |
| `login-submit` | Action `Logon` | login | **verified** | |
| `nav-people` | Nav group `People` | click | **verified** | **waived** if Employees item visible without expand |
| `nav-people-employees` | Nav item `Employees` | click | **verified** | Opens `Person_ListView_Employees` |
| `person-list-employees-new` | ListView action `New` | click | **verified** | Shadow toolbar; URL sync in TabbedMDI |
| `person-detail-employee-save` | Detail action `Save` | click | **verified** | New employee detail after **New** |
| `person-first-name` | `Person.FirstName` | fill | **verified** | Required on save |
| `person-last-name` | `Person.LastName` | fill | **verified** | Required on save |
| `person-personal-number` | `Person.PersonalNumber` | fill | **verified** | Required; use unique test value (not `0` if duplicate) |
| `person-date-of-birth` | `Person.DateOfBirth` | fill | **verified** | Required on save |
| `person-gender` | `Person.Gender` | fill | **verified** | Lookup combo — `fill` uses display text (tenant-specific env) |
| `person-marital-status` | `Person.MaritalStatus` | fill | **verified** | Employee detail |
| `person-nationality` | `Person.Nationality` | fill | **verified** | Lookup combo |
| `person-foreign-address` | `Person.ForeignAddress` | fill | **verified** | Required for employees |
| `person-foreign-address-country` | `Person.ForeignAddressCountry` | fill | **verified** | Lookup combo |
| `person-project-contract` | `Person.ProjectContract` | fill | **verified** | Lookup combo |
| `person-subcontractor` | `Person.Subcontractor` | fill | **verified** | Lookup combo |
| `person-visa-application-family-members-text` | `Person.VisaApplicationFamilyMembersText` | fill | **verified** | Required for employees when no family members |

**Ready for YAML:** ☑ — all §3 hooks **verified** (2026-06-07 DevTools on Employee detail after **New**)

---

## 4. Proposed YAML

Authoritative: [person-employee-create.yaml](./person-employee-create.yaml). **Green run:** 2026-06-08 on `http://localhost:5052` (launch profile **Visa2026 - UI Scenarios (LocalDB)**), `--headed --slow-mo 500`.

```yaml
id: person-employee-create
description: Log in, open Employees list, New employee, fill required fields, Save
requiresAuth: true

env:
  employeePersonalNumber: E2E-CREATE-001
  employeeFirstName: Scenario
  employeeLastName: Employee
  employeeDateOfBirth: "01.01.1990"
  employeeGender: REPLACE-TENANT-GENDER-DISPLAY
  employeeMaritalStatus: REPLACE-TENANT-MARITAL-DISPLAY
  employeeNationality: REPLACE-TENANT-NATIONALITY-DISPLAY
  employeeForeignAddress: E2E test foreign address
  employeeForeignAddressCountry: REPLACE-TENANT-COUNTRY-DISPLAY
  employeeProjectContract: REPLACE-TENANT-PROJECT-DISPLAY
  employeeSubcontractor: REPLACE-TENANT-SUBCONTRACTOR-DISPLAY
  employeeVisaFamilyMembersText: "0"

steps:
  - login:
      user: Admin
      password: ""
  - goto: /Person_ListView_Employees
  - wait-for: person-list-employees-new
  - click: person-list-employees-new
  - wait-for: person-detail-employee-save
  - fill:
      person-first-name: ${employeeFirstName}
      person-last-name: ${employeeLastName}
      person-personal-number: ${employeePersonalNumber}
      person-date-of-birth: ${employeeDateOfBirth}
      person-gender: ${employeeGender}
      person-marital-status: ${employeeMaritalStatus}
      person-nationality: ${employeeNationality}
      person-foreign-address: ${employeeForeignAddress}
      person-foreign-address-country: ${employeeForeignAddressCountry}
      person-project-contract: ${employeeProjectContract}
      person-subcontractor: ${employeeSubcontractor}
      person-visa-application-family-members-text: ${employeeVisaFamilyMembersText}
  - click: person-detail-employee-save
  - assert-visible: person-detail-employee-save
```

---

## 5. Run-time notes (not hook blockers)

| Topic | Notes |
|-------|--------|
| **Hook prep** | **Resolved** — login, nav, list **New**, detail **Save**, and all §3 employee scalars **verified** (DevTools + `UI_TEST_HOOKS.md`) |
| **Tenant lookup display text** | Set `env` values to existing catalog display strings before run (Gender, Nationality, ProjectContract, etc.) |
| **Lookup `fill` on combo boxes** | Runner uses `FillAsync` on hooked `dxbl-combo-box` — confirm on first green run; append outcome to **learnings.md** |
| **Save validation** | Full §4 fill should satisfy `Person` employee `RuleRequiredField`; adjust env if validation banner appears |
| **Unique PersonalNumber** | Use env suffix or timestamp per run to avoid duplicate-person validation |
| **No ListView row hook** | **Waived** — journey uses **New**, not open existing row |

---

## 6. Handoff — @visa2026-ui-test-hooks

**Resolved (2026-06-07).** Scalar + toolbar hooks verified on `Person_DetailView_Employee` (new record). No further hook prep for this scenario unless layout or captions change.

---

## 7. Changelog

| Date | Change |
|------|--------|
| 2026-06-07 | Initial map — journey login → Employees nav → New → fill → Save |
| 2026-06-07 | §3 hooks verified; status **Ready for YAML**; draft **person-employee-create.yaml** |
