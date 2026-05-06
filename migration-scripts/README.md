# Migration scripts (operational)

This folder holds **operational** database migration helpers: Docker SQL Server backup/restore, copying backups off a droplet, and related notes.

**Not covered here:** EF Core model migrations under `Visa2026.Module` (those stay in the .NET project).

## Documentation

| Doc | Purpose |
|-----|---------|
| [docs/docker-backup-restore.md](docs/docker-backup-restore.md) | Full backup/restore flow (droplet ↔ local, host-to-host) |
| [docs/troubleshooting.md](docs/troubleshooting.md) | `RESTORE` path issues, `WITH MOVE`, common errors |

## Environment reference

Compose stacks, volume names, and `DB_NAME` defaults are described in [docs/ENVIRONMENTS.md](../docs/ENVIRONMENTS.md).

Typical SQL containers (depends on compose project name):

- Production: `visa2026-prod-sqlserver-1`, default database `Visa2026DbProd`
- Development (droplet): `visa2026-dev-sqlserver-1`, default database `Visa2026DbDev`

## Scripts

| Script | Where it runs | Purpose |
|--------|----------------|---------|
| [droplet-backup.sh](droplet-backup.sh) | Linux (droplet SSH session) | Backup one database to a `.bak` on the host filesystem |
| [download-prod-backup.ps1](download-prod-backup.ps1) | Windows (developer PC) | `scp` a backup from the droplet using the Visa SSH key |
| [Invoke-DropletSqlBackup.ps1](Invoke-DropletSqlBackup.ps1) | Windows (developer PC) | Trigger droplet backup over SSH (creates `.bak` on droplet) |
| [Restore-BackupToLocalSql.ps1](Restore-BackupToLocalSql.ps1) | Windows (developer PC) | Restore a `.bak` into the local dev SQL container |
| [Mirror-DropletDbToLocal.ps1](Mirror-DropletDbToLocal.ps1) | Windows (developer PC) | End-to-end: backup on droplet → download → restore locally |
| [Enable-VisaSshAgent.ps1](Enable-VisaSshAgent.ps1) | Windows (developer PC) | Start `ssh-agent` and load the Visa SSH key (avoid repeated passphrase prompts) |
| [Mirror-ProdDropletDbToLocal-Unattended.ps1](Mirror-ProdDropletDbToLocal-Unattended.ps1) | Windows (developer PC) | One-command unattended mirror (requires `VISA_SSH_KEY_PASSPHRASE`) |

**Secrets:** never commit `.env.prod` / `.env.dev` or real `.bak` files. Treat production backups as confidential data.

## Recommended run order (mirror droplet DB → local)

### Option A: one command (recommended)

From repo root:

```powershell
.\migration-scripts\Mirror-ProdDropletDbToLocal-Unattended.ps1
```

### Option B: step-by-step (debuggable)

1) Create backup on droplet:

```powershell
.\migration-scripts\Invoke-DropletSqlBackup.ps1 -Environment prod
```

2) Download backup:

```powershell
.\migration-scripts\download-prod-backup.ps1 -RemotePath "~/visa2026/visa2026-prod.bak" -LocalPath ".\visa2026-prod.bak"
```

3) Restore locally:

```powershell
.\migration-scripts\Restore-BackupToLocalSql.ps1 -BackupFile ".\visa2026-prod.bak"
```

## Quick links

- [droplet-scripts/update-app.ps1](../droplet-scripts/update-app.ps1) — deploy app (uses same SSH key pattern as `download-prod-backup.ps1`)
