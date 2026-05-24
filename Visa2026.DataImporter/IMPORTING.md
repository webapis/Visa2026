# Data Importer — Reference Guide

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Running the Importer](#running-the-importer)
3. [Import Files Overview](#import-files-overview)
4. [Phase-by-Phase Execution](#phase-by-phase-execution)
5. [Scenario-Based Importing](#scenario-based-importing)
6. [Fallback Import Modes](#fallback-import-modes)
7. [Lookup Seeding (lookup.xlsm)](#lookup-seeding-lookupxlsm)
8. [Data Sheets (data.xlsx)](#data-sheets-dataxlsx)
9. [Column Mapping Types](#column-mapping-types)
10. [Post-Seed Hooks](#post-seed-hooks)
11. [Error Handling and Logs](#error-handling-and-logs)

---

## Prerequisites

### Runtime

- .NET 8.0 SDK or Runtime
- `Visa2026.Blazor.Server` must be running and reachable at `https://localhost:5001` before the importer starts
- The importer waits up to **300 seconds** for the server, polling every 2 seconds

### Authentication

Default credentials are hardcoded in `Program.cs`:

```
Username: Admin
Password: (empty)
```

SSL certificate validation is disabled for `localhost` (dev environment only).

### Required Files

| File | Required | Purpose |
|------|----------|---------|
| `lookup.xlsm` | Yes | Seeds all reference/lookup tables |
| `data.yaml` | No | Scenario-based import (recommended for optional post-seed business data) |
| `data.xlsx` | No | Scenario-based full data import (used in full mode when `data.yaml` is absent) |
| `employees.xlsx` | No | Simple person-only import (fallback) |
| `employees.csv` | No | CSV-based person import (fallback) |

If none of `data.yaml`, `data.xlsx`, `employees.xlsx`, or `employees.csv` are present, the importer creates a single demo person programmatically.

---

## Running the Importer

```bash
dotnet Visa2026.DataImporter.dll --seed-lookups-only
dotnet Visa2026.DataImporter.dll --import-yaml-only
dotnet Visa2026.DataImporter.dll --import-yaml-only C:\path\to\prod-data.yaml
dotnet Visa2026.DataImporter.dll --full
dotnet Visa2026.DataImporter.dll --full --verbose
```

### Mode flags

- `--seed-lookups-only`: import only `lookup.xlsm`, then exit.
- `--import-yaml-only [path]`: import only YAML scenarios; lookup seeding is skipped by design.
- `--full`: full orchestration mode.
- `--verbose` / `-v`: logs all POST and PATCH payloads to the console before sending.

Only one of `--seed-lookups-only`, `--import-yaml-only`, `--full` can be used in the same run.

### Production-safe pattern

1. Run baseline lookup seeding first (`--seed-lookups-only`)
2. Run business data import separately only when needed (`--import-yaml-only`)

---

## Import Files Overview

```
Visa2026.DataImporter/
├── lookup.xlsm       # Reference/lookup tables — always seeded first
├── data.yaml         # Scenario-based import file (preferred optional data source)
├── data.xlsx         # Full scenario-based import fallback in full mode
├── employees.xlsx    # Person-only simple import fallback
└── employees.csv     # CSV person import fallback
```

In `--full` mode, the importer checks files in this order: `data.yaml` → `data.xlsx` → `employees.xlsx` → `employees.csv`.

---

## Phase-by-Phase Execution

### Phase 0 — Seed Lookup Tables

- Reads `lookup.xlsm` and posts all reference data to the API
- Each sheet is checked for existing records before seeding; **if the table already has data, it is skipped entirely** (idempotent)
- Rows starting with `Start ` or `End ` are skipped (sentinel/placeholder rows)
- Individual row failures are logged but do not stop the remaining rows
- See [Lookup Seeding](#lookup-seeding-lookupxlsm) for the full list of sheets

### Phase 1 — Initialize Importers

- Instantiates all 24+ entity importer classes
- No API calls; purely in-memory setup

### Phase 2 — Verify Prerequisite Lookups

Queries the API to confirm critical lookup tables were seeded. If any of the following are missing, the importer aborts:

| Lookup | Notes |
|--------|-------|
| Country | |
| Position | |
| Department | |
| ValidityDuration | |
| Region | |
| ApplicationType | Warns if missing; suggests seeding via Blazor UI |
| ApplicationTypeFilter | Warns if missing; suggests seeding via `lookup.xlsm` |

Non-critical lookups (Gender, MaritalStatus, VisaCategory, etc.) are fetched here but do not cause abort if empty.

### Phase 3 — Organization profile and project contract

- Loads **`CompanyProfile`** singleton (`$top=1`); aborts if missing (run Blazor app once so `OrganizationSingletonSeedUpdater` seeds it)
- Optionally loads legacy **`Company`** row for `Lodging` / `ProjectContract.Company` FK (tenant `company.json` catalog)
- Loads the project contract where `IsDefault = true`; falls back to the first contract
- Aborts if project contract is not found after fallback

### Phase 4 — Onboard Employees

Checks for import files in this priority order:

1. **`data.xlsx`** — full scenario-based import (see [Scenario-Based Importing](#scenario-based-importing))
2. **`employees.xlsx`** — imports the "Persons" sheet only
3. **`employees.csv`** — CSV import with positional columns: `FirstName, LastName, Email, DateOfBirth, BirthPlace, ForeignAddress`
4. **No file** — creates a demo person (John Smith) with supporting records programmatically

Excel **`Company`** sheet upserts **`CompanyProfile`** (+ patches **`SystemSettings`** app numbering). **`CompanyHead`** / **`Representative`** sheets upsert **`AuthorizedSignatory`** / **`AuthorizedRepresentative`** singletons.

### Phase 5 — Create Application _(skipped if `data.yaml` or `data.xlsx` is present)_

Creates:
1. `Application` — linked to ApplicationType and ApplicationTypeFilter (no org FKs)
2. `ApplicationItem` — links Application to Person, Passport, PositionHistory, EmployeeContract
3. `ApplicationProgress` — initial state and location entry

### Phase 7 — Miscellaneous Records _(skipped if `data.yaml` or `data.xlsx` is present)_

Creates:
- City (Ashgabat / Aşgabat, code ASB, linked to Region)
- Lodging and AddressOfResidence (linked to Person and Lodging)
- TravelHistory as `ExternalArrival` (linked to Person and CheckPoint)
- BusinessTrip (Purpose: "Client Meeting", ~30 days from run date)

---

## Scenario-Based Importing

Triggered when YAML scenarios are imported via `--import-yaml-only` (or when `data.yaml` is present in `--full` mode), and for Excel scenarios when `data.xlsx` is present and contains a **Scenarios** sheet.

### How It Works

The import runs in **two passes**:

**Pass 1 — Read all data:**
- Every mapped sheet in `data.xlsx` is read
- Each data row is grouped by its `Scenario` column value
- Rows with no `Scenario` value are assigned to the `"Shared"` group

**Pass 2 — Execute scenarios in order:**
- Scenarios are sorted by `Order` (ascending)
- For each scenario:
  1. **Idempotency check** — queries the API for the anchor entity; if found, the scenario is skipped
  2. If not yet seeded, all sheets are processed for that scenario's rows in dependency order
  3. Optional post-seed hooks run after each successful row

### Scenarios Sheet Columns

| Column | Description |
|--------|-------------|
| `Order` | Numeric execution sequence (lower = runs first) |
| `Name` | Unique scenario name; used to group rows in data sheets |
| `Description` | Human-readable description (display only) |
| `DependsOn` | Name of another scenario this one depends on — **informational only**; enforced by setting `Order` correctly |
| `AnchorEntity` | OData entity name used for idempotency check (e.g., `Person`) |
| `AnchorKey` | OData property to filter on (e.g., `Email`) |
| `AnchorValue` | Expected value — if a matching record exists, the scenario is skipped |

### DependsOn vs Order

`DependsOn` is a documentation aid only — it is displayed in the console output but is **never enforced in code**. The actual execution order is determined solely by the `Order` column. If scenario B depends on scenario A, set A's `Order` lower than B's.

### Idempotency

Each scenario's anchor (entity + key + value) is checked before seeding. If the anchor record already exists in the database, the entire scenario is skipped. This makes re-runs safe — already-seeded scenarios are left untouched.

If the anchor check itself fails (network or parse error), a warning is logged and the scenario proceeds anyway.

### Scenario Column in Data Sheets

Each data sheet in `data.xlsx` must include a `Scenario` column. The value in that column assigns the row to a specific scenario. Rows with an empty `Scenario` column are assigned to `"Shared"` and are seeded once, before named scenarios run.

---

## Fallback Import Modes

### employees.xlsx

- Imports only the "Persons" sheet
- Uses the same column mapping as the Persons sheet in `data.xlsx`
- Persons are linked to ProjectContract loaded in Phase 3 (no `Person.Company` FK)

### employees.csv

- Custom CSV parser with quoted-field support
- `hasHeader = true` expected
- Column order (0-indexed, positional):

| Index | Field |
|-------|-------|
| 0 | FirstName |
| 1 | LastName |
| 2 | Email |
| 3 | DateOfBirth |
| 4 | BirthPlace |
| 5 | ForeignAddress |

- Parse errors are written to `csv_import_errors.log` in the output directory
- Successfully parsed persons are bulk-imported; a re-query by email retrieves the created record

### Programmatic (no file)

Creates a single demo person with:
- Name: John Smith
- Email: unique (4-char GUID suffix)
- DOB: 1985-01-01, birthplace: London

Also creates: Passport, Education, PositionHistory, EmployeeContract, MedicalRecord.

---

## Lookup Seeding (lookup.xlsm)

> **Current product path:** lookup tables are synced on **app startup** from version-controlled JSON and C# seeds in `Visa2026.Module`. See **[`docs/LOOKUP_SEEDING.md`](../docs/LOOKUP_SEEDING.md)**. The sections below describe the **legacy** `lookup.xlsm` importer path (`--seed-lookups-only` is deprecated).

Sheets are seeded in dependency order. Each sheet maps to an OData entity.

### Independent tables (no dependencies)

| Sheet | OData Entity |
|-------|-------------|
| Country | Country |
| Gender | Gender |
| MaritalStatus | MaritalStatus |
| Urgency | Urgency |
| VisaCategory | VisaCategory |
| VisaPeriod | VisaPeriod |
| VisaType | VisaType |
| EducationLevel | EducationLevel |
| Purpose of Travel | PurposeOfTravel |
| Checkpoint | CheckPoint |
| VisaIssuedPlace | VisaIssuedPlace |
| MigrationService | MigrationService |
| PassportType | PassportType |
| Specialty | Specialty |
| EducInstitution | EducationInstitution |
| Relationships | Relationship |
| ApplicationLocation | ApplicationLocation |
| Department | Department |
| Position | Position |
| Validation Duration | ValidityDuration |
| ApplicationStates | ApplicationState |
| Region | Region |

### Organization and project contract

| Sheet | OData Entity | Notes |
|-------|-------------|-------|
| Company | **CompanyProfile** | Singleton upsert; `AppNumberPrefix` / padding → **SystemSettings** |
| ProjectContract | ProjectContract | Lookup: legacy **Company** by Name (tenant catalog) |
| ApplicationTypeFilter | ApplicationTypeFilter | ValueMap: `0`→Employee, `1`→FamilyMember, `2`→Both |
| ApplicationType | ApplicationType | ValueMap for Category and LifecycleStage enums; references ApplicationTypeFilter |

### Depends on Region

| Sheet | OData Entity | Notes |
|-------|-------------|-------|
| City | City | Lookup: Region by Name |

---

## Data Sheets (data.xlsx)

Sheets are processed in dependency order. Each sheet maps rows to OData entity POSTs.

### Organization structure

| Sheet | OData entity | Notes |
|-------|-------------|-------|
| Company | CompanyProfile | Singleton upsert |
| CompanyHead | AuthorizedSignatory | Singleton upsert (`Full Name`, passport fields) |
| Representative | AuthorizedRepresentative | Singleton upsert (`Full Name`, `Phone`, passport) |
| ProjectContract | ProjectContract | Legacy Company lookup (optional) |
| Persons | ProjectContract | No Company column |

### Person records

| Sheet | Key Lookups |
|-------|------------|
| Passports | Person (by FullName), PassportType, IssuedCountry |
| TravelHistory | Person (by FullName), CheckPoint, PurposeOfTravel |
| MedicalRecords | Person (by FullName), ValidityDuration |
| Education | Person (by FullName), EducationLevel, Institution, Country, Specialty |
| PositionHistory | Person (by FullName), Position, Department |
| EmployeeContracts | Person, PositionHistory (by Position/Name), ValidityDuration |
| Lodging | Company |
| AddressOfResidence | Person, Region, City, Lodging |

### Applications and items

| Sheet | Key Lookups |
|-------|------------|
| Applications | ProjectContract, ApplicationType, ApplicationTypeFilter, VisaCategory, MigrationService, Urgency, VisaPeriod, VisaType |
| ApplicationItems | Application (by FullApplicationNumber), Person, Passport, Visa, PositionHistory, EmployeeContract, WorkPermitItem, InvitationItem, AddressOfResidence, Registration, MedicalRecord, Education |

### Documents

| Sheet | Key Lookups | Notes |
|-------|------------|-------|
| Registrations | Person, Application (by FullApplicationNumber) | PostSeedHook creates/patches TravelHistory |
| Invitations | Application (by FullApplicationNumber), ValidityDuration | |
| InvitationItems | Invitation (by InvitationNumber), Person, Passport | |
| WorkPermits | Application (by FullApplicationNumber) | |
| WorkPermitItems | WorkPermit (by WorkPermitNumber), Person, Passport, PositionHistory | |
| Visas | VisaType, VisaCategory, VisaIssuedPlace, Passport, ApplicationItem, InvitationItem | |
| Rejections | Application (by FullApplicationNumber) | |
| RejectionItems | Rejection (by RejectedDocNumber), Person | |

---

## Column Mapping Types

| Kind | Behavior |
|------|----------|
| `Scalar` | Parsed automatically: tries DateTime → int → decimal → bool → string |
| `StringValue` | Always treated as a plain string; prevents phone numbers or codes being parsed as numbers |
| `Bool` | `"0"`, `"false"`, `"no"` → `false`; anything else → `true` |
| `LookupByName` | Resolves the cell value to an OData record by name; sends `{ ID: guid }` in payload; supports navigation paths like `Position/Name` |
| `PersonLookupByName` | Resolves a full name to a Person; tries `FullName` property first, then splits into `FirstName`/`LastName` |

### Date Formats Accepted

Dates are parsed in this order:

1. ISO 8601 / Roundtrip (`o`)
2. `dd.MM.yyyy`
3. `dd.MM.yyyy HH:mm:ss`
4. `d.M.yyyy`
5. `d.M.yyyy H:mm:ss`

---

## Post-Seed Hooks

### Registrations — TravelHistory Hook

After each Registration row is created, the importer optionally creates or patches a linked `TravelHistory` (MovementRecord).

**Trigger columns** (all optional; hook skipped if all four are empty):

| Column | Notes |
|--------|-------|
| Travel Type | Maps to `@odata.type`: ExternalArrival, ExternalDeparture, InternalArrival, InternalDeparture |
| Travel Date | Parsed using standard date formats |
| Check Point | Lookup by Name |
| Purpose of Travel | Lookup by Name |

**Logic:**
1. GET the newly created Registration to check if a MovementRecord was auto-created
2. If no MovementRecord exists → POST new TravelHistory, then PATCH the Registration to link it
3. If MovementRecord exists → PATCH the existing TravelHistory with travel fields

---

## Error Handling and Logs

### Log Files

| File | Contents |
|------|---------|
| `import_YYYYMMDD_HHmmss.log` | Full import log with timestamps and severity levels |
| `csv_import_errors.log` | Row-level parse errors from `employees.csv` |

Both files are written to the application output directory.

### Log Levels

| Level | Color | Meaning |
|-------|-------|---------|
| `===` | Cyan | Phase header |
| `---` | White | Step marker |
| ` OK` | Green | Success |
| `INF` | Gray | Informational |
| `WRN` | Yellow | Warning (non-fatal) |
| `ERR` | Red | Error |

### Row-Level Behavior

- A failed row is logged and skipped; remaining rows in the batch continue
- A missing **required** column value skips the row entirely
- A missing **optional** lookup omits that field from the payload (the row still posts)
- At the end of each sheet: `Done. ✓ X seeded, ⚠ Y skipped, ✗ Z failed`

### Idempotency Summary

| Scope | Mechanism |
|-------|-----------|
| Lookup tables (Phase 0) | Skip entire sheet if table already has records |
| Scenarios | Skip scenario if anchor entity/key/value match found via OData |
| Data rows | Skip row if a `Required` column is empty |
| Sentinel rows | Skip rows starting with `Start ` or `End ` |
