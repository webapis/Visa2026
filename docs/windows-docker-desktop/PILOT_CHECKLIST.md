# Phase 3 — Docker Desktop pilot checklist

Run on a Windows host with **Docker Desktop** installed (WSL2, Linux containers). Canonical steps: [ON_PREM_WINDOWS_DOCKER_DESKTOP.md](../ON_PREM_WINDOWS_DOCKER_DESKTOP.md).

**Goal:** prove pull/up, `/LoginPage` smoke, backup dump, and rollback of `APP_IMAGE_TAG` on a **non-prod** stack. IIS stays untouched.

## Prerequisites

- [ ] Docker Desktop installed and running (`docker version`, `docker compose version`)
- [ ] Outbound pull to Docker Hub works (`docker pull hello-world`)
- [ ] DevExpress license key available for `.env.prod`
- [ ] Choose a pinned `APP_IMAGE_TAG` that exists on Hub (or use a known published tag)

## Prepare folder

From the Visa2026 repo (PowerShell):

```powershell
.\scripts\windows-docker-desktop\Prepare-Visa2026DesktopPilot.ps1
```

Creates `C:\visa2026-pilot\` with compose files and `.env.prod` (edit secrets before up).

## Deploy (HTTP pilot first)

- [ ] Edit `C:\visa2026-pilot\.env.prod` — `PG_PASSWORD`, `DEVEXPRESS_LICENSEKEY`, `APP_IMAGE_TAG`, `DB_NAME=visa2026_pilot`, `APP_PORT=9080`, and `PG_HOST_PORT=5433` if host **5432** is already taken (common with local Postgres)
- [ ] `cd C:\visa2026-pilot`
- [ ] `docker compose -p visa2026-pilot --env-file .env.prod -f docker-compose.prod.yml pull`
- [ ] `docker compose -p visa2026-pilot --env-file .env.prod -f docker-compose.prod.yml up -d`
- [ ] `docker compose -p visa2026-pilot --env-file .env.prod ps` — app + postgres healthy/up
- [ ] `curl.exe -s -o NUL -w "%{http_code}\n" http://127.0.0.1:9080/LoginPage` — expect 200/302
- [ ] Browser login Admin (change password after)

## Backup

- [ ] `pg_dump` into `C:\visa2026-pilot\backups\` (see runbook)
- [ ] Record current `APP_IMAGE_TAG` in `README.txt`

## Update + rollback drill

- [ ] Note tag A (current). Set tag B (or re-pull same tag if only one available) / or simulate rollback A→A after intentional wrong tag
- [ ] `pull app` + `up -d --no-deps app`
- [ ] Smoke LoginPage again
- [ ] Set `APP_IMAGE_TAG` back to last known good; pull/up; smoke again

## Optional HTTPS (Caddy)

- [ ] Copy override + `Caddyfile`; set `APP_PORT=8080`; firewall 443
- [ ] `up` with both compose files
- [ ] `curl.exe -sk ... https://127.0.0.1/LoginPage`
- [ ] Confirm Resminamalar Edit template only if HTTPS trusted on the test browser

## Pass criteria (one pilot)

- [ ] Stack stays up after reboot of Docker Desktop (or host reboot + Desktop autostart)
- [ ] Smoke OK after update
- [ ] Rollback to previous tag works
- [ ] Backup file non-empty

## After pass

1. Append a short dated note under **Pilot log** in [ON_PREM_WINDOWS_DOCKER_DESKTOP.md](../ON_PREM_WINDOWS_DOCKER_DESKTOP.md).
2. Repeat on a second host/client when ready (2+ pilots → promote skill).
3. Do **not** deprecate IIS until success gate ([DOCKER_DEPLOY_STRATEGY_PLAN.md](../DOCKER_DEPLOY_STRATEGY_PLAN.md)).

## Image requirement

- Hub tag must be a **Postgres / Npgsql** build matching current `docker-compose.prod.yml`. Stale SQL Server-era `latest` fails with `Keyword not supported: 'host'`.
- Prefer a pinned CI-published `AssemblyVersion` tag, or `scripts/local/Build-DockerImages.ps1` (needs apt network or `HTTPS_PROXY` + bundled fonts under `docker/fonts/msttcore/`).

## Blocked on this workstation (2026-08-03)

Agent Phase 3 prep ran on a machine where **`docker` was not installed / not on PATH**. Live pull/up was not executed. Install Docker Desktop, then run this checklist.