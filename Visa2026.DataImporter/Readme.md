# Visa2026.DataImporter

A standalone .NET 8 console application for importing **business/scenario data** into **Visa2026** via the Blazor Server **Web API (OData/REST)**.

**Lookup/reference data** is **never** imported by this tool. Catalogs sync when **`Visa2026.Blazor.Server`** starts (`LookupCatalogSyncUpdater`). See **[`docs/LOOKUP_SEEDING.md`](../docs/LOOKUP_SEEDING.md)**.

`lookup.xlsm` is only for dev export (`--export-lookup-catalogs`, `--dump-lookups`).

---

## Default behavior

With **no arguments**, the importer loads **`data.yaml`** from the output directory (copied at build time):

```powershell
dotnet run --project Visa2026.DataImporter
```

Custom file (optional):

```powershell
dotnet run --project Visa2026.DataImporter -- C:\path\to\scenarios.yaml
# or: set DATA_YAML_PATH=C:\path\to\scenarios.yaml
```

**Prerequisite:** start the Blazor app once so lookup catalogs exist in the database.

---

## Other CLI options

| Flag | Description |
|------|-------------|
| `--full` | Legacy: try `data.yaml`, else `data.xlsx` / `employees.*` / demo fallback |
| `--dump-lookups` | Legacy: `lookup.xlsm` → markdown dump (no server) |
| `--export-lookup-catalogs` | `lookup.xlsm` → Module JSON (no server) |
| `--verbose` / `-v` | Log OData payloads |
| `--import-yaml-only [path]` | Legacy alias for default YAML import |

---

## Docker

```bash
docker compose -p visa2026-dev --env-file .env.dev -f docker-compose.dev.yml --profile tools run --rm db-updater
```

Custom YAML in container: `db-updater /app/custom-data.yaml`

Helper: `scripts/local/Seed-DataYaml.ps1`

---

**Lookup names in `data.yaml`** must match rows defined in **`Visa2026.Module`** (`DatabaseUpdate/LookupCatalogs/`, `ApplicationTypeConfigurationSeed.Data.cs`) — not seeded by this tool. See **`SCENARIO_GUIDE.md`** and **`docs/LOOKUP_SEEDING.md`**.
