# VISA2014 → Visa2026 — import best practices

Applies when **loading** data into Visa2026 (Phase 3+). **Extract** always from **`VISA2015`** (not inferred from VISA2014 repo).

**Prerequisite:** [IMPORT_PLAN_AND_STRATEGY.md](../../../docs/VISA2014_MIGRATION/IMPORT_PLAN_AND_STRATEGY.md) **`approved`** in `import-strategy.yaml`. Phase 1 discovery **`complete`** and Phase **1b** **`importConfirmed: true`** per entity — see [VISA2014_MIGRATION.md § Import confirmation gate](../../../docs/VISA2014_MIGRATION.md). Do not implement import code or POST until gates pass.

**Experience:** read [learnings.md](./learnings.md) before work; **append after every import attempt** (success or failure) per [MATURITY.md](./MATURITY.md).

Discovery/mapping rules live in [SKILL.md](./SKILL.md) and [VISA2014_MIGRATION.md](../../../docs/VISA2014_MIGRATION.md).

**Reuse target patterns from:** [visa2026-dataimporter](../visa2026-dataimporter/SKILL.md) · [IMPORTING.md](../../../Visa2026.DataImporter/IMPORTING.md) · `Visa2026.DataImporter/ApiClient.cs` · `BaseImporter.cs`

---

## Principles

| # | Practice | Why |
|---|----------|-----|
| 0 | **Strategy approved** | `import-strategy.yaml` before any import **implementation** |
| 0b | **Confirm before implement** | Excel preview reviewed; `importConfirmed: true` per BO; read/append **learnings.md** each session — **including failed import runs** |
| 1 | **Headless XAF ObjectSpace** into Visa2026 (`--inprocess`) | Same validation/rules as UI — never direct SQL into Visa2026; **no OData writes** for migration (scalar or file waves) |
| 2 | **Never write** to `VISA2015` | Legacy is read-only source |
| 3 | **Dependency order** from `order.yaml` | FKs and id-map must exist before children |
| 4 | **Three-layer mapping** at transform time | Table → column → lookup value before POST |
| 5 | **Idempotent runs** | Re-run safe via natural-key upsert + id-map |
| 6 | **Pilot → reconcile → expand** | One **confirmed** BO on disposable DB before full prod cutover |
| 7 | **Log skips and failures** | `skip_row`, unmapped lookups, OData 400 — no silent drops |
| 8 | **Partial reimport = dev only** | `reimport/` scripts delete one BO scope on a **disposable** local DB while fixing importers — not staging/prod end-to-end |
| 9 | **Dependency order always** | Full and partial reimport both follow `order.yaml` `dependsOn` — parents must exist (and id-maps resolve) before children; if you partial-reimport a parent, re-run downstream BOs in order |
| 10 | **Reuse scripts first** | Search [scripts/visa2014-migration/README.md](../../../scripts/visa2014-migration/README.md) before adding a `.ps1`; prefer DataImporter CLI + `-StartAt` / `-MaxRows` on existing scripts; new file only when nothing fits |

---

## Migration scripts — reuse first

Before creating a PowerShell wrapper:

1. **Index** — [scripts/visa2014-migration/README.md](../../../scripts/visa2014-migration/README.md) script table + [reference.md § Orchestration](./reference.md).
2. **CLI** — `dotnet run --project Visa2026.DataImporter -- --import-visa2014 --entity <BO> …` covers most single-entity runs.
3. **Orchestrators** — `import/Run-HeadlessChain.ps1` (`-StartAt`), `import/OnPrem-Staging.ps1`, `import/Invoke-TenantCatalogGeneration.ps1`.
4. **Extend** — add parameters to an existing script instead of copying its `dotnet run` block.
5. **Implement in C#** — importer flags, corrections, id-map rebuild belong in `Visa2026.DataImporter` when logic is reusable.

**Create a new script only** when the workflow is distinct (e.g. partial reimport = SQL cleanup + rebuild + import) and cannot be expressed as CLI flags or orchestrator parameters. Document it in the README and append [learnings.md](./learnings.md).

---

## Before first POST (target environment)

Run in order:

0. **`import-strategy.yaml`** → `status: approved` ([IMPORT_PLAN_AND_STRATEGY.md](../../../docs/VISA2014_MIGRATION/IMPORT_PLAN_AND_STRATEGY.md)).
1. **`importConfirmed: true`** — dossier + `order.yaml` for every entity in this batch ([SKILL.md § Phase 1b](./SKILL.md)); **Excel preview** reviewed ([EXCEL_PREVIEW_EXPORT.md](../../../docs/VISA2014_MIGRATION/EXCEL_PREVIEW_EXPORT.md)).
2. **Target DB** — `Visa2026DbDev` or empty disposable DB; **not** production on first pass.
3. **Target DB ready** — Module updaters must have run once (start Blazor host **or** use `--inprocess` which boots the same XAF stack headlessly). Lookups, ApplicationType, org singletons ([LOOKUP_SEEDING.md](../../../docs/LOOKUP_SEEDING.md)). Before **Application** import: `order.yaml` **tenantCatalogGeneration** runs automatically (`--generate-visa2014-tenant-catalogs` or `scripts/visa2014-migration/import/Invoke-TenantCatalogGeneration.ps1` / `import/OnPrem-Staging.ps1`); then DB update seeds `ApprovalLegProfile` from generated JSON.
4. **Headless write path:** pass `--inprocess` + `--target-connection` (or env `ConnectionStrings__DefaultConnection`). No Blazor host or OData login required. Kestrel still binds **`:5002`** by default so it does not take `:5001`; nothing calls HTTP during import.
5. **Verify prerequisite lookups** — Country, Department, Position, ApplicationType, etc. exist in target DB (Module updaters ran once). **Abort** if critical catalogs empty.
6. **Org singletons** — `CompanyProfile`, default `ProjectContract` if BO validation requires them (IMPORTING.md Phase 3).
7. **OData exposure** — every imported BO registered in `WebApiServiceExtensions.cs`.
8. **Lookup translations complete** — layer 3 mapped for every `lookupCatalog` used in current batch.
9. **ApplicationType visibility** — if importing `Application` / `ApplicationItem`, catalog `Show*` flags should match Module ([visa2026-dataimporter](../visa2026-dataimporter/SKILL.md) visibility preflight). Fix catalog or DB before bulk import; avoid `--skip-visibility-preflight` except debug.

---

## Extract → transform → load (ETL)

```text
VISA2015 (read SQL)
  → dedupe by field-map deduplication.keys
  → apply column transforms + lookup-translations.yaml
  → resolve FKs via id-map/ (legacy GUID → new OData ID)
  → upsert via headless ObjectSpace (`--inprocess`)
  → append id-map for new rows
  → reconcile counts
```

| Step | Best practice |
|------|----------------|
| Extract | Parameterized SQL; filter soft-delete (`GCRecord`, `rowFilter`) |
| Dedupe | One canonical row per business key **before** POST |
| Transform | Layer 3 **before** OData lookup query — never `$filter` with raw legacy string on target |
| Load | One entity batch per `order.yaml` row; children after parents |
| Map IDs | Write `id-map/{Entity}-{legacyGuid}.json` on every successful create |

---

## Upsert and idempotency

Mirror [IMPORTING.md](../../../Visa2026.DataImporter/IMPORTING.md) scenario idempotency and [Excelmappings.cs](../../../Visa2026.DataImporter/Excelmappings.cs) upsert keys:

1. Define **natural keys** in field-map (`upsertKey: true` on stable columns — e.g. `PassportNumber`, `VisaNumber`).
2. **Before POST:** OData `$filter` on upsert key(s); if found → **PATCH** (or skip if unchanged); if not → POST.
3. **Never** reuse VISA2014 GUIDs as Visa2026 `ID` unless explicitly verified.
4. **Duplicate legacy rows** → one OData row; all legacy IDs in group → same `id-map` target.
5. **Re-run policy:** second full import should not duplicate business keys — upsert must be deterministic.

**PATCH vs POST:**

| Situation | Prefer |
|-----------|--------|
| First prod migration into empty DB | POST + id-map |
| Re-run / fix mapping / dev refresh | Upsert (GET filter → PATCH or POST) |
| Changed many child rows | Clear target scope or disposable DB — avoid partial PATCH mess (see `--clear-scenario` pattern in dataimporter skill) |

---

## Lookup resolution

1. Read legacy column value.
2. Translate with `lookup-translations.yaml` (`legacy` → `target`).
3. Resolve target row: OData GET on catalog with `$filter={targetMatchProperty} eq '...'`.
4. **Cache** lookups in memory per run (`BaseImporter` `_lookupCache` pattern) — one GET per distinct translated value.
5. **Unmapped legacy value:** honor `unmappedPolicy` (`block_row` for required FKs — do not POST null and hope).

**Do not** POST new global lookup rows during prod migration unless explicitly planned — use existing Module catalogs + translation table.

---

## Batching, logging, and errors

| Practice | Detail |
|----------|--------|
| **Batch by entity** | Finish Person batch + reconcile before Application |
| **Counts** | Log `success`, `failed`, `skipped`, `dedupeMerged` per entity |
| **Verbose pilot** | `--verbose` or equivalent on first BO — log payloads |
| **OData 400** | Stop batch; fix mapping or Web API exposure — do not blindly continue |
| **Partial failure** | Record last successful legacy key; **append** `learnings.md` with failed/partial template — do not wait for a fix |
| **No silent catch** | Every `skip_row` and `block_row` increments a counter in import summary |
| **Audit trail off** | All import writes (OData + `--inprocess`) run under `MigrationImportContext` — `AuditTrailService.Enabled = false` on every Object Space; `X-Visa2014-DataImport: true` on DataImporter HTTP |
| **Tracking log cleanup** | After a successful entity run (`exit 0`, not `--dry-run`), clears `legacy/visa2014/import-logs/*.log`, `import_*.log` in output dir, and (when `--target-connection` / env SQL is set) `AuditDataItemPersistent` + `ApplicationRuntimeLog` rows since run start. Id-maps are kept. Use `--skip-import-log-cleanup` to retain logs |

---

## Reconciliation (after each batch)

| Check | How |
|-------|-----|
| Row count | Legacy distinct keys (after dedupe) vs OData `$count` or SQL MCP on target |
| Spot records | 5–10 random legacy IDs via id-map — field parity |
| Lookup coverage | Zero unmapped legacy values in batch log |
| Duplicate groups | `dedupeMerged` matches discovery SQL duplicate count |
| FK integrity | Sample child rows — parent resolves via id-map |
| **Experience** | Append [learnings.md](./learnings.md) — **required** after each batch attempt (success, failure, or partial); see [MATURITY.md](./MATURITY.md) |

---

## Safety and cutover

- **PII:** treat VISA2014 backup and import logs as sensitive.
- **Prod cutover:** only after pilot + UAT on staging clone; runbook with rollback (restore target DB snapshot).
- **Attachments last** — after owning BO and id-map exist.
- **Service account:** use dedicated import user with officer/admin role — not ad-hoc sa on OData.
- **Freeze window:** avoid officers editing same BOs during cutover import.

---

## Anti-patterns (do not)

- Direct `INSERT` into Visa2026 SQL bypassing XAF
- String-match legacy lookup to target without layer 3
- Import ApplicationItem before Person id-map populated
- Parallel OData POST across entities with FK dependencies
- Invent ministry ApplicationType / catalog rows at import time
- `--skip-visibility-preflight` on production migration without sign-off
- Import entire prod legacy in one run without pilot reconciliation
- Commit `id-map/` with prod GUIDs to git

---

## Headless in-process import (`--inprocess`) — canonical write path

**All** VISA2014 → Visa2026 migration writes use the headless XAF host — scalar BOs, file/image waves (photo, passport/visa/diploma scans, spid kepilnama), and post-import corrections. Same business rules as the UI; **no HTTP per row**.

| Flag | Purpose |
|------|---------|
| `--inprocess` | Boot `HeadlessMigrationHost` (Blazor.Server XAF stack; Kestrel on **:5002** by default, not :5001) |
| `--target-connection` | Visa2026 SQL connection string (or env `ConnectionStrings__DefaultConnection`) |
| `--batch-size` | ObjectSpace commits per batch (default **50**) |
| `--dry-run` | Transform + count only; no legacy SQL required for in-process dry-run |

```powershell
# Scalar BO
dotnet run --project Visa2026.DataImporter -- `
  --import-visa2014 --inprocess --entity ApplicationItem `
  --legacy-source calik-energi-onprem-staging `
  --target-connection "Server=...;Database=Visa2026DbStaging;..." `
  --person-id-map "legacy/visa2014/id-maps/.../Person.json" `
  --application-id-map ".../Application.json" `
  --batch-size 50 --no-wait

# File wave (photo, scans) — also requires --inprocess
dotnet run --project Visa2026.DataImporter -- `
  --import-visa2014-files --inprocess --entity Person --property Photo `
  --legacy-source calik-energi `
  --target-connection "Server=(localdb)\mssqllocaldb;Database=Visa2026;Trusted_Connection=True;TrustServerCertificate=True"

dotnet run --project Visa2026.DataImporter -- `
  --import-visa2014-files --inprocess --entity Passport --property PassportDocument `
  --legacy-source calik-energi --target-connection "..."
```

**Orchestration:** `import/Run-HeadlessChain.ps1` runs scalar entities + file waves in dependency order.

**OData write path:** deprecated for migration — do not use `--api-base-url` for loads. OData may remain for read-only reconcile/UAT only.

Implementation: `Visa2026.Blazor.Server/Services/Migration/HeadlessMigrationHost.cs`, `Visa2026.DataImporter/Migration/ObjectSpaceImportSink.cs`, `Visa2014ObjectSpaceImportTarget`, `Visa2014DocumentImportPayload`.

**Audit trail:** disabled for every BO during import (`MigrationImportContext` + `X-Visa2014-DataImport` on OData). Hooks apply on all Object Spaces (batch, lookup resolver, `ObjectSpaceCreated`).

**Tracking log cleanup:** after a successful entity run (`FailedCount == 0`, not `--dry-run`), `Visa2014ImportTrackingLogCleanup` removes `legacy/visa2014/import-logs/*.log`, `import_*.log` under the output dir, and session-scoped rows in `AuditDataItemPersistent`, `ApplicationRuntimeLog`, and orphan `AuditEFCoreWeakReference`. **Id-map JSON is kept.** Pass `--target-connection` when using OData to a remote DB so DB cleanup targets the correct catalog.

---

## Partial reimport (dev implementation only)

**Terminology:** Re-importing **one BO at a time** after deleting its target scope is **partial reimport** — a **developer workflow** while building and fixing migration code on a disposable local DB.

**Not for end-to-end migration.** Staging cutover and production use the full `order.yaml` sequence (`import/OnPrem-Staging.ps1`, `import/Run-HeadlessChain.ps1`, or entity-by-entity first load with reconcile) — never ad-hoc partial deletes on a DB you intend to keep.

**Dependency order (both modes):** Whether end-to-end or partial, always respect `order.yaml` — walk `entities[]` top-to-bottom and satisfy every `dependsOn` before importing or partial-reimporting a BO. Partial reimport touches **one BO at a time**, but only when its parents are already correct in the target DB (and id-maps match). If you partial-reimport a **parent** (e.g. Application), you must partial-reimport or re-run **downstream** entities in dependency order (e.g. ApplicationItem → ApplicationProgress) before trusting child data.

| Mode | When | Entry |
|------|------|--------|
| **End-to-end migration** | Staging UAT, prod cutover, fresh target DB | `import/OnPrem-Staging.ps1` · `import/Run-HeadlessChain.ps1` · per-entity first import per `order.yaml` |
| **Partial reimport** | Local dev: mapping/transform/correction fix on one BO | `reimport/*.ps1` + `cleanup/*.sql` — **one BO per run**, parents must already be valid |
| **Correction only** | Backfill on existing rows, no delete | `order.yaml` → `postImportCorrections` CLI flags |

Scripts live under `reimport/` for historical path reasons; treat them as **partial reimport** helpers, not the production migration path.

### Partial reimport — ApplicationItem

Use when **Application headers** and parent BO rows are already in the **dev** target DB, but you changed ApplicationItem transform/mapping/correction code and need a **clean item reload** (not a full chain re-run).

| Situation | Use |
|-----------|-----|
| Mapping/transform fix on ApplicationItem only (dev) | `reimport/ApplicationItems.ps1` |
| Application header fields wrong (dev) | `reimport/Applications.ps1` (deletes headers + items) |
| Small backfill on existing rows (no delete) | Correction CLIs only — see `order.yaml` → `postImportCorrections` |
| Greenfield or end-to-end | `import/ApplicationItems.ps1` or full chain — **not** partial reimport |

### Prerequisites

- Target DB already has **Application** rows (`IsManualEntry = 1`) and parent id-map targets (Person, Passport, Visa, Education, EmployeePositionHistory, EmployeeSalary, AddressOfResidence) — the script **rebuilds** parent id-maps from natural keys via `--rebuild-visa2014-id-maps`.
- **Legacy SQL:** user env `VISA2014_SQL_PASSWORD` (for id-map rebuild legacy batch + transform).
- **Module build** current (script runs `dotnet build` before import).
- **Local SQL cleanup** in the script uses `(localdb)\mssqllocaldb` / `Visa2026` — pass `-TargetConnection` for the import/rebuild phases if your DB differs; adjust `sqlcmd` in the script or run cleanup manually when not on LocalDB.

### What the script runs (in order)

1. Stop `Visa2026.DataImporter` processes.
2. `cleanup/ImportedApplicationItems.sql` — delete manual-entry **ApplicationItem** rows (+ linked `TravelHistories`); **keeps Application headers**.
3. Remove `id-maps/<legacy-source>/ApplicationItem.json`.
4. `dotnet build Visa2026.slnx`.
5. `--rebuild-visa2014-id-maps` — refresh Person, Application, Passport, Visa, Education, EmployeePositionHistory, EmployeeSalary, AddressOfResidence maps from target natural keys.
6. `--import-visa2014 --inprocess --entity ApplicationItem` with all parent id-map paths.
7. Post-import corrections (unless `-SkipCorrections`):
   - `--correct-person-address-of-residence` (PIA backfill for `CurrentAddressOfResidence`)
   - `--correct-application-item-person-current` (`CurrentEducation` / `CurrentSalary` when Show* gates apply)

Canonical script: [scripts/visa2014-migration/reimport/ApplicationItems.ps1](../../../scripts/visa2014-migration/reimport/ApplicationItems.ps1)

```powershell
# Partial reimport (~21k rows; dev only — long runtime)
.\scripts\visa2014-migration\reimport\ApplicationItems.ps1

# Pilot: cleanup + id-map rebuild only (no import/corrections)
.\scripts\visa2014-migration\reimport\ApplicationItems.ps1 -DryRun

# Smoke test after build
.\scripts\visa2014-migration\reimport\ApplicationItems.ps1 -MaxRows 50

# Custom target (import + rebuild; sqlcmd cleanup still LocalDB unless you edit the script)
.\scripts\visa2014-migration\reimport\ApplicationItems.ps1 `
  -TargetConnection "Server=...;Database=Visa2026;..." `
  -LegacySource calik-energi `
  -Configuration Release
```

### After partial reimport — reconcile

| Check | How |
|-------|-----|
| Row count | ~21,345 ApplicationItems for calik-energi (manual-entry apps); compare legacy distinct keys after dedupe |
| Person-current | Sample employee lines: `CurrentEducation` / `CurrentSalary` populated where ApplicationType `Show*` applies |
| Address PIA | `CurrentAddressOfResidence` set where legacy PIA had address |
| Id-map | `id-maps/<source>/ApplicationItem.json` recreated |
| Logs | `legacy/visa2014/import-logs/reimport-ApplicationItem-*.log` |

**Experience capture (required):** append [learnings.md](./learnings.md) after **every** partial reimport attempt — success, failure, or partial. Use the **Partial reimport** or **Import run (failed or partial)** template. Do not wait until mapping is fully verified.

---

## Commands

**OData (all entities):**

```powershell
dotnet run --project Visa2026.Blazor.Server   # lookups + updaters (first time)
dotnet run --project Visa2026.DataImporter -- `
  --import-visa2014 --entity Person `
  --api-base-url https://localhost:5001 --no-wait
```

**VISA2014 legacy (representative):**

```powershell
dotnet run --project Visa2026.DataImporter -- --import-visa2014 --dry-run --entity Person --legacy-source calik-energi
dotnet run --project Visa2026.DataImporter -- --import-visa2014 --entity Application --legacy-source calik-energi --no-wait
```

---

## Related

- [SKILL.md](./SKILL.md) — phases, discovery, mapping
- [reference.md](./reference.md) — SQL, paths, pick-next-BO
- [learnings.md](./learnings.md) — verified fixes
