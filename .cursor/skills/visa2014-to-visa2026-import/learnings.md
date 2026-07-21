# Learnings (append-only): visa2014-to-visa2026-import

**Purpose:** Record verified discovery, strategy decisions, mapping corrections, and OData import outcomes so **each session builds on the last**.

**Loop:** [MATURITY.md](./MATURITY.md) — **read `## Entries` before every task**; **append after every import attempt** (success, failure, or partial) and after verified discovery/strategy work.

### 2026-07-21 — ApplicationProgress partial reimport (raw ProcessNumber / InvitationNumber descriptions)

- **Phase**: partial-reimport (local PG, after `FormatLegacyDescriptionValue` — no `ProcessNumber:` / `InvitationNumber:` labels)
- **Prep**: PG cleanup (`cleanup-applicationprogress.sql`); clear `ApplicationProgress.json` id-map; DataImporter Debug build.
- **Result**: Posted **~30680** / Failed **0** / Skipped **18** (terminal progress log truncated at 8500/30764; exit 0 ~13 min). DB = **30696** `ApplicationProgresses`.
- **Shape**: `ProcessNumber:` prefix = **0**; `InvitationNumber:` prefix = **0**; `2_REVIEW_APPROVED` + `MinisteriesDocumentNumber:` = **3274**; `PROCESS_ISSUED` = **4340**.
- **Spot-check**: `7/-1206` → `PROCESS_STARTED` Description `CO0223973`, `PROCESS_ISSUED` `CO323977`.
- **Code**: `Visa2014ApplicationProgressTransform.FormatLegacyDescriptionValue` for started/issued; ministry refs still use `FormatLegacyRef` labels.

### 2026-07-21 — ApplicationProgress partial reimport (MinisteriesDocumentNumber → leg 2)

- **Phase**: partial-reimport (local PG, after `BuildLegApprovedDescription` shift)
- **Prep**: NULL `LatestProgressId`; DELETE manual-entry `ApplicationProgresses`; clear `ApplicationProgress.json` (+ sync-progress) source id-map; rebuild DataImporter if `runtimeconfig.json` missing after killing `dotnet`/`Visa2026.DataImporter`.
- **Result**: Posted **30692** / Failed **0** / Skipped (no Application map) **66** / Parent-skipped **167**. DB = **30692** rows. Exit 0 (~25 min).
- **Shape**: `1_REVIEW_APPROVED` with Description = **0**; `2_REVIEW_APPROVED` with `MinisteriesDocumentNumber:` = **3273**; `PROCESS_ISSUED` = **4339**.
- **Log**: `reimport-ApplicationProgress-localpg-20260721-110757.log`
- **PG cleanup SQL**: `artifacts/local-pg-import/cleanup-applicationprogress.sql` (use `C:\PostgreSQL\16\bin\psql.exe` — not in PATH). **Do not** run cleanup while import is in progress.

### 2026-07-21 — ApplicationProgress: MinisteriesDocumentNumber on leg 2 only

- **Mapping**: `MinisteriesDocumentNumber` → `2_REVIEW_APPROVED` Description (Energetika); leg 1 approved has no doc (ministry from `ApprovalLegProfile`). `DocNumberForwardedToMinConstruction` → leg 3 when present.
- **Code**: `Visa2014ApplicationProgressTransform.BuildLegApprovedDescription`; tests `SynthesizeSteps_LongProcess_MinisteriesDocumentNumberOnLeg2NotLeg1`, `SynthesizeSteps_ThreeLegs_ConstructionDocOnLeg3`.


### 2026-07-21 — ApplicationProgress: ProcessDate/ProcessNumber = processing start (not issued)
### 2026-07-21 — ApplicationProgress: PROCESS_ISSUED from Invitation / WorkPermit evidence
### 2026-07-21 — ApplicationProgress partial reimport (invitation/WP completion)

- **Phase**: partial-reimport (local PG, after completion-index)
- **Result**: Posted **30692** / Failed **0** / Skipped (no Application map) **62** / Parent-skipped **167**. Completion index: **4513** apps. DB = 30692 rows (`PROCESS_ISSUED` = 4339). Exit 0 (~19 min).
- **Log**: `reimport-ApplicationProgress-localpg-20260721-092326.log`


- **Phase**: mapping / transform
- **Rule**: Legacy app is complete when it has issued **ApplicationResult** (Invitation + PersonInInvitation) and/or **PersonInApplication.WorkPermit** → synthesize `PROCESS_ISSUED` with `InvitationNumber` / `WorkPermitNumber` + issued date. `ProcessDate`/`ProcessNumber` remain **PROCESS_STARTED** only.
- **Code**: `Visa2014ApplicationProgressCompletionIndex` + `SynthesizeSteps` completion branch; auto-loaded in `PrepareImportBatch`.
- **Reimport**: partial ApplicationProgress reimport after deploy to add issued rows.

- **Phase**: partial-reimport (local PG, after ProcessDate/ProcessNumber → PROCESS_STARTED fix)
- **Result**: Posted **26348** / Failed **0** / Skipped (no Application map) **60** / Parent-skipped **172**. DB = 26348 rows. Exit 0 (~15 min).
- **Shape**: **0** `PROCESS_ISSUED` rows (was ~12063 before); `PROCESS_STARTED` = 12110 with ProcessNumber on Description. Total rows ~26k vs ~38k prior reimport.
- **Log**: `reimport-ApplicationProgress-localpg-20260721-084658.log`


- **Phase**: mapping / transform correction
- **Decision**: Legacy `Application.ProcessDate` + `ProcessNumber` mark **migration-service processing start** (`PROCESS_STARTED`), not completion. `ProcessNumber` goes on the started step Description. Do **not** synthesize `PROCESS_ISSUED` from these columns.
- **Follow-up**: `PROCESS_ISSUED` will use a separate legacy completion source (TBD).
- **Code**: `Visa2014ApplicationProgressTransform.SynthesizeSteps` — removed `migration_issued` branch; tests updated (+ app `12/-7010` / AS538188 case).
- **Reimport**: partial ApplicationProgress reimport required after deploy to drop bogus issued rows from prior transform.


### 2026-07-21 — ApplicationProgress partial reimport (ProcessDate = start only)

- **Phase**: partial-reimport (local PG, after ProcessDate/ProcessNumber → PROCESS_STARTED fix)
- **Result**: Posted **26348** / Failed **0** / Skipped (no Application map) **60** / Parent-skipped **172**. DB = 26348 rows. Exit 0 (~15 min).
- **Shape**: **0** `PROCESS_ISSUED` rows (was ~12063 before); `PROCESS_STARTED` = 12110 with ProcessNumber on Description. Total rows ~26k vs ~38k prior reimport.
- **Log**: `reimport-ApplicationProgress-localpg-20260721-084658.log`
### 2026-07-20 — ApplicationProgress partial reimport (local PG, state-only model)

- **Phase**: partial-reimport
- **Mode**: single-entity `--entity ApplicationProgress` / `--legacy-source calik-energi-local-pg` / in-process Postgres target
- **Prep**: NULLed `Applications.LatestProgressId`; DELETE 45905 progress rows for `IsManualEntry` apps; cleared `ApplicationProgress.json` (+ sync-progress) under **source and bin** id-maps; copied `Application.json` from bin → source (source tree had stubs only).
- **Result**: Posted **38411** / Failed **0** / Skipped (no Application map) **120** / Parent-skipped **172** / Seeds removed **0**. DB `"ApplicationProgresses"` = 38411. Exit 0 (~24 min).
- **Log**: `legacy/visa2014/import-logs/reimport-ApplicationProgress-localpg-20260720-171707.log`
- **Shape**: transform no longer posts `IS_BEING_PREPARED`; first ministry step = `1_REVIEW_STARTED` (`leg_1_started`). Stock `reimport/ApplicationProgress.ps1` is LocalDB/`calik-energi` — use PG cleanup + `--legacy-source calik-energi-local-pg` for this PC.
- **Note**: `LocationID` column may still exist until Module schema updater runs (F5 / DB update); new rows do not depend on Location.


### 2026-07-20 — Local PG wipe + scalar reimport from .15 (REVIEW_STARTED cleanup)

- **Phase**: end-to-end (local PostgreSQL `visa2026`, source `10.100.128.15` / `VISA2015`, legacy-source `calik-energi-local-pg`)
- **Goal**: Full scalar reimport so `ApplicationProgress` regenerates without `_REVIEW_STARTED` / «N-NJI IŞ YLALAŞYKDA» rows (transform already updated).
- **Prep**: Stopped F5 Blazor; `Wipe-LocalPostgresTransactional.sql` (People/Apps → 0); **must clear id-maps under both** `Visa2026.DataImporter\legacy\...\id-maps\calik-energi-local-pg` **and** `bin\Debug\net8.0\legacy\...\id-maps\calik-energi-local-pg`.
- **Attempt 1 (failed)**: Cleared only source-tree id-maps → Passport Posted 2 / Skipped already-imported 3661; Visa Failed 4 (stale Passport GUIDs) + Skipped already-imported 6089. **Prevent**: always wipe **bin** id-maps on local PG reimport.
- **Attempt 2**: Cleared bin+source maps, rewipe, `Run-LocalPgScalarChain.ps1 -StartAt Person -SkipTenantCatalogGeneration` (tenant catalogs already regenerated from .15). Person 3316 / Passport 3663 / Visa 6093 Failed 0.
- **Education gap**: 1 fail — Specialty `Zähmeti goramak we howpsuzlyk` missing on target (exists on .15; catalog had only `… we tehniki howpsuzlyk` variants). Inserted Specialty row (`GCRecord=0`); appended to `specialty.calik-energi.json`. Resume `-StartAt Education` → Education catch-up Posted 1 Failed 0; EPH 3068; Salary 2961; AoR 5169.
- **In progress**: RunId `20260720-152656` — Application wave Running → WorkPermit…ApplicationProgress. Watch: `.\scripts\visa2014-migration\Watch-OnPremImportLive.ps1 -Profile Local -ClearScreen`. Logs: `artifacts/local-pg-import/chain-console-from15-resume-education2-20260720.log`.
- **Artifacts**: wipe SQL; LocalPgScalarChain; specialty JSON append.

### 2026-07-17 — Visa.VisaType collapsed to WP (LocalizationKey missing on in-process DTOs)

- **Phase**: import / correction
- **Symptom**: In-process Visa import set every `Visa.VisaType` to default WP because `MapLookupDto` omitted `LocalizationKey`.
- **Fix**: copy `LocalizationKey` in `MapLookupDto`; stop silent default fallback in `ResolveVisaType` / related resolvers; `EnsureVisaTypeLookupKeysLoaded` + prepared-row histogram guard; CLI `--correct-visa-type` backfills from legacy `TypeOfVisaL:mgCode`.
- **Artifacts**: `Visa2014VisaTypeCorrection.cs`, `Visa2014VisaODataImporter.cs`, `Visa2014ODataLookupResolver*.cs`, `Program.cs`.


### 2026-07-17 — Application.VisaType inferred from ApplicationType (no legacy FK)

- **Phase**: mapping
- **Dossier**: docs/VISA2014_MIGRATION/discovery/Application.yaml / field-maps/Application.yaml
- **Symptom**: Legacy `dbo.Application` has VisaPeriod/VisaCategory but **no VisaType**; local PG import had every Application.VisaType = WP (catalog IsDefault).
- **Inference map** (`Visa2014ApplicationVisaTypeInference`, LocalizationKey):
  - `App_Inv_And_WP` / `App_Visa_and_WP_Ext` / `App_Inv_According_to_WP` / `App_Visa_Ext_According_to_WP` → **WP** (`WP-Işçi Wiza`)
  - `App_Inv` → **BS1** (`BS1-İşerwürlik`)
  - `App_Inv_FM` / `App_Visa_Ext_FM` / `App_Visa_For_New_Born_FM` → **FM** (`FM-Maşgala`)
  - `App_Visa_Ext` / `App_Exit_Visa` → **EX** (`EX-Çykyş`) — user confirmed App_Visa_Ext = EX
  - `App_Sevice_Passport` → **OF** (`OF-Gulluk`)
- **Artifacts**: transform sets `VisaType`; OData importer posts FK; `Application.TryGetDefaultVisaLookupKeys` aligned; CLI `--correct-application-visa-type` for existing DBs; unit tests 15 passed.
- **Follow-up**: run correction on local PG after stopping F5 lock.

**Canonical plan:** [docs/VISA2014_MIGRATION.md](../../../docs/VISA2014_MIGRATION.md) · [IMPORT_PLAN_AND_STRATEGY.md](../../../docs/VISA2014_MIGRATION/IMPORT_PLAN_AND_STRATEGY.md)

**Not here:** Visa2026 seed scenarios — [visa2026-dataimporter](../visa2026-dataimporter/SKILL.md). **Import runbook:** [import-practices.md](./import-practices.md).

---

## When to append (required)

| Event | Append? |
|-------|---------|
| **Import run succeeds** (pilot, batch, e2e wave, partial reimport) | **Yes** |
| **Import run fails** (exit code, OData 400, build blocked import) | **Yes** |
| **Import partial** (some rows failed/skipped) | **Yes** |
| Correction CLI (`--correct-*`) | **Yes** |
| File/image wave | **Yes** |
| Discovery dossier closed (`complete` / `blocked` / `skip`) | **Yes** |
| Excel preview exported or reviewed | **Yes** (note path + row counts) |
| Strategy decision locked or plan approved | **Yes** |
| Verified mapping fix (no import yet) | **Yes** |
| New migration script created (last resort) | **Yes** | Why existing scripts/CLI were insufficient; README row added |
| Exploratory SQL with no conclusion | No |
| User asked read-only question | No |

**Failure entries are mandatory.** They prevent the next session from repeating the same mistake.

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

### Pilot / import run (success)

```markdown
### YYYY-MM-DD — <TargetODataEntity> — pilot | batch | e2e-wave

- **Phase**: import
- **Mode**: end-to-end | single-entity | partial-reimport | correction | file-wave
- **Outcome**: success
- **Environment**: local | staging | prod
- **Script / CLI**: e.g. scripts/visa2014-migration/import/OnPrem-Staging.ps1 or dotnet … --import-visa2014 --entity Person
- **Legacy source**: calik-energi | …
- **Counts**: legacy SQL __ → imported __ → target __
- **Skipped / dedupeMerged / failed**:
- **Reconciliation**: pass | partial (note)
- **Log**: legacy/visa2014/import-logs/…
- **Follow-up**:
```

### Import run (failed or partial)

```markdown
### YYYY-MM-DD — <TargetODataEntity> — import failed | partial

- **Phase**: import
- **Mode**: end-to-end | single-entity | partial-reimport | correction | file-wave
- **Outcome**: failed | partial
- **Environment**: local | staging | prod
- **Script / CLI**:
- **Exit code**:
- **Error** (snippet or OData message):
- **Counts** (if any): success __ / failed __ / skipped __
- **Root cause** (or hypothesis):
- **Fix / next step**:
- **Log**: …
- **migration-status issue** (if blocking): …
```

### Partial reimport (dev)

```markdown
### YYYY-MM-DD — <TargetODataEntity> — partial reimport

- **Phase**: import
- **Outcome**: success | failed | partial
- **Script**: scripts/visa2014-migration/reimport/<Entity>.ps1
- **Target DB**: (connection summary)
- **Steps run**: cleanup SQL | id-map rebuild | import | correction CLI
- **Counts / reconciliation**:
- **Mapping fix verified**: yes | no
- **Log**: legacy/visa2014/import-logs/reimport-…
- **Follow-up** (downstream BOs to re-run per order.yaml):
```

---

## Entries

> **Script paths (2026-07):** VISA2014 migration PowerShell/SQL moved from `scripts/local/` to **`scripts/visa2014-migration/`** — see [scripts/visa2014-migration/README.md](../../../scripts/visa2014-migration/README.md). Older entries below may still cite `scripts/local/…`; use the README index for current names.

### 2026-07-11 — Demo wipe-business + full reimport started

- **Phase**: end-to-end (on-prem Demo)
- **Mode**: end-to-end (`OnPrem-Sync.ps1 -Profile Demo -Mode Import`)
- **Outcome**: stopped on Passport fail; resumed after fix
- **Environment**: `10.100.128.25` / `Visa2026DbDemo` · `C:\visa2026-sync-demo` · task `Visa2026-OnPrem-DemoImportOnce`
- **Policy**: **do not** pass `-ContinueOnError` on Demo full import — one failed BO must stop the chain so we can fix and `-StartAt` resume
- **Passport fail**: 3642 posted / **5 failed** — legacy IssuedCountry **UAE** translated to Code `UAE` but Demo catalog only has **ARE** → `BuildPayload` null
- **Fix**: `lookup-translations.yaml` `UAE` → target **ARE**; resume `-StartAt Passport` (no ContinueOnError)
- **Side effect of ContinueOnError**: Visa completed; Education/EPH partially ran — resume skips via id-map
- **Watch**: `Watch-OnPremImportLive.ps1 -Profile Demo -ViaSsh -ClearScreen`
- **Cross-skill**: visa2026-onprem-legacy-sync

### 2026-07-10 — ApplicationProgress import live percent (Demo resume)

- **Phase**: end-to-end (on-prem Demo) — ApplicationProgress wave
- **Mode**: end-to-end (`-Profile Demo -Mode Import -StartAt ApplicationProgress`)
- **Outcome**: in progress (progress sidecar live)
- **Environment**: `Visa2026DbDemo` / `C:\visa2026-sync-demo` · RunId **20260710-052517**
- **Change**: Import path now writes `ApplicationProgress.sync-progress.json` every 100 rows with `processed/total/percent` + flushes stdout (same pattern as Sync upsert helper).
- **Counts**: **54985** prepared progress rows from **12354** legacy apps; early sample `100/54985 (0.2%)` posted=49 failed=51
- **Watch**: `Watch-OnPremImportLive.ps1 -Profile Demo -ViaSsh -ClearScreen` shows Wave progress bar from sidecar
- **Prevent**: Do not rely on redirected wave `.log` alone for mid-wave counts; redeploy DataImporter after progress-reporting changes
- **Cross-skill**: visa2026-onprem-legacy-sync

### 2026-07-10 — Full fresh Demo import started (Visa2026DbDemo / :8081)

- **Phase**: end-to-end (on-prem Demo)
- **Mode**: end-to-end (`OnPrem-Sync.ps1 -Profile Demo -Mode Import`)
- **Outcome**: in progress (Person completed; Passport running)
- **Environment**: `10.100.128.25` / `Visa2026DbDemo` · sync host `C:\visa2026-sync-demo` · legacy `10.100.128.15` / `VISA2015`
- **Script / CLI**: Scheduled Task `Visa2026-OnPrem-DemoImportOnce` → `Run-OnPremSyncOnServer.ps1 -Profile Demo -Mode Import -ContinueOnError` (SYSTEM; survives SSH)
- **Preflight**: Demo IIS upgraded **556 → 566**; wiped business rows (NULL `LatestProgressId` before delete); **no `-IncludeFileWaves`** (E: ~17 GB free; prod DB ~41 GB with files)
- **Blockers fixed this run**:
  1. Parallel prod DataImporter holds `:5002` → set `VISA2026_MIGRATION_IMPORT_URLS=http://127.0.0.1:5012` (not `ASPNETCORE_URLS`); `OnPrem-Sync.ps1` now auto-offsets Staging/Demo ports
  2. Fresh id-maps: `Visa2014IdMapHelper.Load` requires file → create empty `{}` stubs; orchestrator now stubs on `-Mode Import`
- **Counts so far**: People **3280** posted; Passport wave started; id-map `Person.json` ~272 KB
- **Watch**: `Watch-OnPremSyncRun.ps1` with `-SyncHostRoot C:\visa2026-sync-demo` (or SSH); UI `http://10.100.128.25:8081/LoginPage`
- **Prevent**: Do not clear id-map dir without leaving `{}` stubs; do not run two headless hosts on `:5002`; move Demo MDF to `E:\visa2026\sql-data\` before file waves
- **Cross-skill**: visa2026-onprem-legacy-sync | visa2026-windows-iis-deploy

### 2026-07-10 — Application ApprovalLegSnapshot backfill (Ministrlik) — tool added

- **Phase**: correction (CLI + patch script)
- **Mode**: correction
- **Outcome**: success (local + prod)
- **Environment**: local LocalDB `Visa2026`; prod `10.100.128.25\SQLEXPRESS` / `Visa2026DbProd`
- **Script / CLI**: `patch/Application-ApprovalLegSnapshots.ps1` → `--backfill-application-approval-leg-snapshots`; prod used SQL `cleanup/BackfillApplicationApprovalLegSnapshots.sql` (EF blocked by missing `BorderZoneLocation`)
- **Symptom**: Migrated via-ministry apps have progress rows but empty **Ministrlik** / status without `- Energetika` — `ApprovalLegSnapshots` never created at import.
- **Do not use**: `patch/ApplicationProgress-MinistryLegs.ps1` / `--correct-application-progress-ministry-legs` (deletes + regenerates progress).
- **Counts (local apply)**: scanned **4705** → needing **4701** → backfilled **4701**
- **Counts (prod apply)**: needing **4708** → inserted **9122** snapshots; **RemainingGaps=0**; active **9130**
- **Prevent**: Prefer snapshot backfill for label-only gaps; on schema-behind hosts use SQL patch; reserve ministry-legs patch for apps missing `*_REVIEW_*` progress rows.

### 2026-07-03 — ApplicationItem — reimport after PersonDomainDownstream FK wipe (success)

- **Phase**: partial-reimport (follow-up to Person-domain downstream cleanup that NULLed ApplicationItem FK columns)
- **Outcome**: success
- **Environment**: local — `(localdb)\mssqllocaldb` / `Visa2026`
- **Script**: `scripts/visa2014-migration/reimport/ApplicationItems.ps1` (`-Configuration Debug`)
- **Symptom before**: 21,345 ApplicationItems with `Person` only — all `CurrentPassport` / `CurrentVisa` / `CurrentPositionHistory` / `CurrentEducation` / `CurrentSalary` / `CurrentAddressOfResidence` NULL (caused by `ImportedPersonDomainChildren.sql` after Person-child reimport)
- **Import**: Prepared 21,588 / skipped 206 → **Posted 21,394** / failed **0** / id-map skip **194** (missing parent passport map)
- **Reconciliation (SQL)**: Total 21,394 — WithPassport **21,394**, WithVisa **15,294**, WithPosition **21,031**, WithEducation **12,495**, WithSalary **5,883**, WithAddress **9,142**
- **Log**: `legacy/visa2014/import-logs/reimport-ApplicationItem-20260703-121015.log`
- **Prevent**: After `reimport/PersonDomainDownstream.ps1`, always run `reimport/ApplicationItems.ps1` (or future FK repair command) — `--correct-application-item-person-current` alone does not restore Passport/Visa/Position/Address

### 2026-07-02 — ApplicationItem — partial reimport (success)

- **Phase**: import
- **Mode**: partial-reimport
- **Outcome**: success (with known skips)
- **Environment**: local — `(localdb)\mssqllocaldb` / `Visa2026`
- **Script**: `scripts/visa2014-migration/reimport/ApplicationItems.ps1` (`-Configuration Release`)
- **Legacy source**: calik-energi
- **Steps run**: cleanup SQL (21,345 deleted) → id-map rebuild → in-process import → PIA correction (started) → person-current correction (manual rerun)
- **Counts**: Prepared 21,588 / skipped 206 → **Posted 21,345** / failed 0 / id-map skip 243
- **Reconciliation**: `ApplicationItems` = 21,345; `CurrentEducation` populated 12,206; `CurrentSalary` populated 5,662 on manual-entry apps
- **Person-current correction**: 11,360 in scope; 0 updated (already set at import)
- **Log**: `legacy/visa2014/import-logs/reimport-ApplicationItem-20260702-172516.log`
- **Fixes this session**: `Get-RepoRoot.ps1` UTF-16 → UTF-8 (broke all `reimport/` scripts); `Visa2014TargetIdMapRebuild` `IdMapDirectory` + `AddressOfResidence` `_legacyRowId` string parse; `ApplicationItemPersonCurrentCorrection` `using DevExpress.ExpressApp`; cleanup SQL path `..\cleanup\ImportedApplicationItems.sql`
- **Follow-up**: PIA warnings for rows missing address id-map keys (3,290 address rows skipped at rebuild) — expected gap; ApplicationProgress unchanged (downstream BO not partial-reimported)

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

### 2026-06-30 — Headless XAF import (`--inprocess`)

- **Phase**: import implementation
- **Goal**: Speed Application / ApplicationItem loads by skipping OData HTTP per row while keeping XAF validation (`MigrationImportContext`, same rules as UI).
- **Architecture**: `HeadlessMigrationHost` in `Visa2026.Blazor.Server` boots `Program.CreateHostBuilder` without Kestrel; `Visa2014ObjectSpaceImportTarget` + `ObjectSpaceImportSink` apply payload dicts via `INonSecuredObjectSpaceFactory`; batch commit default 50.
- **CLI**: `--import-visa2014 --inprocess --entity Application|ApplicationItem --target-connection ...` optional `--batch-size`. Other entities remain OData-only for now.
- **Verified**: Debug build OK; dry-run Application 5 rows; live in-process 1 row on LocalDB `Visa2026` — headless host started, lookup catalogs loaded from ObjectSpace, idempotent skip (already imported).
- **Docs**: `import-practices.md` § Headless in-process; `ON_PREM_IIS_MIGRATION_RUNBOOK.md` staging example.
- **Next**: Benchmark full ApplicationItem (~21k) vs OData; extend `--inprocess` to remaining entities if needed.

### 2026-07-02 — Full clean re-migration via headless XAF host (all entities in-process)

- **Phase**: import (live, LocalDB `Visa2026`, `--inprocess` for the whole chain).
- **Goal**: User asked "all data should be imported using headless xaf host" — a full clean re-migration, not just Application/ApplicationItem.
- **Refactor**: All 8 person-domain + progress importers now write through `IVisa2014ImportTarget` (OData or ObjectSpace) + `Visa2014ODataLookupResolver`; command dispatch unified (no more per-entity in-process allow-list). `EmployeePositionHistory` calls `target.FlushAsync()` right after creating a new `ActualPosition` so the dependent row can reference it.
- **GCRecord gotcha**: Active rows in this schema are `GCRecord = 0`, **not** `NULL`. Verifying seed with `WHERE GCRecord IS NULL` falsely reports "0 active / all soft-deleted". Use `GCRecord = 0`.
- **Bug 1 — resolver subset**: `LoadFromObjectSpace` (in-process) only loaded the Application/ApplicationItem lookups. Passport failed 3585/3585 (`BuildPayload` null: no PassportType/Country) and Person silently dropped Gender/Country/Nationality/MaritalStatus FKs. Fix: mirror `LoadAsync` fully — load every lookup (Gender, Country, MaritalStatus, Relationship, PassportType, VisaType, VisaIssuedPlace, Subcontractor, Education*, Specialty, Position, Department, Region, ApplicationState/Location) plus custom maps for `BaseObject` types (`ActualPosition`, `Lodging`, `Hotel`, `Hospital`, `OtherSite` — City nav → CityId).
- **Bug 2 — GetId missing case**: `Visa2014ODataLookupResolver.GetId<T>` switch had no `ApprovalLegProfile` arm → `ResolveApprovalLegProfile` threw "Unsupported lookup type ApprovalLegProfile" for every application with an approval-leg code (5757 failed, both paths affected). Fix: add `ApprovalLegProfile alp => alp.Id`.
- **Re-run safety**: Person and Passport importers do **not** skip already-imported rows (re-running duplicates) → delete `dbo.People` + clear id-maps for a true clean start. All downstream importers (Visa, Education, EmployeePositionHistory, EmployeeSalary, AddressOfResidence, Application, ApplicationItem, ApplicationProgress) skip via their id-map, so a failed Application run can be re-run to retry only the failed rows.
- **sqlcmd**: `DELETE` against tables with filtered indexes needs `-I` (QUOTED_IDENTIFIER ON) or it errors 1934.
- **Final verified counts (calik-energi)**: People 2967 (Gender+Nationality 2967/2967), Passports 3573, Visas 5965, Educations 3101, EmployeePositionHistories 2993 (+1368 ActualPositions), EmployeeSalaries 2887, AddressesOfResidence 3968, Applications 12129 (ApprovalLegProfile 5757, ProjectContract 5750), ApplicationItems 21345, ApplicationProgresses 32177. 0 hard failures on final runs (only benign per-row data skips).
- **Orchestration**: `scripts/visa2014-migration/import/Run-HeadlessChain.ps1` runs the dependency-ordered chain in-process; tolerates minority row failures, hard-fails only on 0-posted/non-empty batches; `-StartAt <Entity>` to resume.

### 2026-07-02 — File/image waves on headless XAF (no OData)

- **Phase**: import implementation + docs
- **Goal**: User policy — all migration writes (scalar + file copies: photo, passport/visa/diploma scans, spid kepilnama) via headless ObjectSpace, not OData.
- **Code**: `IVisa2014ImportTarget.UpdateAsync`; file importers refactored to `Visa2014DocumentImportPayload.WithNestedFile` (aggregated `FileData` on `*Document`); `--import-visa2014-files` **requires** `--inprocess` (errors without it).
- **CLI examples**: `--import-visa2014-files --inprocess --entity Person --property Photo --target-connection ...`
- **Orchestration**: `Run-HeadlessChain.ps1` extended with file steps after each parent scalar BO; `OnPrem-Staging.ps1` headless-only for all entities.
- **Planned**: `FamilyProofDocument` → `PersonDocument` / `PersonFamilyRelationDocument` on same headless path (not implemented yet).
- **OData**: deprecated for migration writes; scalar `--import-visa2014` without `--inprocess` prints `WRN OData write path is deprecated`.

### 2026-07-03 — Person — PersonalNumber placeholder dedupe fix + partial reimport (calik-energi)

- **Phase**: import
- **Mode**: partial-reimport (person-domain wipe + full chain from Person)
- **Outcome**: Person success; downstream chain running
- **Environment**: local — `(localdb)\mssqllocaldb` / `Visa2026`
- **Mapping fix**: `---`, `...`, `----`, etc. normalize to `0`; sentinel dedupe on FirstName+LastName+DateOfBirth; `Person_IdentityUniqueWhenPersonalNumberIsSentinel` save rule
- **Cleanup**: `scripts/visa2014-migration/cleanup/ImportedPersonDomain.sql` (People + person-domain + manual Applications)
- **Script**: `scripts/visa2014-migration/reimport/Person.ps1` (new); `Run-HeadlessChain.ps1` fails on PS 5.1 (`??`) — ran Person via `dotnet exec` + downstream loop
- **Dry-run**: legacy 3241 → prepared **3222**, skipped 0, dedupe merged **19** (was 274)
- **Person import**: posted **3222**, failed **0**; id-map 3241 entries (+19 dedupe aliases)

### 2026-07-03 — Passport — PRT/ARE country gap + 12-row reimport (calik-energi)

- **Phase**: import (partial Passport resume)
- **Outcome**: success — **12 posted**, **0 failed**; Passports **3585** total (was 3573)
- **Root cause**: legacy `IssuedCountry` **PRT** (10) and **ARE** (2) missing from Visa2026 `Country` catalog (`UAE` exists; ISO **ARE** did not)
- **Fix**: `country.json` + `CountryLookupStrings.json` — added **PRT** (Portugaliýa) and **ARE** (same label as UAE); `manifest.json` version **8**
- **Importer**: `Visa2014PassportODataImporter` now skips rows already in Passport id-map and merges id-map on resume (like Visa)
- **DB note**: headless import did not run `LookupCatalogSyncUpdater` (stored `LookupCatalogManifestVersion` 37 ≫ manifest 8) — inserted PRT/ARE via SQL on LocalDB before reimport; greenfield/deploy should get rows from JSON sync after manifest bump or app restart
- **Reconciliation**: People **3222** (employees **2935**, family **287**); legacy employees 2950 → gap **15** (real-PN duplicate pairs only; was 2686)
- **Log**: `artifacts/headless-import/Person-reimport.log`, `downstream-reimport.log`
- **Follow-up**: restart Blazor for new Person validation rule; monitor downstream chain completion

### 2026-07-03 — Visa — resume import after Passport reimport (calik-energi)

- **Phase**: import (resume — no Visa cleanup; id-map skip for 5965 existing)
- **Outcome**: success — **10 posted**, **0 failed**; Visas **5975** total (was 5965); id-map **5975**
- **CLI**: `--import-visa2014 --entity Visa --legacy-source calik-energi --inprocess --no-build --target-connection (localdb)/Visa2026`
- **Dry-run**: 6016 prepared, 19 transform skip, 6 dedupe merged, **41** missing Passport id-map
- **Live**: skipped already-imported **5965**, skipped no Passport map **41**, posted **10**
- **Reconciliation**: legacy active **6041** → **66** still unmigrated (**41** passport id-map gap + **25** transform/dedupe skips)
- **Log**: `Visa2026.DataImporter/bin/Debug/net8.0/import_20260703_101659.log`
- **Next**: fix remaining **41** Passport id-map gaps (legacy passports not imported or dedupe aliases); optional `--import-visa2014-files` VisaDocument wave for new rows

### 2026-07-03 — Person-domain downstream partial reimport (calik-energi)

- **Phase**: partial-reimport (children only after Person reimport; keeps People + Person.json)
- **Outcome**: success — all 6 entities **0 failed**
- **Script**: `scripts/visa2014-migration/reimport/PersonDomainDownstream.ps1` + `cleanup/ImportedPersonDomainChildren.sql`
- **Cleanup**: cleared ApplicationItem person-current FKs (CurrentVisa/Passport/Education/Salary/PositionHistory/Address) before delete; did **not** delete Applications
- **Posted**: Passport **3585**, Visa **5975** (41 no Passport map), Education **3108**, EmployeePositionHistory **2993** (+1368 ActualPositions), EmployeeSalary **2887**, AddressOfResidence **5054** (includes **1086** PIA-inferred)
- **People unchanged**: **3241**
- **Follow-up**: re-run `--correct-application-item-person-current` and/or `reimport/ApplicationItems.ps1` to repopulate ApplicationItem person-current fields; optional file waves (PassportCopy, VisaDocument, EducationDocument)

### 2026-07-03 — ApplicationProgress synthesis: PROCESS_STARTED before PROCESS_ISSUED

- **Phase**: mapping fix (transform only; DB reimport pending)
- **Symptom**: Long-process apps (e.g. manual **3717**) jumped from ministry approval straight to **Issued - Migration service** — missing **In processing - Migration service**.
- **Root cause**: `Visa2014ApplicationProgressTransform.SynthesizeSteps` emitted single `migration_process` row (`PROCESS_ISSUED` @ `AT_MIGRATION_SERVICE`) with no preceding `PROCESS_STARTED` @ `AT_MIGRATION_SERVICE`.
- **Fix**: Split migration leg into `migration_started` (`PROCESS_STARTED`) + `migration_issued` (`PROCESS_ISSUED`); issued only when `ProcessDate`/`ProcessNumber` set; started also when ministry route complete without process date (no bogus issued row).
- **Tests**: `Visa2014ApplicationProgressTransformTests.cs` (xunit in DataImporter project).
- **Next**: dev partial reimport — delete imported `ApplicationProgress` rows + `--import-visa2014 --entity ApplicationProgress --inprocess` (~+12k rows for apps with process date).

### 2026-07-03 — ApplicationProgress dev reimport (calik-energi)

- **Phase**: partial-reimport (`reimport/ApplicationProgress.ps1` + `cleanup/ImportedApplicationProgress.sql`)
- **Cleanup**: deleted **39,742** progress rows; cleared `ApplicationProgress.json` id-map
- **Import**: **51,681** posted, **0** failed (~**+11,939** vs pre-fix); **11,979** `PROCESS_STARTED` @ `AT_MIGRATION_SERVICE`
- **Build note**: stop `Visa2026.Blazor.Server` if MSB3027 file-lock on `dotnet build`
- **Log**: `import_20260703_112131.log`
- **Edge case**: when legacy `ProcessDate` is before interpolated last ministry date, date sort can persist **Issued** before **In processing** — rare; follow-up clamp in transform if seen in UI

### 2026-07-03 — ApplicationProgress ministry-leg correction (profile-based leg count)

- **Phase**: patch (`patch/ApplicationProgress-MinistryLegs.ps1` + `--correct-application-progress-ministry-legs`)
- **Root cause**: `Visa2014ApplicationMinistryLegCountResolver` counted only empty snapshots; fallback `IsLongProcess` missed ~5.9k ViaMinistries apps with `ApprovalLegProfile` (legacy simple process).
- **Fix**: Resolver falls back to `ApprovalLegProfile.MinistryLegs` count; correction scopes apps missing `*_REVIEW_*` progress, backfills snapshots, prunes id-map prefixes, regenerates progress.
- **Outcome**: **3019** apps in scope; **0** ViaMinistries 2-leg profile apps still missing review steps; **6038** approval-leg snapshots on manual-entry apps.
- **Verify**: `7/-8308` now has `1_REVIEW_*` / `2_REVIEW_*` rows.
- **Follow-up**: step date ordering when `ProcessDate` precedes interpolated ministry slots.


- **Artifact**: `scripts/visa2014-migration/Compare-LegacyMigratedCounts.ps1` — legacy `VISA2015` vs LocalDB `Visa2026` BO row counts (`Legacy`, `Migrated`, `Gap`; `-ShowIdMap` optional)
- **Verified**: Person 3241/3241; Passport 3666/3585; Visa 6041/5975; Education 3133/3108; EPH 2993/2993; Salary 2950/2887; Address 4083/5054; Application 12237/12129; ApplicationItem 21794/21345; ApplicationProgress legacy apps 12237 vs 39742 progress rows

### 2026-07-03 — ApplicationItem CurrentAddressOfResidence correction (calik-energi)

- **Phase**: mapping fix + id-map rebuild + `--correct-person-address-of-residence` (live)
- **Symptom**: **12,252** imported ApplicationItems had Person/Passport/Visa/Position filled but **CurrentAddressOfResidence** empty; e.g. app **909** Milos Krcevinac (lodging) and Tanveer Alam (hotel).
- **Root cause**: `AddressOfResidence.json` id-map rebuild matched **PrivateHouse** only (`FullAddress` + `ExpirationDate`); legacy PIA lines mostly reference **Lodging/Hotel** via `pia.AddressOfResidence` FK — **3,290** legacy AOR OIDs skipped on rebuild. Importer omits FK when id-map miss (`TryAddOptionalFkFromMap`).
- **Fix**: `Visa2014AddressOfResidenceTargetMatcher` (all `ResidenceType` + site joins; `Type` stored as **int** enum in SQL); `Visa2014AddressOfResidenceIdMapAliasAppender` (PIA synthetic keys, direct `Address` OID aliases, sponsor canonical); `Visa2014ApplicationItemLegacyAddressResolver` creates missing `AddressOfResidence` on correct Person when legacy AOR exists but target row/id-map miss.
- **Id-map rebuild**: **3361** matched (+**1812** aliases appended); **607** skipped.
- **Correction live**: ApplicationItems in scope **21,394**; updated **12,248**; unchanged **9,142**; unresolved **4** (lookup gaps / unmappable legacy rows: apps **8424**, **3327**, **8780**, **7531**).
- **Verify SQL**: `CurrentAddressOfResidenceID` populated **21,390 / 21,394**; app **909** Milos + Tanveer both have lodging/hotel address.
- **Code**: `Visa2014TargetIdMapRebuild.cs`, new `Visa2014AddressOfResidence*.cs` helpers under `Visa2026.DataImporter/legacy/visa2014/`.
- **Next**: triage remaining **4** rows manually if UI-visible; consider dedupe-key lodging match in rebuild for near-miss catalog strings.


### 2026-07-03 — WorkPermit + WorkPermitItem — Çalik pilot (calik-energi)

- **Phase**: excel-preview + pilot import (LocalDB Visa2026)
- **Excel preview**: WorkPermit **401** import rows (399 letters + 2 orphan headers); WorkPermitItem **6363** rows, **0** skipped, **0** unmapped lookups
- **Catalog**: `work-permitted-location-name.json` expanded **8 → 30** distinct `NameTm` from preview (bit-matrix heuristic; fixed Şäheri suffix handling)
- **Pilot import**: WorkPermit **401/401** posted; WorkPermitItem **3750/6363** posted, **2613** skipped (EmployeePositionHistory not in id-map — legacy `WorkPermit.Position` = `WorkHistoryOfEmployee.Oid` but EPH import gap), **0** failed
- **Id-maps**: `id-maps/calik-energi/WorkPermit.json`, `WorkPermitItem.json`
- **Script**: `scripts/visa2014-migration/import/WorkPermits.ps1` (headers then items; builds DataImporter only — avoid Blazor F5 lock)
- **Order**: `order.yaml` — Application → WorkPermit → WorkPermitItem → ApplicationItem
- **Ready for importConfirmed**: no (human review Excel + 2613 EPH gap triage)
- **Next**: Invitation/InvitationItem discovery; ApplicationItem reimport with `--work-permit-item-id-map`; triage 2613 rows (reimport EPH subset or accept gap)
### 2026-07-03 — ApplicationItem reimport after WorkPermitItem wave (calik-energi)

- **Phase**: partial-reimport (`reimport/ApplicationItems.ps1` + `--work-permit-item-id-map`)
- **Script**: added `--work-permit-item-id-map` to `reimport/ApplicationItems.ps1` (was only on `import/ApplicationItems.ps1`)
- **Id-map rebuild**: WorkPermitItem **3750** matched, **2613** skipped (stale Position / no EPH)
- **Import**: **21,394** ApplicationItem posted, **0** failed
- **Verify (Visa2026)**: `CurrentWorkPermitItemID` populated **3,446** rows; legacy PIA with `WorkPermit` FK **3,802** → **356** gap (permit row not in WorkPermitItem id-map)
- **WorkPermittedLocations** on ApplicationItem: **34** (gated by `ApplicationType.ShowWorkPermittedLocations`; most types use item-level copy only when flag true)
- **Corrections**: PIA address + person-current ran after import (same script chain)
- **Next**: Invitation wave; optional WorkPermit.Application backfill; triage 2613 permit items / 356 application-line gaps

### 2026-07-03 — WorkPermitItem position FK fix (supplement EPH + fallback)

- **Root cause (2613 skips)**: legacy `WorkPermit.Position` → `WorkHistoryOfEmployee.Oid`; **2582** point at soft-deleted WH; **2523** also have soft-deleted employee (not recoverable). **~59** active employee + soft-deleted WH — fixable.
- **Fix shipped**:
  - `Visa2014EmployeePositionHistoryTransform.SupplementPermitReferencedExtractSql` + `--supplement-permit-positions` on EmployeePositionHistory import (appends soft-deleted WH referenced by active WorkPermit; active person only; `EndDate` null snapshot).
  - `Visa2014WorkPermitItemPositionResolver` — when position OID missing from id-map, pick nearest active WH for employee at permit `StartDate` (legacy GetLastPosition-style).
  - Patch script: `scripts/visa2014-migration/patch/WorkPermitItem-SupplementPositions.ps1` (supplement EPH then WorkPermitItem re-import; skips already in id-map).
- **Tests**: `Visa2014WorkPermitItemPositionResolverTests` (fallback date pick).
- **Next**: run patch on calik-energi pilot; re-run `reimport/ApplicationItems.ps1` if new WorkPermitItem rows posted; expect ~59–90 recovered items not full 2613.

### 2026-07-03 — WorkPermitItem supplement patch run (calik-energi pilot)

- **Patch**: `WorkPermitItem-SupplementPositions.ps1` on LocalDB Visa2026 + SQLEXPRESS VISA2015
- **Supplement EPH** (`--supplement-permit-positions`): **0** legacy rows (no soft-deleted WH with active person on this legacy DB for calik-energi)
- **WorkPermitItem re-import**: **47** posted via **position fallback**; **3750** already imported; **2566** still skipped (missing id-map); **0** failed
- **WorkPermitItem total**: **3797** in target + id-map rebuild match
- **ApplicationItem reimport**: **21,394** posted; id-map rebuild WorkPermitItem **3797** matched, **2566** skipped
- **Verify (Visa2026)**: `CurrentWorkPermitItemID` **3496** (was **3446**, +**50**); legacy PIA with `WorkPermit` FK **3725** on this SQL instance
- **Next**: triage remaining **2566** permit rows (mostly soft-deleted employee); Invitation wave

### 2026-07-03 — Invitation + InvitationItem wave (calik-energi pilot)

- **Legacy mapping**: header `ApplicationResult` (via `PersonInInvitation.Invitation` FK); item `PersonInInvitation` 1:1
- **Preview**: Invitation **2776** import / **185** skipped (missing dates/number); InvitationItem **5239** / **0** skipped
- **Pilot import** (LocalDB Visa2026): Invitation **2776/2776** posted; InvitationItem **4955/5239** posted, **284** skipped (missing Person/Passport/Invitation id-map), **0** failed
- **Target counts**: Invitations **2776**, InvitationItems **4955**
- **Code**: transforms/importers/preview + `scripts/visa2014-migration/import/Invitations.ps1`; `order.yaml` updated
- **Next**: triage 284 item skips; wire `CurrentInvitationItem` on ApplicationItem reimport; human `importConfirmed` after Excel review

### 2026-07-03 — ApplicationItem CurrentInvitationItem wiring (calik-energi)

- **Resolver**: SQL `OUTER APPLY` joins `PersonInApplication` → `PersonInInvitation` via `ApplicationResult.Application` + same Employee/FamilyMember; tie-break `ApplicationResult.IssuedDate DESC` (18 multi-match rows).
- **CLI**: `--invitation-item-id-map` on ApplicationItem import + `import/` / `reimport/ApplicationItems.ps1`.
- **Reimport**: **21,394** posted; `CurrentInvitationItemID` **4929** (was **0**); `CurrentWorkPermitItemID` **3496** (unchanged).
- **Gap**: legacy PIA with invitation match **5213** → **284** not in InvitationItem id-map (same skip set as invitation import).

### 2026-07-03 — Application id-map: FullApplicationNumber + ApplicationDate (no merge)

- **Root cause (`6/-909`)**: legacy has **two** active `Application` rows with same `ManualApplicationNumber` but different dates (2025-07-26 Tanveer, 2026-06-26 Milos). `--rebuild-visa2014-id-maps` matched **FullApplicationNumber only** → both legacy Oids mapped to one Visa2026 `Application` → ApplicationItems from both headers collapsed onto one parent.
- **Rule**: business identity = **FullApplicationNumber + ApplicationDate**; id-map upsert key = **legacy Application.Oid (GUID)**; ApplicationItem parent = legacy **Application** Oid via id-map (never number alone).
- **Fix shipped**: `Visa2014ApplicationTransform` identity helpers + rebuild SQL `FullApplicationNumber` AND `CAST(ApplicationDate AS date)`; collision guard on rebuild + ApplicationItem import abort; dedupe metadata keyed by number+date; tests in `Visa2014WorkPermitItemPositionResolverTests`.
- **Pilot repair**: rebuild `Application.json` id-map; reparent mis-linked ApplicationItems (e.g. Tanveer `AC7D8DDA…` → 2025-07-26 app `34AFE059…`).

### 2026-07-03 — Application id-map rebuild + ApplicationItem reparent (calik-energi pilot)

- **Backup**: `id-maps/calik-energi/Application.json.bak-collapsed` (pre-rebuild, number-only collapse).
- **Rebuild** (`--rebuild-visa2014-id-maps`, LocalDB Visa2026): Application **12129** matched, **0** skipped; cross-date collisions **0** after fix. Example `6/-909`: `f5616776…` → `C022D8D4…` (2026-06-26 Milos), `f538cb62…` → `34AFE059…` (2025-07-26 Tanveer).
- **Reparent** (`--correct-application-item-application-parent`): **21394** in scope, **1594** reparented, **19800** already correct, **0** errors.
- **Verify `6/-909`**: Tanveer item on `34AFE059…` (2025-07-26); Milos item on `C022D8D4…` (2026-06-26) — no longer merged.
- **CLI shipped**: `Visa2014ApplicationItemApplicationParentCorrection.cs` + `--correct-application-item-application-parent` in `Program.cs`; aborts if Application id-map still has cross-date collisions.

### 2026-07-03 — ApplicationItem CurrentWorkPermitItem person fallback

- **Rule**: when legacy `PersonInApplication.WorkPermit` is null but type has `ShowCurrentWorkPermitItem`, use latest `dbo.WorkPermit` per employee (`StartDateOfWorkPermit DESC`, `Oid DESC`) — same as `PersonCurrentItems.GetCurrentWorkPermitItem`.
- **Transform**: `Visa2014PersonCurrentFieldInference.BuildCurrentWorkPermitByPerson` + `TrySetApplicationItemPersonCurrentFields` (does not override explicit PIA FK).
- **Pilot correction** (`--correct-application-item-person-current`, LocalDB): **2057** `CurrentWorkPermitItem` backfilled (+ `WorkPermittedLocations` from item); **3496 → 5553** with permit FK (post-run count).
- **Note**: `6/-909` Milos/Tanveer still empty — no legacy `WorkPermit` rows for either person in VISA2015 (correct).

### 2026-07-03 — ApplicationType composite: SubType enum vs TypeOfApplication*ID

- **Root cause (`2/-291` Ismet Danış)**: importer used `TypeOfApplicationForEmployeeID` (internal seed ID) but legacy UI displays `TypeOfApplicationForEmployee` (`SubType` enum). Example: enum **9** = “Wizany täze pasporta geçirmek” (`App_Change_Passport`) but ID **10** was mapped to `App_Sevice_Passport`. ~6990 employee apps have enum ≠ ID.
- **Fix**: transforms (`Application`, `ApplicationItem`, `ApplicationProgress`) now read enum columns; skip composites `E:33` / `E:55`; added `E:21`/`E:22`/`F:21` mappings for cancel visa/WP enum values.
- **Pilot correction** (`--correct-application-type-composite`): **7933** retyped, **4136** already correct, **60** skipped. `2/-291` → `App_Change_Passport` (705).

### 2026-07-03 — Application id-map identity: number+date+ApplicationType (twin legacy apps)

- **Residual 129 type mismatches** after composite retype were **not** enum/ID bugs — **144 id-map entries** pointed multiple legacy `Application.Oid` values at one Visa2026 row (employee+family twins share `ManualApplicationNumber`+date; also same-type twins).
- **Identity key** extended to `FullApplicationNumber+ApplicationDate+ApplicationType` (`Visa2014ApplicationTransform.ApplicationImportIdentity`, target SQL joins `ApplicationTypes`).
- **Rebuild** (`Visa2014ApplicationIdMapRebuild`): greedy one-to-one target assignment + `ApplicationItem` parent overlap disambiguation; merge preserved prior id-map entries only when target slot still free (`MergePreservedApplicationIdMapEntries`).
- **Pilot** (calik-energi, LocalDB): id-map **11993** entries, **0** duplicate targets; `--correct-application-type-composite --dry-run` → **0** retypes, **11934** already correct, **59** skipped (unmapped composites).
- **Gap**: **~136** legacy apps dropped from id-map (`no target` / twin slot taken) — no matching `Applications` row for resolved type in Visa2026 (e.g. family `9/-3876` `App_Visa_Ext_FM` never imported). Needs missing Application OData import, not retype alone.

### 2026-07-04 — Full application-domain partial reimport (calik-energi)

- **Phase**: partial-reimport (dev chain after ApplicationType / id-map / progress fixes)
- **Environment**: LocalDB `Visa2026` + SQLEXPRESS `VISA2015`
- **Scripts** (in order): `reimport/Applications.ps1` → `import/WorkPermits.ps1` → `import/Invitations.ps1` → `reimport/ApplicationItems.ps1` → `reimport/ApplicationProgress.ps1`
- **Outcome**: success
- **Counts (target)**:

  | BO | Posted / in DB |
  |----|----------------|
  | Application | 12,069 |
  | ApplicationItem | 21,306 |
  | WorkPermit | 401 |
  | WorkPermitItem | 3,797 |
  | Invitation | 2,776 |
  | InvitationItem | 4,955 |
  | ApplicationProgress | 54,267 |

- **Verify `8/-967`**: `App_Reg_Check_Out` (direct migration) — **1** progress step (`IS_BEING_PREPARED` @ `AT_OFFICE`), **2** items, **0** direct-ministry review rows
- **Gotchas**:
  - `ImportedApplications.sql` matched `GCRecord IS NULL` only → deleted **0** rows while **36k** manual apps remained (`GCRecord = 0`) — fixed cleanup to `(GCRecord IS NULL OR GCRecord = 0)` + NULL ApplicationItem permit/invitation FKs before child delete
  - `Applications.ps1` pointed at wrong cleanup path (`reimport/ImportedApplications.sql`) — fixed to `../cleanup/ImportedApplications.sql`
  - After Application wipe, WorkPermit/Invitation imports posted **0** (“already imported”) until orphan BO rows + id-maps purged
  - WorkPermit `ApplicationID` still **null** on all headers — expected pilot (letter-synthesized headers; Application FK backfill deferred)
- **Officer sign-off**: legacy subtypes **E:33** (92) and **E:55** (13) confirmed **no migration** — remain `skip_row` in `lookup-translations.yaml`
- **Prevent**: After `Applications.ps1`, always run WorkPermit → Invitation → ApplicationItem → ApplicationProgress; document in [import-practices.md § Full application domain](./import-practices.md). Do **not** run `--correct-application-progress-ministry-legs` after direct-migration progress reimport.
- **Artifacts**: `cleanup/ImportedApplications.sql`, `reimport/Applications.ps1`, import-practices + scripts README + SKILL troubleshooting

### 2026-07-04 — Document copies wave (calik-energi, LocalDB)

- **Phase**: file-import
- **Environment**: LocalDB `Visa2026` + SQLEXPRESS `VISA2015` (`ReadOnlyUser` + `VISA2014_SQL_PASSWORD`)
- **Script**: `scripts/visa2014-migration/import/DocumentCopies.ps1` (also wired in `Run-HeadlessChain.ps1`)
- **Outcome**: success (FamilyProof required one fix — see below)
- **Counts (target after wave)**:

  | Wave | Posted / in DB | Skips (run log) |
  |------|----------------|-----------------|
  | Person.Photo | 3,170 | 71 no blob |
  | PassportDocument | 3,567 | 35 oversize, 12 no passport map, 1 no blob |
  | VisaDocument | 5,775 | 154 oversize, 66 no visa map, 46 no blob |
  | EducationDocument | 4,222 | 40 oversize, 16 duplicate blob, 10 no education map, 29 no blob |
  | WorkPermitDocument | 1,000 | 2 no blob, 5 already imported (pilot) |
  | InvitationDocument | 2,875 | 203 no parent map, 4 no blob |
  | FamilyProofDocument | 9 `PersonDocument` + 437 `PersonFamilyRelationDocument` | 1 oversize (>5MB), 3 duplicate blob |

- **Legacy mapping**:
  - Passport / Education / WorkPermit / Invitation scans → `dbo.PassportCopy` (implicit FK columns for WorkPermit letter + ApplicationResult)
  - Visa scan → inline `Visa.GöçürmeNusga`
  - Photo → `Person.Photo` varbinary
  - Family proof → `FamilyProofDocument.CopyOfDocument` **inline varbinary(max)** (not `FileData` FK)
- **FamilyProof fix**: initial SQL `CAST(CopyOfDocument AS varchar(36))` + `FileData` join caused SQL error 8152 — read blob per row from `CopyOfDocument` instead (`Visa2014FamilyProofDocumentImporter`)
- **New importers**: `Visa2014WorkPermitDocumentImporter`, `Visa2014InvitationDocumentImporter`, `Visa2014PassportCopyLinkedDocumentImporter`, `Visa2014FamilyProofDocumentImporter`, `Visa2014LegacyTableColumnResolver`
- **Gotcha**: Passport/Visa/Education re-run posted with `Already imported: 0` when id-map files were empty — target counts may exceed prior pilot if those maps were not saved earlier; id-maps now under `id-maps/calik-energi/*Document.json`
- **Gotcha**: `StrReplace` on `.cs` under DataImporter can save UTF-16 — convert to UTF-8 before `dotnet build` (bytes should start `117,115,105,110` = `using`)
- **Prevent**: Use `DocumentCopies.ps1 -StartAt FamilyProofDocument` only after fix; keep 5MB cap consistent with `DocumentBase` rules
- **Artifacts**: `DocumentCopies.ps1`, `Visa2014LegacyFileNameHelper` (work-permit / invitation / family-proof names)

## 2026-07-06 — ApplicationItem cancel flags fan-out + reimport (calik-energi)

- **Phase**: partial-reimport (`reimport/ApplicationItems.ps1` after `Visa2014ApplicationItemCancelledFlagsMapper` + `IsLineCancelled` on BO)
- **Script**: `scripts/visa2014-migration/reimport/ApplicationItems.ps1` (`-Configuration Debug`)
- **Outcome**: success
- **Import**: deleted 21,306 items; posted **21,306**; failed **0**; skipped missing id-map **194** (legacy 21,794 rows)
- **Cancel flags (target DB)**:

  | Column | Count |
  |--------|------:|
  | `IsCancelled` (WP) | 750 |
  | `InvitationItemIsCancelled` | 16 |
  | `VisaIsCancelled` | 3 |
  | Any line flag (OR) | **769** |
  | `RejectionIssued` | 9 |

- **Before fix**: all 769 cancelled lines landed on `IsCancelled` only; invitation/visa flags were 0
- **Mapper**: `Visa2014ApplicationItemCancelledFlagsMapper` — `App_Cancel_*` name heuristics + `Show*IsCancelled` catalog + fallback `IsCancelled`
- **UI**: computed `ApplicationItem.IsLineCancelled` (OR of type flags + `Application.IsCancelled`); column on nested + standalone ListViews
- **Gap vs legacy**: ~774 `PersonInApplication.Cancelled=1` in VISA2015 — 5-row delta expected from 194 skipped items / apps not in id-map
- **Post-corrections**: PersonAddressPia + ApplicationItemPersonCurrent ran; CurrentWorkPermitItem updated 71 on person-current pass
- **Prevent**: new UTF-16 on DataImporter `.cs` — write via PowerShell UTF-8 or verify bytes before build

### 2026-07-06 — P0 document `IsCancelled` backfill (Visa + WorkPermitItem transforms)

- **What**: `Visa2014LegacyDocumentCancellationIndex` loads `PersonInApplication` cancellation evidence and sets `IsCancelled` on **Visa** and **WorkPermitItem** import rows (not only `ApplicationItem` workflow flags).
- **Evidence**: `Cancelled=1` → `Visa2014ApplicationItemCancelledFlagsMapper.ResolveDocumentCancellation`; completed cancel subtypes **12 / 21 / 22** → direct visa/WP mapping; merged per linked `Visa` / `WorkPermit` OID.
- **Files**: `Visa2014LegacyDocumentCancellationIndex.cs`, refactored mapper, `Visa2014VisaTransform`, `Visa2014WorkPermitItemTransform`, OData payload for WP item; field-map notes in `Visa.yaml` / `WorkPermitItem.yaml`.
- **Tests**: 12 passed (`Visa2014LegacyDocumentCancellationResolverTests` + mapper tests).
- **Not done**: idempotent OData PATCH backfill for already-imported rows; `InvitationItem` already had `ApplicationResult.Result==1`.
- **Reimport**: run `WorkPermitItem` + `Visa` waves (or targeted reimport scripts) after deploy; `ApplicationItem` workflow flags unchanged.

### 2026-07-06 — Dev reimport Visa + WorkPermitItem cancellation (`VisaWorkPermitCancellation.ps1`)

- **Script**: `scripts/visa2014-migration/reimport/VisaWorkPermitCancellation.ps1 -Configuration Debug`
- **Cleanup**: `cleanup/ImportedVisaWorkPermitCancellationBackfill.sql` — null `ApplicationItems` visa/WP FKs; delete all `Visas` (+ docs) and `WorkPermitItems`; keep `WorkPermit` headers
- **Import waves** (all **0 failed**):

  | Entity | Posted | Skipped (id-map) | Notes |
  |--------|-------:|-----------------:|-------|
  | Visa | 5975 | 41 (no Passport map) | log: `import-logs/reimport-Visa-cancellation-*.log` |
  | WorkPermitItem | 3797 | 2566 | 47 position fallback |
  | ApplicationItem | 21306 | 194 | relink `CurrentVisa` / `CurrentWorkPermitItem` |

- **Cancellation counts (target `Visa2026` after import)**:

  | Layer | Count |
  |-------|------:|
  | `Visas.IsCancelled` | **685** |
  | `WorkPermitItems.IsCancelled` | **634** |
  | `ApplicationItems.IsCancelled` (WP line) | **750** (unchanged — workflow flags) |
  | `ApplicationItems.VisaIsCancelled` | **3** |
  | `ApplicationItems` with `CurrentVisaId` | 15208 |
  | `ApplicationItems` with `CurrentWorkPermitItemID` | 3580 |

- **Outcome**: **partial success** — all three OData imports completed; script exit **1** on post-import corrections (`ApplicationItems.ps1` → `--correct-person-address-of-residence`) with `hostpolicy.dll` / missing `Visa2026.DataImporter.runtimeconfig.json` after ~37 min run (likely file lock / stale `--no-build` output). Cancellation backfill itself is done; re-run corrections when build is clean: `ApplicationItems.ps1 -SkipCorrections` not needed — run correction flags only via DataImporter after `dotnet build`.
- **Before reimport (stale)**: 863 visa + 195 WP item cancelled (pre-index logic / partial state).

### 2026-07-06 — P0.5 invitation document `IsCancelled` backfill (InvitationItem)

- **What**: `Visa2014LegacyInvitationItemCancellationIndex` — `ApplicationResult.Result == 1` **plus** `PersonInApplication.Cancelled` on cancel-invitation apps matched to `PersonInInvitation` (same OUTER APPLY as ApplicationItem `CurrentInvitationItem` resolver).
- **Files**: `Visa2014LegacyInvitationItemCancellationIndex.cs`, `Visa2014InvitationItemTransform`, `field-maps/InvitationItem.yaml`, tests in `Visa2014LegacyInvitationItemCancellationResolverTests.cs`.
- **Reimport**: `scripts/visa2014-migration/reimport/InvitationCancellation.ps1` + `cleanup/ImportedInvitationItemCancellationBackfill.sql` (InvitationItems delete + ApplicationItem FK relink via `ApplicationItems.ps1`).
- **Before fix (dev)**: `InvitationItems.IsCancelled` **0** vs `ApplicationItems.InvitationItemIsCancelled` **16**.

### 2026-07-06 — Dev reimport InvitationItem cancellation (`InvitationCancellation.ps1`)

- **Script**: `scripts/visa2014-migration/reimport/InvitationCancellation.ps1 -Configuration Debug` — exit **0** (~20 min).
- **Import waves** (all **0 failed**):

  | Entity | Posted | Skipped (id-map) |
  |--------|-------:|-----------------:|
  | InvitationItem | 4955 | 284 |
  | ApplicationItem | 21306 | 194 |

- **Cancellation counts (target `Visa2026` after import)**:

  | Layer | Count | Notes |
  |-------|------:|-------|
  | `InvitationItems.IsCancelled` | **6** | was **0** |
  | `ApplicationItems.InvitationItemIsCancelled` | **16** | unchanged (workflow mirror) |
  | App items with flag **and** `CurrentInvitationItemID` | **6** | all linked docs `IsCancelled=1` |
  | App items with flag **without** `CurrentInvitationItemID` | **10** | cancel-invitation lines with no resolvable `PersonInInvitation` match |

- **Index (dry-run verbose)**: `Legacy invitation-item cancellation index: 260` PersonInInvitation OIDs — but **all 254** legacy rows with `ApplicationResult.Result = 1` are **absent** from `InvitationItem.json` (not imported; invitation header id-map gap). Verified cancelled sample `0328c30b-…` has **Result = 0** and `PersonInApplication.Cancelled = 1` — the **6** dev rows come from the **PIA cancel path**, not `Result == 1`.
- **Follow-up**: revalidate `ApplicationResult.Result == 1 → IsCancelled` heuristic (likely not cancellation); consider dropping or replacing that path in `Visa2014LegacyInvitationItemCancellationIndex` so index count matches importable rows. `InvitationItem` status distribution: 4897 none / 52 `IsChanged` / 6 `IsCancelled`.
- **UTF-8**: rewrite `.ps1` / `.sql` with `[System.IO.File]::WriteAllText(..., UTF8Encoding(false))` if Cursor `Write` corrupts encoding (parse error on first run).


### 2026-07-11 — Demo Import: stop-on-failure; Passport UAE; Education gaps

- **Stop-on-failure**: OnPrem-Sync default (no `-ContinueOnError`) confirmed on Demo — Education fail exit 1 halted chain before EmployeePositionHistory.
- **Passport**: calik Country overlay replaced base values[]; UAE identity-passed → ResolveCountry miss (ARE only). Merge Load() + explicit UAE→ARE in calik overlay. Resume Posted 5 / Failed 0.
- **Education**: 47 incomplete payloads — EducationInstitution / Specialty NameTm not in Demo tenant catalogs (encoding-sensitive labels). Fix catalogs or allow_null policy before `-StartAt Education`.

### 2026-07-11 — Zero FailedCount; Education institution/specialty seed catch-up

- **Rule**: no tolerable FailedCount unless deliberate exclusion (skipped, not failed). Documented in import-practices §7b + onprem-legacy-sync hard rule 8.
- **Education Demo**: missing 47 institution + related specialty labels vs live `.15`; SQL seed + refreshed calik-energi JSON (1500/1083). Resume Posted 47 / Failed 0.

### 2026-07-11 — Intentional exclusions require approval + registry

- **Rule**: skips only via `docs/VISA2014_MIGRATION/import-exclusions.yaml` (`status: approved`) with why, counts, approvedBy/At. FailedCount is never an exclusion.
- **Seeded**: EXC-APPTYPE-E33-E55 (105 apps / 204 items), EXC-VISA-ISSUEDPLACE-EMBASSY (18 visas).
- **Docs**: import-practices §7c, onprem hard rule 8, SKILL link, import-strategy pointer.

### 2026-07-11 — Demo AddressOfResidence diagnosis

- **Initial**: Posted 0 / Failed 5122. Gaps reported as City=...
- **Root cause 1**: `Visa2014CityLookupMatcher` requires Region match; ObjectSpace loader left `City.Region`/`RegionName` empty (Demo `RegionName` all null). Fixed ObjectSpace Region load + name-only fallback when no city has region metadata.
- **Root cause 2**: Demo `Lodgings` count was **0** after wipe (tenant lodging catalog not re-synced). `ForceUpdate` Demo -> Lodgings **76**.
- **After fix**: Posted **3069** + PIA **970**; Failed **80** (City/Lodging gaps: Serdarabat, Beýik Saparmyrat…, Serhetabat + a few lodging/other-site scalars). Chain stopped (no ContinueOnError).
- **Next**: align remaining city region links / lodging FullAddress normalize for ~80 rows; write full error list (not Take(10)).

### 2026-07-11 — AoR geography policy (b): prefer legacy Region when Wiki/OSM agrees

- **Decision**: City.Region catalog aligned to Wikipedia/OSM; import prefers legacy Region+City when that pair matches; if legacy Region is wrong but city name is unique among region-linked rows, use catalog City.Region.
- **city.json**: Serhetabat etraby → Mary; Serdarabat etraby → Lebap; Beýik Saparmyrat… → Lebap (historical Beýik district).
- **Demo SQL**: in-place RegionID updates for those cities; filled 3 Lodging.CityID nulls (Watan/Parahat/Çemenabat).
- **Code**: `Visa2014CityLookupMatcher` unique region-linked fallback; ImportApplier sets Region from City after resolve; gap exporter CLI `--export-visa2014-import-gaps`.
- **Result**: import-gap preview **80 → 9** remaining (mostly lodging/other-site scalar still unresolved after city fix).

- **Follow-up**: unique Region-FK fallback (ignore null-Region orphan + RegionName enrich duplicates) → gap preview **0**; would-post 67 legacy + 14 PIA. Resume Demo -StartAt AddressOfResidence after deploying updated DataImporter to sync host.

### 2026-07-11 — Turkmenistan geography reference SQLite DB

- **Path**: `Visa2026.DataImporter/legacy/visa2014/reference/turkmenistan-geography.db` (6 regions, ~89 cities + aliases).
- **Seed**: `region.json` + `city.json` + `geography-overrides.json` (Wiki/OSM conflict cities).
- **Rebuild**: `--rebuild-visa2014-geography-db`.
- **Import**: AddressOfResidence uses store policy (b) — keep legacy Region when it matches DB; else use DB Region for city name.

### 2026-07-11 — Mandatory lookup preflight before full Import

- **Gate**: `--preflight-visa2014-lookups` (Phase A catalog sampleQuery + Phase B entity transforms + optional target DB key check).
- **Orchestrator**: `OnPrem-Sync.ps1 -Mode Import` runs preflight automatically; `-SkipLookupPreflight` only for approved exceptions; `-LookupPreflight` enables it for Sync.
- **Wrapper**: `scripts/visa2014-migration/import/Preflight-LookupAudit.ps1`.
- **Why**: live lookup drift (Education institutions, City/Region from FullAddress) must be audited → translated → seeded before Import, not discovered mid-wave as FailedCount.

### 2026-07-11 — Skill: Full Import order = lookup resolution → preflight → Import

- **Named process**: **lookup resolution** (audit → translate → seed LookupCatalogs/tenant JSON), then **lookup preflight**, then full Import.
- **Updated**: visa2014-to-visa2026-import `SKILL.md` (§ Full Import order), `import-practices.md`; onprem-legacy-sync hard rule 9 + preflight table.
- **Calik**: base + `lookup-translations.calik-energi.yaml`.

### 2026-07-11 — user-prompts for lookup resolution / preflight

- Added `visa2014-to-visa2026-import/user-prompts.md` (resolution, preflight, Calik, full Import order).
- Linked from import `SKILL.md`; Demo/lookup openers added to onprem `user-prompts.md`.

### 2026-07-11 — Demo lookup preflight (calik-energi-onprem-demo)

- **Run**: `.25` `C:\visa2026-sync-demo`; report `lookup-preflight-demo-20260710-232046.json`.
- **Result**: FAILED — Blocking=30 Allowed=47 TargetCatalogs=0 (target key load still broken).
- **Gaps**: CityByName ~12 distinct (Atamyrat, Baharly, Balkanabat, Beyik…, Dowletli, Hojambaz, Serdar, Serdarabat, Tejen, Turkmenabat, Turkmenbasy) on Application/ApplicationItem; City (null) x2; Region free-text x3; CheckPoint sampleQuery syntax.
- **Next**: lookup resolution for CityByName + fix CheckPoint query / target key loader before full Import.

### 2026-07-11 — Obsolete visa2026-onprem-legacy-sync; sole migration skill

- **Decision**: stop using `@visa2026-onprem-legacy-sync`. All data migration (lookup resolution, preflight, Demo/Prod Import on .25) is `@visa2014-to-visa2026-import` only.
- **Obsolete skill**: `disable-model-invocation: true`; stub points here; learnings/reference kept as archive.
- **Updated**: AGENTS.md, ON_PREM runbook, on-prem-deploy MATURITY, import SKILL + user-prompts (Demo/Prod section).

### 2026-07-11 � Removed delta Sync (keep Import)

- **Removed**: `--sync-visa2014` / `--sync-full` / `--sync-since` / `--sync-state-dir` / `--no-soft-delete-sync`; `Visa2014SyncCommand` + StateStore/IdMapLoader/RowFilter; `RunSyncAsync` on OData importers; soft-delete sync query; Sync-only PS1s (Register task, Compare/Export/Watch SyncState, OnPremSyncState lib); `LegacySyncDashboard` (Module + Blazor); skill folder `visa2026-onprem-legacy-sync`.
- **Kept**: `OnPrem-Sync.ps1` Import-only; `--import-visa2014`; lookup preflight; `Visa2014SyncPayloadFkHelper`; Import progress sidecars on `Visa2014SyncUpsertHelper`; host roots `C:\visa2026-sync*`.
- **Ops**: disable Task Scheduler `Visa2026-OnPrem-LegacySync` on `.25` manually if still registered.

### 2026-07-13 � Demo hard wipe + reimport blocked by lookup preflight

- **Phase**: end-to-end (on-prem Demo wipe + Import)
- **Outcome**: wipe/seed success; Import **not started** (preflight exit 2)
- **Environment**: `10.100.128.25` / `Visa2026DbDemo` � sync host `C:\visa2026-sync-demo` � legacy `10.100.128.15` / `VISA2015`
- **Wipe**: DROP+CREATE `Visa2026DbDemo` (sqlcmd `-E -C`); cleared id-maps + `{}` stubs; `Run-Visa2026DbUpdateOnServer.ps1 -Profile Demo -ForceUpdate`; LoginPage HTTP 200; redeployed DataImporter (post delta-Sync removal)
- **Import**: `Run-OnPremSyncOnServer.ps1 -Profile Demo` stopped at lookup preflight � **30 blocking** gaps
- **Blockers**: CheckPoint sampleQuery syntax (`CheckPoint` reserved); CityByName ~12 cities on Application/ApplicationItem (encoding/normalize); City `(null)` x2; Region free-text x3 (AoR/Lodging)
- **Person..EPH Phase B**: OK in preflight
- **Next**: lookup resolution for CityByName (+ fix CheckPoint brackets on live YAML) then re-run preflight; or user-approved `-SkipLookupPreflight` (Application waves will still hit CityByName)

### 2026-07-13 � Demo preflight fixed (CityByName + CheckPoint); Import started

- **Phase**: end-to-end (Demo after hard wipe)
- **Outcome**: preflight **exit 0**; Import **Running** (RunId `20260712-205532`, wave Person)
- **Fixes**:
  1. CheckPoint sampleQuery: `pia.[CheckPoint]` (reserved word)
  2. CityByName: `identityPassThrough` + city.json NameTm identity enrich in `Visa2014LookupTranslator.Load` + fold-match targets; Application city aliases in YAML
  3. City/Region: `unmappedPolicy: allow_null` for skip-row free-text/null city gaps (still skipped at import)
  4. City sampleQuery keeps `dbo.[S�herEtrap]` (ASCII `SeherEtrap` is invalid on live VISA2015)
- **Deploy**: republished DI + YAML + `LookupCatalogs/city.json` to `C:\visa2026-sync-demo`
- **Watch**: `Watch-OnPremImportLive.ps1 -Profile Demo -ViaSsh`; log `logs\sync-run-20260712-205532.log` / wrap `demo-import-wrap-20260712-205531`

### 2026-07-13 — Demo Import restart after orphaned Person wave

- **Phase**: end-to-end (Demo Import restart)
- **Outcome**: **success** (restart); prior run 20260712-205532 was **dead/orphaned**, not slow
- **Environment**: 10.100.128.25 / Visa2026DbDemo · sync host C:\visa2026-sync-demo · legacy 10.100.128.15 / VISA2015
- **Prior failure**: RunId 20260712-205532 — DI logged headless host on :5012 then stopped (~17s); no Progress lines; People=0; status stuck Overall=Running; DataImporter: alive=False; sync-run log never written. Watch elapsed time was stale status, not slow Person.
- **Restart**: marked stale status Failed; launched wrap via `Win32_Process.Create` (survives SSH); `Run-OnPremSyncOnServer.ps1 -Profile Demo -SkipTenantCatalogGeneration -SkipLookupPreflight`
- **New run**: RunId `20260712-211301` — Person **Posted 3303 / Failed 0** (~1 min); People=3303; advanced to Passport; DI alive
- **Ops tip**: prefer `Win32_Process.Create` or equivalent detach for Demo Import on .25; do not trust Watch `Running` alone — check `alive` + People delta + Progress lines
- **Watch**: `Watch-OnPremImportLive.ps1 -Profile Demo -ViaSsh`; wrap `logs\demo-import-wrap-20260712-211259.log`; sync-run `logs\sync-run-20260712-211300.log`


### 2026-07-13 — Demo Education Failed (54 incomplete payload); catalogs gap; resume OK

- **Phase**: end-to-end (Demo Import after wipe)
- **Outcome**: Education **Failed** exit 1 (54 incomplete Institution/Specialty); after catalog refresh **Completed** Failed=0 (Posted 54 catch-up)
- **Environment**: `Visa2026DbDemo` / live `10.100.128.15` VISA2015
- **Symptom**: Person/Passport/Visa OK; Education 3115 posted / 54 failed; Overall Failed (stop-on-failure)
- **Cause**: live Education labels ahead of Demo tenant catalogs (~52 Institution + ~32 Specialty exact gaps). Preflight Education `UnmappedLookupCount=0` because `identityPassThrough` — does not prove ObjectSpace catalog rows exist.
- **Fix**: regenerated `education-institution*.json` / `specialty*.json` from live DISTINCT (~1507 / ~1085); tenant manifest **37→38**; disk overlay `C:\inetpub\visa2026-demo\LookupCatalogs\tenant\` + `Run-Visa2026DbUpdateOnServer.ps1 -Profile Demo -ForceUpdate`; resume `-StartAt Education`
- **Resume**: RunId `20260712-212802` — Education Posted 54 / already 3115 / Failed 0; Educations=3169; continued to EPH then AddressOfResidence
- **Watch**: `Watch-OnPremImportLive.ps1 -Profile Demo -ViaSsh`


### 2026-07-13 — Import reimport history archive + compare dashboard

- **Phase**: tooling (on-prem Import ops)
- **Outcome**: shipped archive + HTML index + compare CLI
- **Artifacts**:
  - `scripts/visa2014-migration/_lib/OnPremImportRunArchive.ps1`
  - `Archive-OnPremImportRun.ps1` / `Compare-OnPremImportRuns.ps1`
  - `OnPrem-Sync.ps1` archives on Complete / wave-fail / preflight-fail
  - Dashboard: `<SyncHostRoot>\history\index.html`; runs under `history\runs\<RunId>\`
- **Demo**: archived RunId `20260712-212802` (current completed reimport); need a second archive before compare is meaningful
- **Note**: Import-only (hard-delete reimport friendly); not delta Sync


### 2026-07-13 ? Import reimport history in XAF Navigation (Administrators)

- **Phase**: tooling (on-prem Import ops UI)
- **Outcome**: Operations ? Import reimport history (non-persistent host + Blazor editor)
- **Data**: reads `history\runs\<RunId>\*.json` via `ImportHistory:RootPath` (defaults from `DeploymentEnvironment:Slot`)
- **Security**: deny Users type/nav; Administrators via `IsAdministrative`
- **Ops**: app pool must read `C:\visa2026-sync*\history`; `Configure-Visa2026Production.ps1` writes ImportHistory.RootPath per slot


### 2026-07-13 - Archive file waves only after DocumentCopies
- Reimport history must be finalized after scalar corrections and optional `DocumentCopies.ps1`; archive `file-waves-status.json` plus target file-presence metrics, and force a failed archive before a non-continue file-wave exit.
### 2026-07-13 — Document copies on Import reimport history (Phase A + gap inventory)

- **Phase**: tooling (archive + XAF history UI)
- **Archive order**: scalar → postImportCorrections → optional DocumentCopies (`-IncludeFileWaves`) → Complete → Archive
- **Artifacts per RunId**: `file-waves.json` (Included + Steps), `file-presence.json` (Photo / *Document vs parents), `meta.FileWavesIncluded`
- **UI**: Operations → Import reimport history sections for file waves + file presence Left/Right
- **DocumentCopies.ps1 covered today**: Person.Photo, PassportDocument, VisaDocument, EducationDocument, WorkPermitDocument, InvitationDocument, FamilyProofDocument
- **Gap inventory (Phase B — not added yet)**: only add when `importConfirmed` + importer exists
  - MedicalRecordDocument (presence metric already; import step not in DocumentCopies.ps1)
  - RejectionDocument, BorderZoneDocument, AddressOfResidenceDocument, LodgingDocument, ProjectContractDocument, PersonFamilyRelationDocument
  - ApplicationProgress.MinistryLetterFile (FileData) — separate from DocumentCopies child docs
  - EducationDocument / VisaDocument tables may be missing on some Demo DBs (PresentCount null soft-fail) until BO/schema registered
- **Demo backfill**: RunId `20260712-212802` got `file-waves.json` Included=false + live `file-presence.json` (photos/docs mostly 0 without file waves)


### 2026-07-13 — Demo hard wipe + reimport with -IncludeFileWaves (started)

- **Phase**: end-to-end (Demo wipe + Import + DocumentCopies)
- **Outcome**: **started** (in progress)
- **Wipe**: business tables cleared (People/Apps/docs=0); lookups/templates kept; id-maps cleared under `C:\visa2026-sync-demo\data\id-maps\calik-energi-onprem-demo`
- **Import**: scheduled task `Visa2026-OnPrem-DemoImportFileWaves` → `Run-OnPremSyncOnServer.ps1 -Profile Demo -IncludeFileWaves -SkipTenantCatalogGeneration` (no ContinueOnError)
- **RunId**: `20260712-235158` Overall=Running; Person done (~3304 People); Passport Running; DI alive
- **Disk**: C ~18.6 GB free, E ~17.4 GB free — watch space during file waves (photos/scans)
- **Watch**: `Watch-OnPremImportLive.ps1 -Profile Demo -ViaSsh`; wrap `logs\demo-import-wrap-20260712-235156.log`; sync-run `logs\sync-run-20260712-235158.log`
- **Expect**: after scalar waves, DocumentCopies.ps1 then archive with `file-waves.json` Included=true + file-presence


### 2026-07-13 — Demo Education Failed (5) on IncludeFileWaves reimport; fixed + resumed

- **Run**: `20260712-235158` Failed at Education (Posted 3165 / Failed 5) — stop-on-failure
- **Cause**: missing catalog rows for composite labels
  - Institution: `Lizabon s. orta mekdep,CCVD-VCA okuw kursy`
  - Specialty (1 remaining after institution seed): exact legacy `Speciality.TitleOfSpeciality` via `Education.Spcialty` (Unicode İ/ş/ý/ç/ü)
- **Fix**: INSERT into Demo `EducationInstitutions` / `Specialties` with `GCRecord=0` (NULL GCRecord insert fails)
- **Resume**: `-StartAt Education -IncludeFileWaves -SkipLookupPreflight -SkipTenantCatalogGeneration`
- **Outcome**: Education **Completed** Failed=0 (Posted 1 catch-up / already 3169); Educations=3170; continued to EPH then AddressOfResidence (RunId `20260713-000016`)
- **Note**: keep exact Unicode from legacy when seeding; ASCII approximations do not resolve


### 2026-07-13 — Demo IncludeFileWaves: scalars OK, DocumentCopies Failed (id-map path + LocalDB)

- **Run**: `20260713-000016` Overall=Failed after ApplicationProgress Completed (scalar Fail=0; Person/Passport/Visa Pending due to `-StartAt Education`)
- **Cause 1**: `DocumentCopies.ps1` on SyncHostRoot used DataImporter default id-map under `tools\DataImporter\legacy\visa2014\id-maps\...` → `Person id-map not found` (maps live at `data\id-maps\calik-energi-onprem-demo\`)
- **Fix 1**: pass explicit `--id-map` / `--*-id-map` to `$SyncHostRoot\data\id-maps\<LegacySource>\*.json`
- **Cause 2** (file-waves-only re-run): wrap used wrong env `VISA2026_DEMO_CONNECTION` + `appsettings.json` (LocalDB); correct is `VISA2026_DEMO_SQL_CONNECTION` / `appsettings.Production.json`
- **Fix 2**: DocumentCopies sets `ConnectionStrings__DefaultConnection` + safe `--target-connection` (same as OnPrem-Sync); Demo file-waves re-run via task with correct CS
- **Also**: archive history index coerces `ElapsedSeconds` when JSON yields Object[] (avoids `op_Division`)
- **Outcome**: file-waves re-run in progress — Target SQL=`localhost\SQLEXPRESS`/`Visa2026DbDemo`; Id-map=`...\data\id-maps\...\Person.json`; Person-Photo Running

### 2026-07-13 — Production hard wipe + Import (future prod; no backup)

- **Phase**: end-to-end (Prod wipe + scalar Import)
- **Outcome**: **running** after Encrypt/login fixes
- **Wipe**: business tables cleared on `Visa2026DbProd`; id-maps cleared under `C:\visa2026-sync\data\id-maps\calik-energi-onprem-prod`; lookups/templates kept. No prod `.bak` (user: not official prod yet).
- **Disk**: moved `C:\visa2026\backups` → `E:\visa2026\backups` before wipe (C: was ~2.5 GB free)
- **Failures then fixes**:
  1. Stale DI missing `--preflight-visa2014-lookups` → refresh published DataImporter on sync host
  2. `Encrypt=False` → `Invalid value for key 'Encrypt'` (Microsoft.Data.SqlClient) → use `Encrypt=Optional` / `Mandatory`; OnPrem-Sync + ContentRoot normalize
  3. Env-normalize script mangled `VISA2014_SQL_PASSWORD` and created `SQL_SERVER_10.100.128.15=...;Encrypt=Optional` → restore password; embed Password into `VISA2014_SQL_CONNECTION`; drop mangled key
  4. SYSTEM task Login failed for ReadOnlyUser when CS lacked Password (env inject flaky) → embed password in CS
  5. Person wave: `Visa2014LegacySqlGuard.DescribeLegacyConnection` used `SqlConnectionStringBuilder` which rejected `Encrypt=Optional` even when `SqlConnection.Open` worked → try/catch + MaskConnectionForLog fallback
- **RunId**: `20260713-025546` Overall=Running; Person Completed (Posted 3306 / Failed 0; People=3306); Passport Running
- **Task**: `Visa2026-OnPrem-ProdImportOnce` → `-Profile Production -SkipTenantCatalogGeneration -SkipLookupPreflight -StartAt Person` (scalar only; file waves later)
- **Watch**: `Watch-OnPremImportLive.ps1 -Profile Production -ViaSsh`; status `C:\visa2026-sync\sync-run-status.json` (wrap Tee fills only after Run-OnPremSyncOnServer exits)


### 2026-07-13 — Prod Education Failed(15) then Failed(3); fixed with exact Unicode seeds

- **Run**: `20260713-025546` Failed at Education (Posted 3157 / Failed 15)
- **Cause**: missing `EducationInstitutions` / `Specialties` NameTm rows — same Demo gaps plus Turkmen Unicode (dotless `ı`, `ş`, `ý`, `ü`, `ç`) that ASCII/`Riko` seeds do not match
- **Fix**: INSERT exact titles from VISA2015 via hex→UTF-16 (sqlcmd `-y 0 -Y`) into Prod; also copy known Demo rows first
- **Resume**: `-StartAt Education -SkipLookupPreflight -SkipTenantCatalogGeneration`
- **Outcome**: RunId `20260713-031108` Education **Completed** Failed=0 (Posted 3 catch-up; Educations=3172); continued to EmployeePositionHistory
- **Lesson**: seed NameTm from legacy hex when console mangling hides `ı` vs `i`; verify LEN matches legacy LEN


### 2026-07-13 — Prod ApplicationItem fail; sync.env parse bug (same Demo resume path)

- **Phase**: end-to-end (Prod resume ApplicationItem)
- **Symptom**: Watch showed ApplicationItem 0/21780 for ~14m (prepare, not hang); later `-StartAt ApplicationItem` failed in ~3s with exit `-532462766` (`0xE0434352` CLR) or `Invalid value for key 'Multiple Active Result Sets'`
- **Root cause**: `Run-OnPremSyncOnServer.ps1` `Import-SyncEnvFile` used `Read-TextFileAutoEncoding -Path $Path -split` **without parentheses**. PowerShell binds `$Path -split` first → whole `sync.env` becomes one line → only `VISA2014_SQL_PASSWORD` is set (value = rest of file). `VISA2026_PROD_SQL_CONNECTION` never loads → appsettings fallback; CS builders choke on embedded newlines after `MultipleActiveResultSets=true`
- **Fix**: `(Read-TextFileAutoEncoding -Path $Path) -split` in repo + patched on `.25` prod/demo sync hosts; Encrypt normalized on prod `sync.env`
- **Resume (same as Demo)**: `Visa2026-OnPrem-ProdImportOnce` → `Run-OnPremSyncOnServer.ps1 -Profile Production -SkipTenantCatalogGeneration -SkipLookupPreflight -StartAt ApplicationItem -Parallelism 1`
- **Outcome**: RunId `20260713-040316` Overall=Running; `parallel post: 21781 row(s), workers=1`; DI posting in progress (early 0% is normal)
- **Prevent**: always parenthesize `Read-TextFileAutoEncoding` before `-split`; verify password env length ~11 not hundreds after sync.env edits

### 2026-07-13 — ApplicationItem import: sequential post (no ParallelImportPoster)

- **Phase**: import-code
- **Context**: Prod ApplicationItem hung at 0/N on first CreateAsync via ParallelImportPoster (workers=1 or 4); Demo had completed earlier with workers=4
- **Change**: `Visa2014ApplicationItemODataImporter` posts like Education/Passport — sequential `foreach` + `CreateAsync` + `FlushAsync` + progress sidecar; `--parallelism` ignored for this entity
- **OnPrem-Sync**: comment updated (Application/ApplicationProgress may still use parallelism)
- **Next**: publish DataImporter to `C:\visa2026-sync` and resume `-StartAt ApplicationItem`

### 2026-07-13 — Prod ApplicationItem sequential resume (after `_legacyRowId` fix)

- **Phase**: end-to-end (Prod `-StartAt ApplicationItem`)
- **Deploy**: published DataImporter to `C:\visa2026-sync\tools\DataImporter`; RunId `20260713-045401`
- **Bug**: first sequential build used `_legacyOid` (KeyNotFound) — must be `_legacyRowId`
- **Outcome**: sequential post alive — ~2000/21786 (~9%), posted~1984 failed=0, DB ApplicationItems rising (~2050)

### 2026-07-13 — Prod ApplicationProgress hang (batch-size 50) → sequential flush-per-row

- **Phase**: end-to-end (Prod ApplicationProgress after ApplicationItem Completed)
- **Symptom**: Watch `100/55068 posted=49 fail=51` then freeze at `200` with `DbCount=0`; DI CPU climbing; already `workers=1`
- **Root cause**: importer intended one-row commits (`progressBatchSize=1`) but with `parallelism=1` ParallelImportPoster uses the **shared** headless target opened with `--batch-size 50`. First `CommitChanges` of ~50 ApplicationProgress rows fights `Application.LatestProgress` and hangs. Early `49/51` fail ratio matched Demo noise (incomplete State/Location payloads) and is unrelated to the hang.
- **Fix**: `Visa2014ApplicationProgressODataImporter` sequential `foreach` + owned `Visa2014ObjectSpaceImportTarget(batchSize:1)` (flush per row); `--parallelism` ignored; log first 25 payload gaps to stderr
- **Deploy**: published DI to `C:\visa2026-sync\tools\DataImporter`; resume RunId `20260713-051356` `-StartAt ApplicationProgress`
- **Outcome (verified early)**: `sequential post: 55070`; ~3100/55070 posted~3091 failed=0 skipped=9; `ApplicationProgresses` DB count rising in lockstep (~3105)
- **Ops note**: wrap scripts must pass `-StartAt` on **one line** — backtick line-continuation inside `@"..."@` here-strings drops args (first resume wrongly started Person)
- **Prevent**: never batch ApplicationProgress commits; do not trust DbCount=0 alone when shared batch>1 (rows may be uncommitted)
- **Cross-skill**: visa2026-windows-iis-deploy


### 2026-07-14 — Prod file waves started (DocumentCopies only)

- **Phase**: file-wave (Production, after scalar ApplicationProgress Completed)
- **Mode**: file-wave only (`DocumentCopies.ps1`, not full scalar re-run)
- **Environment**: `C:\visa2026-sync` · `Visa2026DbProd` on `E:\visa2026\sql-data\` (~145 GB free on E:; C: ~15 GB)
- **Script**: task `Visa2026-OnPrem-ProdFileWavesOnce` → DocumentCopies `-LegacySource calik-energi-onprem-prod -StartAt Person-Photo`
- **Outcome**: started — `file-waves-status.json` Overall=Running; Person-Photo Running; id-map path correct under `data\id-maps\...`
- **Watch**: `Watch-OnPremImportLive.ps1 -Profile Production -ViaSsh -ClearScreen` (File waves table)
- **Prevent**: do not use `-IncludeFileWaves` without `-StartAt` if you only want files — that re-prepares all scalars; call DocumentCopies directly after scalar Complete


### 2026-07-14 — Prod DocumentCopies failed mid EducationDocument (filegroup full); Demo removed

- **File waves**: Person-Photo / PassportDocument / VisaDocument OK; EducationDocument Posted 3682 / Failed 605 then abort — `Could not allocate space for object dbo.FileData` (PRIMARY full)
- **Mitigation**: removed Demo IIS + `Visa2026DbDemo` (~15 GB on C:) — C: free now ~32 GB; Prod/Staging untouched
- **Resume next**: after confirming Prod data file can grow on E:, `DocumentCopies.ps1 -StartAt EducationDocument` (id-map skip already imported diplomas)
- **Cross-skill**: visa2026-windows-iis-deploy


### 2026-07-14 — Prod FileData hit SQL Express 50 GB cap; reclaim + resume EducationDocument

- **Root cause**: `Visa2026DbProd` is Express Edition — licensed max **51200 MB (50 GB)**. MDF was 100% full (`FileData` ~48.9 GB). `ALTER ... SIZE` beyond 50 GB fails with licensed limit.
- **Reclaim**: deleted ~992k `SyncRuleLogs`; `DBCC SHRINKFILE` → ~**0.7 GB** headroom (used ~49.29 / 50 GB). Demo removal freed C: only (Prod lives on E:).
- **Resume**: DocumentCopies `-StartAt EducationDocument` with EducationDocument id-map **3682** keys (match DB). Task `Visa2026-OnPrem-ProdFileWavesOnce`.
- **Risk**: remaining Education + WorkPermit/Invitation/FamilyProof may refill the 50 GB cap — lasting fix is **upgrade off Express** (Developer/Standard) or externalize FileData.
- **Cross-skill**: visa2026-windows-iis-deploy



### 2026-07-14 — Demo PostgreSQL Person pilot (scalar import, in-process)

- **Phase**: end-to-end pilot (Demo PG target, Person only)
- **Environment**: `10.100.128.25` · IIS `Visa2026-Demo` `:8081` · PG `visa2026_demo` (`EFCoreProvider=Postgres`) · sync host `C:\visa2026-sync-demo` · legacy `10.100.128.15` / `VISA2015`
- **Deploy**: republished `Visa2026.DataImporter` to `C:\visa2026-sync-demo\tools\DataImporter`; `config\sync.env` with `VISA2014_SQL_PASSWORD` + `VISA2026_MIGRATION_IMPORT_URLS=http://127.0.0.1:5012`; target CS from `C:\inetpub\visa2026-demo\appsettings.Production.json` via `ConnectionStrings__DefaultConnection`
- **Code gates (Postgres pilot)**:
  - `Visa2014LookupPreflightCommand.LoadTargetCatalogKeys` — skip SqlClient target catalog load when `DatabaseProviderDetector.IsPostgreSql`
  - `OnPrem-Sync.ps1` `Invoke-DataImporterCli` — do not append `Encrypt=Optional` to Npgsql connection strings
  - `Visa2014PersonIdMapExpander` — skip PN-collision SqlClient scan on PostgreSQL (dedupe aliases still apply)
  - `Visa2014ImportTrackingLogCleanup` — skip T-SQL `OBJECT_ID` cleanup on PostgreSQL
- **Run**: direct `--import-visa2014 --entity Person --inprocess --max-rows 50 --batch-size 25` (log `demo-Person-pilot2-20260714-002442.log`)
- **Outcome**: **exit 0** · Prepared 50 · Posted **50** / Failed **0** · PG `People` count **50** · id-map **50** keys · `PN collision skipped on PostgreSQL`
- **First attempt**: Posted 50 then exit **1** — post-import `Visa2014PersonIdMapExpander` opened `SqlConnection` on Npgsql CS (`Keyword not supported: 'host'`). Fixed by PG skip above.
- **Preflight**: use `-SkipLookupPreflight` for full Demo chain until Npgsql target catalog loader exists (Phase A legacy audit still runs when preflight enabled; target Phase A missing-target checks are empty on PG skip).
- **Still SQL-only on target** (not hit by Person pilot): duplicate guards (`Visa2014PersonIdentityDuplicateGuard`, ApplicationItem/Progress guards), `Visa2014TargetIdMapRebuild`, archive/watch DbCounts — gate or Npgsql port before full scalar chain.
- **File waves**: not attempted on PG (`FileData` / SqlClient paths unchanged).
- **Prevent**: never run `OnPrem-Sync` Encrypt normalization on Postgres CS; verify exit code after pilot — posted count alone is insufficient when post-import hooks use SqlClient.


### 2026-07-14 — Demo PG full scalar Import started (after Person pilot)

- **Phase**: end-to-end (Demo PostgreSQL scalar chain)
- **Prep**: redeployed DataImporter + OnPrem scripts to `C:\visa2026-sync-demo`; wiped pilot `People` (50); reset id-map JSON stubs
- **Task**: `Visa2026-OnPrem-DemoImportOnce` → `Run-OnPremSyncOnServer.ps1 -Profile Demo -SkipTenantCatalogGeneration -SkipLookupPreflight` (SYSTEM)
- **RunId**: `20260714-003304`
- **Early outcome**: Person **Completed** exit 0 — Posted **3310** / Failed **0** (Dedupe merged 21); PG `People`=3310; id-map expand skipped PN collision on PostgreSQL
- **In progress**: Passport done → current wave **Visa** (DI still running)
- **Watch**: `Watch-OnPremImportLive.ps1 -Profile Demo -ViaSsh` · status `C:\visa2026-sync-demo\sync-run-status.json`
- **Note**: wrap script must use real newlines (literal `` `r`n `` text breaks the scheduled task)


### 2026-07-14 — Demo PG Education catalog gap; regenerate from VISA2015 + resume

- **Symptom**: Run `20260714-003304` Education Failed exit 1 — Posted 3158 / Failed 18 (incomplete Institution/Specialty payloads); repo `education-institution.calik-energi.json` (1507 rows) lacked live labels (e.g. Lizabon, IHK Regensburg, Puerto-Riko, Jemgyyet ylymlary)
- **First seed attempt**: deployed repo calik-energi JSON + manifest bump 39 + ForceUpdate → PG counts 1507/1085 but same 18 failures (stale catalog vs live VISA2015)
- **Fix**: `generate-from-legacy.ps1` on `.25` — sqlcmd `10.100.128.15` / `VISA2015` DISTINCT Education institutions + specialties → overlay `C:\inetpub\visa2026-demo\LookupCatalogs\tenant\` + ForceUpdate (manifest 40) → **1512** institutions / **1089** specialties
- **Resume**: `-StartAt Education` RunId `20260714-005140` — Education **Completed** Posted **18** catch-up / Failed **0** (3158 already in id-map); continued to EmployeePositionHistory
- **Prevent**: for Demo PG greenfield, always regenerate tenant education/specialty JSON from live legacy before Import; repo calik-energi files can lag live `.15`


### 2026-07-14 — Demo PG AddressOfResidence SqlClient host; Watch DbCounts blank

- **Watch**: ViaSsh DbCount column empty — `Get-OnPremImportLiveSnapshot.ps1` always used `sqlcmd` / `Visa2026DbDemo` on SQLEXPRESS (DB gone / not PG). Fix: Demo detects `EFCoreProvider=Postgres` from `C:\inetpub\visa2026-demo\appsettings.Production.json` and uses `psql` → `visa2026_demo` (quoted table names). Sample now shows Person|3310 … AddressOfResidence|5148.
- **AddressOfResidence Failed(5148)**: every row `Keyword not supported: 'host'` — `TryMatchExistingAddressAsync` opened `SqlConnection` on Npgsql CS (site-match before Create). Also gated `Visa2014AddressOfResidenceSiteDuplicateGuard.LoadFromSqlAsync` for PG.
- **Fix**: skip site-match SqlClient path when `DatabaseProviderDetector.IsPostgreSql`; republish DI to `C:\visa2026-sync-demo`
- **Resume**: `-StartAt AddressOfResidence` RunId `20260714-005636` — **Completed** Posted **5148** / Failed **0** (incl. PIA-inferred 1125); continued to EmployeeSalary
- **Prevent**: any per-row target SqlClient helper fails loudly N times on Postgres; gate at entry. Watch must know Demo is PG after Express Demo removal.


### 2026-07-14 ? Demo PG ApplicationItem T-SQL bracket DELETE (RegistrationTravelHistorySync)

- **Symptom**: Run `20260714-005636` ApplicationItem ~61% ? sidecar **posted=49 fail=13319+**; PG `ApplicationItems`=0; `.err` `42601: syntax error at or near "["` on `DELETE FROM [TravelHistories] WHERE [SourceApplicationItemID] = ?`
- **Cause**: `RegistrationTravelHistorySyncService` `ExecuteSqlRaw` T-SQL runs on every ApplicationItem save (clear soft-deleted travel rows before unique index). Demo Import uses **`--inprocess`** OData on `:5012` ? fix must land in **`C:\visa2026-sync-demo\tools\DataImporter\Visa2026.Module.dll`**, not IIS alone.
- **Fix**: replace raw DELETE with EF `IgnoreQueryFilters()` + `RemoveRange`/`Remove` + `SaveChanges()` (provider-agnostic). Deploy Module DLL to DataImporter folder; restart `-StartAt ApplicationItem`.
- **Resume**: RunId `20260714-013121` ? after deploy **inserted 1091+ / failed 0** within first ~1100 rows (0 err lines).
- **Prevent**: any `ExecuteSqlRaw` with `[brackets]` breaks on Npgsql; prefer EF or quote/bracket translate like `Visa2026EFCoreDbContext.IndexFilter`. In-process import = DataImporter Module copy, not only `C:\inetpub\visa2026-demo`.
