# person-master-data-crud — EasyTest scenario map

## 0. Header

| Field | Value |
|-------|--------|
| **Scenario id** | `person-master-data-crud` |
| **E2E id** | E2E-001 + E2E-002…E2E-008 |
| **Status** | Ready for YAML |
| **Map version** | 1.0 |
| **Date** | 2026-07-30 |
| **YAML file** | [person-master-data-crud.yaml](./person-master-data-crud.yaml) |
| **C# test** | `PersonOfficerJourneyTests.PersonOfficerJourney_LoginCreateEmployeeMasterDataCrud` |

**Constants:** [`E2ETestDataSeed.cs`](../../../Visa2026.Module/DatabaseUpdate/E2ETestDataSeed.cs)

**Note:** Extends E2E-001 (employee + passport). **Visa** is nested under **Passport → Visas**, not a Person tab. Issued-document tabs are excluded.

---

## 1. Journey

Officer session: **log on** → create **Employee** → **Passport** → nested **Visa** → **Education** → **Address of residence** (Private house) → **Medical record** (create, update, delete) → **Position history** → **Work duty** → **Salary** → **External arrival** travel.

---

## 2. Navigation

| Item | Value |
|------|--------|
| **Base URL** | `http://localhost:5050` |
| **User** | `standarduser` / empty password |
| **List path** | `Person_ListView_Employees` |
| **Employee detail** | `Person_DetailView_Employee` |
| **Nested details** | Passport / Visa / Education / AddressOfResidence / MedicalRecord / EmployeePositionHistory / WorkDuty / EmployeeSalary / ExternalArrival |

---

## 3. Caption inventory

| Caption / action | UI target | Step uses | Status | Notes |
|------------------|-----------|-----------|--------|-------|
| Logon + employee create + Passports | (E2E-001) | create | verified | existing helpers |
| `Visas` | Passport detail tab | activate | pending CI | nested under passport |
| `New Visa` | nested Visas toolbar | create | pending CI | |
| `Visa Number` / dates | Visa detail | fill | pending CI | lookups OnCreated |
| `Educations` | Person tab | activate | pending CI | |
| `New Education` | nested toolbar | create | pending CI | |
| `Education Institution` | Education detail | fill | pending CI | `Adana liseýi` |
| `Addresses Of Residence` / `AddressesOfResidence` | Person tab | activate | pending CI | alias |
| `New Address Of Residence` | nested toolbar | create | pending CI | |
| `Type` = `Private house` | Address detail | fill | pending CI | then Full Address visible |
| `Region` / `City` / `Full Address` / `Expiration Date` | Address detail | fill | pending CI | |
| `Medical Records` / `MedicalRecords` | Person tab | activate | pending CI | |
| `New Medical Record` | nested toolbar | create | pending CI | |
| `Document Number` | Medical detail | fill/update | pending CI | ValidityDuration OnCreated |
| `Delete` | Medical nested list | delete | pending CI | waived if flaky |
| `Position History` / `PositionHistory` | Person tab | activate | pending CI | |
| `Position (visa reports)` / `Position (actual / company)` | Position detail | fill | pending CI | ActualPosition EasyTest seed |
| `Work Duties` / `Gelmeginiň Maksady` | Person tab | activate | pending CI | TM caption alias |
| `Gelmeginiň Maksady` (field) | WorkDuty detail | fill | pending CI | |
| `Salaries` / `New Employee Salary` | Person tab | create | pending CI | Amount + Currency |
| `Travel Histories` / `New External Arrival` | Person tab | create | pending CI | split New (`dxbl-btn-split`); manual CRUD |

**Ready for YAML:** ☐ promote after GHA green

---

## 4. Proposed YAML

See `person-master-data-crud.yaml`.

---

## 5. Blockers

- Nested New title prefixes must match XAF adaptive toolbar titles.
- Address Type must change to Private house before Full Address appears.
- ActualPosition catalog empty except EasyTest seed on `Visa2026EasyTest`.
- PersonDocument (file upload) out of scope.

---

## 6. Changelog

| Date | Change |
|------|--------|
| 2026-07-31 | Re-enable External Arrival after TravelHistory decoupling; fix split New toolbar click |
| 2026-07-30 | Initial Phase A+B master-data CRUD map |
