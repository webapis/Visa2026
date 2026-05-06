---
name: mirror-droplet-db-to-local
description: Mirror (snapshot) the Visa2026 SQL Server database from the production droplet Docker container into a local Docker SQL Server container using backup/download/restore scripts. Use when the user asks to mirror prod droplet DB locally, restore a droplet backup locally, or take a droplet DB snapshot for debugging.
disable-model-invocation: true
---

# Mirror droplet DB → local (snapshot)

## Quick start (recommended)

From repo root (PowerShell):

```powershell
.\migration-scripts\Mirror-ProdDropletDbToLocal-Unattended.ps1
```

### One-time setup for unattended runs

Set the passphrase env var once (then open a new PowerShell window):

```powershell
setx VISA_SSH_KEY_PASSPHRASE "your-passphrase"
```

Ensure `.env.dev` exists and includes `SA_PASSWORD` for the **local** SQL container.

## What this does

- Creates a `.bak` on the droplet (via SSH)
- Downloads it to the repo root (`visa2026-prod.bak` by default)
- Restores it into the local SQL Server container

## Preconditions / safety

- **Production data**: treat `.bak` as sensitive. Don’t commit it. Prefer deleting the local `.bak` after use.
- **Local config**: ensure `.env.dev` exists and has `SA_PASSWORD` (restore uses the **local** password, not the droplet password).
- **Docker running** locally.

## Step-by-step (when debugging)

```powershell
.\migration-scripts\Enable-VisaSshAgent.ps1   # prompts unless VISA_SSH_KEY_PASSPHRASE is set
.\migration-scripts\Invoke-DropletSqlBackup.ps1 -Environment prod
.\migration-scripts\download-prod-backup.ps1 -RemotePath "~/visa2026/visa2026-prod.bak" -LocalPath ".\visa2026-prod.bak"
.\migration-scripts\Restore-BackupToLocalSql.ps1 -BackupFile ".\visa2026-prod.bak"
```

## Troubleshooting

See:
- `migration-scripts/docs/troubleshooting.md`
- [reference.md](reference.md)

