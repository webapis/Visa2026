# Visa2026 on company Windows Server (on-prem)

Runbook for deploying Visa2026 on **Windows Server** on a company LAN using **WSL 2**, **Docker Engine (Linux)**, and **`docker-compose.prod.yml`**.

**Remote administration:** install **OpenSSH** on the server, then from your PC use **`ssh user@<server-ip>`** to run PowerShell, **`wsl`**, and **`docker compose`** (see [`.cursor/skills/visa2026-on-prem-windows-server/SKILL.md`](../.cursor/skills/visa2026-on-prem-windows-server/SKILL.md)).

This is **not** the DigitalOcean droplet path (`droplet-scripts/`, Linux host). Use this document when the server is **Windows Server 2019/2022** and clients reach the app over the internal network.

**Related docs**

- [ENVIRONMENTS.md](./ENVIRONMENTS.md) — compose files, env vars, `FORCE_XAF_DB_UPDATE`, importer
- [PRODUCTION_DEPLOYMENT_RUNBOOK.md](./PRODUCTION_DEPLOYMENT_RUNBOOK.md) — backup, seeding policy, safety
- [scripts/README.md](../scripts/README.md) — script locations

**Automation (run on the server, Administrator PowerShell)**

| Script | Purpose |
|--------|---------|
| [Test-OnPremServerPrerequisites.ps1](../scripts/on-prem/Test-OnPremServerPrerequisites.ps1) | Step 0 — verify OS/RAM/CPU/disk, sshd, WSL, Docker, deploy files |
| [Install-WindowsOpenSshServer.ps1](../scripts/on-prem/Install-WindowsOpenSshServer.ps1) | Step 1 — OpenSSH Server (`sshd`), port 22 firewall |
| [Install-WslDockerEngine.ps1](../scripts/on-prem/Install-WslDockerEngine.ps1) | Step 2 — WSL 2, Ubuntu, systemd, Docker Engine + Compose |
| [Start-Visa2026Compose.ps1](../scripts/on-prem/Start-Visa2026Compose.ps1) | Step 3 — pull/start prod stack from `C:\visa2026` via WSL |
| [Set-OnPremForceXafDbUpdate.ps1](../scripts/on-prem/Set-OnPremForceXafDbUpdate.ps1) | Optional — `FORCE_XAF_DB_UPDATE` one-shot via WSL |

**Agent skill uses only `scripts/on-prem/`** — not `droplet-scripts/` or `scripts/local/`. See [scripts/on-prem/README.md](../scripts/on-prem/README.md).

**Cursor Agent skill:** [`.cursor/skills/visa2026-on-prem-windows-server/SKILL.md`](../.cursor/skills/visa2026-on-prem-windows-server/SKILL.md) (invoke for guided deploy/triage on new LAN servers).

**Try/error log (append after each server):** [`.cursor/skills/visa2026-on-prem-windows-server/learnings.md`](../.cursor/skills/visa2026-on-prem-windows-server/learnings.md)

**SSH config + Docker prep + client SSH connection (detail):** [`.cursor/skills/visa2026-on-prem-windows-server/reference-ssh-and-docker-prep.md`](../.cursor/skills/visa2026-on-prem-windows-server/reference-ssh-and-docker-prep.md)

---

## Architecture

```text
LAN clients  -->  http://<server-ip>:80
                        |
                 Windows Server
                        |
              WSL 2 (Ubuntu, systemd)
                        |
              Docker Engine (Linux)
                        |
         docker compose (visa2026-prod)
              |                |
           app:8080      sqlserver (volume)
         (Blazor)         SQL Express
```

- **Do not** use **Docker Desktop** on the server (not supported on Windows Server; dev PCs only).
- Images are **Linux** (`webapia/visa2026`, `mcr.microsoft.com/mssql/server`).
- Compose runs **inside WSL**, with deploy files on **`C:\visa2026`** (mounted as `/mnt/c/visa2026`).

---

## Server sizing (~10 users)

| Resource | Minimum | Recommended |
|----------|---------|-------------|
| OS | Windows Server 2019/2022 x64 | Server 2022 |
| RAM | 8 GB | 16 GB |
| CPU | 2 cores | 4 cores |
| Disk (free) | 100 GB | 200 GB |
| LAN | Inbound **TCP 80** (app); **22** optional (SSH admin) | HTTPS reverse proxy later |

SQL in compose uses **Express** (10 GB per database cap). See [docker-compose.prod.yml](../docker-compose.prod.yml).

---

## Network / firewall (IT checklist)

Outbound HTTPS from the server (or WSL) to:

| Host | Purpose |
|------|---------|
| `download.docker.com` | Docker apt packages |
| `registry-1.docker.io`, `hub.docker.com` | `webapia/visa2026`, `hello-world` |
| `mcr.microsoft.com` | SQL Server image |
| `github.com` | Win32-OpenSSH zip (SSH repair path only) |

Inbound:

| Port | Purpose |
|------|---------|
| **80** | Visa2026 Blazor (default `APP_PORT`) |
| **22** | SSH admin (optional) |
| **1433** | Not required on LAN by default (SQL bound to `127.0.0.1` in compose) |

---

## Files to place on each new server

Copy from this repo (USB, RDP, or `scp` after SSH works):

```text
C:\visa2026-deploy\                    # staging folder (any path)
  Install-WindowsOpenSshServer.ps1
  Install-WslDockerEngine.ps1
  Start-Visa2026Compose.ps1
  docker-compose.prod.yml              # from repo root
  .env.prod.example                    # rename/fill -> C:\visa2026\.env.prod
```

Production secrets live only in **`C:\visa2026\.env.prod`** (never commit).

Required in `.env.prod`:

```env
SA_PASSWORD=<strong SQL password>
DEVEXPRESS_LICENSEKEY=<license>
```

Optional: `APP_PORT`, `DB_NAME`, `APP_IMAGE_TAG`, `MSSQL_HOST_PORT` — see [.env.prod.example](../.env.prod.example).

---

## Deployment phases (new server checklist)

Use this order on **every** new Windows Server. The Agent skill walks the same steps: [`.cursor/skills/visa2026-on-prem-windows-server/SKILL.md`](../.cursor/skills/visa2026-on-prem-windows-server/SKILL.md).

### Phase 0 — Prerequisites

- [ ] RDP or console to the server
- [ ] **Administrator** PowerShell
- [ ] Record server IP (example: `10.100.128.25`)
- [ ] Run `.\Test-OnPremServerPrerequisites.ps1` — fix **FAIL** before later phases; **WARN** RAM/disk acceptable per table above

### Phase 1 — OpenSSH (remote admin)

```powershell
Set-Location C:\visa2026-deploy   # or Desktop, where scripts were copied
.\Install-WindowsOpenSshServer.ps1
```

- [ ] Script ends with **`OK: Port 22 is listening`**
- [ ] From your PC: `Test-NetConnection -ComputerName <server-ip> -Port 22`
- [ ] `ssh <admin-user>@<server-ip>` works

**Offline SSH zip:** [Install-WindowsOpenSshServer.ps1](../scripts/on-prem/Install-WindowsOpenSshServer.ps1) `-ZipPath C:\Temp\OpenSSH-Win64.zip -SkipCapabilityRepair`

**Known issue:** `OpenSSH.Server` capability shows Installed but only client tools exist under `C:\Windows\System32\OpenSSH` — the script installs from **Win32-OpenSSH** zip.

---

### Phase 2 — WSL optional component

```powershell
wsl.exe --install --no-distribution
```

- [ ] Message: changes effective after **reboot**
- [ ] **Reboot server**

After reboot:

```powershell
wsl --status
```

- [ ] No `WSL_E_WSL_OPTIONAL_COMPONENT_REQUIRED`

Or use the script (first run stops and asks for reboot):

```powershell
.\Install-WslDockerEngine.ps1
```

---

### Phase 3 — Ubuntu + Docker

**First run** (installs Ubuntu if missing):

```powershell
.\Install-WslDockerEngine.ps1
```

Complete Ubuntu first-login if prompted (Linux username/password).

Verify:

```powershell
wsl -l -v
# Ubuntu, VERSION 2, Running or Stopped

wsl -d Ubuntu -u root -- systemctl is-system-running
# running
```

**Docker install** (if Phase 3 script did not finish Docker, or rerun on a new server with Ubuntu already present):

```powershell
.\Install-WslDockerEngine.ps1 -SkipWslInstall -SkipSystemdConfig
```

- [ ] See progress lines `==> apt-get update (1/4)` … `==> Docker install finished OK`
- [ ] `docker run hello-world` succeeds inside the script output

Manual verify:

```powershell
wsl -d Ubuntu -u root -- docker --version
wsl -d Ubuntu -u root -- docker compose version
wsl -d Ubuntu -u root -- docker run --rm hello-world
```

**Notes**

- Install can take **10–30 minutes**; little or no output on older script builds is normal while `apt` runs.
- Use **updated** `Install-WslDockerEngine.ps1` from the repo for **live progress** output.
- Do **not** use `-SkipWslInstall` until `wsl -l -v` lists a distro.

| Flag | When |
|------|------|
| (none) | First time on server; installs WSL component and/or Ubuntu |
| `-SkipWslInstall` | WSL component + Ubuntu already installed |
| `-SkipSystemdConfig` | `/etc/wsl.conf` already has `systemd=true` and `systemctl` reports `running` |

---

### Phase 4 — Visa2026 compose

```powershell
New-Item -ItemType Directory -Path C:\visa2026 -Force
Copy-Item C:\visa2026-deploy\docker-compose.prod.yml C:\visa2026\
# Create C:\visa2026\.env.prod from .env.prod.example and fill secrets
notepad C:\visa2026\.env.prod
```

Start stack:

```powershell
.\Start-Visa2026Compose.ps1
```

Or manually:

```powershell
wsl -d Ubuntu -e bash -c "cd /mnt/c/visa2026 && docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml pull && docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml up -d"
```

- [ ] `docker compose ps` shows **app** and **sqlserver** running
- [ ] Browser: `http://<server-ip>` (or your `APP_PORT`)
- [ ] Windows Firewall allows inbound **TCP 80** (script can add rule)

First DB start: app runs XAF updaters; see [ENVIRONMENTS.md](./ENVIRONMENTS.md) for one-shot `FORCE_XAF_DB_UPDATE`.

**Recommended:** add `restart: unless-stopped` on `app` and `sqlserver` in `docker-compose.prod.yml` for reboot survival (compose change in repo).

---

### Phase 5 — Smoke test

- [ ] Login to Blazor UI
- [ ] One read + one write path
- [ ] `wsl -d Ubuntu -e bash -c "cd /mnt/c/visa2026 && docker compose -p visa2026-prod logs app --tail 50"`

---

## Reference deployment status (template server)

Update this table when you onboard a server (copy section per host).

### Server: `10.100.128.25` (example — update for each host)

| Phase | Status | Notes |
|-------|--------|--------|
| 1 OpenSSH | **Done** | Win32-OpenSSH install; port 22 listening |
| 2 WSL component | **Done** | `wsl --install --no-distribution` + reboot |
| 3 Ubuntu | **Done** | `Ubuntu`, WSL 2; user `adm43419` |
| 3 systemd | **Done** | `systemctl is-system-running` → `running` |
| 3 Docker Engine | **In progress / verify** | Run `.\Install-WslDockerEngine.ps1 -SkipWslInstall -SkipSystemdConfig`; confirm `hello-world` |
| 4 `C:\visa2026` + compose | **Not started** | Need `.env.prod`, `docker-compose.prod.yml`, `Start-Visa2026Compose.ps1` |
| 5 Smoke test | **Not started** | After compose up |

**Next actions on this server**

1. Finish Docker (command above) and confirm `docker compose version`.
2. Create `C:\visa2026\.env.prod`.
3. Run `.\Start-Visa2026Compose.ps1`.
4. Open firewall **80** for LAN; test `http://10.100.128.25`.

---

## App updates (same server, new image tag)

On the server, after changing `APP_IMAGE_TAG` in `.env.prod` or using `latest`:

```powershell
.\Start-Visa2026Compose.ps1 -Pull
```

This pulls and recreates the **app** service only when using the script’s default behavior; for full stack pull, see script help.

---

## Troubleshooting

Detailed narratives from real setups: **[learnings.md](../.cursor/skills/visa2026-on-prem-windows-server/learnings.md)**.

| Symptom | Action |
|---------|--------|
| Ping OK, port **22** closed | `Install-WindowsOpenSshServer.ps1` (Win32-OpenSSH zip) |
| OpenSSH.Server Installed, no `sshd.exe` | Same script; do not trust DISM remove (**Element not found**) |
| Only `ssh-agent`, no `sshd` service | Same script |
| `.ps1` parse errors / `Unexpected token 'Using'` | Re-copy script from git; paste **one block** top-to-bottom |
| `WSL_E_WSL_OPTIONAL_COMPONENT_REQUIRED` | `wsl --install --no-distribution`, **reboot**, rerun Phase 3 |
| `wsl -l -v` empty after reboot | `wsl --install Ubuntu` or `.\Install-WslDockerEngine.ps1` **without** `-SkipWslInstall` |
| `Distro not found: Ubuntu` with `-SkipWslInstall` | Ubuntu not installed yet — install distro first |
| `Remove-WindowsCapability: Element not found` | Ignore DISM repair; script uses Win32-OpenSSH zip |
| Only `ssh-agent`, no `sshd` | Run `Install-WindowsOpenSshServer.ps1` (Win32 zip path) |
| Script stuck at “Installing Docker Engine” | Wait 20+ min, or check `wsl -d Ubuntu -u root -- pgrep apt`; use updated script for live output |
| `curl` / `apt` failures in WSL | Corporate proxy/firewall; allow `download.docker.com` or configure apt proxy |
| SQL login failed right after first up | Wait 60s, `docker compose restart app` — see [DIGITAL_OCEAN_DEPLOYMENT.md](../DIGITAL_OCEAN_DEPLOYMENT.md) |
| App not reachable on LAN | Open Windows Firewall TCP **80**; confirm `APP_PORT` in `.env.prod` |

---

## What we intentionally do not use on-server

| Item | Reason |
|------|--------|
| Docker Desktop | Not for Windows Server |
| SQL Server on Windows | Compose uses Linux SQL container |
| `droplet-scripts/` | Target is Linux droplet, not WSL |
| `fresh-install.ps1` | Destructive (`down -v`) — never on prod |

---

## Quick copy: one-page command sequence

```powershell
# Administrator PowerShell on NEW server
cd C:\visa2026-deploy

.\Install-WindowsOpenSshServer.ps1

wsl.exe --install --no-distribution
# REBOOT

.\Install-WslDockerEngine.ps1
# Complete Ubuntu user setup if prompted
# If Docker not finished:
.\Install-WslDockerEngine.ps1 -SkipWslInstall -SkipSystemdConfig

New-Item C:\visa2026 -Force
Copy-Item .\docker-compose.prod.yml C:\visa2026\
# Edit C:\visa2026\.env.prod (from .env.prod.example)

.\Start-Visa2026Compose.ps1
```

From admin PC: `http://<server-ip>` and `ssh <user>@<server-ip>`.
