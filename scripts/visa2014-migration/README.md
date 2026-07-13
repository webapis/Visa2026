# VISA2014 → Visa2026 migration scripts

All PowerShell/SQL helpers for **legacy prod migration** live here (not under `scripts/local/`).

**Canonical BO order:** `Visa2026.DataImporter/legacy/visa2014/order.yaml`  
**Agent skill:** `.cursor/skills/visa2014-to-visa2026-import/SKILL.md`

---

## Reuse scripts first (mandatory)

**Do not create a new script when an existing one already does the job.** Extra files increase maintenance cost and drift from `order.yaml`.

### Before adding any script

1. **Search this README** and [reference.md orchestration table](../../.cursor/skills/visa2014-to-visa2026-import/reference.md).
2. **Prefer DataImporter CLI** for a single entity when no orchestration is needed:
   ```powershell
   dotnet run --project Visa2026.DataImporter -- --import-visa2014 --entity <BO> --legacy-source calik-energi --inprocess
   ```
3. **Prefer parameters** on an existing script (`-StartAt`, `-MaxRows`, `-DryRun`, `-TargetConnection`, `-LegacySource`) over a copy-paste wrapper.
4. **Prefer orchestrators** (`import/Run-HeadlessChain.ps1`, `import/OnPrem-Staging.ps1`, `import/Invoke-TenantCatalogGeneration.ps1`) over one-off entity scripts.
5. **Extract shared logic** into `_lib/` or C# in `Visa2026.DataImporter` — not a second PowerShell file that duplicates the same `dotnet run` block.

### Create a new script only when

| Criterion | Example |
|-----------|---------|
| No existing script or CLI covers the workflow | First partial-reimport for a BO (cleanup SQL + id-map rebuild + import) |
| Extending the existing script would mix unrelated concerns | `reimport/ApplicationItems.ps1` adds SQL delete + rebuild; `import/ApplicationItems.ps1` is import-only |
| The task is a stable, repeated procedure worth documenting by name | `catalogs/generate/EducationLookup-CalikEnergi.ps1`, `Compare-LegacyMigratedCounts.ps1` |

When you **must** add a script: place it in the correct folder below, dot-source `_lib/Get-RepoRoot.ps1` **after** `param()`, add a one-line entry to this README, and append [learnings.md](../../.cursor/skills/visa2014-to-visa2026-import/learnings.md).

### Preference order (highest first)

1. `dotnet run … Visa2026.DataImporter -- --import-visa2014` / `--correct-*` / `--rebuild-visa2014-id-maps`
2. `import/Run-HeadlessChain.ps1` or `import/OnPrem-Staging.ps1` (full or resumed chain)
3. Entity helper in `import/` or `reimport/` (if listed below)
4. Extend an existing script with a switch or parameter
5. **New** `.ps1` only after steps 1–4 fail

---

## Layout

| Folder | Purpose |
|--------|---------|
| `setup/` | Restore **VISA2015** from `.bak` on a dev workstation |
| `catalogs/generate/` | VISA2015 SQL or preview → tenant `*.calik-energi.json` |
| `catalogs/deploy/` | Copy calik tenant JSON into embedded catalogs + manifest bump |
| `catalogs/` | Shared helpers (`Import-PreviewCatalogRows.ps1`) |
| `import/` | OData / in-process entity import and **end-to-end** orchestration |
| `reimport/` | **Partial reimport** (dev only): delete one BO scope + re-run that entity |
| `cleanup/` | SQL for partial reimport scope deletes |
| `patch/` | One-off headless corrections after import |
| `_lib/` | `Get-RepoRoot.ps1` |

---

## Dependency order (full and partial)

Both **end-to-end** and **partial reimport** follow `order.yaml` `dependsOn`:

- Import/reimport a BO only after its parents exist in the target DB and id-maps resolve.
- Partial reimport = one BO per run, not arbitrary order.
- If you partial-reimport a **parent**, re-run **downstream** BOs in `order.yaml` sequence.

---

## End-to-end vs partial reimport

| Mode | When | Scripts |
|------|------|---------|
| **End-to-end migration** | Staging UAT, prod cutover, fresh DB | `import/OnPrem-Sync.ps1`, `import/OnPrem-Staging.ps1`, `import/Run-HeadlessChain.ps1`, or first load per `order.yaml` |
| **Partial reimport** | Local dev: mapping/transform fix on one BO | `reimport/*.ps1` + `cleanup/*.sql` |

Do **not** use `reimport/` for staging or production cutover.

---

## Script index

### Setup

| Task | Script |
|------|--------|
| Restore VISA2015 backup | `setup/Restore-LegacyDatabase.ps1` |

### Import / orchestration

| Task | Script | Notes |
|------|--------|-------|
| Full local chain (in-process) | `import/Run-HeadlessChain.ps1` | `-StartAt <Entity>` to resume |
| Document copies only (photos + scans) | `import/DocumentCopies.ps1` | `-StartAt WorkPermitDocument` to resume mid-wave |
| On-prem Import (Demo / Staging / Prod) | `import/OnPrem-Sync.ps1` | Import-only (`--import-visa2014`); `-Profile Staging\|Production\|Demo`; `-IncludeFileWaves` for scans |
| **Import host on `.25`** (no SDK) | `import/Install-OnPremSyncHost.ps1` | Publish + `C:\visa2026-sync*`; then `Run-OnPremSyncOnServer.ps1` |
| On-prem staging (wrapper) | `import/OnPrem-Staging.ps1` | Delegates to `OnPrem-Sync.ps1 -Profile Staging` |
| Tenant catalog generation | `import/Invoke-TenantCatalogGeneration.ps1` | Wraps `--generate-visa2014-tenant-catalogs` |
| **Lookup preflight** (before full Import) | `import/Preflight-LookupAudit.ps1` | `--preflight-visa2014-lookups`; also auto-run by `OnPrem-Sync.ps1` unless `-SkipLookupPreflight` |
| ApplicationItem import only | `import/ApplicationItems.ps1` | Parents + id-maps must exist |
| WorkPermit + WorkPermitItem import | `import/WorkPermits.ps1` | After Person/Passport/EPH id-maps |
| Single entity (any BO) | *(no script)* | `dotnet run … --import-visa2014 --entity <BO>` |

### Partial reimport (dev only)

| Task | Script | Cleanup SQL |
|------|--------|-------------|
| **Full application domain reload** (ordered chain) | See [import-practices § Full application domain](../../.cursor/skills/visa2014-to-visa2026-import/import-practices.md) — `Applications.ps1` → `WorkPermits.ps1` → `Invitations.ps1` → `ApplicationItems.ps1` → `ApplicationProgress.ps1` | `ImportedApplications.sql` (+ purge stale WorkPermit/Invitation rows/id-maps if needed) |
| Application headers only | `reimport/Applications.ps1` | `cleanup/ImportedApplications.sql` |
| ApplicationItem lines | `reimport/ApplicationItems.ps1` | `cleanup/ImportedApplicationItems.sql` |
| ApplicationProgress (synthetic steps) | `reimport/ApplicationProgress.ps1` | `cleanup/ImportedApplicationProgress.sql` |
| WorkPermit + WorkPermitItem | `import/WorkPermits.ps1` | *(manual purge if Application scope was wiped)* |
| Invitation + InvitationItem | `import/Invitations.ps1` | *(manual purge if Application scope was wiped)* |
| Ministry legs missing on via-ministry apps | `patch/ApplicationProgress-MinistryLegs.ps1` | (in-place delete/regen per app) |
| **Ministrlik empty** (snapshots missing; progress already OK) | `patch/Application-ApprovalLegSnapshots.ps1` (EF) or `patch/Application-ApprovalLegSnapshots-Sql.ps1` (SQL — use when prod schema behind Module) | (in-place `ApprovalLegSnapshots` from profile — **no** progress delete) |
| Progress steps out of workflow order (date vs leg sequence) | `patch/ApplicationProgress-Order.ps1` | (in-place `ProgressOrder` recompute) |
| Person-domain children (after Person reimport) | `reimport/PersonDomainDownstream.ps1` | `cleanup/ImportedPersonDomainChildren.sql` |
| Visa + WorkPermitItem `IsCancelled` backfill | `reimport/VisaWorkPermitCancellation.ps1` | `cleanup/ImportedVisaWorkPermitCancellationBackfill.sql` (+ `ApplicationItems.ps1` relink) |
| InvitationItem `IsCancelled` backfill | `reimport/InvitationCancellation.ps1` | `cleanup/ImportedInvitationItemCancellationBackfill.sql` (+ `ApplicationItems.ps1` relink) |
| Duplicate ApplicationItem per Person (on-prem prod data fix) | `Repair-DuplicateApplicationItems.ps1` | `cleanup/DuplicateApplicationItemsByPerson.sql` (preview `@Apply=0`, then `-Apply`) |
| Duplicate AddressOfResidence per Person+site (on-prem prod data fix) | `Repair-DuplicateAddressOfResidence.ps1` | `cleanup/DuplicateAddressOfResidenceByPersonSite.sql` (preview `@Apply=0`, then `-Apply`) |
| Duplicate ApplicationProgress per App+Order (on-prem prod data fix) | `Repair-DuplicateApplicationProgress.ps1` | `cleanup/DuplicateApplicationProgressByAppOrder.sql` (preview `@Apply=0`, then `-Apply`) |
| Duplicate Lodging by FullAddress (on-prem prod data fix) | `Repair-DuplicateLodgings.ps1` | `cleanup/DuplicateLodgingsByFullAddress.sql` (preview `@Apply=0`, then `-Apply`) |
| Lodging null CityID backfill from AOR plurality (on-prem prod) | `Backfill-LodgingCityFromAor.ps1` | `cleanup/BackfillLodgingCityFromAor.sql` (preview then `-Apply`) |
| Duplicate employee Persons (bootstrap + supplement twins, on-prem prod) | `Repair-DuplicateEmployees.ps1` | `cleanup/DuplicateEmployeesByIdentity.sql` (default `-Scope BootstrapSupplement`; preview then `-Apply -UpdateIdMap -PersonIdMapPath …`) |
| Duplicate City by NameTm (null vs set Region, on-prem prod) | `Repair-DuplicateCities.ps1` | `cleanup/DuplicateCitiesByNameTm.sql` (default `-Scope NullVsSetRegion`; preview `@Apply=0`, then `-Apply`) |

### Reconcile / live Import status / reimport history

| Task | Script |
|------|--------|
| Live **wave** table from `sync-run-status.json` | `Watch-OnPremSyncRun.ps1` (`-Profile Demo\|Production -ViaSsh -ClearScreen`) |
| Live **import** table (waves + Δ + target DB counts) | `Watch-OnPremImportLive.ps1` (`-Profile Demo -ViaSsh -ClearScreen`) |
| **Archive** current Import run (status + DbCounts) | `Archive-OnPremImportRun.ps1` (`-Profile Demo`) — also auto-run at end of `OnPrem-Sync.ps1` |
| **Compare** two reimport archives (anomaly Δ) | `Compare-OnPremImportRuns.ps1` (`-Profile Demo` [-Left] [-Right] [-FailOnAnomaly]) |
| Local / on-prem legacy vs migrated row counts | `Compare-LegacyMigratedCounts.ps1` (`-ShowIdMap` for id-map column) |

**Reimport history dashboard:** `<SyncHostRoot>\history\index.html` (immutable `history\runs\<RunId>\`: `run-status.json`, `db-counts.json`, `meta.json`).  
**In-app (Administrators):** Operations → **Import reimport history** (reads the same folder via `ImportHistory:RootPath` in appsettings; app-pool identity needs read ACL on `history\`).

Procedure: [import-practices.md § Partial reimport](../../.cursor/skills/visa2014-to-visa2026-import/import-practices.md).

### Catalogs (Çalik tenant)

| Task | Generate | Deploy |
|------|----------|--------|
| Project contract | `catalogs/generate/ProjectContract-CalikEnergi.ps1` | `catalogs/deploy/ProjectContract-CalikEnergi.ps1` |
| Approval leg profile | `catalogs/generate/ApprovalLegProfile.ps1` | — |
| Education institution / specialty | `catalogs/generate/EducationLookup-CalikEnergi.ps1` | `catalogs/deploy/EducationLookup-CalikEnergi.ps1` |
| Position / department | `catalogs/generate/PositionDepartmentLookup-CalikEnergi.ps1` | `catalogs/deploy/PositionDepartmentLookup-CalikEnergi.ps1` |
| Lodging | `catalogs/generate/Lodging-CalikEnergi.ps1` (+ Hotel/Hospital/OtherSite variants) | `catalogs/deploy/LodgingLookup-CalikEnergi.ps1` |
| Hotel / hospital | `catalogs/generate/HotelHospital-CalikEnergi.ps1` | `catalogs/deploy/HotelHospitalLookup-CalikEnergi.ps1` |
| Subcontractor | `catalogs/generate/Subcontractor-CalikEnergi.ps1` | `catalogs/deploy/Subcontractor-CalikEnergi.ps1` |
| Site lookups (bundle) | — | `catalogs/deploy/SiteLookup-CalikEnergi.ps1` |

Preview row helper: `catalogs/Import-PreviewCatalogRows.ps1`.

### Patch

| Task | Script |
|------|--------|
| ApprovalLegProfile after Application | `patch/Application-ApprovalLegProfile.ps1` |

---

## Import host on 10.100.128.25 (on-prem server)

**Layout:** `C:\visa2026-sync\` (Production), `C:\visa2026-sync-staging\`, `C:\visa2026-sync-demo\` — published `DataImporter.exe`, id-maps, import logs. No .NET SDK required on the server.

**Import-only:** these hosts run `--import-visa2014` waves via `OnPrem-Sync.ps1`. There is **no** nightly Task Scheduler Sync, **no** `--sync-visa2014`, and **no** sync-state / sync-dashboard scripts.

### 1. Deploy from dev PC (one-time / after importer updates)

```powershell
# Build publish + scripts into C:\visa2026-sync (local path or UNC to .25)
.\scripts\visa2014-migration\import\Install-OnPremSyncHost.ps1 `
  -SyncHostRoot '\\10.100.128.25\c$\visa2026-sync' `
  -PublishFromRepo `
  -CopyIdMapsFromRepo
```

On **`.25`**, edit `C:\visa2026-sync\config\sync.env` — set `VISA2014_SQL_PASSWORD` (ReadOnlyUser on `.15`). Target SQL defaults from the slot `appsettings.Production.json` (`localhost\SQLEXPRESS`).

### 2. Manual Import on server

```powershell
# Lookup resolution + preflight must pass first (or use -SkipLookupPreflight only after an approved exception)
C:\visa2026-sync\tools\scripts\Run-OnPremSyncOnServer.ps1 -SkipTenantCatalogGeneration

# Demo slot:
C:\visa2026-sync-demo\tools\scripts\Run-OnPremSyncOnServer.ps1 -Profile Demo -SkipTenantCatalogGeneration
```

**Network:** `.25` must reach `.15:1433` (legacy) and local `localhost\SQLEXPRESS` (target slot DB).

**Live status while Import runs:** `Watch-OnPremImportLive.ps1` / `Watch-OnPremSyncRun.ps1` from the repo (SSH or on-server).

---

```powershell
.\scripts\visa2014-migration\setup\Restore-LegacyDatabase.ps1
.\scripts\visa2014-migration\Compare-LegacyMigratedCounts.ps1 -ShowIdMap
.\scripts\visa2014-migration\import\Invoke-TenantCatalogGeneration.ps1
.\scripts\visa2014-migration\import\OnPrem-Sync.ps1 -Profile Staging -TargetConnection "Server=...;Database=...;"
.\scripts\visa2014-migration\import\OnPrem-Sync.ps1 -Profile Production -IncludeFileWaves
.\scripts\visa2014-migration\import\Run-HeadlessChain.ps1 -StartAt ApplicationItem
.\scripts\visa2014-migration\reimport\ApplicationItems.ps1 -MaxRows 50
```

---

## Related

- [import-practices.md](../../.cursor/skills/visa2014-to-visa2026-import/import-practices.md) — batching, reconciliation, partial reimport
- [reference.md](../../.cursor/skills/visa2014-to-visa2026-import/reference.md) — SQL templates, paths
- [scripts/README.md](../README.md) — local vs migration script split