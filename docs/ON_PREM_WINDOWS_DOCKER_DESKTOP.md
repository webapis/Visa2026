# Visa2026 on Windows (Docker Desktop)

Runbook for deploying Visa2026 on a **Windows** host using **Docker Desktop**, **Compose**, and **`docker-compose.prod.yml`**.

**Status:** Multi-client **target** path. **Phase 3 pilot verified** on `10.100.128.25` (`E:\visa2026-staging`, `:8081`). Local `C:\visa2026-pilot` **waived** (disk). Agent skill: [visa2026-windows-docker-desktop](../.cursor/skills/visa2026-windows-docker-desktop/SKILL.md). **IIS remains fully supported** until Docker Desktop is proven — see [DOCKER_DEPLOY_STRATEGY_PLAN.md](./DOCKER_DEPLOY_STRATEGY_PLAN.md). Do not treat IIS as deprecated.

**Prerequisites:** [ON_PREM_PREREQUISITES.md](./ON_PREM_PREREQUISITES.md) (Windows + Docker Desktop section)

**Compose / env:** [ENVIRONMENTS.md](./ENVIRONMENTS.md) · root [`docker-compose.prod.yml`](../docker-compose.prod.yml) · [`.env.prod.example`](../.env.prod.example)

**HTTPS helpers:** [windows-docker-desktop/](./windows-docker-desktop/) (`docker-compose.https.override.yml`, `Caddyfile.example`)

**Strategy / diagrams:** [DOCKER_DEPLOY_STRATEGY_PLAN.md](./DOCKER_DEPLOY_STRATEGY_PLAN.md) · [DOCKER_DEPLOY_STRATEGY_DIAGRAMS.html](./DOCKER_DEPLOY_STRATEGY_DIAGRAMS.html)

**Not this path:**

| Path | Use instead |
|------|-------------|
| Native IIS (no containers) | [ON_PREM_WINDOWS_IIS.md](./ON_PREM_WINDOWS_IIS.md) — **still supported** |
| Ubuntu + Docker Engine | [ON_PREM_LINUX_SERVER.md](./ON_PREM_LINUX_SERVER.md) |
| DigitalOcean droplet | [visa2026-droplet-prod-deploy](../.cursor/skills/visa2026-droplet-prod-deploy/SKILL.md) |
| Dev-only hot reload / `scripts/local` | [ENVIRONMENTS.md](./ENVIRONMENTS.md) — not a client fleet deploy |

**Agent skill:** none yet. Promote `.cursor/skills/visa2026-windows-docker-desktop/` only after 2+ verified Desktop deploys (see strategy plan).

---

## Architecture

HTTP-only (simple pilot):

```text
LAN clients  -->  http://<host-ip>:APP_PORT
                        |
              Windows + Docker Desktop (WSL2)
                        |
         compose project (e.g. visa2026-prod)
              |                |
           app:8080         postgres:16
```

HTTPS (recommended for officers / Resminamalar Edit template):

```text
LAN clients  -->  https://<host>:443
                        |
                     Caddy (TLS)
                        |
                      app:8080  -->  postgres:16
```

- **Linux containers only** — `webapia/visa2026` + `postgres:16` (not Windows containers).
- Resminamalar **Edit template** needs a [secure context](https://developer.mozilla.org/en-US/docs/Web/API/Window/isSecureContext) — use HTTPS on the LAN (see [TEMPLATE_STAGING_EDIT.md](./TEMPLATE_STAGING_EDIT.md)).
- Confirm Docker Desktop **commercial licensing** for the client organization before production use.

---

## Files on the host

| Path | Required |
|------|----------|
| `C:\visa2026\docker-compose.prod.yml` | Yes |
| `C:\visa2026\.env.prod` | Yes |
| `C:\visa2026\docker-compose.https.override.yml` | For HTTPS |
| `C:\visa2026\Caddyfile` | For HTTPS |
| `C:\visa2026\README.txt` | Recommended (client name, last good `APP_IMAGE_TAG`) |

Secrets stay in `.env.prod` — **never commit**.

Copy HTTPS helpers from [windows-docker-desktop/](./windows-docker-desktop/).

---

## Phase 0 — Host ready

- [ ] Windows 10/11 or Windows Server (x64), **8+ GB RAM** (16 GB recommended), **100+ GB** free disk
- [ ] [Docker Desktop for Windows](https://docs.docker.com/desktop/setup/install/windows-install/) installed
- [ ] **WSL 2** backend; Linux containers mode
- [ ] Docker Desktop **start on login**
- [ ] Outbound HTTPS to Docker Hub
- [ ] Firewall: TCP **80/443** (HTTPS path) or `APP_PORT` (HTTP-only path)
- [ ] Docker Desktop license OK for this org

```powershell
docker version
docker compose version
docker run --rm hello-world
```

---

## Phase 1 — Layout and env

```powershell
New-Item -ItemType Directory -Force -Path C:\visa2026 | Out-Null
# Copy docker-compose.prod.yml and .env.prod.example -> .env.prod into C:\visa2026\
```

| Variable | Notes |
|----------|--------|
| `PG_PASSWORD` / `PG_USER` | Strong password; default user `postgres` |
| `DEVEXPRESS_LICENSEKEY` | Required |
| `DB_NAME` | Unique per stack on a shared host (e.g. `visa2026_acme_prod`) |
| `APP_PORT` | HTTP-only: `80`. With Caddy HTTPS: use **`8080`** and do not open 8080 on the LAN firewall |
| `APP_IMAGE_TAG` | **Pinned** Hub tag that supports **Postgres/Npgsql** (stale SQL-era `latest` fails with `Keyword not supported: 'host'`) |
| `IMPORTER_IMAGE_TAG` | Pin when using importer/tools |
| `PG_HOST_PORT` | Host bind for Postgres (default `5432`). Use e.g. `5433` if local Postgres already owns 5432 |
| `CADDY_HTTP_PORT` / `CADDY_HTTPS_PORT` | Optional; defaults 80 / 443 |

---

## Image tag pinning (`APP_IMAGE_TAG`)

Do **not** rely on floating `latest` for multi-client production.

1. Publish/CI must have pushed `webapia/visa2026:<tag>` (often Module `AssemblyVersion` — see publish workflow / [DEPLOYMENT_LIFECYCLE_EXPERIENCE.md](./DEPLOYMENT_LIFECYCLE_EXPERIENCE.md)).
2. Set that exact value in each client `.env.prod`.
3. Record **previous** and **new** tags in `C:\visa2026\README.txt` before every update.
4. Only promote tags that passed CI (unit + relevant E2E).

```env
APP_IMAGE_TAG=1.0.0.250
IMPORTER_IMAGE_TAG=1.0.0.250
```

---

## Per-client isolation

One company / tenant = **one compose stack** (not multi-tenant in one DB).

| Concern | Convention |
|---------|------------|
| Folder | `C:\visa2026` (single client host) or `C:\visa2026-<client>\` (shared host) |
| Compose project | `-p visa2026-prod` or `-p visa2026-<client>` (unique on the machine) |
| `DB_NAME` | Unique, e.g. `visa2026_acme_prod` |
| Ports | Unique `APP_PORT` / Caddy ports per stack on a shared host |
| Volumes | Named volumes are prefixed by compose project name — keep project names unique |
| Secrets | Separate `.env.prod` per stack; never share `PG_PASSWORD` across clients |

---

## First deploy (HTTP)

```powershell
cd C:\visa2026
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml pull
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml up -d
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml ps
curl.exe -s -o NUL -w "%{http_code}`n" http://127.0.0.1/LoginPage
```

Browser: `http://<host-ip>/LoginPage` — **Admin** / empty password (change after). Expect **200** or **302**.

### One-shot schema

Set `FORCE_XAF_DB_UPDATE=true` once if needed, recreate app, then remove the flag — [ENVIRONMENTS.md](./ENVIRONMENTS.md).

```powershell
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml up -d --force-recreate --no-deps app
```

---

## HTTPS (Caddy reverse proxy)

**Why:** Officers on the LAN need `https://` for Resminamalar **Edit template** (File System Access API). Same requirement as IIS HTTPS — [TEMPLATE_STAGING_EDIT.md](./TEMPLATE_STAGING_EDIT.md).

**Pattern:** Caddy terminates TLS on 443 and reverse-proxies to `app:8080` on the compose network.

### Setup

1. Copy helpers into `C:\visa2026\`:
   - [windows-docker-desktop/docker-compose.https.override.yml](./windows-docker-desktop/docker-compose.https.override.yml)
   - [windows-docker-desktop/Caddyfile.example](./windows-docker-desktop/Caddyfile.example) → rename to `Caddyfile`
2. Edit `Caddyfile`: set hostname (or use `:443` block). Default example uses `tls internal` (Caddy local CA).
3. In `.env.prod`: `APP_PORT=8080`.
4. Windows Firewall: allow inbound **TCP 443** (and **80** if redirect). Do **not** allow LAN inbound to **8080**.
5. Start with both compose files:

```powershell
cd C:\visa2026
docker compose -p visa2026-prod --env-file .env.prod `
  -f docker-compose.prod.yml -f docker-compose.https.override.yml pull
docker compose -p visa2026-prod --env-file .env.prod `
  -f docker-compose.prod.yml -f docker-compose.https.override.yml up -d
```

6. Smoke:

```powershell
curl.exe -sk -o NUL -w "%{http_code}`n" https://127.0.0.1/LoginPage
```

7. Officers use `https://<hostname-or-ip>/LoginPage`. Trust the cert:
   - **Enterprise CA:** put cert/key into Caddy (see [Caddy TLS docs](https://caddyserver.com/docs/caddyfile/directives/tls)) instead of `tls internal`.
   - **`tls internal`:** export/trust Caddy’s local root on officer PCs (browser warning until trusted).

Optional: set `TEMPLATE_EDIT_STAGING_ENABLED=true` in app config/env when using Resminamalar local sandbox (see template staging doc).

---

## Backup / update / rollback checklist

### Before every app update

1. [ ] Note current `APP_IMAGE_TAG` in `README.txt` as **last known good**.
2. [ ] Confirm new tag exists on Docker Hub and passed CI.
3. [ ] Backup Postgres (logical dump):

```powershell
cd C:\visa2026
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
New-Item -ItemType Directory -Force -Path C:\visa2026\backups | Out-Null
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml exec -T postgres `
  pg_dump -U postgres -d $env:DB_NAME | Set-Content -Encoding utf8 "C:\visa2026\backups\pg-$stamp.sql"
# Or pass DB name explicitly:
# docker compose ... exec -T postgres pg_dump -U postgres -d visa2026_prod > C:\visa2026\backups\pg-$stamp.sql
```

Prefer redirect via `docker compose exec` into a file on the host. Example with cmd-style redirect:

```powershell
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml exec -T postgres pg_dump -U postgres -d visa2026_prod | Out-File -Encoding utf8 C:\visa2026\backups\pg-$stamp.sql
```

Optional: also `docker volume` backup of `*_postgres_data_prod` if you use volume snapshot tooling.

### Update

```powershell
cd C:\visa2026
# Edit .env.prod: APP_IMAGE_TAG=<new>
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml pull app
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml up -d --no-deps app
# If using HTTPS override, include -f docker-compose.https.override.yml on up/pull as needed
curl.exe -s -o NUL -w "%{http_code}`n" http://127.0.0.1:8080/LoginPage
# or: curl.exe -sk ... https://127.0.0.1/LoginPage
```

### Rollback (app image)

1. Set `APP_IMAGE_TAG` back to last known good in `.env.prod`.
2. `pull app` + `up -d --no-deps app` again.
3. Smoke login.

### Restore DB (if schema/data broken)

```powershell
# Stop app first to avoid writes
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml stop app
Get-Content C:\visa2026\backups\pg-YYYYMMDD-HHMMSS.sql -Raw | docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml exec -T postgres psql -U postgres -d visa2026_prod
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml start app
```

Adjust DB name to match `.env.prod`.

---

## Ops notes

| Topic | Guidance |
|-------|----------|
| **Logs** | `docker compose -p visa2026-prod --env-file .env.prod logs app --tail 80` |
| **Postgres** | Named volume `postgres_data_prod` (project-prefixed) |
| **Data protection keys** | Volume `app_dataprotection_keys_prod` — do not delete casually |
| **IIS on same box** | Do not bind IIS and Caddy/compose to the same host ports |
| **Compose files with HTTPS** | Always pass `-f docker-compose.prod.yml -f docker-compose.https.override.yml` together |

---

## Troubleshooting

| Symptom | Action |
|---------|--------|
| `docker` / daemon down | Start Docker Desktop; wait for engine |
| WSL / virtualization | Enable WSL 2 + BIOS virtualization |
| Pull timeout | Outbound HTTPS / proxy to Docker Hub |
| Port in use | Change `APP_PORT` / Caddy ports; stop IIS site on 80/443 |
| HTTP works, HTTPS cert warning | Trust Caddy/`tls internal` or install enterprise cert |
| Resminamalar Edit template blocked | Officers must use **https://**; enable template staging flags per [TEMPLATE_STAGING_EDIT.md](./TEMPLATE_STAGING_EDIT.md) |
| Login refused from LAN | Firewall allow 443 (or APP_PORT); test curl on host first |
| Schema drift | One-shot `FORCE_XAF_DB_UPDATE` — [ENVIRONMENTS.md](./ENVIRONMENTS.md) |

---

## Dual path (IIS)

| | Docker Desktop (this doc) | IIS |
|--|---------------------------|-----|
| Role | **Target** for multi-client Windows rollouts | **Supported** current path |
| Artifact | Hub image + compose (+ optional Caddy) | Publish + app pool + host Postgres |
| Deprecate IIS? | Only after strategy **success gate** | Keep using until then |

Strategy: [DOCKER_DEPLOY_STRATEGY_PLAN.md](./DOCKER_DEPLOY_STRATEGY_PLAN.md).

## Pilot log

### 2026-08-03 — Live pilot attempt (Docker Desktop on this workstation)

- **Docker Desktop:** installed; engine **29.3.1** started successfully.
- **Layout:** `C:\visa2026-pilot` (`-p visa2026-pilot`, `APP_PORT=9080`, `DB_NAME=visa2026_pilot`).
- **Port clash:** host **5432** already in use → set `PG_HOST_PORT=5433` (document in runbook / checklist).
- **Postgres:** `postgres:16` container **healthy** on `127.0.0.1:5433`.
- **App (Hub `webapia/visa2026:latest`, created 2026-05-26):** exits with `System.ArgumentException: Keyword not supported: 'host'.` — image still treats the connection string as SQL Server / SqlClient; current `docker-compose.prod.yml` uses Npgsql (`Host=postgres;...;EFCoreProvider=Postgres`).
- **Local rebuild:** blocked — `apt-get` against `archive.ubuntu.com` returns **403 Forbidden** inside the build (Times New Roman / jammy packages). Fonts were staged under `docker/fonts/msttcore/` (gitignored); need corporate `HTTPS_PROXY` for apt or a pre-published Postgres-capable Hub tag.
- **`.dockerignore`:** added `dist/`, `_agent_build_out*`, `.publish*`, etc. (context was ~10GB before; ~40MB after).
- **Smoke `/LoginPage`:** not reached (app never stayed up).
- **Unblock options:** (1) publish current AssemblyVersion image to Hub via CI / machine with apt access; (2) retry `scripts/local/Build-DockerImages.ps1` with `HTTPS_PROXY` set; (3) then re-run [PILOT_CHECKLIST.md](./windows-docker-desktop/PILOT_CHECKLIST.md).

### 2026-08-03 — Phase 3 prep (this workstation)

- **Result:** blocked — Docker Desktop / `docker` CLI **not installed** (not on PATH; no `C:\Program Files\Docker`).
- **Done:** pilot checklist [windows-docker-desktop/PILOT_CHECKLIST.md](./windows-docker-desktop/PILOT_CHECKLIST.md); prepare script [scripts/windows-docker-desktop/Prepare-Visa2026DesktopPilot.ps1](../scripts/windows-docker-desktop/Prepare-Visa2026DesktopPilot.ps1); layout `C:\visa2026-pilot\` (compose + `.env.prod` template, `APP_PORT=9080`, `DB_NAME=visa2026_pilot`).
- **Next on a Docker-ready host:** edit secrets + pin `APP_IMAGE_TAG`, then complete the checklist (up, smoke, backup, rollback). Append a new dated entry here when the pilot passes.
### 2026-08-03 — On-prem staging layout (`10.100.128.25`)

- **Folder:** `E:\visa2026-staging` (E: ~163 GB free; separate from IIS `C:\inetpub\visa2026-staging`).
- **Copied:** `docker-compose.prod.yml`, HTTPS override helpers, `.env.prod` (`APP_PORT=9080`, `DB_NAME=visa2026_staging_docker`, `PG_HOST_PORT=5434`, `APP_IMAGE_TAG=1.0.0.644`; secrets seeded from IIS `C:\visa2026\env\staging.env` DevExpress key + `SA_PASSWORD` as `PG_PASSWORD` for container Postgres).
- **Compose project:** `visa2026-staging`.
- **Blocked for up:** Docker Engine/Desktop **not installed** on this host (`com.docker.service` missing; no `docker` on PATH). Install Docker Desktop (or approved engine) on `.25`, then:
  `cd /d E:\visa2026-staging` → `docker compose -p visa2026-staging --env-file .env.prod -f docker-compose.prod.yml pull` → `up -d` → smoke `http://10.100.128.25:9080/LoginPage`.
- **Do not** reuse IIS Staging port 8080 / `visa2026_staging` DB for this Docker stack unless intentionally sharing.
### 2026-08-03 — On-prem staging stack UP (`10.100.128.25`)

- **Docker Desktop:** installed under **`E:\Docker`** (not Program Files); engine **29.6.2** / Desktop **4.84.0** was already running (desktop shortcut).
- **Folder:** `E:\visa2026-staging` — image `webapia/visa2026:1.0.0.644`, project `visa2026-staging`, app **`:9080`**, Postgres host **`127.0.0.1:5434`**, DB `visa2026_staging_docker`.
- **SSH pull caveat:** Docker Hub credential helper (`wincred`/`desktop`) fails over non-interactive SSH. Workaround: stub helpers in `E:\visa2026-staging\bin\` + `DOCKER_CONFIG=E:\visa2026-staging\.docker` (or pull from an interactive RDP session).
- **First boot:** empty Postgres needed `--updateDatabase --forceUpdate --silent` once; then app stayed up.
- **Smoke:** `http://127.0.0.1:9080/LoginPage` → **HTTP 200** (also try `http://10.100.128.25:9080/LoginPage` from LAN).
- **Follow-up:** remove `FORCE_XAF_DB_UPDATE` from `.env.prod` after ModuleUpdaters have run; keep IIS Staging (:8080) separate.
### 2026-08-03 — Local pilot waived; skill promoted

- **Waive:** `C:\visa2026-pilot` full smoke not required — insufficient disk on the workstation; staging Desktop stack already shipped on `10.100.128.25`.
- **Port:** Docker staging moved **9080 → 8081** for LAN reachability (IIS Demo stopped).
- **Skill:** `.cursor/skills/visa2026-windows-docker-desktop/` created from on-prem pilot learnings.
