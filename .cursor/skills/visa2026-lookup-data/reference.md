# Lookup data — reference

## Key files

| Path | Role |
|------|------|
| `Visa2026.Module/BusinessObjects/LookupBusinessObjects.cs` | `ApplicationType`, `LookupBase`, `GlobalLookupCatalogBase`, `Show*` |
| `Visa2026.Module/BusinessObjects/GlobalLookupCatalogKind.cs` | Catalog id for string tables |
| `Visa2026.Module/Localization/LookupLocalization.cs` | Resolves `LocalizedDisplayName` from embedded JSON |
| `Visa2026.Module/Localization/LocalizedLookupTypes.cs` | BO types that use Layer B in UI |
| `Visa2026.Module/Localization/*.json` | Culture strings per catalog (`LocalizationKey`) |
| `Visa2026.Module/Model/LookupLocalizationModelUpdater.cs` | Sets `LookupProperty` = `LocalizedDisplayName` on references |
| `Visa2026.Module/DatabaseUpdate/LookupLocalizationKeyUpdater.cs` | Backfills `LocalizationKey` on deploy |
| `Visa2026.Module/DatabaseUpdate/LookupCatalogs/LookupCatalogEntitySync.cs` | Applies `LocalizationKey` from seed JSON |
| `scripts/local/Generate-CountryLookupStrings.ps1` | Regenerate `CountryLookupStrings.json` |
| `Visa2026.Module/BusinessObjects/Application.cs` | Application-level `[Appearance]` → `ApplicationType.Show*` |
| `Visa2026.Module/BusinessObjects/ApplicationItem.cs` | Item-level `[Appearance]` → `Application.ApplicationType.Show*` |
| `Visa2026.Module/DatabaseUpdate/ApplicationTypeConfigurationSeed.cs` | Seed API (`Rows`, `TryGetByName`) |
| `Visa2026.Module/DatabaseUpdate/ApplicationTypeConfigurationSeed.Data.cs` | **Generated** row data (edit via script) |
| `Visa2026.Module/DatabaseUpdate/ApplicationTypeConfigurationRow.cs` | Row DTO |
| `Visa2026.Module/DatabaseUpdate/ApplicationTypeConfigurationApplier.cs` | Applies row → `ApplicationType` entity |
| `Visa2026.Module/DatabaseUpdate/ApplicationTypeConfigurationUpdater.cs` | Deploy sync; **overwrite all `Show*`** |
| `scripts/local/Generate-ApplicationTypeConfigurationSeed.ps1` | Regenerate `.Data.cs` from `ApplicationTypeConfigurationCatalog.json` |
| `LookupCatalogs/tenant/position.json` | **Company-specific** positions (not in global `manifest.json`) |
| `LookupCatalogs/tenant/manifest.json` | Merged at startup with global manifest |
| `Visa2026.Module/DatabaseUpdate/ApplicationTypeSelectionCodeSeed.cs` | Ministry `SelectionCode` by `Name` |
| `Visa2026.Module/DatabaseUpdate/ApplicationTypeSelectionCodeUpdater.cs` | Fills empty `SelectionCode` on deploy |
| `Visa2026.DataImporter/lookup.xlsm` | Dev export source only (`--export-lookup-catalogs`) |
| `Visa2026.DataImporter/Excelmappings.cs` | `LookupSheets` column mapping for export |
| `Visa2026.DataImporter/LookupCatalogExporter.cs` | `lookup.xlsm` → Module JSON |
| `Visa2026.DataImporter/LookupDumper.cs` | Writes the legacy lookup markdown dump |
| `lookup-dump.md` | Generated snapshot (solution root) |
| `Visa2026.Module/BusinessObjects/ApplicationType.md` | Property glossary (may lag code) |

## Commands (repo root)

```powershell
# Build
dotnet build Visa2026.slnx -c Debug

# Legacy: regenerate lookup-dump.md from lookup.xlsm (no server)
dotnet run --project Visa2026.DataImporter -- --dump-lookups

# Export lookup.xlsm → Module/LookupCatalogs/*.json (no server)
dotnet run --project Visa2026.DataImporter -- --export-lookup-catalogs

# Scenario/business data (app must have synced lookups first)
dotnet run --project Visa2026.DataImporter
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

From `Excelmappings.cs` — ApplicationType sheet includes `Name`, `NameTm`, `Code`, `PdfForm_Code`, `Category`, `LifecycleStage`, `DurationInDays`, and all mapped `Show*` headers. Seeder only sends mapped columns; internal Excel columns (`_RowNum`, `GCRecord`, …) appear in the legacy markdown dump but are not posted.

## Stable keys

| Key | Use |
|-----|-----|
| `ApplicationType.Name` | Code, reports, PDF templates, seed rows (`App_Inv`, …) |
| `ApplicationType.SelectionCode` | Quick code on Application detail (`101`, …) |
| `LookupBase.LocalizationKey` | Stable key into `Localization/*.json` (global catalogs) |
| `LookupBase.Name` / `NameTm` | Report/PDF and `data.yaml` matching; **tenant** lookup UI uses `NameTm` |
| `LookupBase.LocalizedDisplayName` | UI display for global catalogs + `ApplicationType` (culture-aware) |
