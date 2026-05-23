# Lookup data seeding (Visa2026)

How reference / lookup tables are populated and kept in sync across dev, Docker, and customer deployments.

**Agent skill (step-by-step):** [`.cursor/skills/visa2026-lookup-data/SKILL.md`](../.cursor/skills/visa2026-lookup-data/SKILL.md)

---

## Goals

- **Version-controlled** lookup data in git (not Excel as the runtime source).
- **Automatic sync on deploy** when the app starts (XAF `ModuleUpdater`), without editing rows in the Blazor lookup UI.
- **Same product build** can ship shared ministry catalogs to every customer, while **company-specific** data (positions, ministries, etc.) lives in a separate tenant pack.
- **`ApplicationType`** visibility flags (`Show*`) and ministry **`SelectionCode`** values stay correct after every release.

---

## What is *not* used anymore

| Old approach | Status |
|--------------|--------|
| `Visa2026.DataImporter/lookup.xlsm` as runtime seed | **Removed** from import pipeline (`--seed-lookups-only` is deprecated). File may remain in repo only for one-off `--export-lookup-catalogs`. |
| `LookupSeeder` posting all sheets on empty DB | **Replaced** by Module updaters on app startup. |
| `LOOKUPS.md` as source of truth | **Reference only** (legacy `--dump-lookups` from xlsm). Edit JSON in git instead. |
| Blazor lookup screens for product fixes | **Avoid** for routine changes; use seed files + deploy. |

---

## Architecture (high level)

```mermaid
flowchart TB
  subgraph git [Git repository]
    globalJson["LookupCatalogs/*.json + manifest.json"]
    tenantJson["LookupCatalogs/tenant/*.json + tenant/manifest.json"]
    appTypeSeed["ApplicationTypeConfigurationSeed.Data.cs"]
    selectionSeed["ApplicationTypeSelectionCodeSeed.cs"]
  end

  subgraph deploy [App startup / deploy]
    catUpdater["LookupCatalogSyncUpdater"]
    selUpdater["ApplicationTypeSelectionCodeUpdater"]
    cfgUpdater["ApplicationTypeConfigurationUpdater"]
  end

  db[(SQL Server lookup tables)]

  globalJson --> catUpdater
  tenantJson --> catUpdater
  appTypeSeed --> cfgUpdater
  selectionSeed --> selUpdater
  selUpdater --> cfgUpdater
  catUpdater --> db
  cfgUpdater --> db
```

1. **Build** embeds `LookupCatalogs/**/*.json` into `Visa2026.Module`.
2. **App starts** → XAF runs database updaters (after schema is current).
3. **Updaters upsert** rows into SQL Server lookup tables.
4. **DataImporter** `data.yaml` scenarios assume lookups already exist (start app once before `--import-yaml-only`).

---

## Two seeding tracks

### 1. JSON catalogs (`LookupCatalogSyncUpdater`)

**Code:** `Visa2026.Module/DatabaseUpdate/LookupCatalogs/`  
**Updater:** `LookupCatalogSyncUpdater.cs`  
**Loader:** `LookupCatalogResourceLoader.cs` → `LookupCatalogEntitySync.cs`

Each catalog is a JSON file:

```json
{
  "rows": [
    { "Name": "…", "NameTm": "…", "Code": "…", "IsDefault": false }
  ]
}
```

`manifest.json` lists catalogs in **dependency order** (e.g. `Region` before `City`).  
`tenant/manifest.json` is **merged** into the main manifest at runtime.

### 2. ApplicationType configuration (C# seed)

**Not** in JSON catalogs (many `Show*` flags per row).

| Component | Role |
|-----------|------|
| `ApplicationTypeConfigurationSeed` + `.Data.cs` | One row per `ApplicationType.Name`; **all `Show*` flags overwritten** on deploy |
| `ApplicationTypeSelectionCodeUpdater` | Sets ministry `SelectionCode` from `ApplicationTypeSelectionCodeSeed` |
| Regenerate `.Data.cs` | `scripts/local/Generate-ApplicationTypeConfigurationSeed.ps1` (from `LOOKUPS.md` Application Type section) |

Registered in `Visa2026.Module/Module.cs` **after** `LookupCatalogSyncUpdater`.

---

## Global vs company-specific catalogs

### Global (shared — every deployment)

Embedded under `Visa2026.Module/DatabaseUpdate/LookupCatalogs/`.  
Listed in [`manifest.json`](../Visa2026.Module/DatabaseUpdate/LookupCatalogs/manifest.json).

| Entity | JSON file | Match key |
|--------|-----------|-----------|
| Country | `country.json` | Code, else Name |
| Gender | `gender.json` | Name |
| MaritalStatus | `marital-status.json` | Name |
| Urgency | `urgency.json` | Name |
| VisaCategory | `visa-category.json` | Name |
| VisaPeriod | `visa-period.json` | Name |
| VisaType | `visa-type.json` | Name |
| EducationLevel | `education-level.json` | Name |
| PurposeOfTravel | `purpose-of-travel.json` | Name |
| CheckPoint | `checkpoint.json` | Name |
| VisaIssuedPlace | `visa-issued-place.json` | Name |
| MigrationService | `migration-service.json` | Name |
| PassportType | `passport-type.json` | Name |
| Relationship | `relationship.json` | Name |
| ApplicationLocation | `application-location.json` | Name |
| BorderZoneLocation | `border-zone-location.json` | Name |
| ValidityDuration | `validity-duration.json` | Name |
| ApplicationState | `application-state.json` | Name |
| Region | `region.json` | Name |
| City | `city.json` | Name + Region |

### Company-specific / tenant (per deployment)

Embedded under `LookupCatalogs/tenant/` (and/or copied to disk — see below).  
Listed in [`tenant/manifest.json`](../Visa2026.Module/DatabaseUpdate/LookupCatalogs/tenant/manifest.json).

| Entity | JSON file | Match key |
|--------|-----------|-----------|
| Position | `tenant/position.json` | Code, else Name |
| Specialty | `tenant/specialty.json` | Name |
| EducationInstitution | `tenant/education-institution.json` | Name |
| Department | `tenant/department.json` | Name |
| Ministry | `tenant/ministry.json` | Name |
| Company | `tenant/company.json` | Name |
| ProjectContract | `tenant/project-contract.json` | Name + Company |

For a **new customer**, replace these tenant JSON files (or overlay on the server) with that organization’s data. The repo’s tenant files are the **default/reference** company for this product line.

### Not seeded from catalogs

| Entity | Reason |
|--------|--------|
| **ApplicationType** | C# seed + dedicated updaters |
| **MovementPermitLocation** | Intentionally excluded; maintain in app if needed |
| **ApplicationTypeFilter** | Deprecated; not seeded. Quick-code picker groups by `SelectionCode` hundreds (see `ApplicationTypeCodePickerHelper`). |

---

## Sync behavior on deploy

| Rule | Detail |
|------|--------|
| **When** | Each app startup that runs XAF `UpdateDatabaseAfterUpdateSchema` (deploy, local run, Docker recreate). |
| **Upsert** | Match existing row by manifest `matchKey`; otherwise create. |
| **Overwrite** | `syncMode: OverwriteScalars` — scalar properties in JSON **replace** DB values every deploy. |
| **Deletes** | **Never** — rows removed from JSON stay in the DB (avoids FK breakage). |
| **FK resolution** | JSON keys `Region`, `Company`, `Ministry` resolve by **Name** to existing rows. |

If updaters do not run on a DB that already reports “current”, set **`FORCE_XAF_DB_UPDATE=true`** once — see [`docs/ENVIRONMENTS.md`](ENVIRONMENTS.md).

---

## Runtime file locations

| Source | Path |
|--------|------|
| Embedded (ship with build) | `Visa2026.Module` assembly → `DatabaseUpdate/LookupCatalogs/**` |
| Optional disk overlay | `{AppBaseDirectory}/LookupCatalogs/tenant/*.json` and `tenant/manifest.json` |

Disk overlay is useful for **customer-specific** packs without rebuilding the image.

---

## Developer workflows

### Change a global lookup (e.g. Country name)

1. Edit `Visa2026.Module/DatabaseUpdate/LookupCatalogs/country.json`.
2. Build and deploy / restart app.
3. Verify in UI or SQL.

### Change company positions or ministry

1. Edit `LookupCatalogs/tenant/position.json` (or ministry, department, etc.).
2. Deploy / restart app.

### Change ApplicationType visibility

1. Edit `ApplicationTypeConfigurationSeed.Data.cs` (or regenerate — see below).
2. Deploy / restart app — **`Show*` always overwritten**.

### Bootstrap from legacy Excel (one-off)

If `lookup.xlsm` is still in `Visa2026.DataImporter`:

```powershell
dotnet run --project Visa2026.DataImporter -- --export-lookup-catalogs
```

- Writes **global** JSON + `manifest.json`.
- Writes **tenant** JSON + `tenant/manifest.json`.
- Skips **ApplicationType** and **MovementPermitLocation**.

Then commit the generated files.

Regenerate ApplicationType C# seed from `LOOKUPS.md`:

```powershell
.\scripts\local\Generate-ApplicationTypeConfigurationSeed.ps1
```

### Greenfield database

1. Start **Visa2026.Blazor.Server** (schema + updaters run).
2. Optionally set `FORCE_XAF_DB_UPDATE=true` once if needed.
3. Run scenario import:

```powershell
dotnet run --project Visa2026.DataImporter -- --import-yaml-only
```

### `data.yaml` imports

Lookup **names** in YAML (especially Turkmen strings for Country, etc.) must match seeded values exactly. Use [`LOOKUPS.md`](../LOOKUPS.md) as a human-readable snapshot, or inspect JSON.

---

## Key source files

| Path | Purpose |
|------|---------|
| `Visa2026.Module/DatabaseUpdate/LookupCatalogs/manifest.json` | Global catalog registry |
| `Visa2026.Module/DatabaseUpdate/LookupCatalogs/tenant/manifest.json` | Tenant catalog registry |
| `Visa2026.Module/DatabaseUpdate/LookupCatalogSyncUpdater.cs` | Deploy sync entry point |
| `Visa2026.Module/DatabaseUpdate/ApplicationTypeConfigurationUpdater.cs` | ApplicationType `Show*` sync |
| `Visa2026.Module/DatabaseUpdate/ApplicationTypeSelectionCodeUpdater.cs` | Selection codes |
| `Visa2026.DataImporter/LookupCatalogExporter.cs` | xlsm → JSON export |
| `Visa2026.Module/Module.cs` | Updater registration order |

---

## Updater registration order (excerpt)

In `Module.cs`, lookup-related updaters run near the end of the updater list:

1. `LookupCatalogSyncUpdater` — JSON catalogs (global + merged tenant manifest)
2. `ApplicationTypeSelectionCodeUpdater` — fills `SelectionCode` when empty
3. `ApplicationTypeConfigurationUpdater` — full ApplicationType config + **overwrite `Show*`**

---

## Related documentation

- [`docs/ENVIRONMENTS.md`](ENVIRONMENTS.md) — Docker, `FORCE_XAF_DB_UPDATE`, deploy
- [`Visa2026.DataImporter/IMPORTING.md`](../Visa2026.DataImporter/IMPORTING.md) — scenario import (update seeding section when convenient)
- [`AGENTS.md`](../AGENTS.md) — agent skills index
