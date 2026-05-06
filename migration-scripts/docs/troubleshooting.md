# Backup / restore troubleshooting

## `RESTORE` fails: logical / physical file path mismatch

Backups remember **logical file names** and original **physical paths**. A restore on another SQL instance may need new paths under `/var/opt/mssql/data/`.

1. Inspect the backup (run inside the **destination** SQL container, path to `.bak` adjusted):

   ```bash
   docker exec -it <container> /opt/mssql-tools18/bin/sqlcmd -S localhost -C -U sa -P '<SA_PASSWORD>' -Q "RESTORE FILELISTONLY FROM DISK = N'/var/opt/mssql/your.bak'"
   ```

   Note the **LogicalName** values (often `..._Data` and `..._Log`).

2. Restore with **MOVE** (replace logical names and target paths with values from step 1):

   ```sql
   RESTORE DATABASE [YourDb]
   FROM DISK = N'/var/opt/mssql/your.bak'
   WITH REPLACE,
     MOVE 'LogicalDataName' TO '/var/opt/mssql/data/YourDb.mdf',
     MOVE 'LogicalLogName' TO '/var/opt/mssql/data/YourDb_log.ldf';
   ```

Paths must be valid inside the container; Express editions may restrict file placement—use paths under `/var/opt/mssql/data/` when that directory exists.

## `sqlcmd`: login failed / cannot open server

- Wrong `SA_PASSWORD` for **this** stack (source vs destination differ after restore is fine for backup; each `sqlcmd` uses the password for the container you are connected to).
- SQL not ready yet: wait a few seconds after `compose up` and retry.

## `sqlcmd`: certificate / encryption errors

Add **`-C`** (trust server certificate) for localhost inside the container:

```bash
/opt/mssql-tools18/bin/sqlcmd -S localhost -C -U sa ...
```

## `docker exec`: no such file (sqlcmd path)

Image layout can vary. Discover the binary:

```bash
docker exec -it <container> bash -lc 'command -v sqlcmd; ls /opt/mssql-tools*/bin/sqlcmd 2>/dev/null'
```

## Permission denied (publickey) when `scp` from Windows

Use the same private key as production deploy scripts, e.g. `-i "$env:USERPROFILE\.ssh\id_ed25519_visa"`. See [download-prod-backup.ps1](../download-prod-backup.ps1) and [droplet-scripts/update-app.ps1](../../droplet-scripts/update-app.ps1).

## Disk space

Large databases need free space in the container filesystem and on the host for `docker cp`. Check before `BACKUP`:

```bash
docker exec -it <container> df -h /var/opt/mssql
```
