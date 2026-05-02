# Scripts layout

Scripts are grouped so you can tell **where they run** and **what they target**.

## `scripts/local/` — developer workstation

Use on your **own PC** with **Docker Desktop** (or any machine where you edit the repo and run compose locally).

| Script | Purpose |
|--------|---------|
| `Build-DockerImages.ps1` | Build `webapia/visa2026` / importer images locally (same build-args as CI). Optional `-DeployLocal` to recreate the **local** compose app container. |
| `Start-ComposeWatch.ps1` | Optional **hot reload**: `docker-compose.watch.yml` (SDK + `dotnet watch`). Separate compose project from prod-like local stacks. |
| `Export-DockerAppLogs.ps1` | Dump compose **app** logs into `agent-local/` (e.g. for IDE/AI review). |

**Typical env files here:** `.env.local`, `.env.dev` (paths passed into scripts or compose).

---

## `droplet-scripts/` — server (staging / production)

Use on the **DigitalOcean droplet** (or after syncing repo there). These scripts **upload**, **pull Hub images**, or **mutate server data**.

| Script | Purpose |
|--------|---------|
| `update-app.ps1` / `.sh` | Pull and restart app for **prod or dev** stack on the server (`-Environment`). |
| `sync-to-droplet.ps1` | Copy compose/env/scripts to the droplet. |
| `seed-data.ps1` | Interactive / scripted seeding (`-Environment dev` or `prod`). |
| `fresh-install.ps1` / `.sh` | **Destructive** reset — **not** for production (see runbook). |

**“Development” vs “production” on the server** is **not** under `scripts/local/`; it is chosen with **`-Environment dev`** vs **`-Environment prod`** in `droplet-scripts`, and with **`docker-compose.dev.yml`** vs **`docker-compose.prod.yml`** on the droplet.

---

## Quick mental model

- **`scripts/local/`** → *my laptop, local Docker, local volumes.*
- **`droplet-scripts/`** → *remote host, real deploys, `.env.prod` / `.env.dev` on the server.*

For compose file reference and importer commands, see [docs/ENVIRONMENTS.md](../docs/ENVIRONMENTS.md). For production safety, see [docs/PRODUCTION_DEPLOYMENT_RUNBOOK.md](../docs/PRODUCTION_DEPLOYMENT_RUNBOOK.md).
