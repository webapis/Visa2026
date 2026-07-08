# On-prem legacy sync — learnings

Append-only log for verified fixes on **`10.100.128.15`** / **`10.100.128.25`** sync runs.

**Read before every sync session.** Also read [visa2014-to-visa2026-import/learnings.md](../visa2014-to-visa2026-import/learnings.md).

Promotion rules: [MATURITY.md](./MATURITY.md) · shared [on-prem-deploy/MATURITY.md](../on-prem-deploy/MATURITY.md)

---

## Entries

### 2026-07-06 — Skill + OnPrem-Sync.ps1 shipped

- **What**: New skill `visa2026-onprem-legacy-sync`; orchestrator `OnPrem-Sync.ps1` with `-Profile Staging|Production`.
- **Wave order**: Aligned to `order.yaml`; optional `-IncludeFileWaves`.
- **Not done**: `--sync-visa2014` delta upsert.

### 2026-07-06 — MCP legacy host: visa2014-sql-remote

- **What**: On-prem legacy SQL discovery uses Cursor MCP **`visa2014-sql-remote`** (`10.100.128.15`, `VISA2015`, `ReadOnlyUser`) per [`.cursor/mcp.json`](../../../.cursor/mcp.json).
- **Password env**: `SQL_SERVER_10.100.128.15` (MCP) — same login as DataImporter `VISA2014_SQL_PASSWORD`.
- **Do not** use `visa2014-sql-local` for `.15` LAN legacy.

### 2026-07-07 — Locked: prod-only legacy sync; staging from prod backup

- **Legacy sync target**: **`Visa2026DbProd` only** (`OnPrem-Sync.ps1 -Profile Production`). **Do not** schedule legacy import into staging.
- **Staging refresh**: backup prod on `.25` → restore to `Visa2026DbStaging` ([Restore-Visa2026SqlBackup.ps1](../../../scripts/windows-iis/Restore-Visa2026SqlBackup.ps1)); see [visa2026-windows-iis-deploy/learnings.md](../visa2026-windows-iis-deploy/learnings.md) for E: drive paths.
- **Job host**: Task Scheduler on **`.25`** (when enabled — not for first ~1 week).
- **First week**: **manual** prod sync only; check logs manually on failure.
- **Cutover horizon**: ~**1–2 months**; use `-Mode Sync` for legacy field edits (v1 shipped).
- **Attachments**: scalars/domain nightly (when automated); file waves **weekly**.
- **Officers**: read-only on prod + staging until cutover.

### 2026-07-07 — Id-map bootstrap check (dev workstation)

| Path | Status |
|------|--------|
| `id-maps/calik-energi/` | **19 JSON files** (complete dev import, 2026-07-06) |
| `id-maps/calik-energi-onprem-staging/` | Folder exists, **0 files** — not bootstrapped |
| `id-maps/calik-energi-onprem-prod/` | **Missing** on dev PC — must create on **`.25`** before prod import/sync |

**Next on `.25`:** copy `calik-energi` → `calik-energi-onprem-prod` if `Visa2026DbProd` matches same snapshot; else `--rebuild-visa2014-id-maps --legacy-source calik-energi-onprem-prod` against prod DB after first load.

### 2026-07-07 — `--sync-visa2014` v1 shipped

- **CLI**: `--sync-visa2014 --inprocess` — insert + update + soft-delete for 14 scalar entities.
- **Incremental**: legacy audit `ModifiedOn` watermark in `sync-state/<legacy-source>.json`; `--sync-full` for first manual run.
- **Orchestrator**: `OnPrem-Sync.ps1 -Mode Sync [-SyncFull]` on prod.
- **Not in v1**: file-byte delta sync (still `-IncludeFileWaves` weekly).

### 2026-07-07 — First prod `--sync-full` from dev workstation (partial)

- **Run host**: dev PC → `10.100.128.15` (legacy) + `10.100.128.25\SQLEXPRESS` / `Visa2026DbProd` (prod SQL). `.25` has **no .NET SDK** — build DataImporter on dev; copy `id-maps/calik-energi-onprem-prod/` to `.25` before scheduling there.
- **Password**: `VISA2014_SQL_PASSWORD` ≠ `SQL_SERVER_10.100.128.15` on dev — sync must use **`SQL_SERVER_10.100.128.15`** value (or align both env vars).
- **Prod connection**: read from `C:\inetpub\visa2026-prod\appsettings.Production.json` via SSH; replace `localhost\SQLEXPRESS` → `10.100.128.25\SQLEXPRESS` for LAN SQL from dev.
- **Id-map bootstrap**: dev `calik-energi` copy alone **insufficient** — run `--rebuild-visa2014-id-maps`, then **Person** re-expand by personal number; **prune** id-map targets not in `People` (33 stale on first run).
- **Succeeded (SyncFull)**: Person (3254 upd, 20 ins), Passport (3585 upd, 35 ins), WorkPermit (400 upd, 4 ins), **ApplicationProgress** (54244 upd, 80 ins).
- **Partial / failed waves**: Visa (41 insert fails), Education (29), EmployeePositionHistory / AddressOfResidence / EmployeeSalary (`Invalid object name 'dbo.EmployeeSalary'` in legacy audit query), Application (+ tenant catalog **Expected 73 rows, got 74**), WorkPermitItem, Invitation, InvitationItem, ApplicationItem.
- **Next**: fix legacy table names in sync audit SQL; regenerate `project-contract.calik-energi.json`; re-run `-StartAt Application` with `-SkipTenantCatalogGeneration` after catalog fix; nightly `-Mode Sync` (no `-SyncFull`) for deltas.

### 2026-07-07 — Resume sync + code fixes (dev workstation)

- **Code fixes** (uncommitted): `Visa2014LegacySoftDeleteQuery` — correct legacy table names (`PersonInApplication`, `ApplicationResult`, `PersonInInvitation`, `WorkPermit` for WorkPermitItem, `Application` for ApplicationProgress); `Employee` soft-delete via `Person.GCRecord` join (no `GCRecord` on `Employee`). `Visa2014SyncUpsertHelper` — on update/soft-delete **stale id-map** (target row missing in prod), remove mapping and re-insert / skip instead of failing the wave.
- **Resume sync** (`-SyncFull -ContinueOnError -SkipTenantCatalogGeneration`): Application + ApplicationProgress + WorkPermit succeeded on first long run; EmployeeSalary **2920 updated** after soft-delete SQL fix.
- **Second pass** (AddressOfResidence → ApplicationItem): **AddressOfResidence** 4004 updated + 16 soft-deleted; **Invitation** 2815 updated; **EmployeeSalary** 0 failed. **WorkPermitItem** 3839 updated but **2566 incomplete-payload** (missing FK id-maps — Passport/EPH/Person gaps). **InvitationItem** 5034 updated, **284** incomplete-payload. **ApplicationItem** still fails on stale `AddressOfResidence` FK in id-map (`CurrentAddressOfResidence` target not in prod).
- **Reconcile** (legacy `.15` vs `Visa2026DbProd`, 2026-07-07): Person −1, Visa −67, Application −174, ApplicationItem −704, WorkPermitItem −2566, Invitation −252, EmployeeSalary −64; AddressOfResidence **+5548** (expected — PIA-inferred rows in prod exceed legacy child count).
- **Id-maps + sync-state** copied to `.25`: `C:\visa2026-sync\id-maps\calik-energi-onprem-prod\` + `calik-energi-onprem-prod.json` (for future scheduled runs when .NET SDK published there).
- **Env**: use `SQL_SERVER_10.100.128.15` for legacy password on dev; set `VISA2026_PROD_SQL_CONNECTION` with `10.100.128.25\SQLEXPRESS` (from prod `appsettings.Production.json`, replace `localhost`).
- **Next**: prune/rebuild stale `AddressOfResidence` id-map targets; re-run ApplicationItem; investigate WorkPermitItem/InvitationItem incomplete payloads; optional Visa/Education/EPH **incomplete payload** rows (~41/29/35); staging refresh from prod `.bak` (W2); nightly `-Mode Sync` without `-SyncFull`.

### 2026-07-07 — Compare-OnPremSyncState.ps1 reconcile script

- **Added** `scripts/visa2014-migration/Compare-OnPremSyncState.ps1` — scalar + FileData tables, id-map counts, sync watermark for `calik-energi-onprem-prod`.
- **Skill**: W1 reconcile step + S5 scenario + [reference.md](./reference.md) command block; chat opener in [user-prompts.md](./user-prompts.md).
- **Run**: `VISA2026_PROD_SQL_CONNECTION` + `SQL_SERVER_10.100.128.15` on dev PC; `-ShowNotes` for PIA/ApplicationProgress hints.

### 2026-07-07 — Id-map rebuild + wave 3 sync (dev workstation)

- **Rebuild** `--rebuild-visa2014-id-maps --entities AddressOfResidence,WorkPermitItem,InvitationItem` against prod: AddressOfResidence **3350** matched (+1757 PIA aliases); WorkPermitItem **3839** matched (**2566** legacy rows have no prod row — expected insert gap); InvitationItem **5034** matched (**284** skipped).
- **Wave 3 sync**: WorkPermitItem **3839 updated** / 2566 failed (unmapped legacy — not in prod); InvitationItem **5034 updated** / 284 failed; **ApplicationItem** in progress after AddressOfResidence FK fix (prod count rising toward legacy 21987).
- **Remaining structural gaps**: 2566 WorkPermitItem + 284 InvitationItem legacy rows need full parent FK chain before first insert; optional Visa/Education/EPH incomplete payloads from wave 1.

### 2026-07-07 — Prod + staging on `.25` from same calik-energi LocalDB backup

- **Confirmed**: `Visa2026DbProd` (and staging) initial data restored from dev **`(localdb)\mssqllocaldb` / `Visa2026`** — same **`calik-energi`** import lineage as `id-maps/calik-energi/` on dev PC.
- **Id-map bootstrap**: **copy** `calik-energi/*` → `calik-energi-onprem-prod/` on `.25` (do **not** run full Person re-import).
- **First manual sync**: `OnPrem-Sync.ps1 -Profile Production -Mode Sync -SyncFull` after id-map copy.
- **Staging refresh**: prod `.bak` → `Visa2026DbStaging` after each prod sync cycle (not legacy).

### 2026-07-07 — Sync host deployed on `.25` + Task Scheduler

- **Deployed** `C:\visa2026-sync\` (published DataImporter, scripts, 19 id-maps, `config\sync.env`) via dev `Install-OnPremSyncHost.ps1` + `deploy.tgz` over SSH.
- **Task** `Visa2026-OnPrem-LegacySync` daily **02:30** (SYSTEM) → `Run-OnPremSyncOnServer.ps1 -Mode Sync -SkipTenantCatalogGeneration -ContinueOnError`.
- **Logs** `C:\visa2026-sync\logs\sync-run-*.log`.
- **Fix** `Register-OnPremLegacySyncTask.ps1`: `New-ScheduledTaskAction -Argument` must be a single string, not an array.

### 2026-07-07 — ApplicationItem sync: FileData EF + optional FK

- **FileData / INotifyPropertyChanged** in headless import logs was **not** ApplicationItem sync — `ApplicationRuntimeLogRetentionBackgroundService` opened `Visa2026EFCoreDbContext` without `UseChangeTrackingProxies()`. Fixed via `ApplicationRuntimeLogDbContextFactory` (all runtime-log EF helpers).
- **ApplicationItem wave exit 1**: `FK CurrentAddressOfResidence ... not found` when id-map points at AddressOfResidence rows missing in prod (stale/partial PIA map). **Fix**: optional FK payloads use `_optionalFk` in `ObjectSpaceImportSink`; `TryAddOptionalFkFromMap` skips missing targets instead of failing the row.
- **Redeploy** after fix: dev `Install-OnPremSyncHost.ps1` → `.25` `C:\visa2026-sync\tools\DataImporter\` so nightly `02:30` task picks up the build.

### 2026-07-08 — Legacy sync dashboard (scripts + Blazor Operations)

- **Artifacts on sync host** (`C:\visa2026-sync\`): `sync-run-status.json` (per-wave, written by `OnPrem-Sync.ps1`), `sync-dashboard.json` + optional `sync-dashboard.html` (reconcile snapshot).
- **Scripts**: `Export-OnPremSyncDashboard.ps1`; `Watch-OnPremSyncState.ps1 -ExportDashboard -SyncHostRoot C:\visa2026-sync`; libs `OnPremSyncRunStatus.ps1`, `Export-OnPremSyncDashboardCore.ps1` (copied by `Install-OnPremSyncHost.ps1`).
- **Blazor**: Operations → **Legacy sync** (`LegacySyncDashboardHost`, admin-only). Reads `sync-dashboard.json` via `LegacySyncDashboard:SyncHostRoot` in `appsettings.Production.json` (`Enabled: true`, per-slot path).
- **HTTP report (admin)**: `https://<slot-host>/legacy-sync/dashboard` and `/legacy-sync/dashboard.json` on prod (:443), staging (:8080), demo (:8081).
- **Sync roots**: Production `C:\visa2026-sync`, Staging `C:\visa2026-sync-staging`, Demo `C:\visa2026-sync-demo` (`Install-OnPremSyncHost.ps1 -Profile …`).
- **Static fallback**: open `<SyncHostRoot>\sync-dashboard.html` on the server (no auth).

### 2026-07-08 — WorkPermitItem gap triage + Person supplement (prod)

- **Root cause (Person)**: 2,523 pending `WorkPermit` rows referenced **soft-deleted** `Person` (`GCRecord IS NOT NULL`) never in `Person.json`. Not an Employee/Person OID mapping bug — `WorkPermit.Employee` is already `Person.Oid`.
- **Fix shipped**: `--supplement-permit-persons` imports those persons as **`IsArchived=true`** (mirrors `--supplement-permit-positions` for EPH). Patch script `WorkPermitItem-SupplementPositions.ps1` runs Person supplement → Passport supplement → EPH supplement → WorkPermitItem.
- **Prod run**: `--supplement-permit-persons` posted **+1,368** archived persons; `Person.json` **3274 → 4642**; copied to `C:\visa2026-sync\data\id-maps\calik-energi-onprem-prod\`.
- **Resume safety**: supplement import pre-expands `Person.json` from target DB (PN match) and checkpoints id-map every 250 rows (partial run recovery).
- **Remaining (Passport)**: triage after Person fix — **MissingPerson 1**, **MissingPassport 2565** (2,566 pending rows total). `--supplement-permit-passports` (WorkPermit-referenced passports, any `Passport.GCRecord`) dry-run: **0 new** rows (1,599/1,603 already in map). Passport dedupe id-map expand adds only **+1** alias — gap is **not** duplicate_merged passport OIDs at scale.
- **Next**: SQL on `VISA2015` — distinct `WorkPermit.Passport` OIDs on pending rows vs `Passport` table / skip reasons (orphan FK, import-skipped passport rows). Do **not** use Write tool for `.ps1`/`.cs` on Windows without UTF-8 verify (UTF-16 corrupts build); use `[IO.File]::WriteAllText` with UTF-8 no BOM for new files.

### 2026-07-08 — WorkPermitItem supplement wave (prod, continued)

- **Passport root cause**: 2,565 pending rows referenced **soft-deleted** `Passport` rows (`GCRecord IS NOT NULL`). Soft-deleted passports often have **`Person` NULL** — supplement SQL must use `COALESCE(pp.Person, wpRef.Employee)` via `WorkPermit` OUTER APPLY (same pattern as Person supplement).
- **Passport prod**: `--supplement-permit-passports` posted **+1,441**; `Passport.json` **3620 → 5062**.
- **EPH root cause**: 1,389 soft-deleted `WorkHistoryOfEmployee` referenced by `WorkPermit.Position` with **`Employee` NULL** on WH row — supplement SQL needs `COALESCE(w.Employee, wpRef.Employee)`; remove `p.GCRecord IS NULL` on Person join.
- **EPH prod**: posted **+1,251**; **137 failed** on unmapped legacy Position/Department titles (lookup gaps, Turkmen strings). `EmployeePositionHistory.json` **~2997 → 4248**.
- **WorkPermitItem prod**: posted **+2,243**; gap **2566 → 323** pending (`WorkPermitItem.json` **3839 → 6082**). Remaining 323 = missing Person (1) + Passport (57) + EPH id-map (265, includes failed EPH supplement rows).
- **Id-maps copied** to `C:\visa2026-sync\data\id-maps\calik-energi-onprem-prod\` (Person, Passport, EPH, WorkPermitItem).
- **Env var**: legacy SQL password is `SQL_SERVER_10.100.128.15` (dots), not `SQL_SERVER_10_100_128_15`.

### 2026-07-08 — WorkPermitItem gap closed (323 → 0)

- **Remaining blockers (323 rows)**:
  - **Person (1)**: soft-deleted `Person` with empty `FirstName` (LastName only) skipped by `required_null:FirstName|LastName|DateOfBirth`. **Fix**: `permitSupplementMode` on Person transform — coerce empty `FirstName` to `-`, missing `BirthDate` to `1900-01-01`.
  - **Passport (57)**: all had **`ExpirationDate <= IssueDate`** on soft-deleted passports. **Fix**: `permitSupplementMode` on Passport transform — bump expiration to `IssueDate + 1 day` (`_legacy_dateRangeCoerced`).
  - **EPH (137 failed + 265 id-map gap)**: Position/Department not in tenant seed. **Fix**: `ResolveOrCreatePositionAsync` / `ResolveOrCreateDepartmentAsync` in EPH importer (ActualPosition pattern).
- **Prod patch run** (`WorkPermitItem-SupplementPositions.ps1` → `Visa2026DbProd`): Person **+1**, Passport **+32**, EPH **+138**, WorkPermitItem **+323** (0 failed). `WorkPermitItem.json` **6082 → 6405**; triage pending **0**; sync state **WorkPermitItem Complete**.
- **Server**: id-maps scp'd to `C:\visa2026-sync\data\id-maps\calik-energi-onprem-prod\`. DataImporter DLL redeploy blocked if DLL locked (`Permission denied` on tar extract) — stop sync task / IIS app pool before `deploy.tgz` to `tools\DataImporter\`.
- **Dashboard**: `Export-OnPremSyncDashboard.ps1 -LoadProdConnectionFromSsh -IncludeHtml`.

### 2026-07-08 — Duplicate cleanup + sync guards + dashboard dup columns

- **ApplicationItem prod fix (prior)**: `Repair-DuplicateApplicationItems.ps1 -Apply` soft-deleted **63** extras; **0** duplicate `(Application, Person)` groups remain.
- **AddressOfResidence cleanup (preview only)**: `Repair-DuplicateAddressOfResidence.ps1` + `cleanup/DuplicateAddressOfResidenceByPersonSite.sql`. Prod preview: **681** duplicate groups, **4787** extras to soft-delete. Repoints `ApplicationItems.CurrentAddressOfResidenceID` before GC. Run `-Apply` after review.
- **Sync duplicate guards**: `Visa2014ApplicationItemPersonDuplicateGuard`, `Visa2014PassportPersonNumberDuplicateGuard`, `Visa2014WorkPermitItemPersonDuplicateGuard` wired into `Visa2014SyncUpsertHelper` (RELINK on insert when business key exists). `Visa2014SyncCommand` passes `GetTargetConnection(args)` for Passport/WorkPermitItem/ApplicationItem. Unit tests: 3 passed.
- **Dashboard**: `DuplicateGroups` + `DuplicateExtraRows` columns on scalar reconcile (`OnPremSyncState.ps1`, `Compare-OnPremSyncState.ps1`, `Export-OnPremSyncDashboardCore.ps1`, Blazor `LegacySyncDashboardComponent`, Module `LegacySyncDashboardDuplicateDefinitions`).
- **Encoding**: On Windows, Cursor Write tool can save **UTF-16** for `.cs`/`.ps1` — verify first bytes or use `[IO.File]::WriteAllText(..., UTF8Encoding $false)`. SQL `PRINT CONCAT` needs scalar vars, not subqueries (Msg 1046).
- **AddressOfResidence prod apply**: `Repair-DuplicateAddressOfResidence.ps1 -Apply` on `Visa2026DbProd` — **4787** extras soft-deleted, **681** groups resolved, **0** remaining duplicate groups post-check.
- **DataImporter redeploy (.25)**: `dotnet publish` Release → `deploy.tgz` → extract to `DataImporter-20260708`; **kill** stray `Visa2026.DataImporter.exe` (Task Scheduler can respawn); **del + copy** `Visa2026.DataImporter.dll` / `Visa2026.Module.dll` (in-place `tar -xzf` fails when DLL locked). Updated scripts: `OnPremSyncState.ps1`, `Export-OnPremSyncDashboardCore.ps1`, `Compare-OnPremSyncState.ps1`. Task `Visa2026-OnPrem-LegacySync` disabled during deploy, re-enabled after. DLL stamp **2026-07-08 ~03:02 UTC** on `C:\visa2026-sync\tools\DataImporter\`.
### 2026-07-08 — Document copies on Legacy sync dashboard

- Dashboard export always includes **FileData** rows (Person.Photo, PassportDocument, EducationDocument, VisaDocument, WorkPermitDocument, InvitationDocument, FamilyProofDocument, MedicalRecordDocument, FileData all).
- HTML report has a second section **Document copies / FileData** (chart + table). Module `LegacySyncDashboardFileDataDefinitions` + refresher write the same Kind=`FileData` entities on **Refresh**.
- Prod snapshot uploaded to `C:\visa2026-sync\sync-dashboard.json|.html`. IIS redeploy needed for in-app Refresh to regenerate FileData; until then the static HTML already shows the section.

### 2026-07-08 — VisaDocument file wave (prod)

- **Why N/A**: VisaDocument never imported on `calik-energi-onprem-prod` (bootstrap had other docs; no `VisaDocument.json`).
- **Sync host**: published DataImporter **1.0.0.549** to `C:\visa2026-sync\tools\DataImporter-VisaDoc\` (main `tools\DataImporter` had mismatched Blazor.Server 548 vs DataImporter 549 → missing assembly on in-process files).
- **Import**: `--import-visa2014-files --entity Visa --property VisaDocument` → **Posted 5839**, Failed 0, No visa map 70, No blob 47, Oversize >5MB 154. Id-map: `data\id-maps\calik-energi-onprem-prod\VisaDocument.json`.
- **Dashboard**: VisaDocument legacy SQL now counts `Visa.[GöçürmeNusga]` blobs (was null → N/A).
### 2026-07-08 — ApplicationProgress duplicates (prod + sync guard)

- **Symptom**: UI Progress (12) for `7/-1223` (Deniz Sakin) — each of 6 steps duplicated.
- **Root cause**: ApplicationProgress sync uses synthetic keys (`legacyOid:stepCode`) in id-map. When id-map is incomplete/stale, `RunSyncAsync` INSERTs again → exact twins on `(ApplicationID, ProgressOrder)`.
- **Prod scope**: **17** duplicate groups / **17** extras across a handful of apps (not all 12k — most were clean). Fully doubled: `7/-1223` (6+6), `7/-1227`, `7/-1209`, plus seed-step doubles on several apps.
- **Cleanup**: `Repair-DuplicateApplicationProgress.ps1 -Apply` + `cleanup/DuplicateApplicationProgressByAppOrder.sql` — keep `MIN(ID)` per App+Order, soft-delete extras. Post-check: **0** remaining groups; `7/-1223` ProgressCnt **12 → 6**.
- **Prevention**: `Visa2014ApplicationProgressDuplicateGuard` (`Application+Order` → MIN ID) in `RunSyncAsync` — RELINK/update instead of insert when business key exists; write synthetic key into id-map. Unit tests +3 (6 DuplicateGuard tests total).
- **Dashboard**: ApplicationProgress added to `LegacySyncDashboardDuplicateDefinitions` + `OnPremSyncState.ps1` dup queries.
- **Redeploy**: DataImporter DLL **2026-07-08 04:30** on `C:\visa2026-sync\tools\DataImporter\` (task disabled during copy).