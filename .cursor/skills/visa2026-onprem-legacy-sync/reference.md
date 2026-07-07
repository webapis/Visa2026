# On-prem legacy sync — reference

Hosts: legacy **`10.100.128.15`** · Visa2026 **`10.100.128.25`**

Orchestrator: [OnPrem-Sync.ps1](../../../scripts/visa2014-migration/import/OnPrem-Sync.ps1)  
Staging wrapper: [OnPrem-Staging.ps1](../../../scripts/visa2014-migration/import/OnPrem-Staging.ps1) (= `-Profile Staging`)

---

## MCP — legacy SQL on `.15`

Configured in [`.cursor/mcp.json`](../../../.cursor/mcp.json) as **`visa2014-sql-remote`**:

```json
"SERVER_NAME": "10.100.128.15",
"DATABASE_NAME": "VISA2015",
"SQL_USERNAME": "ReadOnlyUser",
"SQL_PASSWORD": "${env:SQL_SERVER_10.100.128.15}"
```

| Use | MCP server |
|-----|------------|
| Legacy row counts, schema checks, DISTINCT lookups on `.15` | **`visa2014-sql-remote`** |
| Local dev `localhost` VISA2015 | `visa2014-sql-local` (if configured) — **not** on-prem sync |
| Visa2026 target DB on `.25` (read-only validation) | **`visa2026-sql-remote`** |

**Preflight query** (via MCP): `SELECT DB_NAME()` → must return `VISA2015`.

---

## Secrets (Windows user env — never commit)

| Variable | Used for |
|----------|----------|
| `SQL_SERVER_10.100.128.15` | MCP **`visa2014-sql-remote`** password (`ReadOnlyUser` on `.15`) |
| `VISA2014_SQL_PASSWORD` | `Visa2026.DataImporter`, `OnPrem-Sync.ps1`, catalog scripts (same `ReadOnlyUser` on `.15`) |
| `VISA2026_STAGING_SQL_CONNECTION` | Full connection string → `Visa2026DbStaging` |
| `VISA2026_PROD_SQL_CONNECTION` | Full connection string → `Visa2026DbProd` |

Use the **same** `ReadOnlyUser` password for both `SQL_SERVER_10.100.128.15` and `VISA2014_SQL_PASSWORD` unless your ops team split them intentionally.

Optional legacy override for DataImporter:

```powershell
$env:VISA2014_SQL_CONNECTION = "Server=10.100.128.15;Database=VISA2015;User Id=ReadOnlyUser;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

### Connection string templates (target `.25`)

```powershell
$env:VISA2026_STAGING_SQL_CONNECTION = "Server=10.100.128.25;Database=Visa2026DbStaging;User Id=visa_import;Password=...;TrustServerCertificate=True;MultipleActiveResultSets=true"
$env:VISA2026_PROD_SQL_CONNECTION = "Server=10.100.128.25;Database=Visa2026DbProd;User Id=visa_import;Password=...;TrustServerCertificate=True;MultipleActiveResultSets=true"
```

---

## W1 — Production full load / manual catch-up (on `.25`)

```powershell
cd C:\path\to\Visa2026
$env:VISA2014_SQL_PASSWORD = [Environment]::GetEnvironmentVariable('VISA2014_SQL_PASSWORD','User')
$env:VISA2026_PROD_SQL_CONNECTION = "Server=localhost\SQLEXPRESS;Database=Visa2026DbProd;..."

# Once: bootstrap id-maps
$src = "Visa2026.DataImporter\legacy\visa2014\id-maps\calik-energi"
$dst = "Visa2026.DataImporter\legacy\visa2014\id-maps\calik-energi-onprem-prod"
New-Item -ItemType Directory -Force -Path $dst | Out-Null
Copy-Item "$src\*" $dst -Force

.\scripts\visa2014-migration\import\OnPrem-Sync.ps1 `
  -Profile Production `
  -Configuration Release
```

File waves (weekly, manual for now): add `-IncludeFileWaves`.

---

## W2 — Refresh staging from prod (not legacy)

```powershell
# Backup prod, restore to Visa2026DbStaging — see Restore-Visa2026SqlBackup.ps1
.\scripts\windows-iis\Run-Visa2026DbUpdateOnServer.ps1 -Profile Staging
```

---

## W3 — Delta sync (manual / nightly)

```powershell
.\scripts\visa2014-migration\import\OnPrem-Sync.ps1 -Profile Production -Mode Sync -SyncFull   # first run
.\scripts\visa2014-migration\import\OnPrem-Sync.ps1 -Profile Production -Mode Sync              # incremental
```

Single entity:

```powershell
dotnet run --project Visa2026.DataImporter -c Release -- `
  --sync-visa2014 --inprocess --entity Application `
  --legacy-source calik-energi-onprem-prod `
  --target-connection $env:VISA2026_PROD_SQL_CONNECTION `
  --sync-state-dir C:\path\to\sync-state
```

## Entity wave order (OnPrem-Sync.ps1)

Matches [order.yaml](../../../Visa2026.DataImporter/legacy/visa2014/order.yaml) — includes WorkPermit/Invitation chains before ApplicationItem; optional `-IncludeFileWaves` for [DocumentCopies.ps1](../../../scripts/visa2014-migration/import/DocumentCopies.ps1).

---

## Future `--sync-visa2014` (shipped v1)

CLI: `--sync-visa2014` (requires `--inprocess`). Options: `--sync-full`, `--sync-since <utc>`, `--sync-state-dir`, `--no-soft-delete-sync`.

See [ON_PREM_IIS_MIGRATION_RUNBOOK.md](../../../docs/VISA2014_MIGRATION/ON_PREM_IIS_MIGRATION_RUNBOOK.md) § Scheduled sync.