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