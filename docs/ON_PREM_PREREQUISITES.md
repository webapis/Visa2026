# On-prem prerequisites — hardware and software

Canonical requirements to **deploy and run Visa2026** on a company LAN using **`docker-compose.prod.yml`**.

## Recommended: Linux Ubuntu (on-prem)

| Item | Detail |
|------|--------|
| **Host** | **Ubuntu 22.04/24.04 LTS** + **Docker Engine** ([official install](https://docs.docker.com/engine/install/ubuntu/)) |
| **Runbook** | [ON_PREM_LINUX_SERVER.md](./ON_PREM_LINUX_SERVER.md) |
| **Skill** | [setup-docker-engine](../.cursor/skills/setup-docker-engine/SKILL.md) |
| **Scripts** | [scripts/linux/](../scripts/linux/README.md) |

**Not this path:** developer PC Docker Desktop for prod (`scripts/local/`).

## Legacy: Windows Server + WSL (deprecated for new deploys)

| Item | Detail |
|------|--------|
| **Host** | Windows Server 2019/2022 + WSL 2 + Docker Engine in Ubuntu |
| **Runbook** | [ON_PREM_WINDOWS_SERVER.md](./ON_PREM_WINDOWS_SERVER.md) |
| **Skills** | [visa2026-windows-server-setup](../.cursor/skills/visa2026-windows-server-setup/SKILL.md) → then setup-docker-engine (old WSL flow) |
| **Scripts** | `scripts/on-prem/` |

## Cloud: Linux droplet

`droplet-scripts/` — [visa2026-droplet-prod-deploy](../.cursor/skills/visa2026-droplet-prod-deploy/SKILL.md).

---

The sections below apply to **both** Linux and legacy Windows hosts unless noted.

---

## Target workload

| Item | Assumption |
|------|------------|
| Users | ~**10** concurrent Blazor users (company LAN) |
| Stack | `webapia/visa2026` (Linux) + **SQL Server Express** in Linux container |
| Host OS | **Ubuntu 22.04/24.04 LTS** (recommended) or legacy **Windows Server 2019/2022 + WSL** — app runs in **Linux containers**, not IIS + Windows SQL |

---

## 1. Hardware requirements

| Resource | Minimum | Recommended | Notes |
|----------|---------|-------------|--------|
| **RAM** | 8 GB | 16 GB | Host must cover **Windows Server**, **WSL 2 VM**, **SQL Server container**, and **app container**. SQL 2025 + .NET Blazor are the main consumers after idle Windows overhead. |
| **CPU** | 2 logical processors | 4 logical processors | x64 required. More cores help SQL and first-start schema updates. |
| **Disk (free on system drive, usually `C:`)** | 100 GB | 200 GB | Docker images (~1–2 GB app + SQL), image layers, SQL data volume growth, logs, Windows updates. Plan headroom for DB backups if stored on same volume. |
| **Architecture** | x64 (AMD64) | x64 | ARM64 Windows Server is out of scope for this runbook. |

### Rough memory budget (recommended 16 GB host)

| Component | Order of magnitude |
|-----------|-------------------|
| Windows Server + services | 2–4 GB |
| WSL 2 (Ubuntu + Docker daemon) | 1–2 GB |
| SQL Server Express (container) | 2–4 GB under load |
| Visa2026 app (container) | 1–3 GB under load |
| Headroom | Remainder for cache spikes |

Below **8 GB**, expect **WARN** from the prereq script; operation may work for light use but is not the supported target.

---

## 2. Software requirements (host)

| Requirement | Detail |
|-------------|--------|
| **Operating system** | **Windows Server 2019** or **Windows Server 2022** (Desktop Experience or Server Core with management tools for `wsl` / RDP as you prefer). |
| **Privileges** | **Administrator** PowerShell for install scripts (WSL feature, Docker install, firewall). |
| **WSL** | **Windows Subsystem for Linux** optional component installed (`wsl --install --no-distribution` + reboot on first setup). |
| **WSL version** | Linux distro must run as **WSL 2** (not WSL 1). |
| **Linux distribution** | **Ubuntu** (22.04 / 24.04 family — match offline Docker debs to distro codename). |
| **init in WSL** | **systemd** enabled (`/etc/wsl.conf` → `[boot]` `systemd=true`, then `wsl --shutdown`). Required for `docker` service. |
| **Docker** | **Docker Engine** + **Compose plugin** inside Ubuntu (not Docker Desktop on Windows Server). |
| **WSL stability (production)** | `C:\Users\<run-user>\.wslconfig` with `[wsl2]` `vmIdleTimeout=-1` so Ubuntu does not **Stop** and kill containers. |

### Explicitly not used on the server

| Item | Why |
|------|-----|
| **Docker Desktop** | Not supported / not desired on Windows Server for this project. |
| **SQL Server on Windows** | Database runs in **Linux container** only. |
| **Windows containers** | Compose images are **Linux** (`webapia/visa2026`, MCR SQL). |
| **IIS hosting the Blazor app** | App runs in Linux container; port **80** published from Docker. |

---

## 3. Network requirements

### Inbound (LAN / firewall)

| Port | Protocol | Required | Purpose |
|------|----------|----------|---------|
| **80** | TCP | **Yes** (default) | Visa2026 Blazor (`APP_PORT` in `.env.prod`, maps to container 8080). |
| **22** | TCP | Optional | Remote administration ([setup-openssh-server](../.cursor/skills/setup-openssh-server/SKILL.md)); not required for app users. |
| **1433** | TCP | No (default) | SQL published to **127.0.0.1** only for local SSMS/tools. |

### Outbound (server or WSL — IT allow list)

| Destination | Purpose |
|-------------|---------|
| `download.docker.com` | Docker Engine packages (online install). |
| `registry-1.docker.io`, `hub.docker.com` | Pull `webapia/visa2026`, `hello-world`, layers. |
| `mcr.microsoft.com` | Pull `mcr.microsoft.com/mssql/server` (default 2025-latest in compose). |
| `archive.ubuntu.com` / Ubuntu mirrors | `apt` during WSL bootstrap and Docker install. |
| `github.com` | Optional: Win32-OpenSSH zip ([setup-openssh-server](../.cursor/skills/setup-openssh-server/SKILL.md) only). |

Air-gapped installs: prepare Docker `.deb` files per [reference-docker-offline-install.md](../scripts/on-prem/reference-docker-offline-install.md).

---

## 4. Deploy files and secrets (before compose)

| Path | Required | Content |
|------|----------|---------|
| `C:\visa2026\docker-compose.prod.yml` | Yes | From repo root [docker-compose.prod.yml](../docker-compose.prod.yml). |
| `C:\visa2026\.env.prod` | Yes | From [.env.prod.example](../.env.prod.example) — **never commit** real values. |

### Required variables in `.env.prod`

| Variable | Purpose |
|----------|---------|
| `SA_PASSWORD` | SQL Server `sa` password (strong; SQL complexity rules apply). |
| `DEVEXPRESS_LICENSEKEY` | Valid DevExpress license for XAF / Reports in container. |

### Common optional variables

| Variable | Default | Purpose |
|----------|---------|---------|
| `APP_PORT` | `80` | Host port for HTTP. |
| `APP_IMAGE_TAG` | `latest` | Docker Hub tag for `webapia/visa2026`. |
| `DB_NAME` | `Visa2026DbProd` | Database name. |
| `MSSQL_HOST_PORT` | `1433` | Host loopback port for SQL tools (change if 1433 busy on Windows). |
| `FORCE_XAF_DB_UPDATE` | empty | One-shot schema updaters; remove after healthy start. |

Staging folder (any path): `C:\visa2026-deploy\` with on-prem PowerShell scripts.

---

## 5. SQL Server edition note

Compose uses **`MSSQL_PID=Express`** in [docker-compose.prod.yml](../docker-compose.prod.yml):

- **10 GB maximum** per database.
- Suitable for typical Visa2026 departmental use; plan migration if the DB approaches the cap.

---

## 6. Automated prerequisite check

Run on the server (Administrator):

```powershell
cd C:\visa2026-deploy
.\Test-OnPremServerPrerequisites.ps1
```

| Script check | Maps to section |
|--------------|-----------------|
| OS, RAM, CPU, disk free | §1 Hardware |
| Administrator session | §2 Software |
| WSL component / distro / VERSION 2 / Running | §2 Software |
| systemd in WSL | §2 Software |
| Docker / Compose plugin | §2 Software (WARN until **setup-docker-engine**) |
| `docker-compose.prod.yml`, `.env.prod` | §4 Deploy files (WARN until copied) |
| sshd / TCP 22 | Optional (§3); **WARN** only |

**Exit code 0** = no **FAIL** (ready for next skill step).

| When | Command |
|------|---------|
| After WSL bootstrap | `.\Test-OnPremServerPrerequisites.ps1` |
| Before first `compose up` | `.\Test-OnPremServerPrerequisites.ps1 -RequireDocker -RequireDeployFiles` |

From admin PC (optional SSH reachability):

```powershell
.\Test-OnPremServerPrerequisites.ps1 -ServerIp <server-ip>
```

---

## 7. Skill and script ownership (strict order)

```text
1. visa2026-windows-server-setup   (required first)
2. setup-docker-engine             (blocked until step 1 complete)
3. optional: setup-openssh-server
```

| Phase | Agent skill | Scripts | May start when |
|-------|-------------|---------|----------------|
| Prereq audit + WSL/Ubuntu/systemd | [visa2026-windows-server-setup](../.cursor/skills/visa2026-windows-server-setup/SKILL.md) | `Test-OnPremServerPrerequisites.ps1`, `Install-WslDockerEngine.ps1 -SkipDockerInstall` | New host |
| Docker install + compose | [setup-docker-engine](../.cursor/skills/setup-docker-engine/SKILL.md) | Gate: `Test-OnPremServerPrerequisites.ps1` (FAIL=0 WSL/systemd); then `Install-WslDockerEngine.ps1 -SkipWslInstall -SkipSystemdConfig`, `Start-Visa2026Compose.ps1`, … | **Only after** windows-server-setup Step 2 |
| SSH (optional) | [setup-openssh-server](../.cursor/skills/setup-openssh-server/SKILL.md) | `Install-WindowsOpenSshServer.ps1`, `Repair-WindowsOpenSshServer.ps1` | Any time |

**Do not** run Docker Engine install or `docker compose up` until **WSL 2**, **Ubuntu**, and **systemd** pass the prereq script with **FAIL=0**.

---

## 8. Success criteria (host is ready for users)

- [ ] Prereq script: **FAIL=0** with `-RequireDocker -RequireDeployFiles`
- [ ] `wsl -l -v` → Ubuntu **Running**, **VERSION 2**
- [ ] `docker compose -p visa2026-prod ps` → **app** and **sqlserver** **Up**
- [ ] Browser: `http://<server-ip>` (or custom `APP_PORT`) shows Visa2026 login
- [ ] Ubuntu stays **Running** under load (`.wslconfig` `vmIdleTimeout=-1`)

---

## Related documentation

- [ON_PREM_WINDOWS_SERVER.md](./ON_PREM_WINDOWS_SERVER.md) — full deployment phases
- [ENVIRONMENTS.md](./ENVIRONMENTS.md) — compose and env reference
- [PRODUCTION_DEPLOYMENT_RUNBOOK.md](./PRODUCTION_DEPLOYMENT_RUNBOOK.md) — backup and safety
- [scripts/on-prem/README.md](../scripts/on-prem/README.md) — script allowlist per skill
