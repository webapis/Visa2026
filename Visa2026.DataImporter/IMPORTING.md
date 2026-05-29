# Data Importer — Reference Guide

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Running the Importer](#running-the-importer)
3. [Import Files Overview](#import-files-overview)
4. [Phase-by-Phase Execution](#phase-by-phase-execution)
5. [Scenario-Based Importing](#scenario-based-importing)
6. [Fallback Import Modes](#fallback-import-modes)
7. [Lookup catalogs (Module)](#lookup-catalogs-module)
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

### Lookup catalogs (before import)

Reference data (Country, Position, ApplicationType, etc.) must already exist in SQL. That happens when **`Visa2026.Blazor.Server`** runs database update (`LookupCatalogSyncUpdater` + embedded JSON under `Visa2026.Module/DatabaseUpdate/LookupCatalogs/`). See **[`docs/LOOKUP_SEEDING.md`](../docs/LOOKUP_SEEDING.md)**.

The importer **verifies** critical lookups in Phase 2 and **aborts** if they are missing.

### Optional import files

| File | Purpose |
|------|---------|
| `data.yaml` | Scenario-based import (**recommended**) |
| `data.xlsx` | Scenario or full sheet import (full mode when `data.yaml` is absent) |
| `employees.xlsx` | Person sheet only (fallback) |
| `employees.csv` | CSV person import (fallback) |
| `lookup.xlsm` | **Not** used at import time — dev export to JSON catalogs only |

If none of the business import files are present (and not in `--import-yaml-only` with a valid YAML), full mode may create a single demo person programmatically.

---

## Running the Importer

```bash
dotnet Visa2026.DataImporter.dll
dotnet Visa2026.DataImporter.dll C:\path\to\prod-data.yaml
dotnet Visa2026.DataImporter.dll --full
dotnet Visa2026.DataImporter.dll --verbose
```

Dev tools (no server):

```bash
dotnet Visa2026.DataImporter.dll --dump-lookups
dotnet Visa2026.DataImporter.dll --export-lookup-catalogs
```

### Mode flags

- **(default)**: import `data.yaml` scenarios (no flags required).
- Optional path: first argument, `DATA_YAML_PATH` env, or legacy `--import-yaml-only [path]`.
- `--full`: legacy orchestration (`data.yaml` → `data.xlsx` → employees → demo).
- `--verbose` / `-v`: log POST/PATCH payloads.

### Recommended pattern

1. Start **Visa2026.Blazor.Server** (or `docker compose up app`) so lookup catalogs sync into the database.
2. Run the importer with no arguments (or `docker compose … run --rm db-updater`).

---

## Import Files Overview

```
Visa2026.DataImporter/
├── data.yaml         # Scenario-based import (preferred)
├── data.xlsx         # Excel scenario / full import fallback
├── employees.xlsx    # Person-only fallback
├── employees.csv     # CSV person fallback
└── lookup.xlsm       # Dev only: --export-lookup-catalogs, --dump-lookups
```

In `--full` mode, the importer checks files in this order: `data.yaml` → `data.xlsx` → `employees.xlsx` → `employees.csv`.

---

## Phase-by-Phase Execution

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
| ApplicationTypeFilter | Warns if missing; ensure app database update has run |

Non-critical lookups (Gender, MaritalStatus, VisaCategory, etc.) are fetched here but do not cause abort if empty.

### Phase 3 — Organization profile and project contract

- Loads **`CompanyProfile`** singleton (`$top=1`); aborts if missing (run Blazor app once so `OrganizationSingletonSeedUpdater` seeds it)
- Loads the project contract where `IsDefault = true`; falls back to the first contract
- Aborts if project contract is not found after fallback

### Phase 4 — Onboard Employees

Checks for import files in this priority order:

1. **`data.xlsx`** — full scenario-based import (see [Scenario-Based Importing](#scenario-based-importing))
2. **`employees.xlsx`** — imports the "Persons" sheet only
3. **`employees.csv`** — CSV import with positional columns: `FirstName, LastName, Email, DateOfBirth, BirthPlace, ForeignAddress`
4. **No file** — creates a demo person (John Smith) with supporting records programmatically

Excel **`Company`** sheet upserts **`CompanyProfile`**. **`ApplicationNumbering`** sheet upserts **`ApplicationNumberingProfile`**. **`CompanyHead`** / **`Representative`** sheets upsert **`AuthorizedSignatory`** / **`AuthorizedRepresentative`** singletons.

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

### Refresh a scenario after editing `data.yaml`

When you change yaml for a scenario that was already imported, a normal re-run **skips** it (anchor still matches). Two options:

#### Recommended: clear + re-import (`--clear-scenario`)

Deletes rows described in that scenario’s yaml (children first, then application scope, then persons/passports, etc.), then runs a fresh POST import. No duplicate Application Items, no PATCH edge cases.

```powershell
dotnet run --project Visa2026.DataImporter -- --clear-scenario InvitationEmployee
```

Only the named scenario runs. **Warning:** if the scenario deletes **Person** rows (by Email), later scenarios that reuse the same person (e.g. `InvitationFM` → John Doe) must be re-imported too. Prefer clear only on the scenario you are editing, or re-run dependents.

#### Optional: sync (`--sync-scenario` / `--sync`)

PATCH existing rows by natural keys — best for one or two field tweaks. More fragile for child collections (see sync docs in `SCENARIO_GUIDE.md`). Mark scenarios with `sync: true` when using `--sync`.

**Not touched:** lookup catalogs and org singletons (Module startup). `VisaImporter.cs` is unrelated.

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

## Lookup catalogs (Module)

Lookup tables are synced on **app startup** from `Visa2026.Module/DatabaseUpdate/LookupCatalogs/*.json` (and tenant overlays). Authoritative doc: **[`docs/LOOKUP_SEEDING.md`](../docs/LOOKUP_SEEDING.md)**.

`lookup.xlsm` in this project is retained for **developers** only:

- `--export-lookup-catalogs` — regenerate JSON in the Module from Excel
- `--dump-lookups` — legacy: dumps lookup.xlsm to markdown (file name chosen by the tool)

Sheet names below match `ExcelMappings.LookupSheets` used by the export tool (not runtime OData POST).

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
| Company | **CompanyProfile** | Tenant singleton (seeded from `tenant/company-profile.json` on app DB update; Excel sheet blocked) |
| ApplicationNumbering | **ApplicationNumberingProfile** | Tenant singleton (`tenant/application-numbering.json`; Excel sheet blocked) |
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
| ApplicationItems | Application (by FullApplicationNumber), Person, Passport, Visa, PositionHistory, EmployeeContract, WorkPermitItem, InvitationItem, AddressOfResidence, MedicalRecord, Education; registration/travel columns when `ShowRegistrations` |

### Documents

| Sheet | Key Lookups | Notes |
|-------|------------|-------|
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

None for registration travel — check-in/out fields live on **ApplicationItem** (`Registration Date`, `Travel Type`, `Travel Date`, `Check Point`, `Purpose of Travel`, …). The legacy **Registrations** sheet is obsolete.

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
