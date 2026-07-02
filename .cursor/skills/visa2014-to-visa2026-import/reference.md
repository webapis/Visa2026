# VISA2014 → Visa2026 import — reference

## Paths

| Path | Role |
|------|------|
| `Visa2026.DataImporter/legacy/visa2014/order.yaml` | **Canonical dependency order** — discovery + OData import (`entities[]`) |
| `docs/VISA2014_MIGRATION/IMPORT_PLAN_AND_STRATEGY.md` | **Import plan — approve before implementation** |
| `docs/VISA2014_MIGRATION/EXCEL_PREVIEW_EXPORT.md` | **Consolidated legacy → Excel** preview before import |
| `Visa2026.DataImporter/legacy/visa2014/preview-export/` | Output folder (`*.xlsx` gitignored) |
| `Visa2026.DataImporter/legacy/visa2014/import-strategy.yaml` | Strategy gate (`status: approved`) |
| `docs/VISA2014_MIGRATION/discovery/` | One dossier YAML per target BO |
| `docs/VISA2014_MIGRATION/discovery/_template.yaml` | Copy when adding a BO |
| `docs/VISA2014_MIGRATION/entity-inventory.yaml` | Summary index (sync from dossiers) |
| `docs/VISA2014_MIGRATION/schema-snapshot.md` | Bootstrap only — global table index |
| `docs/VISA2014_MIGRATION/table-mappings.yaml` | Layer 1 — legacy table → OData entity |
| `docs/VISA2014_MIGRATION/lookup-translations.yaml` | Layer 3 — lookup value semantic map |
| `docs/VISA2014_MIGRATION/discovery-queue.yaml` | Pointer only — use `order.yaml` |
| `docs/VISA2014_MIGRATION/property-gap-registry.yaml` | Cross-BO gap + dedupe index |
| `Visa2026.DataImporter/legacy/visa2014/field-maps/_template.yaml` | Layer 2 field map schema |
| `Visa2026.DataImporter/legacy/visa2014/field-maps/` | Per-BO mapping (drafted in discovery) |
| `Visa2026.DataImporter/legacy/visa2014/id-map/` | Runtime GUID map (gitignored) |
| `C:\Users\webap\Documents\GitHub\VISA2014` | Legacy repo — **supplementary** (MCP filesystem only) |
| `.cursor/skills/visa2014-to-visa2026-import/MATURITY.md` | **Experience loop** — read learnings before; append after **every import attempt** (success or failure) |
| `.cursor/skills/visa2014-to-visa2026-import/learnings.md` | Append-only session log |
| `.cursor/skills/visa2014-to-visa2026-import/import-practices.md` | **Import best practices** (Phase 3+; strategy + `importConfirmed`) |
| `docs/VISA2014_MIGRATION.md` | Canonical migration overview |
| `scripts/visa2014-migration/README.md` | **Migration scripts index** — **search before creating new scripts** |

## Migration scripts — reuse first

**Do not add a new `.ps1` until you confirm nothing existing covers the task.**

| Priority | Mechanism | When |
|----------|-----------|------|
| 1 | `dotnet run … --import-visa2014 --entity <BO>` | Single entity; no cleanup/rebuild |
| 2 | `import/Run-HeadlessChain.ps1` | Full chain or `-StartAt` resume |
| 3 | `import/OnPrem-Staging.ps1` | Staging / on-prem ordered waves |
| 4 | `import/Invoke-TenantCatalogGeneration.ps1` | Tenant catalog JSON before Application |
| 5 | `import/<Entity>.ps1` or `reimport/<Entity>.ps1` | Documented entity workflow (import-only vs partial reimport) |
| 6 | Extend existing script (`-MaxRows`, `-DryRun`, `-TargetConnection`) | Same workflow, different scope |
| 7 | **New script** | Only if 1–6 cannot express the workflow — update README + learnings |

Shared repo root: `_lib/Get-RepoRoot.ps1`. Dot-source **after** `param()`, not inside it.

## Orchestration scripts (`scripts/visa2014-migration/`)

**Order rule:** Full and partial reimport both follow [`order.yaml`](../../../Visa2026.DataImporter/legacy/visa2014/order.yaml) `dependsOn`. Orchestration scripts run entities in that order; partial reimport is one BO at a time but only when parents are already valid — re-run downstream BOs in order after a parent partial reimport.

| When | Script | Notes |
|------|--------|-------|
| Restore **VISA2015** on dev PC | `setup/Restore-LegacyDatabase.ps1` | Wraps `migration-scripts/Restore-BackupToLocalSql.ps1` |
| Tenant JSON before **Application** | `import/Invoke-TenantCatalogGeneration.ps1` | Same as `--generate-visa2014-tenant-catalogs`; steps in `order.yaml` |
| Full on-prem staging waves | `import/OnPrem-Staging.ps1` | Ordered `--import-visa2014` per `order.yaml`; in-process for Application/ApplicationItem |
| Local dev full chain | `import/Run-HeadlessChain.ps1` | In-process all entities; `-StartAt` to resume |
| ApplicationItem only | `import/ApplicationItems.ps1` | In-process; needs parent id-maps |
| Partial reimport Applications (dev only) | `reimport/Applications.ps1` | SQL cleanup + import — **not** end-to-end migration |
| Partial reimport ApplicationItems (dev only) | `reimport/ApplicationItems.ps1` | SQL cleanup → rebuild parent id-maps → in-process import → corrections. **Procedure:** [import-practices.md § Partial reimport](./import-practices.md#partial-reimport-dev-implementation-only) |
| Çalik tenant catalogs | `catalogs/generate/*.ps1`, `catalogs/deploy/*.ps1` | See README in that folder |
| Patch ApprovalLegProfile | `patch/Application-ApprovalLegProfile.ps1` | After Application import |

Per-entity import without a dedicated script: `dotnet run --project Visa2026.DataImporter -- --import-visa2014 --entity <BO> …`

## Import confirmation gate

Per entity — **before** `--import-visa2014` code or OData POST:

| Step | Field / artifact | Requirement |
|------|------------------|-------------|
| Phase 0 | `import-strategy.yaml` `status: approved` | [IMPORT_PLAN_AND_STRATEGY.md](../../../docs/VISA2014_MIGRATION/IMPORT_PLAN_AND_STRATEGY.md) signed off |
| Phase 1 | `discoveryStatus: complete` | Dossier + layers 1–3 YAML synced |
| Phase 1c | Excel preview exported + reviewed | [EXCEL_PREVIEW_EXPORT.md](../../../docs/VISA2014_MIGRATION/EXCEL_PREVIEW_EXPORT.md) |
| Phase 1b | `importConfirmed: true` | Human sign-off after Excel + mapping review |
| Phase 3+ | `importStatus: pilot` \| `imported` | After successful OData run |

Agents must not set `importConfirmed: true` unless the user explicitly confirms. See [VISA2014_MIGRATION.md § Import confirmation gate](../../../docs/VISA2014_MIGRATION.md).

## MCP servers (`.cursor/mcp.json`)

| Server | Instance / DB | Use |
|--------|---------------|-----|
| `visa2014-sql-local` | `localhost\SQLEXPRESS` → **`VISA2015`** | **Source of truth** — schema, counts, samples, DISTINCT lookup values |
| `visa2014-readonly-files` | VISA2014 repo | **Supplementary** — BO/EF hints; never overrides SQL |
| `visa2026-sql-local` | `localhost\SQLEXPRESS` → **`Visa2026DbDev`** | Target validation |

Optional env: **`VISA2014_REPO_PATH`**. **Docker dev:** use `127.0.0.1:1433` in `mcp.json` instead of SQLEXPRESS.

### Excel preview export

```powershell
# Planned — after Phase 2 shell implemented
dotnet run --project Visa2026.DataImporter -- `
  --export-visa2014-preview `
  --entity Person `
  --output Visa2026.DataImporter/legacy/visa2014/preview-export/Person-preview.xlsx
```

See [EXCEL_PREVIEW_EXPORT.md](../../../docs/VISA2014_MIGRATION/EXCEL_PREVIEW_EXPORT.md).

---

## Restore VISA2015 (optional)

Skip if **`VISA2015`** already exists on your SQL instance (SSMS). **Docker** alternative:

```powershell
docker compose -p visa2026-dev --env-file .env.dev -f docker-compose.dev.yml up -d

.\scripts\visa2014-migration\setup\Restore-LegacyDatabase.ps1
# or
.\migration-scripts\Restore-BackupToLocalSql.ps1 -BackupFile .\visa2015-prod.bak -DatabaseName VISA2015
```

Requires **`SA_PASSWORD`** in `.env.dev` for Docker restore only. `*.bak` is gitignored.

## Discovery SQL

### Bootstrap (once — global table index)

Run via **`visa2014-sql-local`**. Write results to `schema-snapshot.md` only.

#### Confirm database

```sql
SELECT DB_NAME() AS current_db;
```

#### Table list with row counts (approximate)

```sql
SELECT
    s.name AS schema_name,
    t.name AS table_name,
    SUM(p.rows) AS row_count
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
INNER JOIN sys.partitions p ON t.object_id = p.object_id
WHERE p.index_id IN (0, 1)
GROUP BY s.name, t.name
ORDER BY row_count DESC, schema_name, table_name;
```

### Per-BO (atomic — one target entity per session)

Replace `@TableName` with the legacy table from the dossier. Record results in `discovery/{Entity}.yaml`, not `schema-snapshot.md`.

#### Columns

```sql
DECLARE @TableName sysname = N'YourLegacyTable';

SELECT
    c.column_id,
    c.name AS column_name,
    ty.name AS type_name,
    c.max_length,
    c.precision,
    c.scale,
    c.is_nullable
FROM sys.columns c
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.' + @TableName)
ORDER BY c.column_id;
```

#### Primary key

```sql
DECLARE @TableName sysname = N'YourLegacyTable';

SELECT kc.name AS pk_name, c.name AS column_name, ic.key_ordinal
FROM sys.key_constraints kc
INNER JOIN sys.index_columns ic
    ON kc.parent_object_id = ic.object_id AND kc.unique_index_id = ic.index_id
INNER JOIN sys.columns c
    ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE kc.type = N'PK'
  AND kc.parent_object_id = OBJECT_ID(N'dbo.' + @TableName)
ORDER BY ic.key_ordinal;
```

#### Foreign keys (incoming + outgoing)

```sql
DECLARE @TableName sysname = N'YourLegacyTable';

SELECT
    fk.name AS fk_name,
    OBJECT_NAME(fk.parent_object_id) AS parent_table,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS parent_column,
    OBJECT_NAME(fk.referenced_object_id) AS referenced_table,
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS referenced_column
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE OBJECT_NAME(fk.parent_object_id) = @TableName
   OR OBJECT_NAME(fk.referenced_object_id) = @TableName
ORDER BY parent_table, fk_name;
```

#### Row count

```sql
SELECT COUNT(*) AS row_count FROM dbo.YourLegacyTable;
```

#### Soft-delete probe (XPO / common patterns)

```sql
DECLARE @TableName sysname = N'YourLegacyTable';

SELECT c.name
FROM sys.columns c
WHERE c.object_id = OBJECT_ID(N'dbo.' + @TableName)
  AND c.name IN (N'GCRecord', N'IsDeleted', N'Deleted', N'OptimisticLockField');
```

#### Lookup distinct values (dossier step 6)

```sql
SELECT TOP 50 YourLookupColumn, COUNT(*) AS cnt
FROM dbo.YourLegacyTable
WHERE YourLookupColumn IS NOT NULL
GROUP BY YourLookupColumn
ORDER BY cnt DESC;
```

## Legacy BO locate (filesystem MCP)

Typical VISA2014 paths to search (read-only):

- `**/BusinessObjects/**/*.cs`
- `**/Module/**/*Persistent*.cs`
- EF: `**/DbContext*.cs`, `**/*Mapping*.cs`

Record `legacy.boClass`, `legacy.boPath`, and mapped `legacy.tables[]` in the dossier.

## Three-layer mapping (summary)

| Layer | File | Key fields |
|-------|------|------------|
| 1 Tables | `table-mappings.yaml` | `legacy.table`, `target.odataEntity` |
| 2 Columns | `field-maps/{Entity}.yaml` | `fields[].source`, `fields[].target`, `lookupCatalog` |
| 3 Values | `lookup-translations.yaml` | `values[].legacy` → `values[].target` (same meaning) |

## Lookup value sampling (layer 3)

```sql
-- Inline on transactional table
SELECT TOP 100 YourLookupColumn, COUNT(*) AS cnt
FROM dbo.YourTransactionalTable
WHERE YourLookupColumn IS NOT NULL
GROUP BY YourLookupColumn ORDER BY cnt DESC;

-- FK to legacy lookup
SELECT TOP 100 l.Title, COUNT(*) AS cnt
FROM dbo.Employee e
INNER JOIN dbo.Department l ON e.DepartmentId = l.Oid
GROUP BY l.Title ORDER BY cnt DESC;
```

## Pick next BO (`order.yaml`)

Walk `entities[]` in **array order**. Select the first row where:

1. `discoveryStatus` is `not_started` or `in_progress` (only one `in_progress` globally)
2. Every name in `dependsOn` has `discoveryStatus` in `complete`, `blocked`, `skip`

When adding a BO, insert at the dependency-correct position — same file drives import.

## Duplicate detection (legacy — per BO)

Replace table and key columns after discovery. Record result count in dossier `mapping.duplicateGroups`.

```sql
DECLARE @Table sysname = N'YourLegacyTable';

-- Single-column business key
SELECT
    PassportNumber AS business_key,
    COUNT(*) AS row_count
FROM dbo.YourLegacyTable
WHERE PassportNumber IS NOT NULL
GROUP BY PassportNumber
HAVING COUNT(*) > 1
ORDER BY row_count DESC;

-- Composite key: add columns to SELECT / GROUP BY
```

### List rows in one duplicate group (spot-check merge)

```sql
SELECT *
FROM dbo.YourLegacyTable
WHERE PassportNumber = N'SAMPLE_KEY'
ORDER BY ModifiedOn DESC;   -- or tieBreakColumn from field-map
```

## Field map keys (summary)

| Section | Purpose |
|---------|---------|
| `propertyGaps.legacyOnly` | Legacy column with no target — usually `drop` |
| `propertyGaps.targetOnly` | Target property without legacy — `default` + `missingBehavior` |
| `propertyGaps.mismatches` | Semantic/type mismatch — transform documented |
| `fields[].missingBehavior` | Per-column null/empty handling on import |
| `fields[].default` / `defaultValue` | Constant or derive when legacy missing |
| `deduplication` | Business key groups → import one canonical row |

Full template: `Visa2026.DataImporter/legacy/visa2014/field-maps/_template.yaml`

## Target-side OData (Visa2026)

- Base URL (local): `https://localhost:5001/api/odata/`
- Importer auth: `Admin` / empty password (dev) — see `Visa2026.DataImporter/Program.cs`
- Entity exposure: `Visa2026.Blazor.Server/WebApi/WebApiServiceExtensions.cs`

## Field-map YAML shape

See [VISA2014_MIGRATION.md](../../../docs/VISA2014_MIGRATION.md) § Mapping protocol and stub `field-maps/Person.yaml`.

## Related skills

- [visa2026-dataimporter](../visa2026-dataimporter/SKILL.md) — target seed/scenario import
- [visa2026-lookup-data](../visa2026-lookup-data/SKILL.md) — ApplicationType / catalog keys
- [mirror-droplet-db-to-local](../mirror-droplet-db-to-local/SKILL.md) — `.bak` restore pattern (Visa2026 prod)
