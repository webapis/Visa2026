# person-officer-journey — EasyTest scenario map

## 0. Header

| Field | Value |
|-------|--------|
| **Scenario id** | `person-officer-journey` |
| **E2E id** | E2E-001 |
| **Status** | Promoted — `scenarios/ready/` |
| **Map version** | 1.0 |
| **Date** | 2026-06-11 |
| **YAML file** | [person-officer-journey.yaml](./person-officer-journey.yaml) |
| **C# test** | `PersonOfficerJourneyTests.PersonOfficerJourney_LoginCreateEmployeeAddPassport` |

**Constants:** [`E2ETestEmployeeCreateValues`](../../../Visa2026.Module/DatabaseUpdate/E2ETestDataSeed.cs), [`E2ETestPassportCreateValues`](../../../Visa2026.Module/DatabaseUpdate/E2ETestDataSeed.cs), [`E2ETestLoginValues`](../../../Visa2026.Module/DatabaseUpdate/E2ETestDataSeed.cs)

---

## 1. Journey

Single officer session: **log on** → **Employees list** → **create employee** (`E2E-EMP-010`) → **add passport** on that employee (`E2E-PASS-020`).

**Outcome:** authenticated shell, saved employee with expected scalars, saved passport on nested **Passports** tab.

**Arrange:** no DB person seed — employee is created in this scenario.

---

## 2. Navigation

| Item | Value |
|------|--------|
| **Base URL** | `http://localhost:5050` |
| **User** | `standarduser` / empty password |
| **List path** | `Person_ListView_Employees` |
| **Employee detail** | `Person_DetailView_Employee` |
| **Passport detail** | `Passport_DetailView` (nested New) |

---

## 3. Caption inventory

| Caption / action | UI target | Step uses | Status |
|------------------|-----------|-----------|--------|
| `User Name` | Logon | login | verified |
| `Password` | Logon | login | verified |
| `Log In` | Logon | login | verified |
| `Application` + `New` | shell smoke | assert | verified |
| *(URL)* | `Person_ListView_Employees` | goto | verified |
| `New` | Employees list | employee create | verified |
| Person required fields | `Person` employee detail | fill | verified |
| `Save` | Person detail | employee create | verified |
| `Personal Number` | grid open | assert employee | verified |
| `Passports` | layout tab | activate | verified | native `GetAction("Passports").Execute()` |
| `Passports.New` | nested Passports list | passport create | verified | fallback `New` |
| Passport required fields | `Passport` detail | fill | verified |
| `Save` | Passport detail | passport create | verified |
| `Passport Number` | assert | verified | `E2E-PASS-020` |
