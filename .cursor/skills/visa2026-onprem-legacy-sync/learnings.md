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

### 2026-07-07 — Prod + staging on `.25` from same calik-energi LocalDB backup

- **Confirmed**: `Visa2026DbProd` (and staging) initial data restored from dev **`(localdb)\mssqllocaldb` / `Visa2026`** — same **`calik-energi`** import lineage as `id-maps/calik-energi/` on dev PC.
- **Id-map bootstrap**: **copy** `calik-energi/*` → `calik-energi-onprem-prod/` on `.25` (do **not** run full Person re-import).
- **First manual sync**: `OnPrem-Sync.ps1 -Profile Production -Mode Sync -SyncFull` after id-map copy.
- **Staging refresh**: prod `.bak` → `Visa2026DbStaging` after each prod sync cycle (not legacy).