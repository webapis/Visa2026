---
name: visa2026-windows-docker-desktop
description: >-
  Deploy and update Visa2026 on Windows with Docker Desktop (WSL2 Linux containers),
  Compose prod stack, Hub images webapia/visa2026 + postgres:16. Client-style folders
  (e.g. E:\visa2026-staging), pin APP_IMAGE_TAG, pull/up/smoke, SSH Hub-cred stubs,
  first-boot DB update. Use for on-prem Desktop staging/clients — not IIS publish,
  not Ubuntu Engine, not local F5. IIS remains supported until success gate.
disable-model-invocation: false
---

# Visa2026: Windows Docker Desktop deploy

## Goal

Run Visa2026 on **Windows + Docker Desktop** (Linux containers / WSL2) using Hub images and `docker-compose.prod.yml`. One compose project + one Postgres volume per client (or per slot).

**Verified pilot (2026-08-03):** Calik on-prem `10.100.128.25` — `E:\visa2026-staging`, project `visa2026-staging`, app **`:8081`**, DB `visa2026_staging_docker` @ host port **5434**, image **`1.0.0.644`**. Local `C:\visa2026-pilot` **waived** (insufficient disk on the workstation).

**Canonical runbook:** [docs/ON_PREM_WINDOWS_DOCKER_DESKTOP.md](../../../docs/ON_PREM_WINDOWS_DOCKER_DESKTOP.md)

**Strategy / dual path:** [docs/DOCKER_DEPLOY_STRATEGY_PLAN.md](../../../docs/DOCKER_DEPLOY_STRATEGY_PLAN.md) — **IIS still supported**; do not deprecate IIS from this skill.

**Scripts:** [scripts/windows-docker-desktop/](../../../scripts/windows-docker-desktop/) · helpers [docs/windows-docker-desktop/](../../../docs/windows-docker-desktop/)

**Experience:** read [learnings.md](./learnings.md) first; append after every deploy/update attempt.

**Commands:** [reference.md](./reference.md) · **Prompts:** [prompts.md](./prompts.md)

**Not this skill:** IIS ([visa2026-windows-iis-deploy](../visa2026-windows-iis-deploy/SKILL.md)), Ubuntu Engine ([setup-docker-engine](../setup-docker-engine/SKILL.md)), droplet, local F5 lifecycle ([visa2026-lifecycle-docker](../visa2026-lifecycle-docker/SKILL.md)), VISA2014 import ([visa2014-to-visa2026-import](../visa2014-to-visa2026-import/SKILL.md)).

---

## Before you start

1. Read **learnings.md**.
2. Confirm Docker Desktop is installed and **Engine running** (path may be `E:\Docker\resources\bin`, not Program Files).
3. Client layout **outside** the git repo (e.g. `E:\visa2026-staging`) with `docker-compose.prod.yml` + `.env.prod`.
4. Pin **`APP_IMAGE_TAG`** to a Hub tag that supports Postgres/Npgsql (e.g. `1.0.0.644`). Avoid stale SQL-era `:latest`.
5. Pick ports that do not fight IIS: on `.25`, IIS Staging=:8080; Docker staging uses **:8081** (IIS Demo stopped).
6. Secrets only in `.env.prod` — never commit.

---

## Standard deploy / update

| Step | Action |
|------|--------|
| Layout | `Prepare-Visa2026DesktopPilot.ps1 -TargetDir …` or copy compose from repo |
| Env | `PG_PASSWORD`, `DEVEXPRESS_LICENSEKEY`, `DB_NAME`, `APP_PORT`, `PG_HOST_PORT`, `APP_IMAGE_TAG` |
| Pull / up | `docker compose -p <project> --env-file .env.prod -f docker-compose.prod.yml pull` then `up -d` |
| First empty DB | One-shot `compose run … app -- --updateDatabase --forceUpdate --silent` if schema missing; optional `FORCE_XAF_DB_UPDATE=true` once then remove |
| Smoke | `http://<host>:<APP_PORT>/LoginPage` → 200 |
| Update | Change `APP_IMAGE_TAG`, `pull`, `up -d` app |

Full command blocks: [reference.md](./reference.md).

---

## Hard rules from pilot

1. **SSH Hub pull:** Windows `docker-credential-wincred` / Desktop helper fails non-interactively. Use stub helpers + `DOCKER_CONFIG` under the client folder (see learnings / reference), or pull from RDP.
2. **DataImporter for import** must match the **app image schema** (OrganizationType removed in current Module — old DI publish fails against new Postgres).
3. **Import** into Docker uses OData/in-process against the live app URL — not automatic on `compose up`. Use [visa2014-to-visa2026-import](../visa2014-to-visa2026-import/SKILL.md); point target SQL at container Postgres.
4. Schema + lookup seed on first app start is automatic; business data is not.

---

## Approval

| Class | Ask first? |
|-------|------------|
| Read-only smoke / `compose ps` / logs | No |
| `pull` / `up` / port or firewall change | Yes unless user said proceed |
| Wipe volumes / reset id-maps / stop IIS site for port | **Yes** |

---

## Chat openers

- `@visa2026-windows-docker-desktop` deploy or update Desktop stack on Windows
- Docker Hub pull fails over SSH / credential helper
- Pin tag, first-boot ForceUpdate, LoginPage smoke on LAN
