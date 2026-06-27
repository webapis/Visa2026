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

### 2026-06-26 — Visa discovery (Çalik VISA2015)

- **Phase**: discovery | mapping
- **Dossier**: docs/VISA2014_MIGRATION/discovery/Visa.yaml
- **Legacy table(s)**: dbo.Visa, dbo.VisaType, dbo.IVisaType_Data, dbo.VisaCategory, dbo.VisaIssuedPlace, dbo.BorderZoneForVisa
- **Counts**: 6041 active visas; 4581 passports; 1460 multi-visa; 0 orphan Passport FK; 7 duplicate VisaNumber groups; 5976 inline scan blobs
- **Surprise**: VisaType labels live on IVisaType_Data (TypeOfVisaL + mgCode); 58 rows GL with null mgCode — no GL in Visa2026 visa-type.json; BorderZone is bit-matrix not comma-separated text
- **SQL / MCP that helped**: sys.columns on dbo.Visa; join counts via sqlcmd on VISA2015 SQLEXPRESS
- **Fix / mapping change**: field-maps/Visa.yaml — Passport id-map FK; sentinel AFV0000000/JLV0000000 dedupe; GöçürmeNusga → VisaDocument file wave
- **Prevent**: Approve VisaType/VisaCategory/VisaIssuedPlace/BorderZoneName layer-3 before importConfirmed; export Excel preview next
- **Artifacts**: discovery/Visa.yaml, field-maps/Visa.yaml, table-mappings visa-main, order.yaml, entity-inventory

### 2026-06-26 — VisaType lookup comparison (Çalik VISA2015)

- **Phase**: discovery | mapping
- **Scope**: dbo.Visa → IVisaType_Data (6041 rows)
- **Verdict**: Approved — 5 buckets; composite TypeOfVisaL:mgCode → LocalizationKey
- **Key mapping**: GL→OF (official/Gulluk visa, not Passport GL→PG); BS:14→BS1; default WP
- **Artifacts**: lookup-comparisons/VisaType.md, VisaType.yaml, lookup-translations.yaml

### 2026-06-26 — VisaCategory lookup comparison (Çalik VISA2015)

- **Phase**: discovery | mapping
- **Scope**: dbo.Visa → VisaCategory (6040 with FK; 1 null → skip)
- **Verdict**: Approved — köp/iki/bir gezeklik + mgCode 4/2/1 → Multiple/Double/Single (perfect 1:1)
- **Artifacts**: lookup-comparisons/VisaCategory.md, VisaCategory.yaml, lookup-translations.yaml

### 2026-06-21 — VisaIssuedPlace lookup comparison (Çalik VISA2015)

- **Phase**: discovery | mapping
- **Scope**: dbo.Visa → VisaIssuedPlace (6041 with FK; 0 null)
- **Verdict**: Approved — 22 distinct labels; 14 map to catalog (6023 rows); 8 embassy labels (18 rows) → skip_row
- **Key aliases**: Türkmenbaşy H.M.→Türkmenbaşy howa menzilindäki MGP; T-abat H.M.→Türkmenabat Howa Menzili; Farap G.Y.→Farap MGP; BERLİN→Berlin; Garabogaz→Garabogaz GY
- **Policy**: Do not default unmapped to catalog IsDefault (Aşgabat MGP) — skip preserves embassy accuracy
- **Artifacts**: lookup-comparisons/VisaIssuedPlace.md, VisaIssuedPlace.yaml, lookup-translations.yaml

### 2026-06-21 — BorderZoneName lookup comparison (Çalik VISA2015)

- **Phase**: discovery | mapping
- **Scope**: dbo.Visa → BorderZoneForVisa bit matrix (589 FK; 5452 null → Ýok)
- **Verdict**: Approved — 8 bits map to NameTm; Garabogaz şäher → Garabogaz şäheri; Sarahs unused on visas
- **Catalog**: Added 6 rows to tenant border-zone-name.json (Daşoguz şäher, Tagtabazar/Serhetabat/Farap/Etrek etrap, Ýolöten etrap)
- **Transform**: comma-separated labels in Helper bit order (not legacy space-concat)
- **Artifacts**: lookup-comparisons/BorderZoneName.md, BorderZoneName.yaml, lookup-translations.yaml

### 2026-06-21 — Visa Excel preview export (Çalik VISA2015)

- **Phase**: mapping | preview
- **CLI**: `--export-visa2014-preview --entity Visa --legacy-source calik-energi`
- **Counts**: 6041 legacy → 6016 import, 19 skipped (18 embassy + 1 null VisaCategory), 6 duplicate_merged
- **Code**: Visa2014VisaTransform.cs, Visa2014VisaPreviewExporter.cs (shared transform with future OData importer)
- **Output**: preview-export/Visa-preview.calik-energi.xlsx
- **Next**: human review → importConfirmed → Visa OData importer + VisaDocument file wave

### 2026-06-21 — Visa OData scalar + VisaDocument file import (implementation)

- **Phase**: import code
- **Files**: Visa2014VisaODataImporter.cs, Visa2014VisaDocumentImporter.cs; OData resolver extended (VisaType/VisaCategory/VisaIssuedPlace)
- **Web API**: VisaDocument registered (like PassportDocument)
- **Dry-run**: 6016 prepared, 251 would skip (Passport not in id-map) — expected orphan-passport gap
- **Blocked**: Blazor not running on :5001 for live POST; restart host then full import

### 2026-06-26 — Visa — pilot OData fix (ShowOptionalFields)

- **Phase**: import
- **Environment**: Visa2026DbDev (localhost:5001)
- **Symptom**: All Visa POSTs returned **400 Bad Request** / **"Incorrect body."**
- **Root cause**: `BuildPayload` included `ShowOptionalFields`, which is `[NotMapped]` on `Visa` — XAF OData rejects non-EDM properties (same pattern as omitting `Category` on Application POST).
- **Fix**: Drop `ShowOptionalFields` from `Visa2014VisaODataImporter.BuildPayload`; keep scalar flags (`IsCancelled`, `IsChanged`, `IsExtended`, `ExtensionRequired`) and lookups.
- **Pilot**: `--import-visa2014 --entity Visa --legacy-source calik-energi --max-rows 5` → **Posted: 5, Failed: 0**
- **Prevent**: Never POST `[NotMapped]` UI-only members (`ShowOptionalFields`, computed state) — mirror `VisaImporter.cs` / `Visa2014PassportODataImporter.cs` payload shape.

### 2026-06-26 — Visa full scalar OData import (calik-energi)

- **Phase**: import
- **CLI**: `--import-visa2014 --entity Visa --legacy-source calik-energi --no-wait`
- **Resume**: `Visa2014VisaODataImporter` loads existing `Visa.json` id-map; skips legacy OIDs already mapped (SkippedAlreadyImported) before POST — required after 5-row pilot.
- **Counts**: legacy 6041 → prepared 6016 (19 transform skip, 6 dedupe); **posted 5760**, failed 0, 251 no Passport id-map, 5 already imported; id-map **5765** entries.
- **Next**: `--import-visa2014-files` VisaDocument wave (GörmeNusga).

### 2026-06-26 — Education discovery started (calik-energi)

- **Phase**: discovery (in_progress)
- **Legacy**: `dbo.Education` — 3133 active rows, 3109 persons, 19 orphan Person FK; no varbinary (no file wave).
- **Visa2026 BO**: `Education.cs` — required lookups + optional `GraduationYear` (derived from `EducationEndDate` year; 2959 rows omit).
- **EducationLevel**: approved — mgCode → LocalizationKey (`lookup-comparisons/EducationLevel.md`).
- **Blocked**: EducationInstitution + Specialty NameTm catalog audits (1537 / 1254 distinct on data).
- **Artifacts**: `discovery/Education.yaml`, `field-maps/Education.yaml`, `education-main` in table-mappings; registered in `order.yaml`.
- **Next**: Institution/Specialty lookup comparisons → Excel preview → `importConfirmed`.

### 2026-06-26 — Education Institution + Specialty lookup gap analysis

- **Tool**: `preview-export/_education-lookup-gap/` (EduGap) — normalize match via `Visa2014CatalogMatchHelper` rules.
- **EducationInstitution**: 1037/3133 rows mapped on current 953-row seed; **2096 rows** need **1471** DISTINCT legacy labels seeded (`education-institution.calik-energi.json`).
- **Specialty**: 956/3133 mapped; **2177 rows** need **1063** DISTINCT `TitleOfSpeciality` seeded (`specialty.calik-energi.json`). Top gap: Tehniki howpsuzlyk we zähmeti goramak (401 rows).
- **Verdict**: `approved_with_catalog_seed` — identity pass-through like ProjectContract; reject skip_row without seed.
- **Artifacts**: `lookup-comparisons/EducationInstitution.md|.yaml`, `Specialty.md|.yaml`, `lookup-translations.calik-energi.yaml`, `analysis.json`.
- **Next**: generate tenant JSON seeds + manifest entries → Excel preview.

### 2026-06-26 — Education calik-energi catalogs + Excel preview

- **Script**: `scripts/local/Generate-EducationLookupCalikEnergiCatalogs.ps1` — union DISTINCT Education labels + existing seed rows.
- **Catalogs**: `education-institution.calik-energi.json` **1471** rows; `specialty.calik-energi.json` **1063** rows; tenant `manifest.json` v21.
- **Preview**: `Education-preview.calik-energi.xlsx` — **3108** import rows, 6 skipped (orphan Person FK), 25 unmapped lookup distinct (mostly edge labels); legacy SQL 3114 with-valid-Person rows.
- **Build fix**: exclude `preview-export/_education-lookup-gap/` from DataImporter csproj (nested EduGap.csproj caused duplicate assembly attributes).
- **Next**: deploy catalogs to dev DB (copy calik JSON → `education-institution.json` / `specialty.json` + `FORCE_XAF_DB_UPDATE`), human `importConfirmed`, `Visa2014EducationODataImporter`.

### 2026-06-26 — Education OData import complete (calik-energi)

- **CLI**: `--import-visa2014 --entity Education --legacy-source calik-energi`
- **Counts**: **2958 posted**, 0 failed, 150 no Person id-map, 6 transform skipped (orphan Person FK).
- **Id-map**: `id-maps/calik-energi/Education.json`
- **Country**: legacy `mgCode` often `ISO3-SUFFIX` (e.g. `GBR-WELIKOBRITANIYA`) — `NormalizeLegacyCountryMgCode` strips prefix; **ALB** added to global `country.json` manifest v3.
- **Institution**: OData import does not POST `EducationInstitution`; resolver uses normalized NameTm keeper when duplicates exist.
- **importConfirmed** 2026-06-26. Next BO: **Application** discovery.

### 2026-06-26 — EmployeePositionHistory discovery started (calik-energi)

- **Legacy**: `dbo.WorkHistoryOfEmployee` — **2993** active rows; FK `Employee` → Person (0 orphan); no `EndDate` column.
- **Visa2026 BO**: `Position` + `ActualPosition` (required) + `Department` + `StartDate`/`EndDate`; omit `ShowOptionalFields` on POST.
- **EndDate**: derive next `StartDateOnThisPosition` per Person (41 multi-history employees).
- **Lookups**: **1579** distinct `TitleOfPosition` vs tenant `position.json` **259**; **74** departments vs seed **3** — calik-energi catalog seeds pending.
- **ActualPosition**: mirror legacy position title (find-or-create by `Name`); not in tenant manifest.
- **Artifacts**: `discovery/EmployeePositionHistory.yaml`, `field-maps/EmployeePositionHistory.yaml`, `employee-position-history-main` in table-mappings; registered in `order.yaml`.
- **Next**: gap analysis scripts + `position.calik-energi.json` / `department.calik-energi.json` → Excel preview → `importConfirmed`.

### 2026-06-26 — EmployeePositionHistory catalogs + Excel preview (calik-energi)

- **Catalogs**: `position.calik-energi.json` **1579** rows, `department.calik-energi.json` **74** rows (from VISA2015 WorkHistory DISTINCT + seed union).
- **Preview**: `EmployeePositionHistory-preview.calik-energi.xlsx` — **2993** import rows, **0** skipped, **0** unmapped lookups; EndDate derived per Person.
- **ActualPosition**: `trim(Position.Code)` or `"-"` on **2289** empty-code rows.
- **Next**: deploy catalogs (`Deploy-PositionDepartmentLookupCalikEnergiCatalogs.ps1` + manifest v25), ensure `ActualPosition` Name `"-"` in target DB, OData importer + pilot.

### 2026-06-26 — EmployeePositionHistory OData import (calik-energi)

- **Deploy**: manifest v25; LookupCatalogSync position created=1377 updated=202, department created=74.
- **OData**: **2838 posted**, 0 failed, 151 no Person id-map, 4 pilot skip-already-imported; **194** ActualPositions find-or-create (~2.3 min).
- **Id-map**: `id-maps/calik-energi/EmployeePositionHistory.json`
- **Code**: `Visa2014EmployeePositionHistoryODataImporter.cs`; resolver Position/Department/ActualPosition.
- **Sign-off**: `discovery/EmployeePositionHistory.yaml` + `order.yaml` — `importConfirmed: true`, `importStatus: done`.

### 2026-06-26 — Visa VisaDocument file wave (calik-energi)

- **CLI**: `--import-visa2014-files --entity Visa --property VisaDocument --legacy-source calik-energi`
- **Counts**: **5571 posted**, 0 failed, 276 no visa map, 45 no blob, 149 oversize (>5MB); ~18 min.
- **Id-map**: `id-maps/calik-energi/VisaDocument.json`
- **Visa entity** scalar + files complete for calik-energi.

### 2026-06-26 — EmployeePositionHistory calik-energi catalogs + Excel preview

- **Scripts**: `Generate-PositionDepartmentLookupCalikEnergiCatalogs.ps1`, `Deploy-PositionDepartmentLookupCalikEnergiCatalogs.ps1` (overlay manifest v25).
- **Catalogs**: `position.calik-energi.json` **1579** rows; `department.calik-energi.json` **74** rows (union DISTINCT WorkHistory labels + tenant seed).
- **Preview**: `EmployeePositionHistory-preview.calik-energi.xlsx` — **2993** import rows, 0 skipped, 0 unmapped lookup distinct; EndDate derived per Person from next StartDate.
- **Transform**: `Visa2014EmployeePositionHistoryTransform` + preview exporter; ActualPosition = trim(Position.Code) or `"-"`.
- **Next**: human `importConfirmed`, `Visa2014EmployeePositionHistoryODataImporter` (not implemented yet).

### 2026-06-26 — Education diploma copies file wave (calik-energi)

- **Legacy source**: `dbo.PassportCopy` rows with `Education` FK (not `Passport` FK) — **4317** rows, **4287** with blob, **40** oversize; up to **15** copies per Education.
- **Target**: `EducationDocument` + `FileData` on parent `Education` (id-map required).
- **CLI**: `--import-visa2014-files --entity Education --property EducationDocument --legacy-source calik-energi`
- **Code**: `Visa2014EducationDocumentImporter.cs`; register `EducationDocument` on OData in `WebApiServiceExtensions.cs` (was missing vs PassportDocument).
- **Gate**: restart Blazor after OData registration rebuild before POST (F5 file lock).

### 2026-06-26 — File copy naming + blob dedupe (Passport / Visa / Education)

- **Naming**: `passport-{PassportNumber}-copy`, `visa-{VisaNumber}-copy`, `diploma-{PersonFirstName LastName}-copy` (+ `-2` suffix when multiple distinct blobs per parent).
- **Dedupe**: SHA256 per target parent; on resume, seed dedupe set from id-map rows (read legacy blob before skip) so duplicate diploma copies are not re-posted.
- **Already imported** (~2360 EducationDocument): still show old `passport-copy-{guid}` names — cleanup/rename separately if needed; duplicates already in DB must be deleted manually.

### 2026-06-27 — EducationDocument cleanup + resume import (calik-energi)

- **Phase**: import | tooling
- **Environment**: Visa2026DbDev (localhost:5001, LocalDB Visa2026)
- **Pre-cleanup state**: 3903 active EducationDocument rows; 3903 id-map entries; all FileName `passport-copy-{guid}`; 3 duplicate blobs (same SHA256 per Education); 1217 educations with multiple docs
- **Cleanup CLI**: `--cleanup-visa2014-education-documents` (`Visa2014EducationDocumentCleanup.cs`) — OData DELETE duplicates (XAF soft-delete GCRecord=1), PATCH FileData.FileName → `diploma-{FirstName LastName}-copy`, prune id-map for removed rows
- **Cleanup result**: 3 duplicates removed, 3900 renamed, 0 failed; id-map 3900 entries
- **Resume import**: `--import-visa2014-files --entity Education --property EducationDocument --legacy-source calik-energi --no-wait`
- **Counts**: legacy 4317 → **posted 109**, skipped already imported 3900, duplicate blob 15, no education map 231, no blob 28, oversize 34, failed 0; id-map **4009**; active DB rows **4009** (all `diploma-*` named)
- **Prevent**: Run cleanup before resuming after partial import with old naming; dry-run import does not load id-map (Already imported always 0 in dry-run summary)
