# visa2026-windows-docker-desktop — reference

## Paths (Calik `.25`)

### Production (Docker Desktop)

| Item | Value |
|------|--------|
| Layout | `E:\visa2026-prod` |
| Compose project | `visa2026-prod` |
| Docker CLI | `E:\Docker\resources\bin\docker.exe` / `docker-compose.exe` |
| App URL | `http://10.100.128.25/LoginPage` (`APP_PORT=80`) |
| Image | `webapia/visa2026:1.0.0.644` |
| DB | `visa2026_prod_docker` via `127.0.0.1:5435` |
| Hub stub config | `E:\visa2026-prod\.docker` + `bin\docker-credential-*.cmd` |
| Note | IIS `Visa2026-Prod` must stay **Stopped** while Docker owns `:80` |

### Staging (Docker Desktop)

| Item | Value |
|------|--------|
| Layout | `E:\visa2026-staging` |
| Compose project | `visa2026-staging` |
| App URL | `http://10.100.128.25:8081/LoginPage` |
| Image | `webapia/visa2026:1.0.0.644` |
| DB | `visa2026_staging_docker` via `127.0.0.1:5434` |
| Hub stub config | `E:\visa2026-staging\.docker` + `bin\docker-credential-*.cmd` |

## Prepare layout (dev PC or server)

```powershell
cd <repo>
# Production (binds host :80 — stop IIS Visa2026-Prod first on shared hosts)
.\scripts\windows-docker-desktop\Prepare-Visa2026DesktopPilot.ps1 `
  -TargetDir 'E:\visa2026-prod' `
  -ProjectName visa2026-prod `
  -DbName visa2026_prod_docker `
  -AppPort 80 `
  -PgHostPort 5435 `
  -AppImageTag '1.0.0.644'

# Staging
.\scripts\windows-docker-desktop\Prepare-Visa2026DesktopPilot.ps1 `
  -TargetDir 'E:\visa2026-staging' `
  -ProjectName visa2026-staging `
  -DbName visa2026_staging_docker `
  -AppPort 8081 `
  -PgHostPort 5434 `
  -AppImageTag '1.0.0.644'
```

## Pull / up (on host; interactive RDP preferred for Hub)

```powershell
$env:Path = 'E:\Docker\resources\bin;' + $env:Path
cd E:\visa2026-staging
docker compose -p visa2026-staging --env-file .env.prod -f docker-compose.prod.yml pull
docker compose -p visa2026-staging --env-file .env.prod -f docker-compose.prod.yml up -d
```

## SSH-safe Hub pull (stub credential helpers)

```powershell
$stub = 'E:\visa2026-staging\bin'
$env:Path = "$stub;" + ((($env:Path -split ';') | Where-Object { $_ -and ($_ -notmatch 'Docker') }) -join ';')
$env:DOCKER_CONFIG = 'E:\visa2026-staging\.docker'
# config.json: {"credsStore":"desktop","auths":{}}
# stub docker-credential-desktop.cmd / wincred.cmd echo {}
& 'E:\Docker\resources\bin\docker.exe' --config $env:DOCKER_CONFIG pull webapia/visa2026:1.0.0.644
```

## First-boot DB update

```powershell
docker compose -p visa2026-staging --env-file .env.prod -f docker-compose.prod.yml run --rm --no-deps `
  --entrypoint /app/visa2026-entrypoint.sh app -- --updateDatabase --forceUpdate --silent
docker compose -p visa2026-staging --env-file .env.prod -f docker-compose.prod.yml up -d app
```

## Firewall (LAN)

```powershell
New-NetFirewallRule -DisplayName 'Visa2026 Docker Staging 8081' -Direction Inbound `
  -Action Allow -Protocol TCP -LocalPort 8081 -Profile Any -EdgeTraversalPolicy Allow
```

## Import (separate skill)

Point `OnPrem-Sync.ps1 -Profile Demo` (or dedicated sync root) `VISA2026_*_SQL_CONNECTION` at Docker Postgres and API at `:8081`. Keep DataImporter publish aligned with app image. See visa2014-to-visa2026-import.
