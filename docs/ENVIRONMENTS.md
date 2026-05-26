# Visa2026 Environment Split (Prod + Dev on One Host)

This guide shows how to run production and development stacks safely on the same droplet without mixing data.

---

## Files

- `docker-compose.prod.yml`
- `docker-compose.dev.yml`
- `docker-compose.watch.yml` — optional local **hot reload** (SDK + `dotnet watch`, see below)
- `scripts/README.md` — which scripts are for **local workstation** vs **droplet**
- [ON_PREM_PREREQUISITES.md](./ON_PREM_PREREQUISITES.md) — on-prem **hardware/software** (Ubuntu recommended)
- [ON_PREM_LINUX_SERVER.md](./ON_PREM_LINUX_SERVER.md) — **Ubuntu on company LAN** ([setup-docker-engine](../.cursor/skills/setup-docker-engine/SKILL.md), `scripts/linux/`)
- [ON_PREM_WINDOWS_IIS.md](./ON_PREM_WINDOWS_IIS.md) — **Windows Server IIS** (no Docker; `scripts/windows-iis/`)
- [ON_PREM_WINDOWS_SERVER.md](./ON_PREM_WINDOWS_SERVER.md) — **legacy** Windows Server + WSL
- `.env.prod.example`
- `.env.dev.example`
- [DEBUGGING_DOCKER_DEPLOYMENTS.md](./DEBUGGING_DOCKER_DEPLOYMENTS.md) — troubleshooting when Droplet and local Docker differ

Create real env files from examples:

```bash
cp .env.prod.example .env.prod
cp .env.dev.example .env.dev
```

Set strong values for `SA_PASSWORD` and `DEVEXPRESS_LICENSEKEY` in both files.

---

## Start Production Stack

```bash
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml up -d
```

### One-shot: force XAF ModuleUpdaters (e.g. `ReportsUpdater`)

Release builds use `UpdateOldDatabase`. With `CheckCompatibilityType.DatabaseSchema`, the DB can look “current” while new `ModuleUpdater` logic never runs. Set **`FORCE_XAF_DB_UPDATE=true`** in `.env.prod` (or export it before `compose up`), **rebuild/restart the app once**, confirm data (e.g. reports / `ReportVisibilities`), then **remove the variable** so every startup is not slow.

You should see a console line in **`docker logs`** when the flag is on. The compose files pass **`FORCE_XAF_DB_UPDATE`** through from the env file when set.

For schema drift symptoms (`Invalid column name`), one-off **`--updateDatabase --forceUpdate`**, and related deploy notes, see **[DEPLOYMENT_LIFECYCLE_EXPERIENCE.md](./DEPLOYMENT_LIFECYCLE_EXPERIENCE.md)** and the Agent skill **[`.cursor/skills/visa2026-lifecycle-docker/SKILL.md`](../.cursor/skills/visa2026-lifecycle-docker/SKILL.md)**.

---

## Start Development Stack

```bash
docker compose -p visa2026-dev --env-file .env.dev -f docker-compose.dev.yml up -d
```

Default ports:
- Prod app: `80`
- Dev app: `8081`

Each stack has its own SQL data volume:
- `visa2026-prod_sqlserver_data_prod`
- `visa2026-dev_sqlserver_data_dev`

---

## Optional: local hot reload (`docker-compose.watch.yml`)

Use this **only on a developer machine** when you want the app to run **inside Docker** but **rebuild on file changes** (`dotnet watch`). It is **not** the same as the published `webapia/visa2026` image: the `app` service uses **`mcr.microsoft.com/dotnet/sdk:8.0`**, mounts the **repo** into `/src`, and runs **`dotnet watch run`**.

**Requirements**

- Outbound HTTPS from the container must work so **`dotnet restore`** can reach **NuGet** (and DevExpress feeds if configured). If you see **NU1301** / “Unable to load the service index for nuget.org”, fix Docker DNS/proxy/firewall before relying on this stack.
- **`DevExpress.Key`** must exist; the compose file mounts it for the SDK container.

**Port and project name**

- Default **`APP_PORT`** in the watch file is **8081** (so it does not clash with a prod-like stack on **80**). Override in your env file if needed.
- Use a **separate** compose project name from `visa2026-dev` / `visa2026-prod`, e.g. **`visa2026-watch`**, so you do not accidentally replace containers from another workflow.

**Start (PowerShell helper)**

```powershell
.\scripts\local\Start-ComposeWatch.ps1
```

Foreground (see logs) is default; background:

```powershell
.\scripts\local\Start-ComposeWatch.ps1 -Detach
```

Use **`.env.dev`** by default (aligns with `DB_NAME` default `Visa2026DbDev` in the watch file). To use another file:

```powershell
.\scripts\local\Start-ComposeWatch.ps1 -EnvFile .env.dev
```

(`.\scripts\start-compose-watch.ps1` still forwards to the same script.)

**Start (manual)**

```bash
docker compose -p visa2026-watch --env-file .env.dev -f docker-compose.watch.yml up -d
```

**Stop**

```bash
docker compose -p visa2026-watch --env-file .env.dev -f docker-compose.watch.yml down
```

**Importer (`--profile tools`)** works the same pattern as dev/prod; point `--env-file` at the same file you used for `up`.

---

## Run Importer Safely

Lookup catalogs sync when the **app** container starts (`LookupCatalogSyncUpdater`). Ensure the app has run at least once on a fresh database before importing business data.

Production YAML import:

```bash
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml --profile tools run --rm db-updater
```

Development YAML import:

```bash
docker compose -p visa2026-dev --env-file .env.dev -f docker-compose.dev.yml --profile tools run --rm db-updater
```

From Windows with interactive yes/no prompt for YAML import:

```powershell
.\droplet-scripts\seed-data.ps1 -Environment dev
.\droplet-scripts\seed-data.ps1 -Environment prod
```

---

## Update One Stack Without Touching the Other

Update production app only:

```bash
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml pull app
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml up -d --no-deps app
```

Or from Windows:

```powershell
.\droplet-scripts\update-app.ps1 -Environment prod
```

Update development app only:

```bash
docker compose -p visa2026-dev --env-file .env.dev -f docker-compose.dev.yml pull app
docker compose -p visa2026-dev --env-file .env.dev -f docker-compose.dev.yml up -d --no-deps app
```

Or from Windows:

```powershell
.\droplet-scripts\update-app.ps1 -Environment dev
```

---

## Stop/Remove

Stop one stack:

```bash
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml down
docker compose -p visa2026-dev --env-file .env.dev -f docker-compose.dev.yml down
```

Do not use `down -v` on production unless you explicitly intend to delete database data.

---

## Operational Rules

- Never run dev seeding commands against prod project name/env file.
- Keep prod and dev secrets in separate env files.
- Backup production DB before releases and before production data imports.
- Do not use `fresh-install.sh` / `fresh-install.ps1` on production.
