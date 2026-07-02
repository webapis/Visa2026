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
| The task is a stable, repeated procedure worth documenting by name | `catalogs/generate/EducationLookup-CalikEnergi.ps1` |

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
| **End-to-end migration** | Staging UAT, prod cutover, fresh DB | `import/OnPrem-Staging.ps1`, `import/Run-HeadlessChain.ps1`, or first load per `order.yaml` |
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
| On-prem staging waves | `import/OnPrem-Staging.ps1` | Ordered per `order.yaml` |
| Tenant catalog generation | `import/Invoke-TenantCatalogGeneration.ps1` | Wraps `--generate-visa2014-tenant-catalogs` |
| ApplicationItem import only | `import/ApplicationItems.ps1` | Parents + id-maps must exist |
| Single entity (any BO) | *(no script)* | `dotnet run … --import-visa2014 --entity <BO>` |

### Partial reimport (dev only)

| Task | Script | Cleanup SQL |
|------|--------|-------------|
| Application headers + items | `reimport/Applications.ps1` | `cleanup/ImportedApplications.sql` |
| ApplicationItem lines | `reimport/ApplicationItems.ps1` | `cleanup/ImportedApplicationItems.sql` |

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

## Common entry points

```powershell
.\scripts\visa2014-migration\setup\Restore-LegacyDatabase.ps1
.\scripts\visa2014-migration\import\Invoke-TenantCatalogGeneration.ps1
.\scripts\visa2014-migration\import\OnPrem-Staging.ps1 -TargetConnection "Server=...;Database=...;"
.\scripts\visa2014-migration\import\Run-HeadlessChain.ps1 -StartAt ApplicationItem
.\scripts\visa2014-migration\reimport\ApplicationItems.ps1 -MaxRows 50
```

---

## Related

- [import-practices.md](../../.cursor/skills/visa2014-to-visa2026-import/import-practices.md) — batching, reconciliation, partial reimport
- [reference.md](../../.cursor/skills/visa2014-to-visa2026-import/reference.md) — SQL templates, paths
- [scripts/README.md](../README.md) — local vs migration script split