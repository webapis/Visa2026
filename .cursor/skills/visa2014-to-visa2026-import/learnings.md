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
### 2026-06-27 — AddressOfResidence — inference pass + re-export

- **Phase**: excel-preview | mapping
- **Export path**: `preview-export/AddressOfResidence-preview.calik-energi.xlsx`
- **Counts**: legacy **3971** → import **3968** (99.92%), skipped **3** (was 1209), unmapped lookups **3** (was 80)
- **Transform**: expanded `InferRegionMgCode` (ş./s. Aşgabat, Askabat typo, Türkmenabat/Daşoguz/Türkmenbaşy ş prefixes) and `InferCityFromAddressLine` (Mary/Lebap/Balkan/Ahal etrap defaults, hotel lines, S.Türkmenbaşy şäherçesi)
- **Remaining skips**: ~3 bare Aşgabat street lines (`1955 köç…`) with no welaýat prefix — accept or add manual override
- **Ready for importConfirmed**: **yes** after spot-check (pending human flag)

### 2026-06-27 — AddressOfResidence — Lodging orphan admin strip

- **Phase**: mapping | excel-preview
- **Problem**: after Region/City prefix removal, Lodging kept fragments like `nyn`, `etr.,`, `aýatynyň`, `Mary etrabynyň`, `etr.Guwlymayak`.
- **Root causes**: (1) `StripKnownPrefix` cut on catalog `welaýaty` left glued `nyn` when legacy used ASCII `welayatynyn`; (2) `wel\.?` regex matched only `wel` inside `welayatynyn`; (3) `etr.` glued to next word without space.
- **Fix** (`Visa2014AddressLineNormalizer.cs`): run `StripWelPrefix`/`StripEtrapPrefix` before catalog prefix match; folded-index cut + Turkmen glued suffix extension; tighten wel/etr regex; expand `StripOrphanAdministrativeFragments` (incl. `çäginde`, glued `etr.`).
- **Re-export**: import **3968**, skipped **3**; orphan Lodging prefix scan **0** bad rows (was ~72).

### 2026-06-27 — Hotel catalog — ş./şäher/wel. name cleanup

- **Phase**: excel-preview | mapping
- **Export path**: `preview-export/Hotel-preview.calik-energi.xlsx`
- **Problem**: legacy hotel `AddressLine` values kept city/region admin fragments in catalog `Name` (`ş."Mary"`, `şäh.`, `Serhetabat ş.`, `wel.Milli syýahatçylyk zolagy "Awaza"`, glued `ş."Ýyldyz"myhmanhanasy`).
- **Fix** (`Visa2014AddressLineNormalizer.NormalizeHotelCatalogName`): hotel-specific strip after Region/City; require `ş.` dot before unquoted capture (avoid eating `şaher` as `ş`+`aher`); partial `äher`/`äh.` orphans; quote unwrap + glued `"myhmanhan` spacing; restore `{city} myhmanhanasy` when strip leaves generic suffix only.
- **Wiring**: `TryBuildHotelSiteAddress` + `Visa2014HotelTransform`; AddressOfResidence Hotel column uses same normalizer.
- **Re-export**: legacy **52** → **26** catalog names (+ **26** dedupe-merged), **0** skipped.

### 2026-06-27 — Hotel + Hospital tenant catalogs (calik-energi)

- **Phase**: lookup | excel-preview
- **Generate**: `scripts/local/Generate-HotelHospitalCalikEnergiCatalog.ps1` from preview xlsx (`Import-Visa2014PreviewCatalogRows.ps1` — C# normalizer output, not PS strip).
- **Output**: `hotel.calik-energi.json` **22** rows; `hospital.calik-energi.json` **4** rows.
- **Deploy**: `scripts/local/Deploy-HotelHospitalLookupCalikEnergiCatalog.ps1` → copy to embedded `hotel.json` / `hospital.json`, manifest **v30**, then `Update-LocalDatabase.ps1 -ForceUpdate`.

### 2026-06-27 — Lodging catalog — wel./ş./w, prefix cleanup (round 2)

- **Phase**: excel-preview | mapping
- **Problem**: `FullAddress` still led with `wel.`, `wel-ň`, `w,`, `we.`, `ş.`, `S.`, `Balkanabat ş,`, orphan `ň`, `etr-n` after region/city strip (lodging used `StripRegionAndCityPrefixes` only; PS generate script was out of sync with C#).
- **Fix** (`NormalizeLodgingCatalogAddress`): lodging-specific admin strip (extends hotel patterns) + `etr-n` / ASCII `s,` şäher shorthand; `TryBuildLodgingSiteAddress` + AddressOfResidence Lodging column; `Generate-LodgingCalikEnergiCatalog.ps1` now reads **Lodging-preview** xlsx (no stale seed merge).
- **Re-export**: legacy **106** → **85** catalog rows (+ **19** dedupe-merged), orphan prefix scan **0** bad rows.

### 2026-06-27 — Lodging/hotel split — Lojman myhmanhan lines → Hotel catalog

- **Phase**: lookup | excel-preview | mapping
- **Pattern**: legacy `DocumentOfAddress=Lojman` rows whose `AddressLine` contains `myhmanhan` (folded) are **Hotel**, not Lodging — `Visa2014ResidenceClassifier.IsHotelAddressLine`; `MapResidenceType` in `Visa2014AddressOfResidenceTransform.cs`.
- **Catalog generate**: move hotel-named lines out of Lodging preview into Hotel preview; regenerate `lodging.calik-energi.json` **67** rows + `hotel.calik-energi.json` **33** rows (no myhmanhan left in lodging catalog).
- **Deploy**: tenant overlay to LocalDB before AddressOfResidence re-export (lodging/hotel FK resolution uses deployed catalogs).
- **AddressOfResidence re-export**: legacy **3971** → import **3968**, skipped **3** (unchanged vs inference pass); Type **Lodging 2378** / **PrivateHouse 1148** / **Hotel 442**; unmapped **3** (Region/City on skipped Patent-only rows — not hotel/lodging gaps).
- **Shell**: `$env:VISA2014_SQL_PASSWORD = [Environment]::GetEnvironmentVariable('VISA2014_SQL_PASSWORD','User')` — User-level env is not inherited by Cursor agent shells by default.

- **Phase**: discovery | excel-preview | lookup
- **Export path**: `preview-export/AddressOfResidence-preview.calik-energi.xlsx`
- **Counts**: legacy SQL **3971** → import **2762** → skipped **1209** → unmapped lookups **80** distinct
- **Surprises**:
  - VISA2015 city table/column is **`ŞäherEtrap`** (U+015E + U+00E4), not `ŞeherEtrap` — `OBJECT_ID` fails on wrong spelling; use `UNICODE(SUBSTRING(name,1,2))` on `sys.tables` to verify.
  - SQL row count < 4083 active because extract joins `Person` with `GCRecord IS NULL`.
  - `LookupCatalogResourceLoader.LoadCatalogFile` preferred embedded tenant JSON over disk overlay — F5 lock kept 7-row embedded `lodging.json` in running app; **fixed** to prefer `{AppBase}/LookupCatalogs/tenant/` first.
- **Lodging catalog**: `lodging.calik-energi.json` **96** rows; manifest **v28**; DB sync pending **Shift+F5 + rebuild** (Module.dll locked by debug session).
- **Ready for importConfirmed**: **no** — review `_Skipped` + `_UnmappedLookups` sheets first.

## 2026-06-27 — AddressOfResidence OData importer (calik-energi)

- **Phase**: import-code
- **Pattern**: `Visa2014AddressOfResidenceODataImporter` mirrors Education/EmployeePositionHistory — transform `PrepareImportBatch`, Person id-map, `Visa2014ODataLookupResolver` for Region/City/Lodging/Hotel/Hospital, POST + id-map.
- **CLI**: `--import-visa2014 --entity AddressOfResidence --legacy-source calik-energi [--dry-run] [--max-rows N]`
- **Gate**: `importConfirmed` still **false** in discovery — dry-run/pilot before full 3968-row load.

### 2026-06-29 — AddressOfResidence OData importer verified (dry-run + pilot gate)

- **Phase**: import-code | pilot
- **Code**: `Visa2014AddressOfResidenceODataImporter.cs`; `Visa2014ODataLookupResolver` extended with Region/City/Lodging/Hotel/Hospital; wired in `Visa2014ImportCommand.cs`.
- **Dry-run** (`--dry-run --no-wait`): legacy **3971** → prepared **3968**, transform skipped **3**, dedupe **0**, would skip **182** (Person not in id-map).
- **Pilot** (`--max-rows 5 --no-wait`): auth OK; **failed** loading lookups — `GET Hotel` returned HTML (`'<' is an invalid start of a value`) because **Hotel/Hospital were not on OData** (only Lodging was registered).
- **Fix**: register `Hotel` + `Hospital` in `WebApiServiceExtensions.cs` (same as Lodging). **Restart Blazor** after rebuild before retrying pilot.
- **Full-import blockers**: `importConfirmed: false`; ~182 rows lack Person id-map; tenant lodging/hotel/hospital catalogs must match deployed LocalDB; server must expose all five lookup entities on OData.

## 2026-06-21 — Lodging dedupe + site catalog deploy + AddressOfResidence importConfirmed (calik-energi)

- **Phase**: excel-preview | deploy | import-pilot
- **Lodging dedupe**: `BuildLodgingDedupeKey` in `Visa2014AddressLineNormalizer` — strip location fluff, compact alphanumeric key, typo folds (`Enerjy`, `Çalik`/`Çalık`, `UÝJf`); `_dedupeKey` column in preview; `ResolveLodging` falls back to dedupe key match.
- **Counts**: Lodging catalog **48 → 37** import rows (**22** duplicate_merged).
- **Deploy**: `scripts/local/Deploy-SiteLookupCalikEnergiCatalogs.ps1` (lodging + hotel + hospital + other-site); `Update-LocalDatabase.ps1 -ForceUpdate -SkipBuild` — sync created lodging **37**, hotel **34**, hospital **4**, other-site **24**.
- **Sign-off**: `Lodging-preview.calik-energi.xlsx` reviewed; `importConfirmed: true` on AddressOfResidence dossier + order.yaml **2026-06-21**.
- **Pilot**: restart Blazor after OData entity registration; use `--max-rows` for first POST batch; expect ~182 skips without full Person id-map on full run.

### 2026-06-29 — AddressOfResidence full OData import (calik-energi)

- **Phase**: pilot | batch | reconcile
- **Dry-run**: legacy **3971** → prepared **3968**, transform skipped **3**, **182** missing Person id-map.
- **Pilot** (`--max-rows 50`): **49** posted + **1** resume on full run; resolver fixes in `Visa2014ODataLookupResolver` (city `RegionName` enrich from `city.json`; lodging/other-site dedupe without OData row `CityId`; region-scoped scalar; hotel name fallback).
- **Full import**: **3737** posted, **0** failed, **182** skipped (no Person map), **49** already imported (pilot id-map); **OData count 3786** matches posted + pilot.
- **Known gaps**: **182** Person-missing rows; **3** transform skips (Patent, no Region/City FK) — unchanged from preview.
- **Docs**: `order.yaml` + `entity-inventory.yaml` `importStatus: done`; discovery dossier `complete` + `odataImport` block.
- **Next**: Application wave (`order.yaml` application-domain); optional backfill of 182 rows if Person id-map grows.

### 2026-06-29 — EmployeeSalary discovery + Excel preview (calik-energi)

- **Phase**: discovery | excel-preview
- **Legacy shape**: `dbo.Employee.Salary` FK → `dbo.Salary.Detail` (lookup text, not history). **2950** active employees; **no** legacy `Currency` or `StartDate` columns.
- **Target**: one `EmployeeSalary` per employee — `Amount` (normalized string), `Currency` **USD** (all rows; legacy dtm ignored), `StartDate` = MAX(`WorkHistoryOfEmployee.StartDateOnThisPosition`), `EndDate` null.
- **Normalizer**: `Visa2014SalaryAmountNormalizer` — extract numeric from labor-contract sentences; `1.667,00` → `1.667.00`; skip unparseable (e.g. `Alesta`).
- **Preview**: `EmployeeSalary-preview.calik-energi.xlsx` — **2887** import, **63** skipped (empty/unparseable Detail); `_AmountParse` audit sheet.
- **Blockers before OData**: `importConfirmed: false`; register `EmployeeSalary` on OData; implement importer + id-map (Person Oid key).
- **Next**: human review `_AmountParse`; then `importConfirmed: true` → OData implementation.

### 2026-06-29 — EmployeeSalary importConfirmed + OData importer

- **Phase**: importConfirmed | implementation
- **Sign-off**: `importConfirmed: true` 2026-06-29; currency fixed USD.
- **Code**: `Visa2014EmployeeSalaryODataImporter.cs`, `WebApiServiceExtensions` + `EmployeeSalary` OData, `Models.EmployeeSalary`.
- **Dry-run**: 2887 POST-ready, 63 transform skipped, 145 missing Person id-map.
- **Pilot**: 400 on POST — **restart Blazor** after `EmployeeSalary` OData registration (running host has old Web API model).
- **Fix**: OData `Currency` must be string `"USD"` not int `1` (400 Incorrect body).
- **Full import** 2026-06-29: **2740** posted, **0** failed, **145** no Person map, **2** pilot resume-skipped, **63** transform skipped. Id-map: `id-maps/calik-energi/EmployeeSalary.json`.

### 2026-06-29 — MedicalRecord discovery (SpidKepilnama file chain, calik-energi)

- **Phase**: discovery
- **Legacy path**: `IPersonn_SpidKepilnama` → `Copy` → `FileData` (`IPerson.SpidKepilnama` in VISA2014 repo) — **not** scalar medical fields on Person/Employee.
- **Çalik counts**: **2** active link rows, **0** resolvable `Copy` rows (orphan FKs), **0** importable blobs.
- **Scalar sign-off**: `DocumentNumber` = `"0"`; `IssueDate` = `MIN(AuditDataItemPersistent.ModifiedOn)` on `ObjectCreated` for `Copy` + `FileData` OIDs via `AuditedObjectWeakReference.GuidId` (sample verified 2014-01-25); `ValidityDuration` = **Month3** (90 days) → `ExpirationDate` derived on save.
- **Skip**: orphan Copy link, null `FileData.Content`, no audit row (`_issueDateSource: no_audit`), Person not in id-map.
- **Artifacts**: `discovery/MedicalRecord.yaml`, `field-maps/MedicalRecord.yaml`, `table-mappings.yaml` `medical-record-spid-kepilnama`, `order.yaml` attachments entry.
- **importConfirmed**: `true` 2026-06-29 (developer). Çalik file wave still expected 0 rows; Application wave can proceed.
- **Next**: implement file importer (`--import-visa2014-files --entity MedicalRecord --property MedicalRecordDocument`).

### 2026-06-29 — MedicalRecord file importer (calik-energi)

- **Phase**: implementation | file-import
- **Code**: `Visa2014MedicalRecordDocumentImporter.cs`, `Visa2014LegacyAuditIssueDateHelper.cs`; `MedicalRecordDocument` OData registration; CLI in `Visa2014FilesImportCommand`.
- **Flow**: Spid link → resolve Person id-map → audit `ObjectCreated` → POST `MedicalRecord` (Doc# `0`, Month3) → `FileData` → `MedicalRecordDocument`.
- **Dry-run + full run** 2026-06-29: **0** posted, **2** orphan copy links, **0** failed. Çalik has no importable blobs.
- **Note**: restart Blazor after `MedicalRecordDocument` OData registration before first POST on a host with blobs.

### 2026-06-29 — Application — Phase 1 discovery complete

- **Phase**: discovery
- **Dossier**: docs/VISA2014_MIGRATION/discovery/Application.yaml
- **Legacy table(s)**: dbo.Application (12,237 active / 18,118 total) + dbo.IRegistration_Data (numbering); SimpleProcess 8,392 / LongProcess 3,845 via XPObjectType
- **Symptom / surprise**:
  - Legacy type is **not** a single FK — composite ForEmployee/ForFamilyMember + ApplicationTypeForEmployee/FamilyMember SubType ID + invitation/visa WP flags
  - **862** duplicate `ManualApplicationNumber` groups (e.g. `1/-2` × 8) — upsert on Oid, not FullApplicationNumber
  - Contract FK only on long-process rows (3,845) — matches ministry workflow; ProjectContract calik overlay already approved
  - SubType IDs **44** (92 rows) and **55** (13 rows) have no Visa2026 ApplicationType mapping yet
- **SQL / MCP that helped**: sqlcmd `localhost\SQLEXPRESS` / VISA2015 — INFORMATION_SCHEMA + DISTINCT composite type query
- **Fix / mapping change**: `application-main` table map, `field-maps/Application.yaml`, layer 3 Urgency + VisaPeriod (Application scope) + ApplicationType composite in `lookup-translations.yaml`
- **Prevent**: Discover ApplicationItem before Excel preview (34,161 PersonInApplication rows); resolve E:44/E:55 before importConfirmed
- **Artifacts**: discovery/Application.yaml, field-maps/Application.yaml, table-mappings.yaml, lookup-translations.yaml, entity-inventory.yaml, property-gap-registry.yaml

### 2026-06-29 — ApplicationItem — Phase 1 discovery complete

- **Phase**: discovery
- **Dossier**: docs/VISA2014_MIGRATION/discovery/ApplicationItem.yaml
- **Legacy table(s)**: dbo.PersonInApplication (21,794 active / 40,414 total), TravelInformation, AddressOnBusinessTrip, WorkPermit/WorkPermitLocation
- **Symptom / surprise**:
  - schema-snapshot ~34,161 is partition total — **21,794** active after `GCRecord IS NULL` (reconcile imports on active count)
  - FM lines set **both** Employee + FamilyMember (2,759 rows) — Person FK must use Application.ForFamilyMember flag, not COALESCE
  - Legacy **WorkPermit** FK → Visa2026 **CurrentWorkPermitItem** (WorkPermitItem id-map, same Oid) — ApplicationItem ordered before WorkPermitItem in order.yaml
  - **NextVisa** not a column — 5,744 Visa rows link `ProcessNumber = PersonInApplication.Oid`
  - Parent ApplicationType **E:44** (187 item rows) / **E:55** (17 item rows) inherit header block
- **SQL / MCP that helped**: sqlcmd VISA2015 — INFORMATION_SCHEMA PersonInApplication; DISTINCT PurposeOfTravelL + CheckPoint mgCode
- **Fix / mapping change**: application-item-main table map, field-maps/ApplicationItem.yaml, layer 3 PurposeOfTravel + CheckPoint in lookup-translations.yaml
- **Prevent**: Dedupe 925 (Application+Person) groups before POST; omit ShowOptionalFields; gate FKs by ApplicationType Show* flags
- **Artifacts**: discovery/ApplicationItem.yaml, field-maps/ApplicationItem.yaml, table-mappings.yaml, entity-inventory.yaml, property-gap-registry.yaml

### 2026-06-29 — ApplicationType — E:44/E:55 approved skip_row

- **Phase**: mapping
- **Decision**: User approved skipping legacy composite keys `E:44:na:na:na` (92 apps, 187 items) and `E:55:na:na:na` (13 apps, 17 items) instead of blocking import.
- **Policy change**: `unmappedPolicy: skip_row` on ApplicationType catalog; `missingBehavior: skip_row` on Application field-map composite transform.
- **Counts**: 105 Application headers + 204 ApplicationItem rows skipped (items cascade with parent).
- **Not done**: `importConfirmed` left false — skip decision only; broader applicationWaveComplete gate still applies.
- **Artifacts**: lookup-translations.yaml#ApplicationType, field-maps/Application.yaml, lookup-comparisons/ApplicationType.md, lookup-review-queue.yaml, migration-status.yaml ISS-008

### 2026-06-29 — Application Excel preview export (calik-energi)

- **Phase**: excel-preview
- **Code**: `Visa2014ApplicationTransform.cs`, `Visa2014ApplicationPreviewExporter.cs`; wired in `Visa2014PreviewExportCommand` + `legacy-sources.yaml`.
- **SQL**: `dbo.Application` + `IRegistration_Data` + type/WP/urgency/visa/contract/border-zone/business-trip joins; `ŞäherEtrap` unicode table name; `GoşmaçaIşlemägeRugsatÝeri` movement-permit FK.
- **Transform**: ManualApplicationNumber → prefix/number; ApplicationType composite `{E|F}:{subtype}:{invWp}:{wizaWp}:{changeInfo}`; dedupe groups in `_DedupeSummary` with `keep_all_import_with_oid_upsert` (no duplicate_merged).
- **Export**: `Application-preview.calik-energi.xlsx` — **12237** legacy, **12129** import, **108** skipped (105 E:44/E:55 + 3 required-null), **862** dedupe groups, **0** duplicate_merged.
- **Next**: human review skipped sheet; then ApplicationItem preview export.

### 2026-06-29 — ApplicationProgress preview reviewed; importConfirmed

- **Phase**: excel-preview sign-off
- **Decision**: Developer approved simple/long synthesis in `ApplicationProgress-preview.calik-energi.xlsx` (32,177 rows / 108 parent skips).
- **Gate**: `importConfirmed: true` on discovery + order.yaml; OData implementation still after Application id-map.

### 2026-06-29 — ApplicationProgress synthesis approved + Excel preview

- **Decision**: Developer approved synthesis matrix (simple vs long process steps).
- **Export**: `ApplicationProgress-preview.calik-energi.xlsx` — **12,237** legacy apps → **32,177** progress rows, **108** parent skips (E:44/E:55).
- **Code**: `Visa2014ApplicationProgressTransform.cs`, `Visa2014ApplicationProgressPreviewExporter.cs`.
- **Next**: preview review → importConfirmed; OData after Application id-map; transition validation TBD.

### 2026-06-29 — Application preview reviewed; importConfirmed

- **Phase**: excel-preview sign-off
- **Decision**: Developer approved `Application-preview.calik-energi.xlsx` (12,129 import / 108 skipped).
- **Mapping lock**: `IsManualEntry=true` for all import rows (not `!AutoRegistration`) — preserves legacy numbers on OData POST.
- **Gate**: `importConfirmed: true` on discovery/Application.yaml + order.yaml.
- **OData (2026-06-29)**: `Visa2014ApplicationODataImporter` — POST `IsManualEntry=true` + `FullApplicationNumber` only (omit `ApplicationNumber`/`AppNumberPrefix`) so `Application.OnSaving` copies legacy full number without company-format rebuild. Resolver: ApplicationType by Name, Urgency by Code, VisaPeriod by LocalizationKey, BorderZoneLocation first non-Ýok label from comma list.
- **OData full (2026-06-29)**: 12,120 posted + 9 resume-skipped, 0 failed, 108 transform-skipped; ~7 min; id-map 12,129 entries. Unblocks ApplicationProgress + ApplicationItem OData.


- **Phase**: mapping + data fix
- **Symptom / surprise**: Visa2026 Person.FullName (`FirstName MiddleName LastName`) showed job titles in the middle
  (e.g. "Abdullah PROJECT MANAGER BAYSAL"). Root cause: legacy `dbo.Person.MiddleName` was used to store the
  employee's free-text **actual/company position** — VISA2014 had no dedicated field. Person.yaml mapped it 1:1 to
  Visa2026 Person.MiddleName.
- **User decisions**: (1) target = **current/latest** position-history row only (EndDate null / max StartDate);
  (2) scope = **employees only** (IsEmployee=true) — leave family members' MiddleName untouched;
  (3) employee with MiddleName but **no** EmployeePositionHistory row → **keep** MiddleName, report (nothing to attach).
- **Fix / mapping change**:
  - `Visa2014PersonTransform`: stop exporting MiddleName → Person.MiddleName; keep `_legacy_MiddleName` audit column.
  - `Visa2014PersonODataImporter`: removed MiddleName from POST payload.
  - `Visa2014EmployeePositionHistoryTransform`: extract `p.MiddleName`; on the current/latest row per person set
    ActualPosition = trim(MiddleName) when non-empty; else fall back to trim(Position.Code) or "-".
  - field-maps: Person.yaml MiddleName → propertyGaps.legacyOnly `relocate` → EmployeePositionHistory.ActualPosition;
    EmployeePositionHistory.yaml ActualPosition source updated.
- **Existing-data cleanup (already imported)**: new CLI `--cleanup-visa2014-person-middlename`
  (`Visa2014PersonMiddleNameToActualPositionCleanup`) — OData only. For each employee with MiddleName: find current
  EmployeePositionHistory, resolve/create ActualPosition by Name, PATCH it, then PATCH Person MiddleName="".
  `--dry-run` supported. PATCH clears MiddleName with **""** (JsonOptions ignores nulls → null would be omitted).
- **Prevent**: legacy "MiddleName"/name-ish columns may be repurposed free-text — verify sample values before 1:1 name mapping.
- **Artifacts**: field-maps/Person.yaml, field-maps/EmployeePositionHistory.yaml, Visa2014PersonTransform.cs,
  Visa2014PersonODataImporter.cs, Visa2014EmployeePositionHistoryTransform.cs,
  Visa2014PersonMiddleNameToActualPositionCleanup.cs, Program.cs

### 2026-06-29 — ApplicationItem Excel preview export

- **Phase**: excel-preview export
- **Export**: `ApplicationItem-preview.calik-energi.xlsx` — **21,794** legacy → **21,588** import / **206** skipped (204 parent E:44/E:55, 2 dedupe_duplicate); 925 dedupe groups from discovery dossier — only **2** groups on current VISA2015 attach.
- **SQL fixes**: bracket `dbo.[CheckPoint]` (reserved keyword); `OUTER APPLY TOP 1` for NextVisa (`Visa.ProcessNumber` has duplicate groups — naive JOIN inflated row count to 24,392).
- **Transform**: parent ApplicationType composite skip via `IsSkippedApplicationTypeComposite`; Person by ForEmployee/ForFamilyMember; (Application+Person) dedupe canonical lowest Oid → `_Skipped` `dedupe_duplicate`; WorkPermittedLocations null + `_audit_WorkPermittedLocations=pending_work_permit_location_audit`.
- **Code**: `Visa2014ApplicationItemTransform.cs`, `Visa2014ApplicationItemPreviewExporter.cs`, `Visa2014PreviewExportCommand.cs`, `Program.cs` help.
- **Next**: preview review → `importConfirmed`; OData after Application + Person + Passport + Visa id-maps.

### 2026-06-29 — ApplicationProgress seed suppression

- **Symptom**: Application OData POST auto-created `IS_BEING_PREPARED` @ `AT_OFFICE` progress rows via `OnCreated` → duplicate with synthetic ApplicationProgress import.
- **Fix**: `Application.SuppressInitialProgress` (hidden); `Visa2014ApplicationODataImporter` POST `SuppressInitialProgress=true`; `--cleanup-visa2014-application-progress-seeds` DELETEs initializer rows on Application id-map apps; `Visa2014ApplicationProgressODataImporter` removes seeds before POST + posts synthesized history.
- **Artifacts**: Application.cs, ApplicationProgressInitializer.cs, Visa2014ApplicationProgressSeedHelper.cs, Visa2014ApplicationProgressSeedCleanup.cs, Visa2014ApplicationProgressODataImporter.cs, Visa2014ODataLookupResolver (ApplicationState/Location by Code).

### 2026-06-30 — ApplicationMigrationServiceInference — excel preview

- **Phase**: excel-preview
- **Export path**: `preview-export/ApplicationMigrationServiceInference-preview.calik-energi.xlsx`
- **Scope**: `App_Reg_Check_In` (`E:2` / `F:2`) with null `DepartmentForRegistration` only — **58** legacy apps
- **Counts**: **58** total — confidence **high 7**, **medium 44**, **low 0**, **none 7** (no address / null region / DZ gap)
- **Artifacts**: `migration-service-inference.yaml`, `MigrationService-inference.md`, `Visa2014ApplicationMigrationServiceInferencePreview.cs`, `Visa2014MigrationServiceInferenceRules.cs`
- **Ready for PATCH**: **no** — `approvedForPatch: false`; review Excel first

### 2026-06-30 — ApplicationItem — OData importer

- **Phase**: import
- **Environment**: Visa2026DbDev (local OData https://localhost:5001)
- **Code**: `Visa2014ApplicationItemODataImporter.cs`, `Visa2014ODataLookupResolver.ResolveCheckPoint`, `Visa2014ImportCommand` ApplicationItem wave.
- **Transform**: reuses `Visa2014ApplicationItemTransform.PrepareImportBatch` (21,588 prepared / 206 skipped from preview).
- **POST rules**: required Application+Person+CurrentPassport id-maps; optional FKs allow_null on miss; PurposeOfTravel omitted; BorderZoneLocation string; nested BusinessTripAddress when city+address; CheckPoint OData NameTm.
- **order.yaml**: `importConfirmed: true` (developer, 2026-06-30).

### 2026-06-30 — ApplicationProgress — OData live import (calik-energi)

- **Phase**: import (live, not dry-run)
- **Environment**: https://localhost:5001 (HTTP 302), VISA2015 read-only via `VISA2014_SQL_PASSWORD` (User env; must set from User scope in Agent shells).
- **Built-in seed cleanup**: 8135 initializer rows removed before progress POST phase (do not run standalone seed cleanup CLI separately).
- **Counts**: prepared 32177; parent-skipped 108; posted **0**; failed **0**; skipped (already imported) **32177**; legacy applications 12237.
- **Id-map**: `Visa2026.DataImporter/legacy/visa2014/id-maps/calik-energi/ApplicationProgress.json` — **32177** entries.
- **Note**: Idempotent re-run — all rows already present from prior load; seed cleanup still ran on this pass.
- **order.yaml**: `importStatus: complete` with counts in notes.

### 2026-06-21 — On-prem IIS migration runbook + parallel period

- **Decision**: Officers **view/search only** in Visa2026 until cutover; legacy `VISA2015` on `10.100.128.15` remains system of record.
- **Hosts**: Visa2026 IIS `10.100.128.25` (Prod :80, Staging :8080, Demo :8081); legacy SQL `10.100.128.15`.
- **Sync**: One-way legacy → Visa2026 planned (nightly off-peak); safe because no officer writes in Visa2026 during parallel period. Full delta upsert (`--sync-visa2014`) not implemented yet — v1 catch-up is new-row id-map skip on some entities only.
- **Artifacts**: `docs/VISA2014_MIGRATION/ON_PREM_IIS_MIGRATION_RUNBOOK.md`, `import-strategy.yaml` `onPremDeployment`, `legacy-sources.yaml` profiles `calik-energi-onprem-{staging,prod,demo}`.

