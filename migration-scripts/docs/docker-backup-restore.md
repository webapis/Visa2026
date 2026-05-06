# Docker SQL Server: backup and restore

Portable **`.bak`** files are the recommended way to copy a Visa2026 database between hosts (droplet → local PC, droplet → new droplet, etc.). This matches how the app runs SQL Server in Docker with named volumes (`docker-compose.prod.yml` / `docker-compose.dev.yml`).

## Prerequisites

- `SA_PASSWORD` and `DB_NAME` for the stack you are touching (see `.env.prod` or `.env.dev` on that machine).
- `sqlcmd` inside the Microsoft SQL Server image (path below).
- **Security:** production backups contain real data; encrypt in transit, limit who can read the file, delete copies when done.

## sqlcmd path and TLS

Inside recent `mcr.microsoft.com/mssql/server` images, `sqlcmd` is often at:

`/opt/mssql-tools18/bin/sqlcmd`

Use **`-C`** so the client trusts the server certificate (avoids trust errors on localhost inside the container).

Example:

```bash
docker exec -it <sql-container> /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -C -U sa -P '<SA_PASSWORD>' -Q "SELECT @@VERSION"
```

If that path is missing, locate the binary:

```bash
docker exec -it <sql-container> bash -lc 'command -v sqlcmd || ls /opt/mssql-tools*/bin/sqlcmd 2>/dev/null'
```

## Find the SQL container

On the droplet (from `~/visa2026` or wherever compose runs):

```bash
docker ps --filter "name=sqlserver"
```

Common names for this repository:

| Stack | Typical container name | Default `DB_NAME` |
|-------|-------------------------|-------------------|
| prod (`-p visa2026-prod`) | `visa2026-prod-sqlserver-1` | `Visa2026DbProd` |
| dev (`-p visa2026-dev`) | `visa2026-dev-sqlserver-1` | `Visa2026DbDev` |

## Backup on source host (e.g. droplet)

1. Confirm database name (optional):

   ```bash
   grep -E '^DB_NAME=' .env.prod
   ```

2. Run backup inside the container (writes under `/var/opt/mssql/` in the container):

   ```bash
   docker exec -it visa2026-prod-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd \
     -S localhost -C -U sa -P '<SA_PASSWORD>' \
     -Q "BACKUP DATABASE [Visa2026DbProd] TO DISK = N'/var/opt/mssql/visa2026-prod.bak' WITH INIT, FORMAT"
   ```

3. Copy the file to the host (so you can `scp` it):

   ```bash
   docker cp visa2026-prod-sqlserver-1:/var/opt/mssql/visa2026-prod.bak ./visa2026-prod.bak
   ```

Or use the helper [../droplet-backup.sh](../droplet-backup.sh) from an SSH session on the source machine.

## Transfer the `.bak`

Use `scp`, `rsync`, or the Windows helper [../download-prod-backup.ps1](../download-prod-backup.ps1).

Example `scp` from your PC (same SSH key as [droplet-scripts/update-prod.ps1](../../droplet-scripts/update-prod.ps1)):

```powershell
scp -i "$env:USERPROFILE\.ssh\id_ed25519_visa" root@<DROPLET_IP>:~/visa2026/visa2026-prod.bak .
```

## Restore on destination (e.g. local Docker)

1. Start SQL Server locally (see [ENVIRONMENTS.md](../../docs/ENVIRONMENTS.md)), e.g. dev stack:

   ```powershell
   docker compose -p visa2026-dev --env-file .env.dev -f docker-compose.dev.yml up -d
   ```

2. Find the local SQL container:

   ```powershell
   docker ps --filter "name=sqlserver"
   ```

3. Copy the backup in and restore. Use the **destination** `SA_PASSWORD` from `.env.dev` (it does not need to match the source):

   ```powershell
   docker cp .\visa2026-prod.bak <local-sql-container>:/var/opt/mssql/visa2026-prod.bak
   docker exec -it <local-sql-container> /opt/mssql-tools18/bin/sqlcmd `
     -S localhost -C -U sa -P '<LOCAL_SA_PASSWORD>' `
     -Q "RESTORE DATABASE [Visa2026DbDev] FROM DISK = N'/var/opt/mssql/visa2026-prod.bak' WITH REPLACE"
   ```

4. Align the app: set `DB_NAME` in `.env.dev` to the database name you restored into (`ConnectionStrings__DefaultConnection` in compose uses `DB_NAME`).

If `RESTORE` fails with physical file path errors, see [troubleshooting.md](troubleshooting.md).

## After restore

When the Blazor app starts, XAF/EF may run updaters against the restored database. For one-off updater issues on an “already current” schema, see `FORCE_XAF_DB_UPDATE` in [ENVIRONMENTS.md](../../docs/ENVIRONMENTS.md).

## Alternative: raw volume copy (advanced)

Copying `/var/opt/mssql` with `tar` across hosts is faster but **fragile** (version, permissions). Prefer `.bak` unless you have a strong reason. A sketch of the volume approach was kept in the historical [DATA_MIGRATION.md](../../DATA_MIGRATION.md) pointer at the repo root.

## In-place upgrade (no DB copy)

To deploy a new app image on the **same** SQL volume, use normal compose pull/restart; the app updates schema on startup. See [ENVIRONMENTS.md](../../docs/ENVIRONMENTS.md) and [droplet-scripts/update-app.ps1](../../droplet-scripts/update-app.ps1).
