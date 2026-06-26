# Learnings (append-only): visa2014-to-visa2026-import

**Purpose:** Record verified discovery, strategy decisions, mapping corrections, and OData import outcomes so **each session builds on the last**.

**Loop:** [MATURITY.md](./MATURITY.md) — **read `## Entries` before every task**; **append after verified work** (not optional).

**Canonical plan:** [docs/VISA2014_MIGRATION.md](../../../docs/VISA2014_MIGRATION.md) · [IMPORT_PLAN_AND_STRATEGY.md](../../../docs/VISA2014_MIGRATION/IMPORT_PLAN_AND_STRATEGY.md)

**Not here:** Visa2026 seed scenarios — [visa2026-dataimporter](../visa2026-dataimporter/SKILL.md). **Import runbook:** [import-practices.md](./import-practices.md).

---

## When to append (required)

| Event | Append? |
|-------|---------|
| Discovery dossier closed (`complete` / `blocked` / `skip`) | **Yes** |
| Excel preview exported or reviewed | **Yes** (note path + row counts) |
| Strategy decision locked or plan approved | **Yes** |
| Pilot or batch reconciled | **Yes** |
| Verified mapping or OData fix | **Yes** |
| Exploratory SQL with no conclusion | No |
| User asked read-only question | No |

Promote repeated patterns into [SKILL.md](./SKILL.md) after **2+** occurrences ([MATURITY.md](./MATURITY.md)).

---

## Entry templates

### Discovery / mapping

```markdown
### YYYY-MM-DD — <TargetODataEntity> — <short title>

- **Phase**: discovery | mapping
- **Dossier**: docs/VISA2014_MIGRATION/discovery/{Entity}.yaml
- **Legacy table(s)**:
- **Symptom / surprise**:
- **SQL / MCP that helped**:
- **Fix / mapping change**:
- **Reconciliation** (if any):
- **Prevent** (next session):
- **Artifacts**: field-map, lookup-translations, inventory
```

### Strategy / plan

```markdown
### YYYY-MM-DD — strategy — <decision title>

- **Phase**: strategy
- **Open decision id** (import-strategy.yaml):
- **Chosen option**:
- **Why**:
- **Artifacts**: IMPORT_PLAN_AND_STRATEGY.md, import-strategy.yaml
```

### Excel preview

```markdown
### YYYY-MM-DD — <TargetODataEntity> — excel preview

- **Phase**: excel-preview
- **Export path**: preview-export/{Entity}-preview.xlsx
- **Counts**: legacy __ → after dedupe __ → main sheet __ → skipped __
- **Surprises** (_UnmappedLookups, bad defaults):
- **Mapping fixes**:
- **Ready for importConfirmed**: yes | no
```

### Pilot / import run

```markdown
### YYYY-MM-DD — <TargetODataEntity> — pilot | batch

- **Phase**: import
- **Environment**: Visa2026DbDev | staging | prod
- **Counts**: legacy SQL __ → target __
- **Skipped / dedupeMerged**:
- **OData errors**:
- **Fix**:
- **Reconciliation pass**: yes | no
```

---

## Entries

### 2026-06-20 — Person — bootstrap + discovery complete

- **Phase**: discovery
- **Dossier**: docs/VISA2014_MIGRATION/discovery/Person.yaml
- **Legacy table(s)**: dbo.Person (2,569 active), dbo.Employee (1:1), dbo.Passport (child)
- **Symptom / surprise**:
  - `Person.IDNumber` holds employer names, not civil ID — use `Passport.PersonalNumber`
  - Legacy `MaritalStatus.Status` is free-text family narrative, not Visa2026 catalog
  - 270 persons with multiple passports; 6 PersonalNumber collisions across different Person Oids
- **SQL / MCP that helped**: sqlcmd to `localhost\SQLEXPRESS` / VISA2015 (MCP visa2014-sql-local not in mcps folder — reload Cursor MCP)
- **Fix / mapping change**: table-mappings `person-main`, field-map with canonical passport join, Gender layer-3 rows
- **Reconciliation**: 2,569 active Person; 2,410 IsEmployee; 159 IsFamilyMember; 0 active without passport
- **Prevent**: Always read Passport for PersonalNumber; do not map IDNumber; audit MaritalStatus at importConfirmed
- **Artifacts**: schema-snapshot.md, Person.yaml, field-maps/Person.yaml, lookup-translations.yaml (Gender; Country completed in follow-up entry)

### 2026-06-20 — Person — Passport.PersonalNumber deep dive

- **Phase**: discovery
- **Legacy table(s)**: dbo.Passport (2,860 active rows)
- **Symptom / surprise**:
  - Civil ID lives on **Passport.PersonalNumber**, not Person (Person has no PersonalNumber column)
  - Person.**IDNumber** = employer/subcontractor text in production samples; legacy ImpPersonID → IDNumber link unused (0 rows)
  - Placeholders: **822** passports with `-`, **282** with `.` — map to Visa2026 sentinel `0`
  - **781** persons share PersonalNumber `-` (not unique across persons)
  - **29** persons have different PersonalNumber on different passports for same person
  - Dominant real ID length **11 digits** (Turkish TC-style)
- **Fix / mapping change**: canonical passport ORDER BY non-sentinel first, then PassportIssuedDate DESC; normalize `-`/`.` → `0`
- **Prevent**: Never upsert Person from Person.IDNumber; Passport import BO keeps per-passport PersonalNumber copy (Visa2026 Passport.PersonalNumber is hidden/legacy)

### 2026-06-20 — Person — Visa2026 PersonalNumber uniqueness

- **Phase**: discovery
- **Visa2026 rules**: `IX_People_PersonalNumber` (unique except NULL/''/'0'); `Person_PersonalNumberUniqueAmongActive` on save
- **Legacy impact**: ~1,024 persons → `"0"` (OK); **5 real PN values** each on **2 Person Oids** (same name+DOB — duplicate legacy rows)
- **Fix**: Dedupe merge on real PN before POST; **upsert/id-map on legacy Person.Oid** — not PersonalNumber as sole OData upsert key
- **Prevent**: Importer must normalize `-`/`.` → `"0"` and merge PN duplicates or OData/DB will reject second insert

### 2026-06-20 — Country — Person-scope lookup audit complete

- **Phase**: mapping
- **Legacy table(s)**: dbo.Country (1,861 rows, 240 distinct codes; many duplicate Oids per code)
- **Symptom / surprise**: Only **64** DISTINCT `NameOfCountryL` codes used on active Person (BirthCountry, ForeignAddressCountry, Passport.Citizenship) — all match Visa2026 `country.json` `Code` **1:1** (including `UAE`, not `ARE`)
- **SQL / MCP that helped**: sqlcmd UNION DISTINCT across three Person FK paths on VISA2015
- **Fix / mapping change**: 64 identity rows in `lookup-translations.yaml`; resolve by string Code not legacy Oid; `unmappedPolicy: block_row` safe for Person import
- **Prevent**: Re-audit Country DISTINCT when Application/other BOs add country FKs; do not import legacy Country table
- **Artifacts**: lookup-translations.yaml (Country audit block + values[]), migration-status.yaml (ISS-002 resolved)

### 2026-06-21 — strategy — file/image import separate from Excel

- **Phase**: strategy
- **Open decision id** (import-strategy.yaml): file-blob-strategy
- **Chosen option**: Planning locked — two tracks (scalar Excel/OData vs file wave); Person.Photo follow-up after scalar Person; attachments wave last. Transport TBD (recommend base64 PATCH for Photo, FileData two-step for scans).
- **Why**: Excel cannot hold photo/scan bytes for human review; 2,567/2,569 active Person rows have Photo (avg ~473 KB, max ~15 MB). PassportCopy ~9,157 rows deferred to attachments wave.
- **Artifacts**: FILE_AND_IMAGE_IMPORT.md, EXCEL_PREVIEW_EXPORT.md, import-strategy.yaml, field-maps/Person.yaml (Photo stubs)

### 2026-06-21 — strategy — import plan approved

- **Phase**: strategy
- **Open decision id** (import-strategy.yaml): (global approval — openDecisions[] remain for prod cutover)
- **Chosen option**: Baseline strategy in IMPORT_PLAN_AND_STRATEGY.md approved; `implementationBlocked: false`
- **Why**: Developer sign-off in chat; unblocks Excel preview CLI and `--import-visa2014` scaffolding. OData load still gated per BO by Excel preview + `importConfirmed`.
- **Artifacts**: import-strategy.yaml (status approved), IMPORT_PLAN_AND_STRATEGY.md, migration-status.yaml (ISS-001 resolved)

### 2026-06-21 — Person — excel preview export

- **Phase**: excel-preview
- **Export path**: Visa2026.DataImporter/legacy/visa2014/preview-export/Person-preview.xlsx
- **Counts**: legacy 2569 → import 2553 + duplicate_merged 5 + skipped 11
- **Surprises**: 3 sqlcmd parse junk rows skipped; 22 distinct unmapped Relationship/ProjectContract values on _UnmappedLookups sheet
- **Ready for importConfirmed**: pending human review

### 2026-06-21 — MaritalStatus — Status int approved + lookup review gate

- **Phase**: mapping | strategy
- **Legacy table(s)**: dbo.MaritalStatus (Status int 0–5 + StatusL narrative; 1,965 lookup rows)
- **Symptom / surprise**: Not free-text-only — coarse bucket is `Status` int; StatusL is family narrative (1,582 distinct prefixes for Status=0 alone)
- **Fix / mapping change**: Approved map Status 0–5 → Visa2026 `Code` (0→Öýlenen per user sign-off); StatusL → `VisaApplicationFamilyMembersText`; layer 3 in lookup-translations.yaml; preview exporter joins ms and translates
- **Prevent**: Do not set Person `importConfirmed` until person-wave queue complete (Relationship + ProjectContract next); application-wave gate before Application importConfirmed
- **Artifacts**: lookup-translations.yaml (MaritalStatus values[]), lookup-comparisons/lookup-review-queue.yaml, MaritalStatus.md/.yaml (approved), ISS-003 resolved, ISS-012 open

### 2026-06-21 — Multi-company legacy path — Çalik VISA2025

- **Phase**: strategy | tooling
- **Decision**: One legacy DB per company per Visa2026 deployment; `legacy-sources.yaml` + `--legacy-source calik-energi|gap-insaat`
- **Çalik pilot**: VISA2025 on SQLEXPRESS → LocalDB `Visa2026`; default CLI source `calik-energi`
- **Gap path**: VISA2015 + `lookup-translations.gap-insaat.yaml` (GT-15 remap preserved)
- **ProjectContract**: Çalik uses `identityPassThrough`; Gap keeps explicit GT-15 remap
- **importConfirmed**: reset for Person until `Person-preview.calik-energi.xlsx` reviewed
- **Blocker**: VISA2025 not listed on local SQLEXPRESS at agent check — attach DB in SSMS (ISS-015)
- **Artifacts**: MULTI_COMPANY_LEGACY_SOURCES.md, legacy-sources.yaml, Visa2014LegacySource.cs, lookup-translations.calik-energi.yaml, lookup-translations.gap-insaat.yaml

### 2026-06-26 — Unicode fix — sqlcmd → SqlClient

- **Phase**: tooling | excel-preview
- **Symptom**: Turkish/Turkmen characters (ö, ü, ş, ý, …) garbled in Person-preview.xlsx
- **Cause**: `sqlcmd` stdout decoded as UTF-8 on Windows; console/OEM code page mangled nvarchar text
- **Fix**: `Visa2014SqlCmdReader` now uses **Microsoft.Data.SqlClient** (`ExecuteReader`) — proper Unicode from `VISA2015`
- **Verify**: Re-export `Person-preview.calik-energi-unicode-fix.xlsx`; sheet XML contains `Gökhan`, `ý`, `ş` counts in thousands
- **Note**: Close Excel before re-exporting to default path (file lock fallback still applies)

### 2026-06-26 — ProjectContract — Çalik Energi re-audit (VISA2015)

- **Phase**: discovery | mapping
- **Legacy table(s)**: dbo.Contract; Person.Contract; Application.Contract; dbo.AppliedMinistery
- **Symptom / surprise**: Gap GT-15 remap irrelevant; 73 union codes vs 3-row tenant seed; Application-heavy codes (1574 -KIYANLI, 14306 Mary); no GT-15 in Çalik DB
- **SQL / MCP that helped**: sqlcmd ReadOnlyUser @ VISA2015 — counts 95/83/73, union Person+Application refs
- **Fix / mapping change**: Documented identity pass-through on Code; catalog seed 73 rows required before import
- **Artifacts**: ProjectContract.calik-energi.md, lookup-translations.calik-energi.yaml audit complete

### 2026-06-26 — ProjectContract deploy + Person dry-run (LocalDB)

- **Phase**: tooling | pilot-import
- **Catalog**: `project-contract.calik-energi.json` (73 rows); `Deploy-ProjectContractCalikEnergiCatalog.ps1`
- **Surprise**: Disk overlay alone does **not** override embedded `tenant/project-contract.json` — `LookupCatalogResourceLoader` prefers embedded. Deploy script copies calik → embedded, rebuilds, bumps overlay manifest **19**, then `updateDatabase --forceUpdate` with `FORCE_XAF_DB_UPDATE=true`
- **Verify**: LocalDB `Visa2026` — `project-contract created=73`; **87** `ProjectContracts` total (was 14)
- **Dry-run**: `--import-visa2014 --entity Person --legacy-source calik-energi --dry-run --max-rows 10` → **10 prepared, 0 skipped** (no POST; no API login)
- **Next**: Start Blazor on `:5001`, then live `--max-rows 10` (Admin password); full 2924 rows after spot-check
- **OData POST fixes (2026-06-26)**: ProjectContract resolve by `NameTm` prefix (Code not in EF); default Subcontractor; `PersonRole` string `"Employee"` not int; omit `IsArchived`, `VisaApplicationFamilyMembersText`, empty `Email` on POST
- **Pilot**: 7/10 posted on second batch; 3 failed duplicate PersonalNumber when prior test rows not deleted; OData DELETE returned 401 via curl — remove duplicates in UI or re-run after cleanup
- **Photo import (2026-06-26)**: `--import-visa2014-files --entity Person --property Photo` — SQL `dbo.Person.Photo` → OData PATCH via id-map; pilot 10/10 patched

### 2026-06-26 — Passport discovery (Çalik VISA2015)

- **Phase**: discovery | mapping
- **Legacy table(s)**: dbo.Passport, dbo.PassportType, dbo.Country
- **Counts**: 3684 active passports; 3241 persons; 353 multi-passport; 18 orphan Person FK; 4 duplicate PassportNumber groups (2 sentinel placeholders × 8)
- **PassportType**: Only 3 buckets on data — AD→P (3611), GL→PG (72), DP→PD (1); 231 rows reference soft-deleted type rows — map by TypeOfPassportL+mgCode composite
- **Visa2026 gaps**: Authority ← PassportIssuedPlace; Citizenship legacy column dropped (on Person); PersonalNumber hidden on Passport BO
- **Dedupe**: Visa2026 PassportNumber unique among active — sentinel `AF000000000` / `JL000000000` need Oid suffix strategy
- **Artifacts**: discovery/Passport.yaml, field-maps/Passport.yaml, lookup-comparisons/PassportType.md, order.yaml entry (ISS-005 resolved)
- **Blocked**: `--import-visa2014 --entity Passport` not implemented; importConfirmed false; needs full Person id-map first
