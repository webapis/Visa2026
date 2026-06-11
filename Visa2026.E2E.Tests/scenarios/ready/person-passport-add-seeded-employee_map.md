# person-passport-add-seeded-employee — EasyTest scenario map

## 0. Header

| Field | Value |
|-------|--------|
| **Scenario id** | `person-passport-add-seeded-employee` |
| **E2E id** | E2E-020 |
| **Status** | Promoted — `scenarios/ready/` |
| **Map version** | 1.0 |
| **Date** | 2026-06-11 |
| **YAML file** | [person-passport-add-seeded-employee.yaml](./person-passport-add-seeded-employee.yaml) |
| **C# test** | `PassportTests.Passport_AddOnSeededEmployee_SavesAndShowsPassportNumber` |

**Seed constants:** [`E2ETestDataSeed`](../../../Visa2026.Module/DatabaseUpdate/E2ETestDataSeed.cs), [`E2ETestPassportCreateValues`](../../../Visa2026.Module/DatabaseUpdate/E2ETestDataSeed.cs)

---

## 1. Journey

Log on as **`standarduser`**, open seeded employee **`E2E-TEST-001`** (`E2ETestDataSeedUpdater`), open **Passports** tab, **New** nested passport, fill required fields, **Save**, assert **Passport Number** on detail.

**Outcome:** second passport **`E2E-PASS-020`** on seeded employee (first seed passport remains **`E2E-PASS-001`**).

**Parent arrange:** seed only — no UI employee create in this scenario.

---

## 2. Navigation

| Item | Value |
|------|--------|
| **Base URL** | `http://localhost:5050` |
| **User** | `standarduser` / empty password |
| **List path** | `Person_ListView_Employees` |
| **Parent** | `Personal Number` = `E2E-TEST-001` |
| **Tab hook** | `person-employee-tab-passports` |
| **Nested New hook** | `person-employee-tab-passports-new` |
| **Child detail** | `Passport_DetailView` |

---

## 3. Caption inventory

| Caption / action | UI target | Step uses | Status | Notes |
|------------------|-----------|-----------|--------|-------|
| `User Name` | Logon | login | **verified** | |
| `Password` | Logon | login | **verified** | |
| `Log In` | Logon | login | **verified** | |
| *(URL)* | `Person_ListView_Employees` | goto | **verified** | `NavigateEmployeesList()` |
| `Personal Number` | grid open | open parent | **verified** | seed row |
| *(tab)* | Passports | activate | **verified** | `person-employee-tab-passports` |
| `New` | nested Passports list | action | **verified** | hook / EasyTest action |
| `Passport Number` | `Passport.PassportNumber` | fill / assert | **verified** | `passport-passport-number` fallback |
| `Passport Type` | lookup | fill | **verified** | `AML — Accredited national passport` |
| `Issue Date` | date | fill | **verified** | |
| `Expiration Date` | date | fill | **verified** | |
| `Authority` | text | fill | **verified** | |
| `Issued Country` | lookup | fill | **verified** | `Türkiye` |
| `Save` | Passport detail | action | **verified** | `passport-detail-save` fallback |
