# Scenario Guide — data.yaml Import Reference

This document describes how to create, structure, and maintain import scenarios in `data.yaml`.
It is the authoritative reference for developers and AI agents adding new scenarios.

---

## Table of Contents

1. [Overview](#overview)
2. [YAML File Structure](#yaml-file-structure)
3. [Scenario Block Fields](#scenario-block-fields)
4. [Idempotency and the Anchor](#idempotency-and-the-anchor)
5. [Sync mode (re-apply yaml changes)](#sync-mode-re-apply-yaml-changes)
6. [Sheet Processing Order](#sheet-processing-order)
7. [All Available Sheets — Columns and Lookups](#all-available-sheets--columns-and-lookups)
8. [ApplicationType Reference](#applicationtype-reference)
9. [Shared Scenario](#shared-scenario)
10. [Rules for Writing Scenarios](#rules-for-writing-scenarios)
11. [Numbering Conventions](#numbering-conventions)
12. [Common Patterns by ApplicationType Category](#common-patterns-by-applicationtype-category)
13. [Existing Scenarios](#existing-scenarios)

---

## Overview

`data.yaml` drives the full scenario-based import. Each scenario represents one real-world
immigration workflow (e.g. CheckIn, VisaExtension, BusinessTrip). The importer executes
scenarios in `order` sequence, skipping any whose anchor record already exists in the database.

The importer reads all scenario sheets in one pass, then seeds each scenario's rows to the
OData API in sheet-dependency order (see [Sheet Processing Order](#sheet-processing-order)).

**Lookup catalogs are not imported from `data.yaml`.** Scenario rows only **reference** existing
lookup rows by `Name` / `Code` (and similar). Those rows must already be in the database after
**Visa2026.Blazor.Server** runs module updaters.

When authoring or fixing lookup column values, use the **main project** (`Visa2026.Module`) as
the source of truth:

| Need | Where to look |
|------|----------------|
| Countries, regions, visa types, genders, … | `Visa2026.Module/DatabaseUpdate/LookupCatalogs/*.json` |
| Tenant-specific (position, department, project contract, company profile, signatories, …) | `Visa2026.Module/DatabaseUpdate/LookupCatalogs/tenant/*.json` |
| Application types, `Show*` flags, selection codes | `Visa2026.Module/DatabaseUpdate/ApplicationTypeConfigurationSeed.Data.cs` |
| Seeding / deploy behavior | [`docs/LOOKUP_SEEDING.md`](../docs/LOOKUP_SEEDING.md) |
| Human-readable snapshot (secondary) | `LOOKUPS.md` at repo root (`dotnet run --project Visa2026.DataImporter -- --dump-lookups`) |

Do not add catalog seed blocks to `data.yaml` for entities synced by `LookupCatalogSyncUpdater`.

---

## YAML File Structure

```yaml
scenarios:

  - order: 0              # integer — execution sequence (lower runs first)
    name: Shared          # unique identifier — used in dependsOn references
    description: ...      # human-readable, shown in console output
    dependsOn: Shared     # informational only; enforced by setting order correctly
    anchor:
      entity: Lodging     # OData entity name to check
      key: Name           # OData property to filter on
      value: Some Name    # if a record with this value exists, skip the scenario
    data:
      <SheetName>:        # must match a SheetName in ExcelMappings.ScenarioSheets
        - Column A: value
          Column B: value
```

All keys under `data:` must exactly match the `SheetName` values defined in
`ExcelMappings.Sheets` (see [All Available Sheets](#all-available-sheets--columns-and-lookups)).

---

## Scenario Block Fields

| Field | Required | Description |
|-------|----------|-------------|
| `order` | Yes | Integer. Scenarios execute in ascending order. Gaps are allowed. |
| `name` | Yes | Unique string. Used in console output and `dependsOn` references. |
| `description` | No | Human-readable summary shown in logs. |
| `dependsOn` | No | Name of the scenario this one logically follows. **Documentation only — never enforced in code.** Enforce dependency by setting `order` correctly. |
| `sync` | No | When `true`, scenario can be re-imported with `--sync` (PATCH existing rows). See [Sync mode](#sync-mode-re-apply-yaml-changes). |
| `anchor` | Yes | Idempotency check (see below). |
| `data` | Yes | Map of sheet names to lists of row objects. |

---

## Idempotency and the Anchor

Before seeding, the importer queries the OData API:
```
GET /api/odata/{anchor.entity}?$filter={anchor.key} eq '{anchor.value}'&$top=1
```

- If a record is found → the entire scenario is **skipped**.
- If no record is found → all sheets in `data:` are seeded in order.

Sync mode (below) **ignores** the anchor and PATCHes rows that match natural keys in `ExcelMappings`.

**Choose an anchor that is:**
- Created early in the scenario (ideally the first entity seeded)
- Uniquely identifies this scenario's data
- Stable — won't change between runs

**Preferred anchor patterns:**

| Scenario type | Preferred anchor |
|---------------|-----------------|
| Company/Lodging setup | `Lodging` → `Name` |
| Any Application-based scenario | `Application` → `FullApplicationNumber` (e.g. `4/-003`) |
| Person-only scenarios | `Person` → `Email` |

**Important:** The Shared scenario (order 0) uses `Lodging.Name` as anchor, not `Company.Name`.
This is because Company existed before Lodging was moved to Shared — anchoring on Lodging
ensures the Lodging records are always (re)created even if Company already exists.

---

## Sync mode (re-apply yaml changes)

After the first import, editing `data.yaml` and running the importer again **does nothing** for that scenario (anchor still matches). Use sync mode to push changes without deleting the database:

```powershell
dotnet run --project Visa2026.DataImporter -- --sync-scenario InvitationEmployee
```

Only the named scenario(s) run; all others are skipped. Or mark `sync: true` on scenarios and run:

```powershell
dotnet run --project Visa2026.DataImporter -- --sync
```

(Runs every scenario with `sync: true` only.)

| Sheet | Upsert match |
|-------|----------------|
| Persons | Email |
| Passports | Passport Number |
| Applications | Application Number + Date, or optional **Full Application Number** column |
| ApplicationItems | `ApplicationItemName` = `{Person} - {Application}` (e.g. `John Doe - 4/-001`) |
| InvitationItems | `InvitationItemName` = `{Person} - {Invitation Number}` |
| Visas | Visa Number |
| MedicalRecords | Document Number |
| Lodging | Name |
| AddressOfResidence | Person + Full Address |
| Education | Person + Graduation Year + Institution |
| PositionHistory | Person + Start Date + Position |
| EmployeeContracts | Person + Start Date |

Sheets without upsert keys still POST in sync mode (possible duplicates). Extend `SheetMap.UpsertKeys` in `Excelmappings.cs` when you need more.

---

## Sheet Processing Order

Within a scenario, sheets are processed in the order they appear in `ExcelMappings.Sheets`.
This order encodes all dependency constraints. **Do not seed a sheet whose lookups depend
on another sheet that hasn't been seeded yet.**

The enforced order is:

```
1.  Company                     ← legacy Excel export only (runtime: Module tenant JSON)
2.  ProjectContract             ← optional legacy Company lookup
3.  Persons                     ← depends on ProjectContract
4.  CompanyHead                 ← upserts AuthorizedSignatory singleton
5.  Representative              ← upserts AuthorizedRepresentative singleton
6.  Passports                   ← depends on Persons
7.  TravelHistory               ← depends on Persons
8.  MedicalRecords              ← depends on Persons
9.  Education                   ← depends on Persons
10. PositionHistory             ← depends on Persons
11. EmployeeContracts           ← depends on Persons, PositionHistory
12. Lodging                     ← depends on Company
13. AddressOfResidence          ← depends on Persons, Region, City, Lodging
14. Applications                ← depends on ProjectContract, ApplicationType,
                                   ApplicationTypeFilter (org from singletons at runtime)
15. Visas                       ← MUST come before ApplicationItems and Registrations
16. ApplicationItems            ← depends on Application, Person, Passport, Visa
17. BusinessTripPurpose         ← simple lookup (seeded in Shared)
18. BusinessTripAddress         ← MUST come before BusinessTrips
19. BusinessTrips               ← depends on Application, Person, BusinessTripAddress
20. Registrations               ← depends on Application, Person, Passport, Visa
21. Invitations                 ← depends on Application
22. InvitationItems             ← depends on Invitation, Person, Passport
23. WorkPermits                 ← depends on Application
24. WorkPermitItems             ← depends on WorkPermit, Person, Passport, PositionHistory
25. Rejections                  ← depends on Application
26. RejectionItems              ← depends on Rejection, Person
27. ApplicationProgresses       ← depends on Application, ApplicationState, ApplicationLocation
```

**Critical ordering rules:**
- `Visas` must come **before** `ApplicationItems` — ApplicationItem.CurrentVisa lookup
  requires the Visa to already exist.
- `Visas` must come **before** `Registrations` — same reason.
- `BusinessTripAddress` must come **before** `BusinessTrips` — BusinessTrip.BusinessTripAddress
  lookup requires the address to already exist.
- `CompanyHead` / `Representative` sheets should run **before** `Applications` (singleton signatory/rep data).
- `Passports` must come **before** `ApplicationItems` (required field).

---

## All Available Sheets — Columns and Lookups

### Company (→ CompanyProfile — Module tenant JSON only)

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Name | Yes | Scalar | Company profile name |
| Code | | Scalar | Letterhead code (e.g. CLK) |
| Address, PhoneNumber, Email, TaxInformation | | Scalar / StringValue | |

### ApplicationNumbering (→ ApplicationNumberingProfile — `tenant/application-numbering.json`)

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Name | Yes | Scalar | Profile name (e.g. `Default`) |
| AppNumberPrefix, AppNumberFormat | | Scalar | Seeded from Module JSON; not imported via `data.yaml` |
| ApplicationNumberPadding, ApplicationNumberSeed | | Scalar | Same |

### ProjectContract

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Name | Yes | Scalar | |
| Code | | StringValue | Preserve as string (e.g. `GT-15`) |
| Company | | LookupByName | Company.Name |
| IsDefault | | Bool | |

### Persons

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| First Name | Yes | Scalar | |
| Last Name | Yes | Scalar | |
| Date of Birth | Yes | Scalar | ISO date `YYYY-MM-DD` |
| Email | | Scalar | Used as anchor key for person-based scenarios |
| Gender | | LookupByName | Turkmen: `Erkek` (Male), `Aýal` (Female) |
| Nationality | | LookupByName | Country.Name in Turkmen |
| Country of Birth | | LookupByName | Country.Name in Turkmen |
| Marital Status | | LookupByName | Turkmen values (e.g. `Öýlenen`) |
| Project Contract | | LookupByName | Required — validated as `[RuleRequiredField]` on save |
| Is Employee | | Bool | `true` for expat employees |
| Relationship | | LookupByName | For family members only |
| Sponsoring Employee | | PersonLookupByName | For family members — full name of sponsoring employee |

### CompanyHead (→ AuthorizedSignatory singleton)

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Full Name | Yes | Scalar | |
| Position (Tm) | | Scalar | Turkmen position title string |
| Passport Number, Passport Authority, Passport Issue Date | | Scalar / StringValue | |

### Representative (→ AuthorizedRepresentative singleton)

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Full Name | Yes | Scalar | |
| Position (Tm) | | Scalar | |
| Phone | | StringValue | |
| Passport Number, Passport Authority, Passport Issue Date | | Scalar / StringValue | |

### Passports

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Passport Number | Yes | Scalar | |
| Person | Yes | PersonLookupByName | |
| Personal Number | | StringValue | **Must be StringValue** — long numeric IDs (e.g. Turkish TC) overflow int |
| Authority | | StringValue | **Must be StringValue** — free-text issuing authority |
| Issue Date | | Scalar | ISO date |
| Expiration Date | | Scalar | ISO date |
| Passport Type | | LookupByName | e.g. `P - MILLI PASPORT` |
| Issued Country | | LookupByName | Country.Name in Turkmen |

### MedicalRecords

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Document Number | Yes | Scalar | Unique identifier (e.g. `MED-2024-001`) — used as lookup key in ApplicationItems |
| Issue Date | Yes | Scalar | |
| Person | Yes | PersonLookupByName | |
| Validity Duration | Yes | LookupByName | e.g. `1 ýyl` |

### Education

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Graduation Year | Yes | Scalar | |
| Person | Yes | PersonLookupByName | |
| Education Level | Yes | LookupByName | e.g. `Ýokary` |
| Institution | Yes | LookupByName | EducationInstitution.Name |
| Country | Yes | LookupByName | Country.Name |
| Specialty | Yes | LookupByName | Specialty.Name |

### PositionHistory

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Start Date | Yes | Scalar | ISO date |
| Person | Yes | PersonLookupByName | |
| Position | Yes | LookupByName | Position.Name (Turkmen) — used as lookup key |
| Department | Yes | LookupByName | Department.Name |

### EmployeeContracts

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Person | Yes | PersonLookupByName | |
| Start Date | Yes | Scalar | ISO date — used as lookup key (`ContractStartDate`) |
| Salary | Yes | Scalar | |
| Validity Duration | Yes | LookupByName | |
| Position History | | LookupByName | Filter: `Position/Name` — use position name string |

### Lodging

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Name | Yes | StringValue | Unique name — used as lookup key |
| Full Address | | StringValue | |
| Company | | LookupByName | |

### AddressOfResidence

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Person | Yes | PersonLookupByName | |
| Full Address | Yes | StringValue | Must be unique per person — used as lookup key in ApplicationItems |
| Region | Yes | LookupByName | Region.Name |
| City | Yes | LookupByName | City.Name |
| Type | | Scalar (ValueMap) | `0` → Lodging, `1` → Hotel, `2` → PrivateHouse |
| Lodging | | LookupByName | Required when Type = `0`; populates FullAddress automatically |
| Start Date | | Scalar | |
| Expiration Date | | Scalar | |

> **Note:** When `Type: 0` (Lodging) and a Lodging is specified, the server sets
> `FullAddress` from `Lodging.FullAddress`. Still provide `Full Address` in YAML so
> that the importer can use it as a lookup key for ApplicationItems.

### Applications

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Application Number | Yes | StringValue | Numeric portion only (e.g. `003`); combined with prefix server-side |
| Date | Yes | Scalar | ISO date |
| Company | | LookupByName | |
| Project Contract | | LookupByName | |
| Filter | | LookupByName | ApplicationTypeFilter.Name (e.g. `Visa`, `Registration`, `BusinessTrip`) |
| Application Type | | LookupByName | ApplicationType.Name (e.g. `App_Exit_Visa`) |
| Category | | Scalar (ValueMap) | `0` → Employee, `1` → FamilyMember, `2` → Both |
| Migration Service | | LookupByName | MigrationService.Name (long Turkmen name) |
| Urgency | | LookupByName | e.g. `Adaty tertipde !` |
| Visa Period | | LookupByName | e.g. `3 (üç) aý`, `1 (bir) aý` |
| Visa Category | | LookupByName | e.g. `Köp gezeklik`, `Bir gezeklik` |
| Visa Type | | LookupByName | e.g. `WP-Işçi Wiza`, `EX-Çykyş` |
| From City | | LookupByName | City.Name — for internal movement scenarios |
| To City | | LookupByName | City.Name — for internal movement / business trip departure |
| Movement Permit Location | | LookupByName | |
| Border Zone Location | | LookupByName | |
| Business Trip Start Date | | Scalar | ISO date — for business trip scenarios |
| Business Trip End Date | | Scalar | ISO date — for business trip scenarios |
| Business Trip Purpose | | LookupByName | BusinessTripPurpose.Name |

> **FullApplicationNumber:** Built on save from **ApplicationNumberingProfile**
> (`tenant/application-numbering.json`) using `AppNumberFormat` tokens. Current tenant
> format: `{MONTH}/-{NUMBER}` → e.g. `4/-003` for Application Number `003` with
> Application Date in April. Use the **full** value in child sheets (ApplicationItems,
> Registrations, anchors). See `Visa2026.Module/Resources/AppNumberFormat.md`.

### Visas

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Visa Number | Yes | Scalar | Unique (e.g. `TM-2026-V-001`) |
| Issue Date | Yes | Scalar | |
| Visa Type | Yes | LookupByName | VisaType.Name (e.g. `WP-Işçi Wiza`, `EX-Çykyş`, `FM-Maşgala`) |
| Start Date | | Scalar | |
| Expiration Date | | Scalar | |
| Visa Category | | LookupByName | e.g. `Köp gezeklik` |
| Issued Place | | LookupByName | VisaIssuedPlace.Name |
| Passport Number | | LookupByName | Filter: `PassportNumber` |

### ApplicationItems

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Application | Yes | LookupByName | Filter: `FullApplicationNumber` (e.g. `4/-003`) |
| Person | Yes | PersonLookupByName | |
| Passport Number | Yes | LookupByName | Filter: `PassportNumber` |
| Visa Number | | LookupByName | Filter: `VisaNumber` — the **current/existing** visa being worked on |
| Position History | | LookupByName | Filter: `Position/Name` — use position name string |
| Contract | | LookupByName | Filter: `ContractStartDate` — use ISO date string |
| Previous Passport | | LookupByName | Filter: `PassportNumber` — for ChangePassport scenarios |
| Work Permit Item | | LookupByName | Filter: `WorkPermitNumber` |
| Work Permit Item 2 | | LookupByName | Filter: `WorkPermitNumber` — second WP item |
| Invitation Item | | LookupByName | Filter: `InvitationItemName` |
| Address | | LookupByName | Filter: `FullAddress` — AddressOfResidence |
| Registration | | LookupByName | Filter: `RegistrationNumber` |
| Medical Record | | LookupByName | Filter: `DocumentNumber` |
| Education | | LookupByName | Filter: `EducationDescription` |
| Invitation Issued | | Bool | `true` when invitation is issued |
| Work Permit Issued | | Bool | `true` when work permit is issued |
| Rejection Issued | | Bool | `true` when rejected |
| Visa Issued | | Bool | `true` when new visa is issued |
| Inv Item Cancelled | | Bool | `true` when invitation item is cancelled |
| WP Item Cancelled | | Bool | `true` when WP item is cancelled |
| Inv Item Changed | | Bool | `true` when invitation item is changed |
| WP Item Changed | | Bool | `true` when WP item is changed |
| Visa Cancelled | | Bool | `true` when current visa is cancelled |
| Visa Changed | | Bool | `true` when visa is changed (not cancelled) |

**Visibility (required for stakeholder seed):** include a column only when the scenario’s `Application Type` has the matching `Show*` flag in `Visa2026.Module` (`application-type-visibility.json`). Examples:

| Column | ApplicationType flag |
|--------|----------------------|
| Visa Number | `ShowCurrentVisa` |
| Address | `ShowCurrentAddressOfResidence` |
| Contract | `ShowCurrentEmployeeContract` |
| Medical Record | `ShowCurrentMedicalRecord` |
| Work Permit Item | `ShowCurrentWorkPermitItem` |
| Invitation Issued | `ShowInvitationItemIsIssued` |
| Travel Date / Check Point | `ShowRegistrations` (usually on **Registrations** sheet, not ApplicationItems) |

When `ShowApplicationItems=false` (most `App_Reg_*` types), do **not** use the `ApplicationItems` sheet — use `Registrations` or `BusinessTrips` instead.

Run `dotnet run --project Visa2026.DataImporter -- --validate-seed` or `--prune-seed` to check or fix yaml.

**Obsolete (omit from yaml):** `Filter` on Applications, `Company` on Applications/Persons, `ApplicationTypeFilter`, deprecated sheets per `docs/DEPRECATED.md`.

### BusinessTripPurpose

Seeded in the **Shared** scenario only. Simple name/description lookup.

| Column | Required | Type |
|--------|----------|------|
| Name | Yes | Scalar |
| Description | | Scalar |

### BusinessTripAddress

One record per destination. Referenced by `BusinessTrips` rows via `Full Address`.
Each `BusinessTrip` (per person) can share one `BusinessTripAddress` if going to the same location.

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Full Address | Yes | Scalar | Unique — used as lookup key |
| City | | LookupByName | City.Name |

### BusinessTrips

One row per person per application. Must come **after** `BusinessTripAddress`.

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Person | Yes | PersonLookupByName | |
| Application | Yes | LookupByName | Filter: `FullApplicationNumber` |
| Passport Number | | LookupByName | Filter: `PassportNumber` |
| Visa Number | | LookupByName | Filter: `VisaNumber` |
| Address | | LookupByName | Filter: `FullAddress` — CurrentAddressOfResidence |
| Position History | | LookupByName | Filter: `Position/Name` |
| Business Trip Address | | LookupByName | Filter: `FullAddress` — BusinessTripAddress |

### Registrations

One row per person per application. Travel fields (Travel Type, Date, Check Point,
Purpose of Travel) are read-only — the server creates the linked `TravelHistory` record
automatically. Provide them for documentation and consistency.

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Person | Yes | PersonLookupByName | |
| Application | Yes | LookupByName | Filter: `FullApplicationNumber` |
| Registration Date | | Scalar | ISO date |
| Passport Number | | LookupByName | Filter: `PassportNumber` |
| Visa Number | | LookupByName | Filter: `VisaNumber` |
| Address | | LookupByName | Filter: `FullAddress` — AddressOfResidence |
| Position History | | LookupByName | Filter: `Position/Name` |
| Travel Type | | Scalar | `ExternalArrival`, `ExternalDeparture`, `InternalArrival`, `InternalDeparture` |
| Travel Date | | Scalar | ISO date |
| Check Point | | LookupByName | CheckPoint.Name |
| Purpose of Travel | | LookupByName | PurposeOfTravel.Name |

### Invitations

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Invitation Number | Yes | StringValue | e.g. `INV-2026-001` — used as lookup key |
| Start Date | Yes | Scalar | |
| Application | | LookupByName | Filter: `FullApplicationNumber` |
| Validity Duration | | LookupByName | ValidityDuration.Name |

### InvitationItems

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Invitation Number | Yes | LookupByName | Filter: `InvitationNumber` |
| Person | Yes | PersonLookupByName | |
| Passport Number | Yes | LookupByName | Filter: `PassportNumber` |
| Is Used | | Bool | |
| Is Cancelled | | Bool | |
| Is Changed | | Bool | |

### WorkPermits

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Work Permit Number | Yes | StringValue | e.g. `WP-2026-001` — used as lookup key |
| Start Date | Yes | Scalar | |
| Application | | LookupByName | Filter: `FullApplicationNumber` |

### WorkPermitItems

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Work Permit Number | Yes | LookupByName | Filter: `WorkPermitNumber` — links to parent WorkPermit |
| Item Number | Yes | StringValue | e.g. `WPI-2026-001` — used as lookup key |
| Person | Yes | PersonLookupByName | |
| Passport Number | Yes | LookupByName | Filter: `PassportNumber` |
| Position History | Yes | LookupByName | Filter: `Position/Name` |
| Start Date | Yes | Scalar | |
| Expiration Date | Yes | Scalar | |

### Rejections / RejectionItems

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Rejection Number | Yes | StringValue | e.g. `REJ-2026-001` |
| Date | Yes | Scalar | |
| Application | | LookupByName | Filter: `FullApplicationNumber` |

RejectionItems link to Rejection by `RejectedDocNumber` and to Person by full name.

### ApplicationProgresses

One row per state transition per application. Sets `Application.CurrentState` server-side via `OnSaving`.
Must come **after** `ApplicationItems` — the Application must already exist.

| Column | Required | Type | Notes |
|--------|----------|------|-------|
| Application | Yes | LookupByName | Filter: `FullApplicationNumber` |
| State | Yes | LookupByName | Filter: `Code` — use ApplicationState.Code (e.g. `1_REVIEW_STARTED`) |
| Location | Yes | LookupByName | Filter: `Code` — use ApplicationLocation.Code (e.g. `AT_THE_MINISTERY_1`) |
| Date | Yes | Scalar | ISO date |
| Description | | Scalar | Human-readable note |

**Available State codes:** `IS_BEING_PREPARED`, `1_REVIEW_STARTED`, `2_REVIEW_STARTED`, `1_REVIEW_APPROVED`, `2_REVIEW_APPROVED`, `1_REVIEW_REJECTED`, `2_REVIEW_REJECTED`, `PROCESS_STARTED`, `PROCESS_CANCELLED`, `PROCESS_REJECTED`, `PROCESS_ISSUED`

**Available Location codes:** `AT_OFFICE`, `AT_THE_MINISTERY_1`, `AT_THE_MINISTERY_2`, `AT_MIGRATION_SERVICE`

---

## ApplicationType Reference

Consult `ApplicationTypeConfigurationSeed.Data.cs` (and optionally `LOOKUPS.md § ApplicationType`) for the full list. Key fields that drive
which sheets/columns are required in a scenario:

| Flag | Meaning | Sheets needed |
|------|---------|---------------|
| `ShowVisas` | New visa documents | `Visas` |
| `ShowApplicationItems` | Per-person items | `ApplicationItems` |
| `ShowRegistrations` | Registration records | `Registrations` |
| `ShowInvitations` | Invitation documents | `Invitations`, `InvitationItems` |
| `ShowWorkPermits` | Work permit documents | `WorkPermits`, `WorkPermitItems` |
| `ShowRejections` | Rejection records | `Rejections`, `RejectionItems` |
| `ShowBusinessTrips` | Business trip records | `BusinessTripAddress`, `BusinessTrips` |
| `ShowFromCity` | Origin city on Application | `From City` in Applications |
| `ShowToCity` | Destination city on Application | `To City` in Applications |
| `ShowMigrationService` | Migration service on Application | `Migration Service` in Applications |
| `ShowBusinessTripPlan` | Business trip dates/purpose | `Business Trip Start/End Date`, `Business Trip Purpose` |
| `ApplicationTypeFilter` | Filter category | `Filter` in Applications must match |

**Common ApplicationTypeFilter values:**

| Filter | Used for |
|--------|---------|
| `Invitation` | `App_Inv` |
| `InvitationAndWorkPermit` | `App_Inv_And_WP` |
| `Registration` | `App_Reg_*` |
| `Visa` | `App_Visa_Ext`, `App_Exit_Visa`, `App_Change_*` |
| `VisaAndWorkPermit` | `App_Visa_And_WP_Ext`, `App_Cancel_Visa_and_WP` |
| `BusinessTrip` | `App_Business_Trip_Departure`, `App_Business_Trip_Arrival` |
| `BorderZone` | `App_Border_Zone_Permission` |

---

## Shared Scenario

`order: 0`, `name: Shared` — **always runs first** and is a prerequisite for all others.

Seeds:
- `Company` — the single default company
- `ProjectContract` — the default project contract linked to Company
- `Lodging` — all company-managed lodging locations referenced by AddressOfResidence records
- `BusinessTripPurpose` — business trip purpose lookup values

**Anchor:** `Lodging` → `Name` → `Çalyk Enerji - Aşgabat (Bitarap)`

> The anchor is on Lodging (not Company) because Company may already exist from a
> prior partial import, while Lodging must always be created fresh alongside it.
> If the anchor were Company, a re-import would skip Shared and leave Lodging missing.

---

## Rules for Writing Scenarios

1. **Order 0 is reserved for Shared.** All other scenarios use order ≥ 1.

2. **Use `Application.FullApplicationNumber` as the anchor** for any scenario that
   creates an Application. This is the most stable and unique identifier.

3. **Never reuse Application Numbers.** The auto-generated `FullApplicationNumber`
   (e.g. `10/-007`) must be globally unique across all scenarios.

4. **Visa numbers must be globally unique.** Use sequential numbering
   `TM-2026-V-001`, `TM-2026-V-002`, etc. Family member visas use a separate series:
   `TM-2026-V-FM-001`. Exit visas continue the main series.

5. **Set `dependsOn` correctly** even though it is not enforced — it documents the
   intent and is displayed in console output. Set `order` to enforce the actual sequence.

6. **Visas must be listed before ApplicationItems** within the same scenario's `data:` block.
   The sheet processing order guarantees this as long as both use the correct sheet names.

7. **For Registration scenarios** (`App_Reg_*`), include both `Travel Type` and `Travel Date`
   even though they are server-managed — this keeps the YAML self-documenting.

8. **For family member persons** (`Is Employee: false`), include **`Project Contract`**
   (required). Do not seed legacy **`Company`** — removed from `Person` / `Application` schema.

9. **When changing passports**, the new Passport must be seeded in `Passports` before
   being referenced in `ApplicationItems` as `Passport Number` (current) or
   `Previous Passport` (old).

10. **Lookup values must match Module catalog JSON / ApplicationType seed exactly** (see table in [Overview](#overview)). Names are often in Turkmen. Do not invent values that are not in `Visa2026.Module/DatabaseUpdate/LookupCatalogs/`.

11. **`Personal Number` and `Authority` in Passports must use `StringValue` kind.**
    Long numeric strings (e.g. Turkish TC Kimlik No `23456789012`) are parsed as
    `decimal` by `DataParser.ParseScalar`, causing OData 400 errors.

---

## Numbering Conventions

Application display numbers come from **`ApplicationNumberingProfile`**
(`LookupCatalogs/tenant/application-numbering.json`). Current seed: format `{MONTH}/-{NUMBER}`,
padding `3`, empty prefix → **`{month}/-{number}`** (month from Application Date).

| Entity | Pattern | Example |
|--------|---------|---------|
| Application Number | `{seq:D3}` in yaml | `003`, `024` |
| Full Application Number | `{month}/-{Application Number}` | `4/-003` (April + `003`) |
| Visa (employee) | `TM-2026-V-{seq:D3}` | `TM-2026-V-010` |
| Visa (family member) | `TM-2026-V-FM-{seq:D3}` | `TM-2026-V-FM-007` |
| Invitation | `INV-2026-{seq:D3}` | `INV-2026-001` |
| Work Permit | `WP-2026-{seq:D3}` | `WP-2026-001` |
| Work Permit Item | `WPI-2026-{seq:D3}` | `WPI-2026-001` |
| Medical Record | `MED-YYYY-{seq:D3}` | `MED-2024-001` |

Always check the last used number in `data.yaml` before adding a new one.

---

## Common Patterns by ApplicationType Category

### Entry Scenarios (new employee arrives)

```yaml
data:
  Persons: [...]
  Passports: [...]
  MedicalRecords: [...]
  Education: [...]
  PositionHistory: [...]
  EmployeeContracts: [...]
  CompanyHead: [...]
  Representative: [...]
  AddressOfResidence: [...]
  Visas: [...]          # issued at arrival
  Applications: [...]
  Registrations: [...]  # App_Reg_Check_In type
  ApplicationItems: [...] # if ShowApplicationItems = 1
```

### Stay Scenarios (visa/permit change while in country)

```yaml
data:
  Visas: [...]           # new visa issued (MUST come before ApplicationItems)
  Applications: [...]
  ApplicationItems:
    - ...
      Visa Number: <old-visa>   # CurrentVisa = the visa being replaced
      Visa Issued: true
      Visa Cancelled: true      # or Visa Changed: true
```

### Business Trip Scenarios

```yaml
data:
  Applications:
    - ...
      Filter: BusinessTrip
      Application Type: App_Business_Trip_Departure   # or Arrival
      To City: <city>           # Departure: where they're going
      # From City: <city>       # Arrival: where they came from
      Business Trip Start Date: YYYY-MM-DD
      Business Trip End Date: YYYY-MM-DD
      Business Trip Purpose: Iş duşuşygy
      Migration Service: ...
  BusinessTripAddress:
    - City: <city>
      Full Address: <unique address string>
  BusinessTrips:
    - Person: ...
      Application: 4/-003   # FullApplicationNumber — month from Application Date
      Passport Number: ...
      Visa Number: ...
      Address: <current residence full address>
      Position History: <position name>
      Business Trip Address: <same full address as above>
```

### Family Member Scenarios

Family member `Person` records must always include:
```yaml
Company: Çalyk Enerji Sanaýi we Tijaret A.Ş. Türk kärhanasynyň Türkmenistandaky şahamçasy
Project Contract: GT-15
Is Employee: false
Relationship: <Relationship.Name>
Sponsoring Employee: <employee full name>
```

---

## Stakeholder demo scenarios

Active seed: **`seed/scenarios.index.yaml`** (Shared + core flows + `27-app-cancel-bz`). One scenario per application **workflow** for UI/report demos; not every `ApplicationType.Name` has a dedicated file yet.

Legacy state-notification / ministry duplicates: **`seed/scenarios/_archive/legacy-state-dashboard/`** (not in the index).

See **`seed/STAKEHOLDER_DEMO.md`**.

## Existing Scenarios (reference)

| Order | Name | ApplicationType | Filter | FullApplicationNumber |
|-------|------|----------------|--------|--------------|
| 0 | Shared | — | — | — |
| 1 | InvitationEmployee | App_Inv | Invitation | 4/-001 |
| 2 | InvitationAndWorkPermit | App_Inv_And_WP | InvitationAndWorkPermit | 4/-002 |
| 3 | CheckIn | App_Reg_Check_In | Registration | 4/-003 |
| 4 | CheckOut | App_Reg_Check_Out | Registration | 7/-004 |
| 5 | CheckInInternal | App_Reg_Check_In_Internal | Registration | 8/-005 |
| 6 | CheckOutInternal | App_Reg_Check_Out_Internal | Registration | 9/-006 |
| 7 | RegExtension | App_Reg_ext | Registration | 10/-007 |
| 8 | RegInfoChangePassport | App_Reg_Info_Change_Passport | Registration | 10/-008 |
| 9 | RegInfoChangeVisa | App_Reg_Info_Change_Visa | Registration | 10/-009 |
| 10 | RegInfoChangeAddress | App_Reg_Info_Change_Address | Registration | 11/-010 |
| 11 | CancelVisa | App_Cancel_Visa | Visa | 12/-011 |
| 12 | VisaExt | App_Visa_Ext | Visa | 1/-012 |
| 13 | VisaExtAccordingToWP | App_Visa_Ext_According_to_WP | Visa | 1/-013 |
| 14 | ChangeVisaCategory | App_Change_Visa_Category | Visa | 2/-014 |
| 15 | ChangePassport | App_Change_Passport | Visa | 2/-015 |
| 16 | VisaExtFM | App_Visa_Ext_FM | Visa | 1/-016 |
| 17 | InvitationFM | App_Inv_FM | Invitation | 4/-017 |
| 18 | VisaAndWPExt | App_Visa_And_WP_Ext | VisaAndWorkPermit | 12/-018 |
| 19 | AdditionalWPLocation | App_Additional_WP_Location | InvitationAndWorkPermit | 6/-019 |
| 20 | CancelVisaAndWP | App_Cancel_Visa_and_WP | VisaAndWorkPermit | 2/-020 |
| 21 | ChangeInvitation | App_Change_Inv | Invitation | 5/-021 |
| 22 | CancelInvAndWP | App_Cancel_Inv_WP | InvitationAndWorkPermit | 7/-022 |
| 23 | BorderZonePermission | App_Border_Zone_Permission | BorderZone | 8/-023 |
| 24 | BusinessTripDeparture | App_Business_Trip_Departure | BusinessTrip | 3/-024 |
| 25 | BusinessTripArrival | App_Business_Trip_Arrival | BusinessTrip | 3/-025 |
| 26 | ExitVisa | App_Exit_Visa | Visa | 4/-026 |
| 27 | VisaExtMinistry1 | App_Visa_Ext | Visa | 4/-027 |
| 28 | VisaExtMinistry2 | App_Visa_Ext | Visa | 4/-028 |
| 29 | VisaExtMinistry1Approved | App_Visa_Ext | Visa | 4/-029 |
| 30 | VisaExtMinistry2Approved | App_Visa_Ext | Visa | 4/-030 |
| 31 | VisaExtMinistry1Rejected | App_Visa_Ext | Visa | 4/-031 |
| 32 | VisaExtMinistry2Rejected | App_Visa_Ext | Visa | 4/-032 |
| 33 | VisaExtProcessStarted | App_Visa_Ext | Visa | 4/-033 |
| 34 | VisaExtProcessCancelled | App_Visa_Ext | Visa | 4/-034 |
| 35 | VisaExtProcessRejected | App_Visa_Ext | Visa | 4/-035 |
| 36 | VisaExtVisaIssued | App_Visa_Ext | Visa | 3/-036 |
| 37 | VisaExtVisaIssuedLink | — (Visa only) | — | TM-2026-V-023 (anchor) |
| 38 | VisaTransferInitiated | App_Change_Passport | Visa | 4/-038 |
| 39 | VisaTransferRejected | App_Change_Passport | Visa | 4/-039 |
| 40 | VisaTransferCompleted | App_Change_Passport | Visa | 3/-040 |
| 41 | VisaTransferCompletedLink | — (Visa only) | — | TM-2026-V-027 (anchor) |
| 42 | ExpiringSoonNotRequired | — (Visa only) | — | TM-2026-V-028 (anchor); ExtensionRequired=false |
| 43 | ExpiringSoonExtCancelled | App_Cancel_Visa_Ext | Visa | 4/-043 |
| 44 | CancelledOnCancellation | App_Cancel_Visa | Visa | 4/-044; TM-2026-V-030 |
| 45 | CancelledToBeCheckedOut | App_Cancel_Visa | Visa | 3/-045; TM-2026-V-031 |
| 46 | CancelledOnCheckOut | App_Cancel_Visa + App_Reg_Check_Out | Visa + Registration | 3/-046, 4/-047; TM-2026-V-032 |
| 47 | CancelledIsCheckedOut | App_Cancel_Visa + App_Reg_Check_Out | Visa + Registration | 2/-048, 3/-049; TM-2026-V-033 |
| 48 | ExpiredToBeCheckedOut | — (Visa only) | — | Person anchor `v06a.checkout.pending@visa2026.local`; TM-2026-V-034 |
| 49 | ExpiredOnCheckOutProcess | App_Reg_Check_Out | Registration | Anchor: `Registration.Application/FullApplicationNumber = 4/-051`; TM-2026-V-035 |
| 50 | ExpiredCheckedOut | App_Reg_Check_Out | Registration | Anchor: `Registration.Application/FullApplicationNumber = 4/-052`; TM-2026-V-036 |
| 51 | ExpiredMissedTimelyCheckout | — (Visa only) | — | Person anchor `v06d.checkout.missed@visa2026.local`; TM-2026-V-037 |
| 52 | ExpiredToBeCheckedOutLink | App_Reg_Check_In | Registration | 4/-053; links person `v06a.checkout.pending@visa2026.local` |
| 53 | ExpiredMissedTimelyCheckoutLink | App_Reg_Check_In | Registration | 4/-054; links person `v06d.checkout.missed@visa2026.local` |

**Next available:** Application number `055`, Visa number `TM-2026-V-038`
