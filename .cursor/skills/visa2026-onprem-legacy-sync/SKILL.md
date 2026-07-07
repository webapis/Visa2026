---
name: visa2026-onprem-legacy-sync
description: >-
  One-way legacy sync from VISA2015 on 10.100.128.15 to Visa2026 production on 10.100.128.25
  (:80 / Visa2026DbProd). Staging (:8080) is refreshed from prod DB backup — not legacy sync.
  Server .25 runs OnPrem-Sync.ps1 / DataImporter in-process — not SQL replication.
  Manual sync first week; nightly Task Scheduler after. MCP visa2014-sql-remote for discovery.
disable-model-invocation: false
---

# On-prem legacy sync (10.100.128.15 → 10.100.128.25)

## Goal

**One-way legacy sync** from **`10.100.128.15` / `VISA2015`** → **production only** on **`10.100.128.25`**:

| Slot | URL | Database | Legacy sync? |
|------|-----|----------|--------------|
| **Production** | `https://10.100.128.25` | `Visa2026DbProd` | **Yes** — `calik-energi-onprem-prod` id-maps |
| **Staging** | `https://10.100.128.25:8080` | `Visa2026DbStaging` | **No** — restore from **prod `.bak`** ([visa2026-windows-iis-deploy](../visa2026-windows-iis-deploy/SKILL.md)) |

**Run from:** **`10.100.128.25`** (Task Scheduler when enabled). Needs `.15:1433` + local SQL.

**Canonical runbook:** [ON_PREM_IIS_MIGRATION_RUNBOOK.md](../../../docs/VISA2014_MIGRATION/ON_PREM_IIS_MIGRATION_RUNBOOK.md)

**Config:** [import-strategy.yaml](../../../Visa2026.DataImporter/legacy/visa2014/import-strategy.yaml) `onPremDeployment` · [legacy-sources.yaml](../../../Visa2026.DataImporter/legacy/visa2014/legacy-sources.yaml)

**Commands:** [reference.md](./reference.md) · **Chat openers:** [user-prompts.md](./user-prompts.md)

**Experience loop:** [learnings.md](./learnings.md) · [MATURITY.md](./MATURITY.md)

## MCP (legacy discovery on `.15`)

For **read-only SQL** against production legacy on the LAN, use Cursor MCP **`visa2014-sql-remote`** ([`.cursor/mcp.json`](../../../.cursor/mcp.json)):

| MCP setting | Value |
|-------------|--------|
| Server | `visa2014-sql-remote` |
| Host | `10.100.128.15:1433` |
| Database | `VISA2015` |
| Login | `ReadOnlyUser` |
| Password env | **`SQL_SERVER_10.100.128.15`** (Windows user env) |

**Do not** use `visa2014-sql-local` for on-prem `.15` work — that points at local dev SQL, not the LAN legacy host.

**Target validation on `.25`:** optional MCP **`visa2026-sql-remote`** (`10.100.128.25\SQLEXPRESS`, per-slot `DATABASE_NAME` in mcp.json).

**Import writes** still go through `Visa2026.DataImporter` / `OnPrem-Sync.ps1` (not MCP).

## Not this skill

| Topic | Use |
|-------|-----|
| BO discovery, mapping, `importConfirmed` | [visa2014-to-visa2026-import](../visa2014-to-visa2026-import/SKILL.md) |
| IIS publish, HTTPS, `.bak` restore, DB update | [visa2026-windows-iis-deploy](../visa2026-windows-iis-deploy/SKILL.md) |
| Runtime errors after sync | [visa2026-runtime-error-tracking](../visa2026-runtime-error-tracking/SKILL.md) |
| SQL Express replication | Not supported — application-level import only |

## Hard rules

1. **Read** [learnings.md](./learnings.md) and [visa2014-to-visa2026-import/learnings.md](../visa2014-to-visa2026-import/learnings.md) before any sync run.
2. **Never** write to `VISA2015` on `.15`. `ReadOnlyUser` is enough for sync reads (not for `.bak` backup).
3. **Legacy SQL discovery:** **`visa2014-sql-remote`** MCP first; confirm `SELECT DB_NAME()` → `VISA2015`.
4. **Prod id-maps only** for legacy sync — `id-maps/calik-energi-onprem-prod/` on `.25`. Staging does not get legacy id-map sync.
5. **First ~1 week:** manual prod sync only — **no** Task Scheduler until ops are comfortable.
6. **Reuse** [scripts/visa2014-migration/README.md](../../../scripts/visa2014-migration/README.md) — orchestrator: [OnPrem-Sync.ps1](../../../scripts/visa2014-migration/import/OnPrem-Sync.ps1) **`-Profile Production` only** for legacy.
7. **Append learnings** after every sync attempt (success or failure).

## Parallel period (locked)

| Rule | Value |
|------|-------|
| System of record | Legacy `VISA2015` on `.15` until cutover |
| Officer writes in Visa2026 | **No** — read-only on Staging/Prod |
| Sync direction | Legacy → Visa2026 only |
| Conflict policy | Legacy wins |

## Preflight (on `.25`)

| Check | How |
|-------|-----|
| Network | `.15:1433` (legacy), local `localhost\SQLEXPRESS` (prod DB) |
| MCP legacy | **`visa2014-sql-remote`** enabled; `SQL_SERVER_10.100.128.15` set (dev workstation) |
| Import secrets | `VISA2014_SQL_PASSWORD`, `VISA2026_PROD_SQL_CONNECTION` on `.25` |
| Target DB | `Visa2026DbProd` loaded (e.g. **calik-energi LocalDB `.bak` restore**); id-maps bootstrapped → `calik-energi-onprem-prod/` |
| Mapping gates | `importConfirmed: true` per entity in [order.yaml](../../../Visa2026.DataImporter/legacy/visa2014/order.yaml) |
| Officers | Read-only XAF role on prod + staging — [ROLE_PERMISSIONS_GUIDE.md](../../../docs/ROLE_PERMISSIONS_GUIDE.md) |

## Workflows

### W1 — Production bootstrap + catch-up (on `.25`)

**Current state (Çalik Enerji):** prod DB restored from dev **calik-energi** LocalDB backup — copy id-maps, then catch-up (not full Person re-import).

1. Backup `Visa2026DbProd`.
2. **Id-map bootstrap** (once): copy dev `id-maps/calik-energi/*` → `id-maps/calik-energi-onprem-prod/` (expect **19** JSON files).
3. Manual catch-up: `OnPrem-Sync.ps1 -Profile Production` with application-domain entities (see [reference.md](./reference.md) W3).
4. Weekly: `-IncludeFileWaves` for photos/scans.
5. Reconcile: [Compare-LegacyMigratedCounts.ps1](../../../scripts/visa2014-migration/Compare-LegacyMigratedCounts.ps1).
6. Refresh staging via W2.

### W2 — Refresh staging from prod (not legacy)

1. Backup `Visa2026DbProd` on `.25`.
2. Restore to `Visa2026DbStaging` ([Restore-Visa2026SqlBackup.ps1](../../../scripts/windows-iis/Restore-Visa2026SqlBackup.ps1)).
3. `Run-Visa2026DbUpdateOnServer.ps1 -Profile Staging` if app build is newer than backup.
4. Officer read-only UAT on `:8080`.

### W3 — Manual / nightly delta sync (`--sync-visa2014`)

```powershell
# First manual run on .25 (prod) — push all legacy field edits since bootstrap
.\scripts\visa2014-migration\import\OnPrem-Sync.ps1 -Profile Production -Mode Sync -SyncFull

# Nightly incremental (after first --sync-full)
.\scripts\visa2014-migration\import\OnPrem-Sync.ps1 -Profile Production -Mode Sync
```

| Works | Limitation |
|-------|------------|
| Insert new legacy rows | Requires id-map bootstrap |
| Update changed rows (audit `ModifiedOn` or `--sync-full`) | ApplicationProgress uses synthetic step keys |
| Soft-delete when legacy `GCRecord` set | Files still weekly (`-IncludeFileWaves`) |
| Interim catch-up only | `-Mode Import` — new rows, no field PATCH |

**Manual week 1:** `-Mode Sync -SyncFull` once, then `-Mode Sync` nightly. No Task Scheduler until ops sign off.

### W4 — Cutover day

1. Stop legacy writes (or legacy read-only).
2. Final catch-up (`OnPrem-Sync.ps1 -Profile Production` all entities + `-IncludeFileWaves`).
3. Reconcile + smoke critical flows.
4. Promote officers to full write role on Prod.
5. Disable scheduled sync tasks.
6. Archive id-maps/logs securely.

### W5 — Rollback

Restore target slot `.bak` on `.25`. **Never** write to `.15`.

## Scenarios

| ID | Symptom | Fix |
|----|---------|-----|
| S1 | Cannot reach `.15:1433` | Firewall/VPN; verify `VISA2014_SQL_PASSWORD` / `SQL_SERVER_10.100.128.15` |
| S2 | MCP legacy queries fail | Enable **`visa2014-sql-remote`**; reload Cursor MCP; test `SELECT DB_NAME()` |
| S3 | `TargetConnection required` | Set `VISA2026_*_SQL_CONNECTION` or `-TargetConnection` |
| S4 | Wave fails mid-chain | `-StartAt <Entity>` on [OnPrem-Sync.ps1](../../../scripts/visa2014-migration/import/OnPrem-Sync.ps1) |
| S5 | Counts ≠ legacy | [Compare-LegacyMigratedCounts.ps1](../../../scripts/visa2014-migration/Compare-LegacyMigratedCounts.ps1); MCP `visa2014-sql-remote` row counts |
| S6 | Officers can edit during parallel | Tighten read-only role |
| S7 | Post-import corrections fail (`hostpolicy.dll`) | `dotnet build` DataImporter; re-run `--correct-*` flags only |
| S8 | Id-map missing on `.25` | Copy `calik-energi` → `calik-energi-onprem-prod` or `--rebuild-visa2014-id-maps` on prod |
| S9 | Staging drift vs legacy | Expected — staging mirrors **prod**, not `.15`; refresh from prod `.bak` |

## Experience loop

1. **READ** [learnings.md](./learnings.md) + migration [learnings.md](../visa2014-to-visa2026-import/learnings.md).
2. **WORK** — one workflow (W1–W5); ask before mutating **Production**.
3. **RECORD** — append [learnings.md](./learnings.md) after every run (success or failure).