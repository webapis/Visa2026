# VISA2014 → Visa2026 — import plan and strategy

**Purpose:** Define **how** production data will be imported **before** any `--import-visa2014` implementation or OData load. Discovery and per-BO mapping answer *what* maps where; this document answers *when*, *where*, *in what order*, and *with what safeguards*.

**Status:** `approved` (2026-06-21) — [`import-strategy.yaml`](../../Visa2026.DataImporter/legacy/visa2014/import-strategy.yaml). Per-BO `importConfirmed` and Excel preview still required before OData load.

**Related:**

- Migration overview: [VISA2014_MIGRATION.md](../VISA2014_MIGRATION.md)
- Per-BO mapping: discovery dossiers, `field-maps/`, `lookup-translations.yaml`
- Runbook (after strategy approved): [import-practices.md](../../.cursor/skills/visa2014-to-visa2026-import/import-practices.md)
- Agent skill: [visa2014-to-visa2026-import](../../.cursor/skills/visa2014-to-visa2026-import/SKILL.md)

---

## Gate order (do not skip)

```text
1. Import strategy approved     → import-strategy.yaml status: approved
2. Per-BO discovery complete      → discoveryStatus: complete
3. Excel preview export           → consolidated data in .xlsx (see EXCEL_PREVIEW_EXPORT.md)
4. Per-BO human confirmation      → importConfirmed: true (after Excel review)
5. Implement importer + OData run → Phase 3+ only
```

**Agents:** may draft or update this plan and mapping YAML. **Must not** implement import handlers or POST to Visa2026 until steps **1–4** are satisfied for the entity (strategy **1** globally).

---

## 1. Objectives

| Goal | Success measure |
|------|-----------------|
| Load legacy **production** data into Visa2026 with reviewable mapping | All layers 1–3 in git; dossiers `complete` |
| **Preview import content in Excel** before OData | Consolidated `--export-visa2014-preview` workbooks reviewed |
| Safe, repeatable runs | Idempotent upsert + `id-map/`; reconciliation passes |
| No silent data loss | `propertyGaps`, dedupe, import logs with skip counts |
| Cutover without blocking officers | Staging UAT on realistic volume before prod |

**Non-goals for v1 import:** real-time sync with VISA2014; bidirectional merge; editing legacy DB.

---

## 2. Strategic decisions (baseline)

These are **defaults** until changed here and in `import-strategy.yaml`. Update both when a decision changes.

| Topic | Decision | Rationale |
|-------|----------|-----------|
| Legacy source of truth | **`VISA2015` SQL** | Production schema and values; VISA2014 repo is hints only |
| Target write path | **Visa2026 OData only** | XAF validation, security, audit — same as `Visa2026.DataImporter` |
| Importer host | **Extend `Visa2026.DataImporter`** with `--import-visa2014` | Reuse `ApiClient`, upsert patterns, one toolchain |
| Mapping storage | **YAML in git** | Reviewable; code does ETL only |
| Legacy GUIDs | **Do not reuse** as Visa2026 `ID` | Use natural-key upsert + `id-map/` for FK resolution |
| Lookup alignment | **Module seed + `lookup-translations.yaml`** | No string-equality match between DBs |
| Dedupe | **Before POST**, one row per business key | Document keys in field-map `deduplication` |
| **Import preview** | **Excel export** of consolidated transform output | Same pipeline as OData; [EXCEL_PREVIEW_EXPORT.md](../VISA2014_MIGRATION/EXCEL_PREVIEW_EXPORT.md) — **scalar only**; binary stubs, not bytes |
| **Files / images** | **Separate track** after owning BO id-map | [FILE_AND_IMAGE_IMPORT.md](../VISA2014_MIGRATION/FILE_AND_IMAGE_IMPORT.md); attachments wave last |
| First target DB | **`Visa2026DbDev`** (disposable) | Never first full run on production |
| Attachments / files | **Separate wave, last** | After owning BOs and id-map stable; Person Photo = follow-up after scalar Person |
| Prod cutover | **Staging UAT → prod runbook → rollback plan** | See § 8 |

### Open decisions (fill before `approved`)

| # | Question | Options | Decision | Owner | Date |
|---|----------|---------|----------|-------|------|
| 1 | Service account for OData import | Dedicated migration user vs admin dev user | _TBD_ | | |
| 2 | Staging DB for UAT | `Visa2026DbStaging` on IIS slot vs Docker dev | _TBD_ | | |
| 3 | Partial prod cutover | Big-bang vs domain waves (person → application → permits) | _TBD_ | | |
| 4 | Legacy rows with unmapped lookups | `block_row` vs quarantine table vs manual fix first | _TBD_ | | |
| 5 | File storage for scans | Copy blobs to Visa2026 file store vs re-link paths | _TBD_ | | See [FILE_AND_IMAGE_IMPORT.md](../VISA2014_MIGRATION/FILE_AND_IMAGE_IMPORT.md) § options |

---

## 3. Import waves (aligned with `order.yaml`)

Waves match [`importPhases`](../../Visa2026.DataImporter/legacy/visa2014/order.yaml) in `order.yaml`. Each wave completes **discovery → confirmation → pilot → reconcile** before the next wave starts implementation at scale.

| Wave | `importPhase` | Entities (extend as dossiers added) | Prerequisites |
|------|---------------|-------------------------------------|---------------|
| **0 — Strategy** | — | Plan approved | MCP + `VISA2015` accessible |
| **1 — Prerequisites** | `prerequisites` | Target lookups seeded; layer 3 for shared catalogs | Strategy `approved`; Blazor updaters run once |
| **2 — Person domain** | `person-domain` | Person (+ children: Education, …) | Wave 1; Person dossier complete; **Excel preview reviewed**; `importConfirmed` |
| **3 — Application domain** | `application-domain` | Application, ApplicationItem | Person imported + id-map; dossiers confirmed |
| **4 — Permits & visas** | `permits-and-visas` | Invitation, WorkPermit, Visa, BorderZone, Rejection | Application domain stable |
| **5 — Progress & history** | `progress-and-history` | ApplicationProgress, legacy state | Owning applications imported |
| **6 — Attachments** | `attachments` | File blobs, scan links (`PassportCopy`, `FileData`, …) | Parent BO id-map complete; scalar OData reconciled |

**Person pilot:** wave 2 loads **scalar Person** first; **`Person.Photo`** in a **file follow-up pass** (same pilot, after id-map) — not in Excel preview bytes. See [FILE_AND_IMAGE_IMPORT.md](../VISA2014_MIGRATION/FILE_AND_IMAGE_IMPORT.md).

**Pilot rule:** first OData load in each wave = **one BO**, verbose logging, count reconciliation, learnings appended — then expand within the wave.

---

## 4. Environment strategy

| Environment | Database | Use |
|-------------|----------|-----|
| **Local dev** | `VISA2015` + `Visa2026DbDev` | Discovery, mapping, first pilot |
| **Staging** | Fresh or restored Visa2026 staging DB | Full-wave UAT, officer spot-check |
| **Production** | `Visa2026DbProd` | Cutover only after staging sign-off |

**Rules:**

- Import **code** can be developed against dev; **full-volume** runs use staging before prod.
- Keep `id-map/` and import logs **out of git** for prod runs (PII).
- Document connection targets in run notes, not in committed secrets.

---

## 5. Technical approach (implementation blueprint)

When strategy is `approved`, implement in this order:

### 5.1 Shell (once)

- [ ] CLI flag `--import-visa2014` on `Visa2026.DataImporter`
- [ ] CLI flag **`--export-visa2014-preview`** — consolidated SQL → Excel ([EXCEL_PREVIEW_EXPORT.md](../VISA2014_MIGRATION/EXCEL_PREVIEW_EXPORT.md))
- [ ] Read `legacy/visa2014/order.yaml`, `field-maps/`, `lookup-translations.yaml`
- [ ] **Shared transform** used by preview export and OData load (no duplicate mapping logic); **binary fields stubbed in Excel**, loaded in file pass ([FILE_AND_IMAGE_IMPORT.md](../VISA2014_MIGRATION/FILE_AND_IMAGE_IMPORT.md))
- [ ] SQL extract from **`VISA2015`** (connection from env — not hardcoded prod)
- [ ] Shared pipeline: extract → dedupe → transform → id-map resolve → OData upsert → reconcile
- [ ] Import summary: success / failed / skipped / dedupeMerged
- [ ] `--verbose`, `--entity Person`, `--dry-run` (transform only, no POST)

### 5.2 Per entity (after that entity's `importConfirmed`)

- [ ] Entity-specific extract SQL from field-map
- [ ] Lookup resolution via layer 3 + OData cache
- [ ] Upsert keys from field-map
- [ ] Reconciliation query template ([import-practices.md](../../.cursor/skills/visa2014-to-visa2026-import/import-practices.md))
- [ ] Pilot on dev → append [learnings.md](../../.cursor/skills/visa2014-to-visa2026-import/learnings.md)

### 5.3 Reuse from existing importer

| Component | Use |
|-----------|-----|
| `ApiClient` | JWT + OData |
| `BaseImporter` | Lookup cache, batch patterns |
| `Excelmappings.cs` | Upsert key reference |
| [visa2026-dataimporter skill](../../.cursor/skills/visa2026-dataimporter/SKILL.md) | Visibility preflight, Web API exposure |

---

## 6. Data quality strategy

| Risk | Strategy |
|------|----------|
| Schema mismatch | Three-layer mapping; no name guessing |
| Duplicate legacy rows | `deduplication` in field-map; probe SQL in discovery |
| Missing legacy columns | `propertyGaps.targetOnly` + documented defaults |
| Extra legacy columns | `propertyGaps.legacyOnly` + `disposition` |
| Wrong lookup value | `lookup-translations.yaml`; `unmappedPolicy: block_row` for required FKs |
| Broken FK after import | Strict `order.yaml` order; id-map before children |
| Silent skips | Log every `skip_row`; fail pilot if skip rate above threshold (define in open decisions) |

---

## 7. Reconciliation strategy

After **every** entity batch (pilot and full):

| Check | Method |
|-------|--------|
| Row counts | Legacy SQL vs Visa2026 OData `$count` or target SQL MCP |
| Business keys | Sample N legacy IDs via `id-map/`; spot-check fields |
| Lookups | No unresolved lookup transforms in log |
| Dedupe | Legacy duplicate groups vs single OData row per key |
| Skips | Logged count matches expected `propertyGaps` / unmapped policy |

**Block next entity** in the wave until reconciliation passes or waiver recorded in dossier `mapping.notes`.

---

## 8. Cutover and rollback

### Staging UAT (required before prod)

- [ ] Full wave imported on staging DB
- [ ] Officer spot-check on critical flows (person search, application list, document copies)
- [ ] Reconciliation checklist signed
- [ ] Open decisions table has no blocking `TBD` for that wave

### Prod cutover (outline)

1. Backup Visa2026 prod DB.
2. Maintenance window or read-only legacy (if applicable).
3. Run import in `order.yaml` sequence (or approved partial wave).
4. Reconcile counts; smoke-test OData/UI.
5. Keep rollback `.bak` until UAT period ends.

### Rollback

- Restore Visa2026 DB from pre-import backup.
- Do **not** write to `VISA2015`.
- Document incident in [learnings.md](../../.cursor/skills/visa2014-to-visa2026-import/learnings.md).

---

## 9. Roles

| Role | Responsibility |
|------|----------------|
| **Developer / agent** | Discovery, mapping YAML, draft strategy, implement after gates |
| **Reviewer (human)** | Approve strategy; set `importConfirmed` per BO |
| **Officer UAT** | Staging spot-check before prod |

---

## 10. Approval

When this plan and open decisions are acceptable:

1. Set [`import-strategy.yaml`](../../Visa2026.DataImporter/legacy/visa2014/import-strategy.yaml) `status: approved`, `approvedAt`, `approvedBy`.
2. Append a learnings entry (strategy locked).
3. Only then start **§ 5.1 Shell** implementation.

**Approved** 2026-06-21 — import implementation may proceed after per-BO gates (Excel preview + `importConfirmed`).

---

## Revision log

| Date | Change |
|------|--------|
| 2026-06-21 | Strategy **approved** — `import-strategy.yaml` status: approved; implementation unblocked |
| 2026-06-20 | Initial import plan and strategy (draft); gates strategy before implementation |
| 2026-06-20 | **Excel preview export** — consolidated VISA2015 → xlsx before import confirmation |
| 2026-06-21 | **File/image import track** — separate from Excel; link FILE_AND_IMAGE_IMPORT.md |
