# VISA2015 → Visa2026 — on-prem IIS migration runbook

**Hosts:** legacy SQL **`10.100.128.15`** · Visa2026 IIS **`10.100.128.25`** (Prod / Staging / Demo)

**Canonical strategy:** [IMPORT_PLAN_AND_STRATEGY.md](./IMPORT_PLAN_AND_STRATEGY.md) · [import-strategy.yaml](../../Visa2026.DataImporter/legacy/visa2014/import-strategy.yaml)

**IIS slot layout:** [ON_PREM_WINDOWS_IIS.md](../ON_PREM_WINDOWS_IIS.md) · [visa2026-windows-iis-deploy](../../.cursor/skills/visa2026-windows-iis-deploy/SKILL.md)

**Import commands:** [import-practices.md](../../.cursor/skills/visa2014-to-visa2026-import/import-practices.md) · [order.yaml](../../Visa2026.DataImporter/legacy/visa2014/order.yaml) · **Agent skill:** [visa2014-to-visa2026-import](../../.cursor/skills/visa2014-to-visa2026-import/SKILL.md)

**Legacy SQL (read-only discovery):** Cursor MCP **`visa2014-sql-remote`** → `10.100.128.15` / `VISA2015` / `ReadOnlyUser` (see [`.cursor/mcp.json`](../../.cursor/mcp.json)).

---

## Summary

| Layer | Host | Role |
|-------|------|------|
| **Legacy (read-only)** | `10.100.128.15` | SQL Server · database **`VISA2015`** · user **`ReadOnlyUser`** |
| **Visa2026 Prod** | `10.100.128.25:80` | `Visa2026DbProd` — officer production after cutover |
| **Visa2026 Staging** | `10.100.128.25:8080` | `Visa2026DbStaging` — full-volume UAT before prod |
| **Visa2026 Demo** | `10.100.128.25:8081` | `Visa2026DbDemo` — training / familiarization |

**Write path:** `Visa2026.DataImporter --import-visa2014` → **OData** (default) or **`--inprocess`** headless XAF for **Application** / **ApplicationItem** bulk loads. Never direct SQL into Visa2026; never write to `VISA2015`.

**Do not** ship migration by copying LocalDB `.mdf` files from a developer machine.

---

## Parallel period (locked decision)

During the period when officers still work in the **legacy** system and use Visa2026 for **preview only**:

| Rule | Decision |
|------|----------|
| Officers write in legacy | **Yes** — `VISA2015` on `10.100.128.15` remains **system of record** |
| Officers write in Visa2026 | **No** — **view and search only** until cutover |
| Catch-up path | **Import-only** (`OnPrem-Sync.ps1` / `--import-visa2014`) — **no** delta Sync |
| Conflict policy | **Legacy wins** (safe because Visa2026 has no officer edits) |

**Enforcement (recommended):** assign officers a **read-only** XAF role on Prod/Staging during parallel period (Navigate + Read; deny Create/Write/Delete on business types). See [ROLE_PERMISSIONS_GUIDE.md](../ROLE_PERMISSIONS_GUIDE.md). Importer uses a separate service account with write access.

**Cutover:** stop legacy writes → run **final Import catch-up** → switch officers to full Prod role. Ops: disable any leftover Task Scheduler task `Visa2026-OnPrem-LegacySync` on `.25` if still present.

---

## Network and secrets

### Firewall

Import workstation (or scheduled-task host) must reach:

- `10.100.128.15:1433` (legacy SQL)
- `10.100.128.25:80`, `:8080`, `:8081` (Visa2026 OData)

### Secrets (never commit)

| Secret | Storage |
|--------|---------|
| `ReadOnlyUser` password (DataImporter) | Windows **user** env `VISA2014_SQL_PASSWORD` on import machine |
| `ReadOnlyUser` password (Cursor MCP) | Windows **user** env `SQL_SERVER_10.100.128.15` — MCP **`visa2014-sql-remote`** in [`.cursor/mcp.json`](../../.cursor/mcp.json) |
| OData import user password | Windows user env or secure vault per slot |
| JWT / SQL on `.25` | `C:\visa2026\env\prod.env`, `staging.env`, `demo.env` on server |

Legacy connection override (prod source on LAN):

```powershell
$env:VISA2014_SQL_CONNECTION = "Server=10.100.128.15;Database=VISA2015;User Id=ReadOnlyUser;TrustServerCertificate=True;MultipleActiveResultSets=true"
$env:VISA2014_SQL_PASSWORD = [Environment]::GetEnvironmentVariable('VISA2014_SQL_PASSWORD','User')
```

---

## Id-map directories (per target slot)

Legacy GUID → Visa2026 OData `ID` maps are **per deployment**. Do not reuse LocalDB or staging maps on prod.

| Slot | Suggested folder (under `Visa2026.DataImporter/legacy/visa2014/`) |
|------|---------------------------------------------------------------------|
| Local dev | `id-maps/calik-energi/` (existing pilots) |
| Staging | `id-maps/calik-energi-onprem-staging/` |
| Production | `id-maps/calik-energi-onprem-prod/` |
| Demo | `id-maps/calik-energi-onprem-demo/` (or clone staging DB — see below) |

Pass explicit paths with `--id-map-output` and entity-specific flags (`--person-id-map`, `--application-id-map`, …). Keep maps and `import-logs/` **out of git** (PII).

Optional CLI profile: `--legacy-source calik-energi-onprem-staging` / `calik-energi-onprem-prod` in [`legacy-sources.yaml`](../../Visa2026.DataImporter/legacy/visa2014/legacy-sources.yaml).

---

## Phase 0 — Prerequisites (each slot)

1. IIS site running current `Visa2026.Blazor.Server` build ([ON_PREM_WINDOWS_IIS.md](../ON_PREM_WINDOWS_IIS.md)).
2. Target database exists (`Visa2026DbProd` / `Staging` / `Demo`); Module updaters have run once (lookups seeded).
3. **Empty** business data (or restore pre-migration empty backup).
4. Dedicated **OData import user** per slot (not officer accounts).
5. Officer users: **read-only** role during parallel period (Prod/Staging).
6. Backup target DB before first import.

---

## Phase 1 — Demo (`:8081`)

**Purpose:** Training and UI familiarization — not the authoritative migration test.

| Approach | When |
|----------|------|
| **A — Clone staging** | After staging UAT passes: backup `Visa2026DbStaging` → restore to `Visa2026DbDemo` |
| **B — Subset import** | `--max-rows` on selected entities for a small sandbox |

Do **not** run continuous scheduled sync into Demo from live legacy unless you accept frequent overwrites.

---

## Phase 2 — Staging (`:8080`) — UAT mirror of prod

**Staging does not receive legacy sync.** Refresh from **production backup** after prod import/sync.

### 2.1 Refresh from prod

1. Backup `Visa2026DbProd` on `.25`.
2. Restore to `Visa2026DbStaging` ([Restore-Visa2026SqlBackup.ps1](../../scripts/windows-iis/Restore-Visa2026SqlBackup.ps1)); use `E:\visa2026\` paths if `C:` is tight (see [visa2026-windows-iis-deploy/learnings.md](../../.cursor/skills/visa2026-windows-iis-deploy/learnings.md)).
3. `Run-Visa2026DbUpdateOnServer.ps1 -Profile Staging` if published app is newer than backup.
4. Officer read-only UAT on `https://10.100.128.25:8080`.

**Do not** run `OnPrem-Sync.ps1 -Profile Staging` for legacy `.15` data.

### 2.2 Sign-off gate

- [ ] Staging reflects prod after restore
- [ ] Rollback `.bak` retained until cutover completes

---

## Phase 3 — Production (`:80`) — legacy sync target

**All legacy import/sync runs on `.25` against prod only** (`calik-energi-onprem-prod` id-maps).

### 3.1 Pre-flight

- [ ] Per-entity `importConfirmed: true` in [`order.yaml`](../../Visa2026.DataImporter/legacy/visa2014/order.yaml)
- [ ] Prod backup taken
- [ ] `VISA2014_SQL_PASSWORD` and `VISA2026_PROD_SQL_CONNECTION` set on `.25`
- [ ] Id-map bootstrap: copy `id-maps/calik-energi/*` → `id-maps/calik-energi-onprem-prod/` **or** `--rebuild-visa2014-id-maps` after first load

### 3.2 Initial full import / manual catch-up

```powershell
$env:VISA2014_SQL_PASSWORD = [Environment]::GetEnvironmentVariable('VISA2014_SQL_PASSWORD','User')
$env:VISA2026_PROD_SQL_CONNECTION = "Server=localhost\SQLEXPRESS;Database=Visa2026DbProd;..."

.\scripts\visa2014-migration\import\OnPrem-Sync.ps1 -Profile Production -IncludeFileWaves
```

**First ~1 week:** run manually; check logs. **No** Task Scheduler until ops sign off.

Resume: `-StartAt Application`. Single entity: `-Entity ApplicationItem`. Transform-only: `-DryRun`.

### 3.3 Reconciliation

- Legacy row counts vs prod ([Compare-LegacyMigratedCounts.ps1](../../scripts/visa2014-migration/Compare-LegacyMigratedCounts.ps1))
- Spot-check 5–10 records via id-map
- Officer read-only smoke on prod

### Cutover day

1. Announce legacy **read-only** (or stop legacy app).
2. Run **final Import** catch-up (all entities + attachments).
3. Reconcile counts; smoke-test critical flows.
4. Promote officers to full write role on Prod.
5. Disable leftover Task Scheduler task `Visa2026-OnPrem-LegacySync` on `.25` if still present.
6. Archive id-maps and logs securely.

### Rollback

- Restore `Visa2026DbProd` from pre-import backup.
- **Never** write to `VISA2015` on `10.100.128.15`.

---

## Catch-up Import (parallel period)

**Status:** Delta Sync (`--sync-visa2014`, nightly Sync task, LegacySyncDashboard) was **removed**. Catch-up uses **Import** only.

| Slot | How |
|------|-----|
| **Production** | Manual `OnPrem-Sync.ps1 -Profile Production` on `.25` (lookup preflight auto) |
| **Staging** | Prod `.bak` restore — not legacy Import |
| **Demo** | `OnPrem-Sync.ps1 -Profile Demo` |

```powershell
.\scripts\visa2014-migration\import\OnPrem-Sync.ps1 -Profile Production
```

File/image bytes remain a separate wave (`--import-visa2014-files`). Check `import-logs/` after each run.

---

## Entity order reference

Follow [`order.yaml`](../../Visa2026.DataImporter/legacy/visa2014/order.yaml) strictly. Attachments / file blobs **last**.

---

## Revision log

| Date | Change |
|------|--------|
| 2026-06-21 | Initial runbook — `.25` IIS slots, `.15` legacy SQL, read-only Visa2026 parallel period |
| 2026-07-11 | Removed delta Sync; Import-only catch-up; deleted onprem-legacy-sync skill |
