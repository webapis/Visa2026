---
name: visa2026-restore-from-bak
description: Restores a Visa2026 SQL Server .bak on a developer PC into either LocalDB (Visual Studio default connection) or the local Docker dev SQL container. Use when the user asks to restore visa2026-prod.bak, reload prod data locally, LocalDB restore, or import a backup while developing.
disable-model-invocation: true
---

# Restore Visa2026 from a `.bak` file

## Pick the local SQL target

| How you run the app | Connection (default) | Restore script |
|---------------------|----------------------|----------------|
| **Visual Studio** (Blazor Server on the host) | `(localdb)\mssqllocaldb`, database `Visa2026` — see `Visa2026.Blazor.Server/appsettings.json` | `migration-scripts/Restore-BackupToLocalDb.ps1` |
| **Docker dev stack** (`visa2026-dev`, `docker-compose.dev.yml`) | SQL in container; database from `DB_NAME` / `.env.dev` (default `Visa2026DbDev`) | `migration-scripts/Restore-BackupToLocalSql.ps1` |

Do **not** use the Docker restore script when the app is pointed at LocalDB, or the database will not match what Visual Studio opens.

## Visual Studio → LocalDB (recommended for this workflow)

**Preconditions**

- Stop debugging / close anything holding database `Visa2026` on that LocalDB instance (SSMS, another app).
- Have a `.bak` path on disk (for example `visa2026-prod.bak` at repo root after download). Do **not** commit production backups.

**Command** (repo root, PowerShell):

```powershell
.\migration-scripts\Restore-BackupToLocalDb.ps1 -BackupFile ".\visa2026-prod.bak"
```

Optional overrides: `-ServerInstance "(localdb)\mssqllocaldb"`, `-TargetDatabase "Visa2026"` (must match `ConnectionStrings:DefaultConnection`).

The script runs `RESTORE FILELISTONLY`, then `RESTORE … WITH REPLACE` and **`MOVE`** so Linux/Docker source paths work on Windows LocalDB.

### Visual Studio against Docker SQL (not LocalDB)

`Visa2026.Blazor.Server/appsettings.Development.json` sets `DefaultConnection` to **`127.0.0.1,1433`** / **`Visa2026DbDev`** / **`sa`**, with `Password=CHANGE_ME_STRONG_PASSWORD` aligned to `.env.dev.example`. If your real `.env.dev` uses a different `SA_PASSWORD`, either update that password in `appsettings.Development.json` or run **`migration-scripts/Sync-BlazorDevConnectionUserSecret.ps1`** (writes **`dotnet user-secrets`**, which overrides the JSON). Ensure **`docker compose … up -d sqlserver`** (or full dev stack) is running and the DB is restored (`Restore-BackupToLocalSql.ps1`).

## Docker dev SQL

**Preconditions**

- Docker running; dev SQL container up (for example `docker compose -p visa2026-dev --env-file .env.dev -f docker-compose.dev.yml up -d`).
- `.env.dev` contains `SA_PASSWORD` (restore uses the **local** password).

**Command**:

```powershell
.\migration-scripts\Restore-BackupToLocalSql.ps1 -BackupFile ".\visa2026-prod.bak"
```

## Getting a `.bak` (droplet / prod snapshot)

For backup on the droplet, download, and restore into **Docker** only, follow `.cursor/skills/mirror-droplet-db-to-local/SKILL.md`.

If you only need the file, see also `migration-scripts/docs/docker-backup-restore.md` and `migration-scripts/download-prod-backup.ps1`.

## After restore

- Start the app; XAF/EF may run updaters. If updaters must run once on a DB that already looks current, see `FORCE_XAF_DB_UPDATE` in `docs/ENVIRONMENTS.md`.
- **Security:** treat prod backups as sensitive; delete local copies when finished if policy requires.

## Troubleshooting

- **Logical / physical path errors on restore:** `migration-scripts/docs/troubleshooting.md` (MOVE / `RESTORE FILELISTONLY`).
- **“Database in use” / exclusive access:** stop consumers (VS debugger, SSMS, app container) and retry; the LocalDB script sets `SINGLE_USER` before `RESTORE`.
- **LocalDB: `sysfiles1 is corrupted` / `RESTORE could not start database` after a valid `RESTORE VERIFYONLY`:** some backups taken on **Linux/Docker SQL Server** recover cleanly on full SQL (including the dev Docker image) but **not** on **LocalDB**, even on the same major version. Use `Restore-BackupToLocalSql.ps1` (start `sqlserver` from `docker-compose.dev.yml`), then point Visual Studio at `127.0.0.1` (or `localhost`) and the host-mapped port (default **1433**), database **`Visa2026DbDev`** (or pass `-DatabaseName` when restoring), with `User Id=sa` and `SA_PASSWORD` from `.env.dev`.
