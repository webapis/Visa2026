---
name: visa2026-droplet-prod-deploy
description: >-
  Production deploy to the DigitalOcean droplet via droplet-scripts/update-prod.ps1 (or update-app.ps1),
  post-deploy health checks (compose ps, HTTP, app logs), and SSH-based Docker triage when the rollout
  fails (pull errors, crash loops, SqlException / schema drift, disk, wrong image tag). Use when the user
  deploys prod from Windows, droplet Docker misbehaves after update, or wants a runbook for safe prod rollout.
disable-model-invocation: false
---

# Visa2026: droplet production deploy and health triage

## Goal

Run the **safe prod app update** (pull + recreate **app** only; SQL volume untouched), then **verify** the deployment and **investigate** common droplet Docker failures without ad-hoc guesswork.

**Canonical docs:** [docs/PRODUCTION_DEPLOYMENT_RUNBOOK.md](../../../docs/PRODUCTION_DEPLOYMENT_RUNBOOK.md), [docs/DEBUGGING_DOCKER_DEPLOYMENTS.md](../../../docs/DEBUGGING_DOCKER_DEPLOYMENTS.md), [docs/ENVIRONMENTS.md](../../../docs/ENVIRONMENTS.md).

**Overlap:** Deep local-vs-container version checks and scripted local lifecycle live in **[`.cursor/skills/visa2026-lifecycle-docker/SKILL.md`](../visa2026-lifecycle-docker/SKILL.md)**; use this skill when the **target is the droplet** over SSH.

**Copy-paste SSH / compose commands:** [reference.md](./reference.md).

### Chat openers (copy-paste)

- `@.cursor/skills/visa2026-droplet-prod-deploy/` — **Deploy prod to droplet** with `update-prod.ps1` and verify health.
- **Prod deploy failed on droplet — Docker logs show …** (paste redacted error class / first line only).

---

## 1. Approval mode (ask before each command)

When the user requests “step-by-step” or “ask for approval”, do **not** run a batch of commands.
Propose each command and wait for an explicit **OK** before running it.

Deterministic prod sequence (Windows operator machine → droplet):

```powershell
.\droplet-scripts\backup-prod.ps1
.\droplet-scripts\update-prod.ps1
.\droplet-scripts\Test-DropletProdHealth.ps1
```

If the deploy fails, or the health check is unhealthy, switch to **read-only** triage first (no restarts), using the commands in [reference.md](./reference.md) (compose `ps`, `docker logs`, `curl`, `df`, `docker system df`, `docker inspect`). Only propose the next corrective action after the error is classified.

---

## 2. Approval policy (critical operations)

This skill operates on **production infrastructure**. Follow this rule:

- **Always ask for explicit “OK”** before running any **critical operation**.
- You may run **read-only diagnostics** (status, logs, curl, disk) without asking, unless the user explicitly requested step-by-step approval for everything.

### Critical operations (require OK)

- **Deploy / restart**
  - `.\droplet-scripts\update-prod.ps1`
  - `.\droplet-scripts\update-app.ps1` (any environment)
  - Running `update-app.sh` on the droplet
  - Any `docker compose up …` / `docker restart …`
- **Production env / flags**
  - `.\droplet-scripts\Set-ForceXafDbUpdate.ps1` (enable/disable)
  - Editing or uploading `.env.prod` changes that affect runtime behavior
- **Database & data mutation**
  - Backups / restore operations
  - Any importer `db-updater` runs that write business data (YAML import)
- **Destructive Docker / disk operations**
  - `docker compose down -v`
  - `docker system prune` / `docker image prune` when it may impact rollback
  - Running `fresh-install.*` (explicitly forbidden on prod unless the operator is intentionally rebuilding)

### Safe diagnostics (OK not required)

- `docker compose … ps`
- `docker logs … --tail …`
- `curl http://127.0.0.1:<APP_PORT>/…` from the droplet
- `df -h`, `docker system df`
- `docker inspect …` (image/env visibility checks)

---

## 3. What `update-prod.ps1` does

[`droplet-scripts/update-prod.ps1`](../../../droplet-scripts/update-prod.ps1) is a thin wrapper: it calls [`update-app.ps1`](../../../droplet-scripts/update-app.ps1) with **`-Environment prod`** and a fixed **`-IdentityFile`** (`id_ed25519_visa`). That script:

1. SCPs `docker-compose.prod.yml`, `.env.prod`, and `update-app.sh` to **`~/visa2026`** on the droplet (see **`DROPLET_IP`** in `update-app.ps1`).
2. Runs **`update-app.sh prod`**, which **`docker compose pull app`** and **`up -d --no-deps app`** for project **`visa2026-prod`** — **not** SQL Server.

If your SSH key path differs, call `update-app.ps1 -Environment prod -IdentityFile <path>` instead.

---

## 4. Pre-flight (human, before clicking deploy)

Do **not** skip on production without explicit operator approval:

- Release tested; deployment window and rollback owner known.
- SQL backup taken and verified when migrations are risky ([runbook §3–§5](../../../docs/PRODUCTION_DEPLOYMENT_RUNBOOK.md)).
- **`APP_IMAGE_TAG`** in **local** `.env.prod` matches an image **pushed to Docker Hub** (CI publishes tags from **`Visa2026.Module` AssemblyVersion** — see [`.cursor/skills/ci-failed-triage/SKILL.md`](../ci-failed-triage/SKILL.md) if pulls fail).
- Droplet has **disk headroom** and Docker is healthy (quick check: SSH `df -h` / `docker info` — [reference.md](./reference.md)).

---

## 5. Deploy (from repo root on Windows)

```powershell
.\droplet-scripts\update-prod.ps1
```

Expect exit code **0**. If **SCP/SSH** fails, fix keys, firewall, or `DROPLET_IP` in `update-app.ps1` — not the app container.

---

## 6. Post-deploy health checks (in order)

Run these after the script succeeds (or when verifying an already-running prod stack).

### Fast path: run the health-check script

From repo root on Windows:

```powershell
.\droplet-scripts\Test-DropletProdHealth.ps1
```

Use `-IdentityFile` if your SSH key path differs, or `-Environment dev` to check the dev stack on the droplet.

### Manual path (same checks, one-by-one)

1. **Compose status** — `app` and `sqlserver` **Up**; note if `app` restarts repeatedly ([reference.md](./reference.md)).
2. **App logs (first errors)** — tail logs; expect **`Application started`** / listening on **`8080`** inside the container; scan for the **first** `fail:`, **`SqlException`**, **`Invalid column name`** ([reference.md](./reference.md)).
3. **HTTP from the droplet** — `curl` **loopback** on **`APP_PORT`** (default **80** mapped to app **8080**) — expect **2xx** or a sane redirect chain for `/` ([reference.md](./reference.md)).
4. **Optional:** Browser smoke: login, one read path, one write path ([runbook §4](../../../docs/PRODUCTION_DEPLOYMENT_RUNBOOK.md)).

**Do not paste** production logs with secrets or PII into chat; summarize error **type** and **first** stack line, or use redacted excerpts ([DEBUGGING_DOCKER_DEPLOYMENTS.md §3](../../../docs/DEBUGGING_DOCKER_DEPLOYMENTS.md)).

---

## 7. Investigation map (droplet Docker)

Classify by **first** stable signal, then apply **one** fix path before repeating deploy.

| Signal | Likely cause | Actions |
|--------|----------------|---------|
| `docker compose pull` / **manifest unknown** / **not found** | Wrong or unpublished **`APP_IMAGE_TAG`** | Confirm tag on registry; align `.env.prod` with CI; see [lifecycle-docker §7](../visa2026-lifecycle-docker/SKILL.md) / [ci-failed-triage](../ci-failed-triage/SKILL.md). |
| Container **Restarting** / exit non-zero | Crash on startup (config, license, DB) | Logs; compare `.env.prod` to `.env.prod.example`; **DevExpress** license env; SQL **reachable** from `app` network. |
| **`SqlException` `Invalid column name`** | Schema drift vs image | One-off DB update path: local **[`Compose-UpdateDatabase.ps1`](../../../scripts/local/lifecycle-docker/Compose-UpdateDatabase.ps1)** is for **local** compose; on droplet use **`db-updater` / tools profile** or documented prod DB procedure — then **`FORCE_XAF_DB_UPDATE`** once ([ENVIRONMENTS.md](../../../docs/ENVIRONMENTS.md), [`Set-ForceXafDbUpdate.ps1`](../../../droplet-scripts/Set-ForceXafDbUpdate.ps1)). |
| **No space left on device** / pull write errors | Disk full | `df -h`, `docker system df`, prune **images** carefully; **do not** `prune -v` on prod without backup story. |
| **Works locally, fails on droplet** | Env, data, resources, proxy | [DEBUGGING_DOCKER_DEPLOYMENTS.md](../../../docs/DEBUGGING_DOCKER_DEPLOYMENTS.md) parity checklist (`docker inspect` image id, env). |

After a config or env fix, re-upload env/compose and rerun **`update-app.sh prod`** or the equivalent **`compose up -d --no-deps app`** (see [reference.md](./reference.md)).

---

## 8. Related automation (do not confuse)

| Script | Use |
|--------|-----|
| `update-app.ps1` / `update-prod.ps1` | Safe **app-only** rollout |
| `Set-ForceXafDbUpdate.ps1` | Toggle **`FORCE_XAF_DB_UPDATE`** on droplet + recreate **app** |
| `fresh-install.ps1` / `fresh-install.sh` | **Destructive** — **never** on prod ([runbook §2](../../../docs/PRODUCTION_DEPLOYMENT_RUNBOOK.md)) |

---

## 9. Escalation

Stop and get explicit human approval before:

- `docker compose down -v`, volume deletes, or **`fresh-install`**
- Restoring DB from backup or running bulk importers on prod
- Changing production secrets without a rollback plan
