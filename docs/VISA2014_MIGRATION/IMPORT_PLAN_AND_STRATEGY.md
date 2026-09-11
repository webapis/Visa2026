# VISA2014 → Visa2026 — import plan and strategy

**Purpose:** Define **how** production data will be imported **before** any `--import-visa2014` implementation or OData load. Discovery and per-BO mapping answer *what* maps where; this document answers *when*, *where*, *in what order*, and *with what safeguards*.

**Status:** `approved` (2026-06-21) — [`import-strategy.yaml`](../../Visa2026.DataImporter/legacy/visa2014/import-strategy.yaml). **Amendment 2026-09-04:** Application Profile Instances import **by ApplicationType slice** ([application-type-import-order.yaml](../../Visa2026.DataImporter/legacy/visa2014/application-type-import-order.yaml)). **Amendment 2026-09-04 (profile lock):** source composite → target `ApplicationType.Name` → unique Application Profile template ([application-type-profile-lock.yaml](../../Visa2026.DataImporter/legacy/visa2014/application-type-profile-lock.yaml)); do not pick a profile from shared `ApplicationType.Code`. **Amendment 2026-09-07:** optional **development** sample import + human comparison (`pilotThenHumanVerify`) — not a required step on Demo/Prod full Import. Per-BO `importConfirmed` and Excel preview still required before OData load.

**Related:**

- Migration overview: [VISA2014_MIGRATION.md](../VISA2014_MIGRATION.md)
- Per-BO mapping: discovery dossiers, `field-maps/`, `lookup-translations.yaml`
- Runbook (after strategy approved): [import-practices.md](../../.cursor/skills/visa2014-to-visa2026-import/import-practices.md)
- Agent skill: [visa2014-to-visa2026-import](../../.cursor/skills/visa2014-to-visa2026-import/SKILL.md)
- Application Type slices: [application-type-import-order.yaml](../../Visa2026.DataImporter/legacy/visa2014/application-type-import-order.yaml)

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

## 3. Import waves (aligned with `order.yaml` + Application Type slices)

Waves match [`importPhases`](../../Visa2026.DataImporter/legacy/visa2014/order.yaml) in `order.yaml`. **Application Profile Instances** (via-ministry headers) and interdependent issued BOs are **not** imported as “all headers, then all Invitation/WorkPermit/Visa.” Execution follows [`application-type-import-order.yaml`](../../Visa2026.DataImporter/legacy/visa2014/application-type-import-order.yaml) (locked 2026-09-04).

Each wave still completes **discovery → confirmation → pilot → reconcile** before scaling that BO.

| Wave | `importPhase` | What runs | Prerequisites |
|------|---------------|-----------|---------------|
| **0 — Strategy** | — | Plan approved | MCP + `VISA2015` accessible |
| **1 — Prerequisites** | `prerequisites` | Target lookups seeded; layer 3 for shared catalogs | Strategy `approved`; Blazor updaters run once |
| **2 — Person domain** | `person-domain` | Person, Passport, Education, position/salary/address, … **Visa is not in this wave** | Wave 1; Person dossier complete; **Excel preview reviewed**; `importConfirmed` |
| **3 — Application Type slices** | `application-domain` + in-slice progress/issued/visa | **Per type** (living list; **`App_Inv` first**): instance header → roster → progress → Invitation/WorkPermit if the type generates them → Visa if that instance generates Visa. Then the **next** type. | Person + Passport (+ other person scalars except Visa); profiles seeded |
| **4 — After all types** | `permits-and-visas` (remainder) | Rejection + RejectionItem; BorderZone **documents**; **`--visa-remainder`** (no type filter); then **`--correct-visa2014-application-person-document-links`**; then **`--epa-roster-backfill-only`** (Address/Position required on every Calik profile, including **App_Change_Inv** — re-run if `RequirePerson*` turned on after a prior EPA); then **`--travel-roster-backfill-only`** (Registration only) | Every type slice finished |
| **5 — Attachments** | `attachments` | File blobs, scan links (`PassportCopy`, `VisaDocument`, …) | Parent BO id-map complete; scalar import reconciled |

**Application Type bands** (type names inside a band are filled as we go — do not invent):

1. Produce **invitation** (including invitation + work permit) and may result in visa — **`App_Inv` first**.
2. Produce **Visa**, **WorkPermit** (without invitation), or **BorderZone** — instances only for BorderZone; documents wait in wave 4.
3. **Change** invitation or visa (`App_Change_Inv`, `App_Change_Visa_Category`, `App_Change_Passport`, …). These types **also produce** Invitation and/or Visa — still run the issued + visa inner steps.
4. **Last:** types that do **not** produce Invitation, WorkPermit, Visa, or BorderZone — including **cancel** types (`App_Cancel_Inv`, `App_Cancel_Visa`, `App_Cancell_WP`, `App_Cancel_BZ`, …).

**Inner sequence (one type):**

```text
ApplicationProfileInstance
  → ApplicationProfileInstancePerson
  → ApplicationProfileInstanceProgress
  → Invitation + InvitationItem     (if type generates invitation)
  → WorkPermit + WorkPermitItem     (if type generates work permit)
  → Visa                            (if that instance generates Visa; no --visa-remainder)
```

**Historical snapshot (locked 2026-09-08):** every imported application is **past**. Roster `ResolvedLinks` must match `PersonInApplication` FKs (Passport / PreviousPassport / Visa / WorkPermit), not `PersonCurrentItems` / latest-N as of import day. Skip auto-link during `IsDataImport`; pin at roster import. `--correct-visa2014-application-person-document-links` also pins WorkPermitItem. Officer Relink after go-live may still use today. A PIA vs link gap is **only** expected when the source Oid is not in the id-map yet.

**Calik Address + Position on all profiles (locked 2026-09-11):** tenant catalog forces `RequirePersonAddressOfResidence` and `RequirePersonPosition` on **every** profile, including invitation-change (`App_Change_Inv` / `change_invitation`). Pin and EPA SQL only fire when those flags are true — a first EPA pass while flags were off left Address/Position unlinked (local 3/-352). After document-link correction, **always** `--epa-roster-backfill-only`. If flags turn on after a prior import, **re-run EPA**. Position links are employees only.

**Visa:** never before `IssuingApplicationProfileInstance`. Invitation-producing types must **not** require Visa on roster create. After Invitation (+ items) on `App_Inv`, still import visas whose issuing instance is that case. **Never** `--visa-remainder` on a type slice. After **all** types: `--entity Visa --visa-remainder`, then `--correct-visa2014-application-person-document-links`. Local PG: **wipe** the old Passport-first Visa load and reimport per type when the slice reaches Visa. Do not run remainder before later types’ headers if first POST must set issuing FK.

**Person files:** wave 2 loads **scalar Person** first; **`Person.Photo`** in a **file follow-up pass** (after id-map) — not in Excel preview bytes. See [FILE_AND_IMAGE_IMPORT.md](../VISA2014_MIGRATION/FILE_AND_IMAGE_IMPORT.md).

### Sample import + human comparison (development only)

Optional. Locked in `import-strategy.yaml` `pilotThenHumanVerify` (2026-09-07). Use on **local / mapping-development** loads to confirm a BO maps correctly. **Not** required on Demo/Prod full Import chains, and **not** a step before every BO once mapping for that BO is trusted.

Distinct from Excel preview (that is **before** `importConfirmed`). Enable when the reviewer asks (or when first proving a new/changed importer).

```text
1. POST --max-rows N of the current BO only
     N = 1 unless the reviewer asks for 2, 3, … (“compare 2”, “compare 5”)
2. Halt. One Markdown table **per sample row**, friendly for a human scan:
     header: BO + legacy Oid → Visa2026 ID
     columns: Field | Legacy (VISA2015) | Imported (Visa2026) | Result
     Result is Match, Not match, or Expected difference (documented transform / later wave)
     lookups as labels (not GUIDs); empty as (empty)
     plus a short list of Not match rows under that table
3. Reviewer in chat:
     - accept → import remainder of this BO (no --max-rows)
     - suggest a mapping change → fix field-map / lookup-translations / importer C#
       (permanent; never a one-off SQL patch of the sample rows) → re-import sample → show table again
     - raise N → import additional sample rows, one table each, still halt
4. Only after accept (when this gate is on): remainder of this BO.
```

Example (one sample Person):

**Person** — legacy `a1b2…` → Visa2026 `c3d4…`

| Field | Legacy (VISA2015) | Imported (Visa2026) | Result |
|-------|-------------------|---------------------|--------|
| First name | Annaguly | Annaguly | Match |
| Last name | Orazow | Orazow | Match |
| Birth date | 1984-03-12 | 1984-03-12 | Match |
| Citizenship | Turkmenistan | Turkmenistan | Match |

When this gate is **off**: import the full BO (FailedCount = 0), then the next `order.yaml` / inner-sequence step.

---

## 4. Environment strategy

| Environment | Database | Use |
|-------------|----------|-----|
| **Local dev** | `VISA2015` + `Visa2026DbDev` | Discovery, mapping, first pilot |
| **Staging** | Fresh or restored Visa2026 staging DB | Full-wave UAT, officer spot-check |
| **Production** | `Visa2026DbProd` | Cutover only after staging sign-off |
| **On-prem IIS** | `10.100.128.25` (:80 / :8080 / :8081) | See [ON_PREM_IIS_MIGRATION_RUNBOOK.md](./ON_PREM_IIS_MIGRATION_RUNBOOK.md) — legacy SQL `10.100.128.15` |

**Parallel period (on-prem):** officers **view/search only** in Visa2026; legacy `VISA2015` remains system of record; catch-up via **Import** (`OnPrem-Sync.ps1` / `--import-visa2014`) — **no** delta Sync (`import-strategy.yaml` → `onPremDeployment.parallelPeriod`).

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
