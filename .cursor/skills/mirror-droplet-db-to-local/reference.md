# Reference: droplet → local DB mirror

## Files involved

- `migration-scripts/Mirror-DropletDbToLocal.ps1` (wrapper)
- `migration-scripts/Invoke-DropletSqlBackup.ps1` (SSH to droplet and run backup)
- `migration-scripts/download-prod-backup.ps1` (SCP download)
- `migration-scripts/Restore-BackupToLocalSql.ps1` (restore into local container)
- `migration-scripts/docs/troubleshooting.md` (restore path/logical-name issues)

## One-time setup (local)

Create `.env.dev` if it does not exist:

```powershell
Copy-Item .\.env.dev.example .\.env.dev
notepad .\.env.dev
```

Set at minimum:
- `SA_PASSWORD=...` (for local SQL container)

## SSH key passphrase

If your droplet key is passphrase-protected, load it once per session:

```powershell
.\migration-scripts\Enable-VisaSshAgent.ps1
```

For unattended runs, set an env var once and use the wrapper:

```powershell
setx VISA_SSH_KEY_PASSPHRASE "your-passphrase"
.\migration-scripts\Mirror-ProdDropletDbToLocal-Unattended.ps1
```

## Common failure modes

- **Permission denied (publickey)**: wrong identity file, key not loaded, or key not authorized on droplet.
- **Hangs waiting for host-key prompt**: fixed in scripts via `StrictHostKeyChecking=accept-new`.
- **RESTORE errors (MOVE / FILELISTONLY)**: follow `migration-scripts/docs/troubleshooting.md`.
- **Non-interactive terminal**: restore script uses `docker exec -i` (no TTY).

## Cleanup

Delete local `.bak` when finished (don’t commit it):

```powershell
Remove-Item .\visa2026-prod.bak
```

