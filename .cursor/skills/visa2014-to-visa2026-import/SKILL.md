---
name: visa2014-to-visa2026-import
description: >-
  VISA2014 → Visa2026 data migration (sole skill for this): Excel preview; lookup resolution;
  lookup preflight; approve strategy; VISA2015 source of truth; Calik Energi Demo/Prod Import on
  10.100.128.25 (OnPrem-Sync.ps1); OData / in-process import; partial reimport (dev); learnings
  after every import attempt. Chat openers: user-prompts.md. Import only (no delta Sync).
disable-model-invocation: false
---

# VISA2014 → Visa2026 import

**Canonical plan:** [docs/VISA2014_MIGRATION.md](../../../docs/VISA2014_MIGRATION.md)

**Import plan and strategy (approve before implementation):** [IMPORT_PLAN_AND_STRATEGY.md](../../../docs/VISA2014_MIGRATION/IMPORT_PLAN_AND_STRATEGY.md) · [import-strategy.yaml](../../../Visa2026.DataImporter/legacy/visa2014/import-strategy.yaml)

**Excel preview (before importConfirmed):** [EXCEL_PREVIEW_EXPORT.md](../../../docs/VISA2014_MIGRATION/EXCEL_PREVIEW_EXPORT.md) — scalar columns only; binary fields use audit stubs, not bytes.

**File/image import (separate wave):** [FILE_AND_IMAGE_IMPORT.md](../../../docs/VISA2014_MIGRATION/FILE_AND_IMAGE_IMPORT.md)

**Dependency order (discovery + import):** [order.yaml](../../../Visa2026.DataImporter/legacy/visa2014/order.yaml)

**Status tracker (done / in progress / issues):** [STATUS.md](../../../docs/VISA2014_MIGRATION/STATUS.md) · [migration-status.yaml](../../../docs/VISA2014_MIGRATION/migration-status.yaml)

**Intentional exclusions (approved skips only):** [import-exclusions.yaml](../../../docs/VISA2014_MIGRATION/import-exclusions.yaml) — why, counts, approver; FailedCount ≠ exclusion

**Three-layer mapping:** [table-mappings.yaml](../../../docs/VISA2014_MIGRATION/table-mappings.yaml) · [field-maps/](../../../Visa2026.DataImporter/legacy/visa2014/field-maps/) · [lookup-translations.yaml](../../../docs/VISA2014_MIGRATION/lookup-translations.yaml)

**Per-BO dossiers:** [discovery/README.md](../../../docs/VISA2014_MIGRATION/discovery/README.md)

**Target OData import patterns:** [visa2026-dataimporter](../visa2026-dataimporter/SKILL.md) · [IMPORTING.md](../../../Visa2026.DataImporter/IMPORTING.md)

**Lookup alignment:** [visa2026-lookup-data](../visa2026-lookup-data/SKILL.md) · [LOOKUP_SEEDING.md](../../../docs/LOOKUP_SEEDING.md) · [LOOKUP_RESOLUTION_STRATEGY.md](../../../docs/VISA2014_MIGRATION/LOOKUP_RESOLUTION_STRATEGY.md)

**Import best practices:** [import-practices.md](./import-practices.md) — **read before Phase 3+**

**Commands and SQL templates:** [reference.md](./reference.md) · **Migration scripts (reuse first):** [scripts/visa2014-migration/README.md](../../../scripts/visa2014-migration/README.md)

**Chat openers:** [user-prompts.md](./user-prompts.md) (`@visa2014-to-visa2026-import`)

---

## Migration scripts — reuse first

**Do not reinvent the wheel.** Before writing a new `.ps1`, search [scripts/visa2014-migration/README.md](../../../scripts/visa2014-migration/README.md) and [reference.md § Orchestration](./reference.md).

| Need | Use (in order) |
|------|----------------|
| Import one BO | `dotnet run … --import-visa2014 --entity <BO>` |
| Resume / full local chain | `import/Run-HeadlessChain.ps1` (`-StartAt`) |
| **On-prem Demo / Staging / Prod Import** | `import/OnPrem-Sync.ps1` (`-Profile Demo\|Staging\|Production`) · sync host `C:\visa2026-sync*` on `.25` · [ON_PREM_IIS_MIGRATION_RUNBOOK.md](../../../docs/VISA2014_MIGRATION/ON_PREM_IIS_MIGRATION_RUNBOOK.md) |
| Tenant JSON before Application | `import/Invoke-TenantCatalogGeneration.ps1` |
| **Lookup resolution** (audit → translate → seed) | [LOOKUP_RESOLUTION_STRATEGY.md](../../../docs/VISA2014_MIGRATION/LOOKUP_RESOLUTION_STRATEGY.md) · `lookup-translations.yaml` (+ company overlay) · `LookupCatalogs/` / tenant JSON |
| **Lookup preflight** (gate before full Import) | `import/Preflight-LookupAudit.ps1` / `--preflight-visa2014-lookups` |
| Dev: fix one BO after delete | `reimport/<Entity>.ps1` + `cleanup/*.sql` |
| Same script, different scope | Add **parameters** (`-MaxRows`, `-DryRun`, `-TargetConnection`) — not a new file |

**New script only** when no CLI flag, orchestrator, or existing helper covers the workflow — then add one README row and append [learnings.md](./learnings.md). Prefer C# in `Visa2026.DataImporter` over duplicate PowerShell wrappers.

**Experience loop:** [MATURITY.md](./MATURITY.md) — **read before every task** · [learnings.md](./learnings.md) — **append after every import attempt** (success **or** failure)

---

## Full Import order (locked)

Before any full / Demo / on-prem **Import** chain:

```text
1. Lookup resolution   — audit live VISA2015 DISTINCT values
                         → translate (lookup-translations.yaml + company overlay)
                         → seed gaps into Visa2026 LookupCatalogs / tenant JSON + ForceUpdate
2. Lookup preflight    — Preflight-LookupAudit.ps1 / --preflight-visa2014-lookups (exit 0)
3. Full Import         — OnPrem-Sync.ps1 / Run-HeadlessChain / --import-visa2014 waves
```

| Name | Role |
|------|------|
| **Lookup resolution** | Human + YAML + catalog seed work ([LOOKUP_RESOLUTION_STRATEGY.md](../../../docs/VISA2014_MIGRATION/LOOKUP_RESOLUTION_STRATEGY.md)). Do **not** bulk-import legacy lookup tables. |
| **Lookup preflight** | Automated gate; `OnPrem-Sync.ps1` runs it unless `-SkipLookupPreflight`. |
| **Import-time translate** | During POST, resolve FKs via layer 3 only — no inventing catalog rows mid-wave. |

**Çalik Enerji:** base `lookup-translations.yaml` + overlay `lookup-translations.calik-energi.yaml` (see `legacy-sources.yaml` `calik-energi*`).

Do **not** run full Import first and “fix lookups after FailedCount” — resolve + preflight first.

---

## On-prem hosts (Calik Energi) — under this skill

All Demo/Prod migration chat and agent work uses **this** skill. Orchestrator `OnPrem-Sync.ps1` is **Import-only** (`--import-visa2014`). Delta Sync (`--sync-visa2014`, nightly Sync task, LegacySyncDashboard) was **removed**.

| Slot | URL | Database | Legacy source id | Sync host on `.25` |
|------|-----|----------|------------------|--------------------|
| **Demo** | `http://10.100.128.25:8081` | `Visa2026DbDemo` | `calik-energi-onprem-demo` | `C:\visa2026-sync-demo` |
| **Staging** | `:8080` | `Visa2026DbStaging` | (refresh from prod `.bak` — IIS skill) | `C:\visa2026-sync-staging` |
| **Production** | `:80` | `Visa2026DbProd` | `calik-energi-onprem-prod` | `C:\visa2026-sync` |

| Need | How |
|------|-----|
| Legacy SQL (`.15`) | MCP **`visa2014-sql-remote`** → `VISA2015` |
| Lookup preflight on Demo | `Preflight-LookupAudit.ps1` / published DI on sync-demo · [user-prompts.md](./user-prompts.md) |
| Full Import | `OnPrem-Sync.ps1 -Profile Demo\|Production` (preflight auto) |
| Watch Import | `Watch-OnPremImportLive.ps1 -Profile Demo -ViaSsh` |
| IIS / ForceUpdate / staging bak | [visa2026-windows-iis-deploy](../visa2026-windows-iis-deploy/SKILL.md) |

**Hard rules for Import:** no `-ContinueOnError` on full/Demo Import; zero FailedCount unless [import-exclusions.yaml](../../../docs/VISA2014_MIGRATION/import-exclusions.yaml); lookup resolution → preflight → Import.

---

## Experience loop (mandatory)

Follow [MATURITY.md](./MATURITY.md) on **every** migration session. The skill **accumulates experience** from each importing action — not only when things go well.

1. **READ** [learnings.md](./learnings.md) (`## Entries`), [migration-status.yaml](../../../docs/VISA2014_MIGRATION/migration-status.yaml) (`currentFocus`, open `issues`), and [import-strategy.yaml](../../../Visa2026.DataImporter/legacy/visa2014/import-strategy.yaml) `status`. Skim recent import entries for the BO you are about to run.
2. **WORK** — discovery, strategy draft, or import (respect gates below).
3. **RECORD** (required after **every** import run):
   - **Success** — append [learnings.md](./learnings.md) with counts, reconciliation, log path, what worked.
   - **Failure / partial success** — append with exit code, error snippet, root cause (or hypothesis), next fix, log path. Update [migration-status.yaml](../../../docs/VISA2014_MIGRATION/migration-status.yaml) `issues` when the run blocks progress.
   - Also record when a dossier closes, strategy locks, or a mapping fix is verified (without a full import).
4. **PROMOTE** — same issue **2+** times → update Troubleshooting or a scenario here; **3+** → [reference.md](./reference.md).

Do not skip step 1 or 3. Skipping failure entries guarantees the next session repeats the same mistake.

**Import modes that must be logged:** end-to-end chain, single-entity first load, partial reimport, correction CLI, file wave — each attempt counts.

---

## Preflight (every session)

0. **Read** [migration-status.yaml](../../../docs/VISA2014_MIGRATION/migration-status.yaml) (`currentFocus`, open `issues`) and [learnings.md](./learnings.md); check **`import-strategy.yaml`** → `status` (`approved` required before any import **implementation**).
1. Workspace root is **Visa2026** — not VISA2014.
2. **`visa2014-sql-local`** → **`VISA2015`** — **legacy source of truth** (schema, data, lookup values). Read-only.
3. **`visa2026-sql-local`** → Visa2026 DB (target validation only).
4. **`visa2014-readonly-files`** → VISA2014 repo — **supplementary** (BO names, EF hints). Never write. If repo ≠ **`VISA2015`**, trust the database.
5. Prefer checked-in artifacts:
   - **`order.yaml`** — dependency order
   - **`table-mappings.yaml`** — layer 1 tables
   - **`field-maps/{Entity}.yaml`** — layer 2 columns
   - **`lookup-translations.yaml`** — layer 3 lookup values
   - `discovery/{Entity}.yaml`, `entity-inventory.yaml`, `property-gap-registry.yaml`
   - **`IMPORT_PLAN_AND_STRATEGY.md`**, **`import-strategy.yaml`**
6. First imports: **Visa2026DbDev** — not production Visa2026. Read [import-practices.md](./import-practices.md) before Phase 3+.
7. **One BO at a time** — only one `discoveryStatus: in_progress`.
8. **Dependency order** — never discover or import a BO before its `dependsOn` dossiers are closed.
9. **Strategy before code** — do **not** implement `--import-visa2014` until [IMPORT_PLAN_AND_STRATEGY.md](../../../docs/VISA2014_MIGRATION/IMPORT_PLAN_AND_STRATEGY.md) is **`approved`** in `import-strategy.yaml`.
10. **Document before implement** — do **not** POST OData for an entity until dossier + `order.yaml` have **`importConfirmed: true`**.

---

## Decision: which phase?

```
Need local VISA2015?
  → § Restore legacy backup

First time on this backup?
  → § Bootstrap (once) — global table index

Draft or review import plan / waves / cutover?
  → § Phase 0 — Import plan and strategy (before any import code)

Discover next Business Object?
  → § Phase 1 — pick from order.yaml in dependency order

BO documented — export consolidated preview to Excel?
  → § Phase 1c — Excel preview export

BO preview reviewed — ready for human sign-off?
  → § Phase 1b — Import confirmation (importConfirmed)

Binary photos / scans / attachments after scalar BO import?
  → § File and image import (FILE_AND_IMAGE_IMPORT.md)

Strategy approved + BO confirmed — build importer?
  → § Phase 2 — Implementation (shell then per entity)

Legacy lookup values / catalog gaps before Import?
  → § Full Import order (locked) — **lookup resolution** then **lookup preflight**
  → § Phase 2 (lookup) · LOOKUP_RESOLUTION_STRATEGY.md

One entity end-to-end (Person pilot)?
  → § Phase 3 — Pilot OData import (after lookup resolution for that BO’s catalogs)

Full transactional import?
  → § Full Import order (locked) · Phase 4+ · import-practices.md
  → lookup preflight must pass first

Import run / upsert / reconciliation?
  → § Import best practices (import-practices.md)

Need to run import / reimport / catalog step?
  → scripts/visa2014-migration/README.md (reuse existing script or CLI first — § Reuse scripts first)
  → reference.md orchestration table · only create new .ps1 if nothing fits

Partial reimport one BO during migration implementation (local dev)?
  → import-practices.md § Partial reimport · scripts/visa2014-migration/reimport/
  → Application header fix: § Full application domain (not Applications.ps1 alone)
  → Respect order.yaml dependsOn — parents before children; re-run downstream if parent changed

End-to-end migration (Demo / staging / prod cutover)?
  → import/OnPrem-Sync.ps1 or import/Run-HeadlessChain.ps1 · order.yaml — § On-prem hosts
  → not reimport/ · IIS slot work: visa2026-windows-iis-deploy

End of session with verified outcome?
  → Append learnings.md (MATURITY.md loop)
```

---

## Phase 0 — Import plan and strategy (before implementation)

**No `--import-visa2014` code and no OData load until strategy is approved.**

| Artifact | Role |
|----------|------|
| [IMPORT_PLAN_AND_STRATEGY.md](../../../docs/VISA2014_MIGRATION/IMPORT_PLAN_AND_STRATEGY.md) | Waves, environments, cutover, open decisions, implementation blueprint |
| [import-strategy.yaml](../../../Visa2026.DataImporter/legacy/visa2014/import-strategy.yaml) | `status: draft` \| `review` \| `approved` |

### Agent workflow

1. Read existing plan and [learnings.md](./learnings.md) for prior strategy notes.
2. Fill **open decisions** table and wave scope as discovery progresses.
3. Keep `import-strategy.yaml` in sync with locked decisions.
4. **May** draft plan updates; **must not** set `status: approved` unless user explicitly approves.
5. When user approves: set `status: approved`, `approvedAt`, `approvedBy`, `implementationBlocked: false`; append learnings entry.

### What strategy covers (vs discovery)

| Discovery (Phase 1) | Strategy (Phase 0) |
|---------------------|-------------------|
| Per-BO table/column/lookup maps | Global waves, environments, cutover |
| `field-maps/{Entity}.yaml` | `--import-visa2014` shell design |
| One entity at a time | When to build code vs when to run pilots |

Discovery can run in parallel while strategy is `draft`. **Implementation** waits for `approved`.

---

## Restore legacy backup (optional)

If **`VISA2015`** already exists on your SQL instance (e.g. `localhost\SQLEXPRESS` in SSMS), skip restore and verify MCP → `SELECT DB_NAME()` = `VISA2015`.

**Docker dev SQL** (alternative): restore from `.bak`:

```powershell
.\scripts\visa2014-migration\setup\Restore-LegacyDatabase.ps1
# or
.\scripts\visa2014-migration\setup\Restore-LegacyDatabase.ps1 -BackupFile D:\backups\visa2015-prod.bak
```

Verify via **`visa2014-sql-local`**: `SELECT DB_NAME()` (see [reference.md](./reference.md)).

---

## Bootstrap (once per restore)

Before any per-BO dossier:

1. Run global table list + row counts via **`visa2014-sql-local`** ([reference.md](./reference.md) § Bootstrap SQL).
2. Write results to **`docs/VISA2014_MIGRATION/schema-snapshot.md`** only.
3. Set `bootstrapOnce[0].status: complete` in **`order.yaml`**.

---

## Phase 1 — Atomic per-BO discovery (dependency order)

**Rules:**

- Complete one dossier (`complete` | `blocked` | `skip`) before the next eligible BO.
- **Same order as import** — walk [`order.yaml`](../../../Visa2026.DataImporter/legacy/visa2014/order.yaml) `entities[]` top-to-bottom.
- Do not skip ahead to a convenient BO if its `dependsOn` are not closed.

### 1. Pick next BO

1. Read **`order.yaml`** → `entities`.
2. Select the **first** row where:
   - `discoveryStatus` ∉ `complete`, `blocked`, `skip`
   - every entity in `dependsOn` has `discoveryStatus` ∈ `complete`, `blocked`, `skip`
3. If no row qualifies, either all done or a **blocked** dependency is stopping the chain — read upstream dossiers.
4. Open **`discovery/{Entity}.yaml`** (copy [`_template.yaml`](../../../docs/VISA2014_MIGRATION/discovery/_template.yaml) if missing).
5. Set dossier + **`order.yaml`** row `discoveryStatus: in_progress`.

### 2. Execute checklist (single BO)

| # | Layer | Step | Output |
|---|-------|------|--------|
| 1 | — | Review **Visa2026** BO | `BusinessObjects/{Entity}.cs` |
| 2 | — | **Legacy schema (authoritative)** — tables, columns, PK, FKs, counts | **`visa2014-sql-local`** → **`VISA2015`** |
| 3 | — | **Legacy BO hints (optional)** — class/file; reconcile with step 2 | `visa2014-readonly-files` → dossier `legacy.boClass`, `legacy.boPath` |
| 4 | **1** | **Table mapping** | [`table-mappings.yaml`](../../../docs/VISA2014_MIGRATION/table-mappings.yaml) |
| 5 | **2** | **Column mapping** — each legacy column → target property | [`field-maps/{Entity}.yaml`](../../../Visa2026.DataImporter/legacy/visa2014/field-maps/) |
| 6 | **3** | **Lookup value mapping** — DISTINCT legacy values → target catalog values | [`lookup-translations.yaml`](../../../docs/VISA2014_MIGRATION/lookup-translations.yaml) |
| 7 | — | Property gaps, dedupe, defaults | `propertyGaps`, `deduplication` |
| 8 | — | Sync inventory + gap registry | `entity-inventory.yaml`, `property-gap-registry.yaml` |
| 9 | — | Close dossier + `order.yaml` | checklist flags |

**Do not mark `complete` without layers 1–3** (or documented `skip`/`blocked`).

Use [`field-maps/_template.yaml`](../../../Visa2026.DataImporter/legacy/visa2014/field-maps/_template.yaml).

### 3. Close session

- **`order.yaml`** row must match dossier status.
- **Append [learnings.md](./learnings.md)** — required when dossier reaches `complete`, `blocked`, or `skip` (see [MATURITY.md](./MATURITY.md)).
- **Stop** after one BO unless the user asked for multiple **and** the next BO is dependency-eligible.

### Adding a new BO

1. Decide **`dependsOn`** from Visa2026 FK / collection relationships.
2. **Insert** a new row in `order.yaml` **after** all dependencies, **before** first dependent (maintain topological order).
3. Add `discovery/{Entity}.yaml` and `entity-inventory.yaml` row.

Current seed order: **Person** → **Application** → **ApplicationItem** (see `order.yaml`).

---

## Phase 1b — Import confirmation (document before implement)

**Gate between discovery and import.** A BO with `discoveryStatus: complete` is **not** authorized for import until a human sets **`importConfirmed: true`**.

Canonical detail: [VISA2014_MIGRATION.md § Import confirmation gate](../../../docs/VISA2014_MIGRATION.md).

### When to run

After Phase 1 closes a dossier (`discoveryStatus: complete`) and all mapping YAML is synced.

### Agent rules

- **May:** remind user to review; summarize mapping artifacts; fix YAML from review feedback.
- **Must not:** implement `--import-visa2014` handler for the entity; run OData POST/PATCH; set `importConfirmed: true` unless the user explicitly confirms in this session.

### Human sign-off checklist

Review dossier + `field-maps/{Entity}.yaml` + related `lookup-translations.yaml` rows + **Excel preview workbook**:

- [ ] **Excel preview** reviewed ([EXCEL_PREVIEW_EXPORT.md](../../../docs/VISA2014_MIGRATION/EXCEL_PREVIEW_EXPORT.md))
- [ ] Legacy schema from **`VISA2015`**, not repo guesses
- [ ] Layers 1–3 complete (or documented `skip`/`blocked`)
- [ ] `propertyGaps`, `deduplication`, upsert keys acceptable
- [ ] `dependsOn` entities `importConfirmed: true` or waived

### Record confirmation

Update **both** dossier `importConfirmation` and matching **`order.yaml`** row:

```yaml
importConfirmed: true
importConfirmedAt: 2026-06-20
importConfirmedBy: <reviewer name>
```

Sync `entity-inventory.yaml` `importConfirmed`.

Only then proceed to Phase 2 (implementation), Phase 3 (pilot), or Phase 4+ (batch).

---

## Phase 1c — Excel preview export (see before you import)

**Purpose:** Show **consolidated, import-ready** data in Excel — same transforms as OData load, **no target writes**.

**Canonical spec:** [EXCEL_PREVIEW_EXPORT.md](../../../docs/VISA2014_MIGRATION/EXCEL_PREVIEW_EXPORT.md)

### When to run

After **`discoveryStatus: complete`** and `field-maps/{Entity}.yaml` has column mappings. **Before** **`importConfirmed: true`**.

Does **not** require Blazor or OData. Requires read access to **`VISA2015`**.

### Planned CLI

```powershell
dotnet run --project Visa2026.DataImporter -- `
  --export-visa2014-preview `
  --entity Person `
  --output Visa2026.DataImporter/legacy/visa2014/preview-export/Person-preview.xlsx
```

*(Not implemented until strategy `approved` and shared transform shell exists — see Phase 2.)*

### Workbook (summary)

| Sheet | Content |
|-------|---------|
| `{Entity}` | Target property columns = values after dedupe + transform + lookup translation |
| `_Skipped` | Rows that would not import |
| `_UnmappedLookups` | Legacy lookup values still missing from layer 3 |
| `_DedupeSummary` | Duplicate groups and canonical row |

Output: `legacy/visa2014/preview-export/` — **`.xlsx` gitignored** (PII).

### After export

1. Reviewer opens workbook ([EXCEL_PREVIEW_EXPORT.md § Review checklist](../../../docs/VISA2014_MIGRATION/EXCEL_PREVIEW_EXPORT.md)).
2. Set `field-maps/{Entity}.yaml` → `export.lastExportedAt`, `lastExportPath`.
3. Set dossier checklist `excel_preview_exported: true`.
4. Proceed to Phase **1b** confirmation (or fix mapping and re-export).

**Binary fields:** Photo and file attachments are **not** in Excel cells — stub columns only. Import bytes in a **file follow-up pass** after scalar OData ([FILE_AND_IMAGE_IMPORT.md](../../../docs/VISA2014_MIGRATION/FILE_AND_IMAGE_IMPORT.md)).

**Append [learnings.md](./learnings.md)** if export surfaced mapping gaps or surprise row counts.

---

## File and image import (after scalar BO + id-map)

**Canonical spec:** [FILE_AND_IMAGE_IMPORT.md](../../../docs/VISA2014_MIGRATION/FILE_AND_IMAGE_IMPORT.md)

### When to run

After owning BO scalar import reconciled and `id-map/` populated. **Person.Photo** immediately after Person pilot; `PassportCopy` / scans in **attachments** wave last.

### Rules

- Excel preview never embeds bytes — `_hasPhoto`, `_photoByteLength`, `_photoSha256` stubs only.
- CLI: `--import-visa2014-files --inprocess` (required). Writes via headless ObjectSpace — not OData PATCH/POST.
- Planned: `FamilyProofDocument` → `PersonDocument` / `PersonFamilyRelationDocument` on same headless path.
- Idempotency via hash + byte length; quarantine corrupt/oversize files.
- Resolve `openDecisions.file-blob-strategy` before prod cutover.

---

## Phase 2 — Implementation (after strategy approved + per-BO confirmed)

**Gates:**

1. **`import-strategy.yaml`** → `status: approved`
2. Entity **`importConfirmed: true`**
3. Read [IMPORT_PLAN_AND_STRATEGY.md § 5](../../../docs/VISA2014_MIGRATION/IMPORT_PLAN_AND_STRATEGY.md) for shell vs per-entity checklist

### 2a. Shell (once, after strategy approved)

Build shared `--import-visa2014` infrastructure only — no entity POST until that entity is confirmed:

- CLI wiring on `Visa2026.DataImporter`
- **`--export-visa2014-preview`** + **`--import-visa2014`** sharing one transform pipeline
- Read `order.yaml`, field-maps, lookup-translations
- Pipeline stub: extract → dedupe → transform → upsert → reconcile
- `--dry-run`, `--verbose`, `--entity`

Append learnings when shell builds and dry-run succeeds.

### 2b. Per entity (after `importConfirmed`)

- Entity extract + transforms from `field-maps/{Entity}.yaml`
- Pilot on dev → Phase 3

---

## Three-layer mapping (tables, columns, lookup values)

**Never assume** VISA2014 table/column/lookup strings match Visa2026. Map explicitly:

| Layer | File | Discover |
|-------|------|----------|
| 1 Tables | `table-mappings.yaml` | Legacy `dbo.*` + BO → `target.odataEntity` |
| 2 Columns | `field-maps/{Entity}.yaml` | `fields[].source` (column) → `fields[].target` (property) |
| 3 Lookup values | `lookup-translations.yaml` | `catalogs[].values`: `{ legacy: "X", target: "Y" }` same meaning |

### Layer 3 workflow (per lookup field)

1. Field map sets `transform: lookup` + `lookupCatalog: Department` (etc.).
2. Sample **every distinct legacy value** ([reference.md](./reference.md) § Lookup sampling).
3. Add row to `lookup-translations.yaml` under that `targetCatalog`:
   - `legacy` — exact value stored in VISA2014 (string, code, or resolved label from FK)
   - `target` — existing Visa2026 catalog `Name` or `Code`
4. Set `unmappedPolicy` for stragglers (`block_row` recommended for required lookups).

**Import transform path:** read legacy column → translate via `values[]` → resolve Visa2026 lookup OData reference by `targetMatchProperty`.

---

## Data quality — gaps, defaults, mismatches, dedupe

Canonical detail: [VISA2014_MIGRATION.md](../../../docs/VISA2014_MIGRATION.md) § Data quality.

### Bidirectional gaps (per BO)

| Direction | Record in | Action |
|-----------|-----------|--------|
| Legacy column, no Visa2026 property | `propertyGaps.legacyOnly` | Usually `disposition: drop` + note |
| Visa2026 property, no legacy column | `propertyGaps.targetOnly` | Set `default` + `missingBehavior: use_default` |
| Both exist, different shape | `propertyGaps.mismatches` or `fields[].transform` | Document transform; do not guess at import |

Update **`property-gap-registry.yaml`** when the dossier closes (counts + `deduplicationEnabled`).

### Missing values on import

Per field or target-only gap:

- `use_default` — apply `defaultValue` / `default` kind
- `allow_null` — when validation allows
- `skip_row` — legacy row not imported (log count). **Requires** an **`approved`** entry in [import-exclusions.yaml](../../../docs/VISA2014_MIGRATION/import-exclusions.yaml) (why, how many, who approved). Unapproved skips are bugs; `FailedCount > 0` is never an exclusion.
- `block_import` / `block_entity` — stop until mapping fixed

Never invent ministry/legal catalog values — use `lookup-translations.yaml` or block.

### Duplicate consolidation (legacy)

Before OData POST for an entity:

1. Group legacy rows by `deduplication.keys` (business identity).
2. Pick **one canonical row** per group (`canonicalRule`).
3. Optionally **merge** non-null values from duplicates into canonical (`mergeBeforeImport: coalesce_non_null`).
4. Import **one** OData record per group; map **all** legacy IDs in the group to the same new ID in `id-map/`.

Run duplicate probe SQL during discovery; record `duplicateGroups` in dossier `mapping`.

---

## Phase 2 (lookup) — Lookup resolution (layer 3)

**Name:** **lookup resolution** — audit → translate → seed (not “import lookups”).

Part of each BO dossier (step 6); **required for all catalogs** before a full Import (see § Full Import order):

1. For each `lookupCatalog` in field-maps, ensure `lookup-translations.yaml` (and company overlay) has a `catalogs[]` entry.
2. Every distinct legacy value used in live data has a `values[]` row **or** documented `unmappedPolicy` / `identityPassThrough`.
3. Target values exist in Visa2026 — global [`LookupCatalogs/`](../../../Visa2026.Module/DatabaseUpdate/LookupCatalogs/) and/or tenant JSON ([`LOOKUP_SEEDING.md`](../../../docs/LOOKUP_SEEDING.md), [`visa2026-lookup-data`](../visa2026-lookup-data/SKILL.md)). Add missing rows via seed path, then map.
4. **ApplicationType** targets use catalog `Name` keys (`App_Inv`, not display title).
5. Run **lookup preflight** (`Preflight-LookupAudit.ps1`) — exit 0 before full Import.

Canonical strategy: [LOOKUP_RESOLUTION_STRATEGY.md](../../../docs/VISA2014_MIGRATION/LOOKUP_RESOLUTION_STRATEGY.md) · comparisons: [lookup-comparisons/](../../../docs/VISA2014_MIGRATION/lookup-comparisons/).

Do not import lookup FKs by matching strings between databases. Do not POST new global lookup rows during transactional import.

---

## Import best practices (Phase 3+)

**Full guide:** [import-practices.md](./import-practices.md) — aligns with [visa2026-dataimporter](../visa2026-dataimporter/SKILL.md) and [IMPORTING.md](../../../Visa2026.DataImporter/IMPORTING.md).

### Before any POST

0. **`import-strategy.yaml`** → `status: approved`.
1. **`importConfirmed: true`** on dossier + `order.yaml` for this entity (Phase 1b).
2. Target = **Visa2026DbDev** (or disposable DB) on first runs.
3. **Lookup resolution complete** for catalogs used by this batch (§ Full Import order) — then **lookup preflight** exit 0 for full chains.
4. **Blazor.Server / Module updaters** — lookups + org singletons seeded ([LOOKUP_SEEDING.md](../../../docs/LOOKUP_SEEDING.md)).
5. **Server ready** — wait for `https://localhost:5001` (DataImporter pattern) when using OData.
6. **Prerequisite lookups** verified via OData GET / SQL — abort if critical catalogs empty.
7. **BO exposed** in `WebApiServiceExtensions.cs` (OData path).
8. **Layer 3 complete** for all `lookupCatalog` fields in the batch.
9. **Application** imports: ApplicationType visibility preflight ([visa2026-dataimporter](../visa2026-dataimporter/SKILL.md)) — do not skip on prod migration without sign-off.

### ETL pipeline (every entity)

```text
SQL extract → dedupe → transform (columns + lookup values) → resolve id-map FKs → upsert OData → reconcile
```

- **Dedupe** legacy rows before POST ([import-practices.md](./import-practices.md) § ETL).
- **Translate lookups** via `lookup-translations.yaml` **before** OData `$filter` on target catalog.
- **Upsert** on natural keys (`PassportNumber`, …) — GET → PATCH or POST; update `id-map/`.
- **Cache** lookup OData resolves per run (`BaseImporter` pattern).
- **Log** success / failed / skipped / dedupeMerged — no silent `skip_row`.

### Pilot pattern (required before full run)

1. One BO (`Person`) on dev DB.
2. `--verbose` (or equivalent) on first run.
3. **Reconcile** counts + spot-check via id-map ([import-practices.md](./import-practices.md) § Reconciliation).
4. **Append [learnings.md](./learnings.md)** — required.
5. Only then next entity in `order.yaml`.

### Do not

Direct SQL into Visa2026 · raw legacy strings on target lookups · import children before parent id-map · parallel cross-entity POST · invent catalog rows · first run on production · **implement import before strategy `approved`** · **POST OData before `importConfirmed: true`** · **skip learnings append after verified work**.

---

## Phase 3 — Pilot (Person)

Requires **`import-strategy.yaml`** `approved`, **Person** `discoveryStatus: complete`, **`importConfirmed: true`**. Follow [import-practices.md](./import-practices.md).

1. Pre-flight § Before any POST (dev DB, Blazor up, lookups verified).
2. **`field-maps/Person.yaml`** — dedupe + upsert keys defined.
3. **Person** in `WebApiServiceExtensions.cs`.
4. Import with **verbose** logging; upsert on `PassportNumber`.
5. **Reconcile** — legacy distinct passports vs OData count; spot-check 5 records.
6. Set `importStatus: pilot`; **append [learnings.md](./learnings.md)** (required — success or failure).

---

## Phase 4+ — Full import

Follow [import-practices.md](./import-practices.md) for every batch.

1. Entity row: `discoveryStatus: complete` | `skip` | `blocked` **and** **`importConfirmed: true`** (or parent skip waiver).
2. Import in **same `entities[]` order** — one entity batch at a time until reconciled.
3. Per batch: dedupe → transform → upsert → **reconcile before next entity**.
4. Log summary: success / failed / skipped / dedupeMerged.
5. **`id-map/`** updated continuously; attachments **last**.
6. Prod cutover only after staging UAT + DB rollback plan ([import-practices.md](./import-practices.md) § Safety).
7. **After each entity batch** (and after each failed attempt): append [learnings.md](./learnings.md) per [MATURITY.md](./MATURITY.md) — do not wait until the full chain finishes.

---

## Troubleshooting (short)

| Symptom | Likely fix |
|---------|------------|
| Started import code without plan | Complete [IMPORT_PLAN_AND_STRATEGY.md](../../../docs/VISA2014_MIGRATION/IMPORT_PLAN_AND_STRATEGY.md); get `import-strategy.yaml` `approved` |
| Repeated mistake across sessions | Read **learnings.md** first; append after **every** import attempt, including failures ([MATURITY.md](./MATURITY.md)) |
| New `.ps1` for every import task | Search [scripts/visa2014-migration/README.md](../../../scripts/visa2014-migration/README.md); use CLI or `-StartAt` / `-MaxRows` on existing scripts first |
| Import run without user sign-off | Set **`importConfirmed: true`** only after human review (Phase 1b) |
| Agent wrote importer before mapping done | Complete Phase 1 + confirmation gate first |
| Wrong BO discovered first | Use **`order.yaml`** pick algorithm — do not choose by convenience |
| Application before Person | Invalid — `dependsOn` not satisfied |
| Two BOs `in_progress` | Finish or reset one |
| Blocked dependency | Fix upstream dossier or document waiver before downstream |
| OData 400 | BO missing from `WebApiServiceExtensions.cs` |
| Duplicate persons in Visa2026 after import | Enable `deduplication`; verify probe SQL; upsert keys |
| Required field null on POST | Add `targetOnly` default or `missingBehavior: skip_row` |
| Legacy data lost silently | Check `propertyGaps.legacyOnly` — use `archive_in_notes` if needed |
| Wrong lookup on import | Add layer 3 row in `lookup-translations.yaml` — do not match by string equality |
| Unmapped legacy lookup value | Add to `catalogs[].values` or set `unmappedPolicy` |
| Import duplicates on re-run | Upsert on natural keys; maintain id-map |
| Silent missing rows | Check import summary counters; enforce `skip_row` logging |
| Prod import first try | Use Visa2026DbDev pilot + import-practices.md |
| Application cleanup deleted 0 rows | `ImportedApplications.sql` must match `GCRecord = 0` (not `NULL` only) — see [import-practices § Full application domain](./import-practices.md) |
| Reimport skipped all rows (“already imported”) | Stale downstream id-maps / orphan WorkPermit or Invitation rows after Application wipe — purge BO tables + id-maps before re-import |
| Direct-migration apps have ministry progress | `reimport/ApplicationProgress.ps1` only — do **not** run `--correct-application-progress-ministry-legs` after that fix |
| Progress **Ministrlik** empty (status missing `- Energetika`) | `patch/Application-ApprovalLegSnapshots.ps1` — backfills snapshots only; **not** `ApplicationProgress-MinistryLegs.ps1` (that deletes/regens progress) |
| `Applications.ps1` only — items/progress empty | Run full application-domain chain (WorkPermit → Invitation → ApplicationItem → ApplicationProgress) — [import-practices](./import-practices.md) |

Longer fixes → [learnings.md](./learnings.md) · [import-practices.md](./import-practices.md).
