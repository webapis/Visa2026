# Scripts layout

Scripts are grouped so you can tell **where they run** and **what they target**.

## `scripts/local/` — developer workstation

Use on your **own PC** with **Docker Desktop** (or any machine where you edit the repo and run compose locally).

| Script | Purpose |
|--------|---------|
| `Build-DockerImages.ps1` | Build `webapia/visa2026` / importer images locally (same build-args as CI). Optional `-DeployLocal` to recreate the **local** compose app container. Sets `DOCKER_BUILDKIT=1` so NuGet **cache mounts** in the Dockerfiles apply (first build still downloads packages; later builds reuse cache on this PC). |
| `Start-ComposeWatch.ps1` | Optional **hot reload**: `docker-compose.watch.yml` (SDK + `dotnet watch`). Separate compose project from prod-like local stacks. |
| `Export-DockerAppLogs.ps1` | Dump compose **app** logs into `agent-local/` (e.g. for IDE/AI review). |
| `Get-ModuleInfoFromSql.ps1` | Query SQL **`ModuleInfo`** (XAF module versions in the DB). Compare to `Visa2026.Module` **AssemblyVersion** when debugging **`UpdateOldDatabase`** / missing reports. Requires SQL reachable from the PC (e.g. host port **1433** or VPN). |
| `Set-ForceXafDbUpdate.ps1` | **` -Enable`** / **` -Disable`**: set or remove **`FORCE_XAF_DB_UPDATE`** in an env file (default **`.env.prod`**) and **`docker compose … up -d --force-recreate --no-deps app`** (omit recreate with **`-NoCompose`**). |
| `Seed-DataYaml.ps1` | Run **`db-updater`** (imports bundled **`data.yaml`** by default, or **`-HostYamlPath`** to bind-mount another file). Requires **app + SQL** up; fresh DB: start **app** once so lookup catalogs sync first. |
| `Install-MsEdgeDriver.ps1` | Download **Edge WebDriver** (`msedgedriver.exe`) from Microsoft’s CDN into **`%USERPROFILE%\.local\bin`** and prepend that folder to your **user PATH**. Run once per machine (or after a major Edge upgrade) so **`Visa2026.E2E.Tests`** can launch Edge via EasyTest. |

**Typical env files here:** `.env.dev` (paths passed into scripts or compose).

Example:

```powershell
.\scripts\local\Get-ModuleInfoFromSql.ps1 -EnvFile .env.dev
```

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

## `scripts/on-prem/` — company Windows Server (LAN)

**Agent skills:** [visa2026-windows-server-setup](../.cursor/skills/visa2026-windows-server-setup/SKILL.md) (prereqs + WSL), [setup-docker-engine](../.cursor/skills/setup-docker-engine/SKILL.md) (Docker + compose), [setup-openssh-server](../.cursor/skills/setup-openssh-server/SKILL.md) (OpenSSH). See [scripts/on-prem/README.md](on-prem/README.md).

| Script | Skill |
|--------|--------|
| `Test-OnPremServerPrerequisites.ps1`, `Install-WslDockerEngine.ps1 -SkipDockerInstall` | visa2026-windows-server-setup |
| `Install-WslDockerEngine.ps1 -SkipWslInstall -SkipSystemdConfig`, `Install-WslDockerEngine-Offline.ps1`, `Start-Visa2026Compose.ps1`, `Set-OnPremForceXafDbUpdate.ps1` | setup-docker-engine |
| `Install-WindowsOpenSshServer.ps1`, `Repair-WindowsOpenSshServer.ps1` | [setup-openssh-server](../.cursor/skills/setup-openssh-server/SKILL.md) |

**Full runbook:** [docs/ON_PREM_WINDOWS_SERVER.md](../docs/ON_PREM_WINDOWS_SERVER.md)

Example (Administrator PowerShell on the server):

```powershell
# visa2026-windows-server-setup
.\Test-OnPremServerPrerequisites.ps1
.\Install-WslDockerEngine.ps1 -SkipDockerInstall

# setup-docker-engine
.\Install-WslDockerEngine.ps1 -SkipWslInstall -SkipSystemdConfig
.\Start-Visa2026Compose.ps1 -Pull -OpenHttpFirewall
```

If the host blocks `.ps1` execution entirely:

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\Install-WslDockerEngine.ps1
```

---

## Quick mental model

- **`scripts/local/`** → *my laptop, local Docker, local volumes.*
- **`scripts/on-prem/`** → *company Windows Server (visa2026-windows-server-setup, setup-docker-engine, setup-openssh-server).*
- **`droplet-scripts/`** → *remote Linux host, real deploys, `.env.prod` / `.env.dev` on the server.*

For compose file reference and importer commands, see [docs/ENVIRONMENTS.md](../docs/ENVIRONMENTS.md). For production safety, see [docs/PRODUCTION_DEPLOYMENT_RUNBOOK.md](../docs/PRODUCTION_DEPLOYMENT_RUNBOOK.md).
