---
name: visa2014-to-visa2026-import
description: >-
  VISA2014 → Visa2026 migration: Excel preview of consolidated import data; approve strategy before
  code; read learnings each session; VISA2015 source of truth; confirm each BO; OData import.
disable-model-invocation: false
---

# VISA2014 → Visa2026 import

**Canonical plan:** [docs/VISA2014_MIGRATION.md](../../../docs/VISA2014_MIGRATION.md)

**Import plan and strategy (approve before implementation):** [IMPORT_PLAN_AND_STRATEGY.md](../../../docs/VISA2014_MIGRATION/IMPORT_PLAN_AND_STRATEGY.md) · [import-strategy.yaml](../../../Visa2026.DataImporter/legacy/visa2014/import-strategy.yaml)

**Excel preview (before importConfirmed):** [EXCEL_PREVIEW_EXPORT.md](../../../docs/VISA2014_MIGRATION/EXCEL_PREVIEW_EXPORT.md)

**Dependency order (discovery + import):** [order.yaml](../../../Visa2026.DataImporter/legacy/visa2014/order.yaml)

**Three-layer mapping:** [table-mappings.yaml](../../../docs/VISA2014_MIGRATION/table-mappings.yaml) · [field-maps/](../../../Visa2026.DataImporter/legacy/visa2014/field-maps/) · [lookup-translations.yaml](../../../docs/VISA2014_MIGRATION/lookup-translations.yaml)

**Per-BO dossiers:** [discovery/README.md](../../../docs/VISA2014_MIGRATION/discovery/README.md)

**Target OData import patterns:** [visa2026-dataimporter](../visa2026-dataimporter/SKILL.md) · [IMPORTING.md](../../../Visa2026.DataImporter/IMPORTING.md)

**Lookup alignment:** [visa2026-lookup-data](../visa2026-lookup-data/SKILL.md) · [LOOKUP_SEEDING.md](../../../docs/LOOKUP_SEEDING.md)

**Import best practices:** [import-practices.md](./import-practices.md) — **read before Phase 3+**

**Commands and SQL templates:** [reference.md](./reference.md)

**Experience loop:** [MATURITY.md](./MATURITY.md) — **read before every task** · [learnings.md](./learnings.md) — **append after verified work**

---

## Experience loop (mandatory)

Follow [MATURITY.md](./MATURITY.md) on **every** migration session:

1. **READ** [learnings.md](./learnings.md) (`## Entries`) and [import-strategy.yaml](../../../Visa2026.DataImporter/legacy/visa2014/import-strategy.yaml) `status`.
2. **WORK** — discovery, strategy draft, or import (respect gates below).
3. **RECORD** — append [learnings.md](./learnings.md) when a dossier closes, a strategy decision is locked, a pilot reconciles, or a fix is verified.
4. **PROMOTE** — same issue **2+** times → update Troubleshooting or a scenario here; **3+** → [reference.md](./reference.md).

Do not skip step 1 or 3. Optional learnings defeats the purpose of this skill.

---

## Preflight (every session)

0. **Read [learnings.md](./learnings.md)** and check **`import-strategy.yaml`** → `status` (`approved` required before any import **implementation**).
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

Strategy approved + BO confirmed — build importer?
  → § Phase 2 — Implementation (shell then per entity)

Legacy lookup values for current BO?
  → § Lookup audit (often dossier step 6)

One entity end-to-end (Person pilot)?
  → § Phase 3 — Pilot OData import

Full transactional import?
  → § Phase 4+ — order.yaml sequence + import-practices.md

Import run / upsert / reconciliation?
  → § Import best practices (import-practices.md)

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
.\scripts\local\Restore-Visa2014Db.ps1
# or
.\scripts\local\Restore-Visa2014Db.ps1 -BackupFile D:\backups\visa2015-prod.bak
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

**Append [learnings.md](./learnings.md)** if export surfaced mapping gaps or surprise row counts.

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
- `skip_row` — legacy row not imported (log count)
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

## Phase 2 (lookup) — Lookup value audit (layer 3)

Part of each BO dossier (step 6); optional cross-BO audit before full import:

1. For each `lookupCatalog` in field-maps, ensure `lookup-translations.yaml` has a `catalogs[]` entry.
2. Every distinct legacy value sampled from SQL has a `values[]` row (`legacy` → `target`).
3. Target values exist in Visa2026 ([`LOOKUP_SEEDING.md`](../../../docs/LOOKUP_SEEDING.md), [`visa2026-lookup-data`](../visa2026-lookup-data/SKILL.md)).
4. **ApplicationType** targets use catalog `Name` keys (`App_Inv`, not display title).

Do not import lookup FKs by matching strings between databases.

---

## Import best practices (Phase 3+)

**Full guide:** [import-practices.md](./import-practices.md) — aligns with [visa2026-dataimporter](../visa2026-dataimporter/SKILL.md) and [IMPORTING.md](../../../Visa2026.DataImporter/IMPORTING.md).

### Before any POST

0. **`import-strategy.yaml`** → `status: approved`.
1. **`importConfirmed: true`** on dossier + `order.yaml` for this entity (Phase 1b).
2. Target = **Visa2026DbDev** (or disposable DB) on first runs.
2. **Blazor.Server running** — lookups + org singletons seeded ([LOOKUP_SEEDING.md](../../../docs/LOOKUP_SEEDING.md)).
3. **Server ready** — wait for `https://localhost:5001` (DataImporter pattern).
4. **Prerequisite lookups** verified via OData GET — abort if critical catalogs empty.
5. **BO exposed** in `WebApiServiceExtensions.cs`.
6. **Layer 3 complete** for all `lookupCatalog` fields in the batch.
7. **Application** imports: ApplicationType visibility preflight ([visa2026-dataimporter](../visa2026-dataimporter/SKILL.md)) — do not skip on prod migration without sign-off.

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
6. Set `importStatus: pilot`; **append [learnings.md](./learnings.md)** (required).

---

## Phase 4+ — Full import

Follow [import-practices.md](./import-practices.md) for every batch.

1. Entity row: `discoveryStatus: complete` | `skip` | `blocked` **and** **`importConfirmed: true`** (or parent skip waiver).
2. Import in **same `entities[]` order** — one entity batch at a time until reconciled.
3. Per batch: dedupe → transform → upsert → **reconcile before next entity**.
4. Log summary: success / failed / skipped / dedupeMerged.
5. **`id-map/`** updated continuously; attachments **last**.
6. Prod cutover only after staging UAT + DB rollback plan ([import-practices.md](./import-practices.md) § Safety).

---

## Troubleshooting (short)

| Symptom | Likely fix |
|---------|------------|
| Started import code without plan | Complete [IMPORT_PLAN_AND_STRATEGY.md](../../../docs/VISA2014_MIGRATION/IMPORT_PLAN_AND_STRATEGY.md); get `import-strategy.yaml` `approved` |
| Repeated mistake across sessions | Read **learnings.md** first; append after fix ([MATURITY.md](./MATURITY.md)) |
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

Longer fixes → [learnings.md](./learnings.md) · [import-practices.md](./import-practices.md).
