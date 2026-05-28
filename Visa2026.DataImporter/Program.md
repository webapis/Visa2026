# Technical Reference: Program.cs (Orchestration Engine)

## 1. Overview

`Program.cs` is the entry point for the `Visa2026.DataImporter` console application. It orchestrates **business data** import over OData, handles authentication, waits for the server, and writes timestamped logs.

**Lookup catalogs** are **not** seeded here — they sync when `Visa2026.Blazor.Server` runs `LookupCatalogSyncUpdater` (see [`docs/LOOKUP_SEEDING.md`](../docs/LOOKUP_SEEDING.md)).

## 2. Startup and connectivity

1. **Wait-for-server** — `api.WaitForServerAsync` polls the app base URL (up to 300s).
2. **Authentication** — `POST /api/Authentication/Authenticate` as `Admin`; JWT on subsequent OData calls.

Dev-only paths (no server): `--dump-lookups`, `--export-lookup-catalogs`.

## 3. Import workflow (server required)

Phases run in order to avoid missing-parent OData errors.

### Phase 1 — Initialize importers

Instantiates entity importer classes (`PersonImporter`, `ApplicationImporter`, …).

### Phase 2 — Verify prerequisite lookups

`SafeQuery` checks that critical lookup tables have at least one row. **Aborts** if Country, Position, Department, ValidityDuration, Region, ApplicationType, or ApplicationTypeFilter are empty.

**Prerequisite:** start the Blazor app once on a fresh database so Module updaters populate catalogs.

### Phase 3 — Organization profile and project contract

Loads `CompanyProfile` singleton and default `ProjectContract`. Aborts if missing.

### Phase 4 — Business data import

Priority:

1. `data.yaml` → `ExcelImporter.ImportByScenariosFromYamlAsync`
2. `data.xlsx` → scenario or full Excel import
3. `employees.xlsx` / `employees.csv` → persons only
4. **Demo fallback** (full mode, no files) — programmatic John Smith + related records

Default run imports `data.yaml` in Phase 4 (after phases 1–3). Use `--full` for legacy fallbacks.

### Phases 5–7 — Programmatic demo

Only when **no** `data.yaml` / `data.xlsx` in full mode: creates sample Application, lodging, travel history, etc.

## 4. CLI

| Argument | Description |
|----------|-------------|
| *(no flags)* | Import `data.yaml` (default) |
| `[path]` or `DATA_YAML_PATH` | Custom YAML file |
| `--full` | Legacy multi-source orchestration |
| `--dump-lookups` | Legacy: `lookup.xlsm` → markdown dump (no server) |
| `--export-lookup-catalogs` | `lookup.xlsm` → Module JSON (no server) |
| `--verbose` / `-v` | Log OData payloads |

Removed: `--seed-lookups-only`, `--sync-positions`, `--delete-missing`.

## 5. Logging

Nested `Log` class: console + `import_YYYYMMDD_HHmmss.log` under the output directory.

---

*Project: Visa2026.DataImporter*
