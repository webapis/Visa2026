# Lookup data — reference

## Key files

| Path | Role |
|------|------|
| `Visa2026.Module/BusinessObjects/LookupBusinessObjects.cs` | `ApplicationType` entity and `Show*` properties |
| `Visa2026.Module/BusinessObjects/Application.cs` | Application-level `[Appearance]` → `ApplicationType.Show*` |
| `Visa2026.Module/BusinessObjects/ApplicationItem.cs` | Item-level `[Appearance]` → `Application.ApplicationType.Show*` |
| `Visa2026.Module/DatabaseUpdate/ApplicationTypeConfigurationSeed.cs` | Seed API (`Rows`, `TryGetByName`) |
| `Visa2026.Module/DatabaseUpdate/ApplicationTypeConfigurationSeed.Data.cs` | **Generated** row data (edit via script) |
| `Visa2026.Module/DatabaseUpdate/ApplicationTypeConfigurationRow.cs` | Row DTO |
| `Visa2026.Module/DatabaseUpdate/ApplicationTypeConfigurationApplier.cs` | Applies row → `ApplicationType` entity |
| `Visa2026.Module/DatabaseUpdate/ApplicationTypeConfigurationUpdater.cs` | Deploy sync; **overwrite all `Show*`** |
| `scripts/local/Generate-ApplicationTypeConfigurationSeed.ps1` | Regenerate `.Data.cs` from `LOOKUPS.md` |
| `LookupCatalogs/tenant/position.json` | **Company-specific** positions (not in global `manifest.json`) |
| `LookupCatalogs/tenant/manifest.json` | Merged at startup with global manifest |
| `Visa2026.Module/DatabaseUpdate/ApplicationTypeSelectionCodeSeed.cs` | Ministry `SelectionCode` by `Name` |
| `Visa2026.Module/DatabaseUpdate/ApplicationTypeSelectionCodeUpdater.cs` | Fills empty `SelectionCode` on deploy |
| `Visa2026.DataImporter/lookup.xlsm` | Bulk catalogs; greenfield ApplicationType bootstrap |
| `Visa2026.DataImporter/Excelmappings.cs` | `LookupSheets` column mapping |
| `Visa2026.DataImporter/LookupSeeder.cs` | POST seed; **skips entity if any row exists** |
| `Visa2026.DataImporter/LookupDumper.cs` | Writes `LOOKUPS.md` |
| `LOOKUPS.md` | Generated snapshot (solution root) |
| `Visa2026.Module/BusinessObjects/ApplicationType.md` | Property glossary (may lag code) |

## Commands (repo root)

```powershell
# Build
dotnet build Visa2026.slnx -c Debug

# Regenerate LOOKUPS.md from lookup.xlsm (no server)
dotnet run --project Visa2026.DataImporter -- --dump-lookups

# Seed all lookup sheets into empty DB (app running, OData)
dotnet run --project Visa2026.DataImporter -- --seed-lookups-only

# Full import pipeline
dotnet run --project Visa2026.DataImporter -- --full

# Position upsert only (example of sync pattern)
dotnet run --project Visa2026.DataImporter -- --sync-positions
```

## Find Appearance ↔ Show* usage

```powershell
# Application-level
rg "ApplicationType\.Show" Visa2026.Module/BusinessObjects/Application.cs

# ApplicationItem-level
rg "Application\.ApplicationType\.Show" Visa2026.Module/BusinessObjects/ApplicationItem.cs

# All Show* properties on ApplicationType
rg "public virtual bool Show" Visa2026.Module/BusinessObjects/LookupBusinessObjects.cs
```

## Deploy / updater did not run

See `docs/ENVIRONMENTS.md` — set `FORCE_XAF_DB_UPDATE=true` once, restart app, verify, remove flag.

Docker lifecycle: `.cursor/skills/visa2026-lifecycle-docker/reference.md`.

## ApplicationType sheet columns (importer)

From `Excelmappings.cs` — ApplicationType sheet includes `Name`, `NameTm`, `Code`, `PdfForm_Code`, `Category`, `LifecycleStage`, `DurationInDays`, and all mapped `Show*` headers. Seeder only sends mapped columns; internal Excel columns (`_RowNum`, `GCRecord`, …) appear in `LOOKUPS.md` but are not posted.

## Stable keys

| Key | Use |
|-----|-----|
| `ApplicationType.Name` | Code, reports, PDF templates, seed rows (`App_Inv`, …) |
| `ApplicationType.SelectionCode` | Quick code on Application detail (`101`, …) |
| `LookupBase.Name` / `NameTm` | Display; OData lookups in `data.yaml` use **exact** Turkmen names for catalogs |
