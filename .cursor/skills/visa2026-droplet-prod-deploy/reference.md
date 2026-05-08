# Reference: droplet prod deploy and SSH diagnostics

Paths assume **`~/visa2026`**, project **`visa2026-prod`**, **`docker-compose.prod.yml`**, **`.env.prod`**. Replace **`root@DROPLET_IP`** with values from [`droplet-scripts/update-app.ps1`](../../../droplet-scripts/update-app.ps1) (`DROPLET_IP`, `REMOTE_USER`). Add **`-i path/to/key`** if needed.

## Deploy from Windows (wrapper)

```powershell
cd <repo-root>
.\droplet-scripts\update-prod.ps1
```

## Pre-deploy: create a SQL backup (Windows → Droplet)

Creates a `.bak` on the droplet (inside the SQL container volume). Optional `-DownloadTo` pulls it back to your workstation.

```powershell
.\droplet-scripts\backup-prod.ps1

# With explicit key:
# .\droplet-scripts\backup-prod.ps1 -IdentityFile "C:\Users\you\.ssh\your_key"
#
# Download the .bak locally:
# .\droplet-scripts\backup-prod.ps1 -DownloadTo ".\backups"
```

## Post-deploy: one-command health check (Windows → Droplet)

```powershell
.\droplet-scripts\Test-DropletProdHealth.ps1

# If your key path differs:
# .\droplet-scripts\Test-DropletProdHealth.ps1 -IdentityFile "C:\Users\you\.ssh\your_key"
```

Equivalent with explicit key:

```powershell
.\droplet-scripts\update-app.ps1 -Environment prod -IdentityFile "C:\Users\you\.ssh\your_key"
```

## Compose status

```bash
ssh root@DROPLET_IP "cd ~/visa2026 && docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml ps"
```

## App logs (tail)

```bash
ssh root@DROPLET_IP "docker logs visa2026-prod-app-1 --tail 200"
```

If the container name differs, discover it:

```bash
ssh root@DROPLET_IP "docker ps --format '{{.Names}}' | grep visa2026-prod-app"
```

## HTTP check on the droplet (host → published app port)

Default publish is **`80:8080`** (`APP_PORT` in `.env.prod` overrides host side):

```bash
ssh root@DROPLET_IP "curl -sS -o /dev/null -w '%{http_code}\n' http://127.0.0.1:80/ || true"
```

Follow redirects if your app requires it:

```bash
ssh root@DROPLET_IP "curl -sSL -o /dev/null -w '%{http_code}\n' http://127.0.0.1:80/ || true"
```

## Disk and Docker

```bash
ssh root@DROPLET_IP "df -h"
ssh root@DROPLET_IP "docker system df"
```

## Image running on app container

```bash
ssh root@DROPLET_IP "docker inspect --format='{{.Config.Image}}' visa2026-prod-app-1"
```

## Re-run on-droplet update only (after you already synced files)

```bash
ssh root@DROPLET_IP "cd ~/visa2026 && ./update-app.sh prod"
```

## Importer / DB maintenance (`tools` profile)

See [docs/ENVIRONMENTS.md](../../../docs/ENVIRONMENTS.md) for **`--profile tools`** `db-updater` examples (seed / YAML). Run the same `docker compose …` line **on the droplet** inside `~/visa2026` after SSH, with prod project and `.env.prod`.
