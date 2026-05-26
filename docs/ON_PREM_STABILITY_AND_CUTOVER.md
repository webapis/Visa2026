# On-prem stability and Linux deploy

**Current recommendation:** deploy on **Ubuntu** — [ON_PREM_LINUX_SERVER.md](./ON_PREM_LINUX_SERVER.md) · [setup-docker-engine](../.cursor/skills/setup-docker-engine/SKILL.md) · [scripts/linux/](../scripts/linux/README.md).

This doc also covers:

1. **[§1 Windows + WSL keepalive](#1-harden-windows-server-keepalive-checklist)** — **legacy** only (e.g. `10.100.128.25` if still on WSL).
2. **[§2 Linux deploy / cutover](#2-linux-cutover-outline)** — **primary** path for new on-prem prod (LAN VM or droplet).

**Do not install Docker Desktop on the server** — it still uses WSL 2 and conflicts with Ubuntu + Docker Engine. See [ON_PREM_PREREQUISITES.md](./ON_PREM_PREREQUISITES.md).

**Related:** [ON_PREM_WINDOWS_SERVER.md](./ON_PREM_WINDOWS_SERVER.md) · [setup-docker-engine/learnings.md](../.cursor/skills/setup-docker-engine/learnings.md) · [visa2026-droplet-prod-deploy](../.cursor/skills/visa2026-droplet-prod-deploy/SKILL.md)

---

## 1. Harden Windows Server keepalive (checklist)

Run on the **server console** as **Administrator** (RDP preferred; SSH is fine for repair but avoid `wsl --shutdown` over SSH).

### 1.1 One-time setup

| Step | Action | Pass criteria |
|------|--------|----------------|
| A | Copy repo scripts to `C:\visa2026-deploy\` and `C:\WslDocker-Setup\` | Files include `Repair-OnPremVisa2026Stack.ps1`, `Register-Visa2026WslKeepAliveTask.ps1`, `Set-OnPremWslPortProxy.ps1`, `remote-compose-sql-up.sh`, `install-docker-engine.sh` |
| B | Deploy files at `C:\visa2026\` | `docker-compose.prod.yml`, `docker-compose.restart.override.yml`, `.env.prod` (secrets filled) |
| C | `.wslconfig` for **Administrator** and the user that runs WSL (e.g. `adm43419`) | Only:<br>`[wsl2]`<br>`vmIdleTimeout=-1`<br>`memory=10GB`<br>No unsupported keys (`autoMemoryReclaim`, etc.) |
| D | After editing `.wslconfig`: `wsl --shutdown`, wait 10s, then one `wsl -d Ubuntu -u root -- echo ok` | `wsl -l -v` → Ubuntu **Running** |
| E | `wsl --set-default Ubuntu` | Default distro is Ubuntu, not `docker-desktop` |
| F | **Do not install Docker Desktop** on this server | `wsl -l -v` lists only **Ubuntu** (no `docker-desktop` distros) |
| G | Register scheduled tasks | `powershell -File C:\visa2026-deploy\Register-Visa2026WslKeepAliveTask.ps1` |
| H | Windows Firewall inbound TCP **80** | `Start-Visa2026Compose.ps1 -OpenHttpFirewall` once, or rule `Visa2026-HTTP-In-TCP` exists |
| I | First deploy / recovery | `powershell -File C:\visa2026-deploy\Repair-OnPremVisa2026Stack.ps1` (not `Start-Visa2026Compose.ps1` alone) |
| J | `FORCE_XAF_DB_UPDATE` | `true` only until first login works; then `Set-OnPremForceXafDbUpdate.ps1 -Disable` |

### 1.2 Scheduled tasks (after step G)

| Task | When | Purpose |
|------|------|---------|
| `Visa2026-WslPersistent` | At boot | `wsl -d Ubuntu -u root -- sleep infinity` — keeps WSL VM open |
| `Visa2026-Startup` | Boot + 1 min | `Repair-OnPremVisa2026Stack.ps1` — SQL-first compose + port proxy |
| `Visa2026-WslKeepAlive` | Every 1 min | Backup: start hidden keepalive if missing |

Verify:

```powershell
schtasks /Query /TN Visa2026-WslPersistent /FO LIST | findstr Task
schtasks /Query /TN Visa2026-Startup /FO LIST | findstr Task
schtasks /Query /TN Visa2026-WslKeepAlive /FO LIST | findstr Task
```

### 1.3 After every reboot

```powershell
# Wait ~2 minutes for Visa2026-Startup, then:
wsl -l -v
powershell -File C:\visa2026-deploy\Set-OnPremWslPortProxy.ps1
```

If browser still fails:

```powershell
schtasks /Run /TN Visa2026-WslPersistent
powershell -File C:\visa2026-deploy\Repair-OnPremVisa2026Stack.ps1
```

Wait **60 seconds**, then open `http://<server-ip>/LoginPage`.

### 1.4 Health check (2 minutes)

```powershell
powershell -File C:\visa2026-deploy\Monitor-OnPremWslStack.ps1 -Checks 12 -IntervalSeconds 10
```

**Pass:** every line shows Ubuntu **Running**; container status grows (`Up 30 seconds` → `Up 2 minutes`), not resetting to `Up 1 second`.

From a LAN PC:

```powershell
curl.exe -s -o NUL -w "http_code=%{http_code}`n" http://10.100.128.25/LoginPage
```

**Pass:** `http_code=200` or `302`.

### 1.5 Daily / incident recovery (single command)

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File C:\visa2026-deploy\Repair-OnPremVisa2026Stack.ps1
```

### 1.6 Do not

| Avoid | Why |
|-------|-----|
| `wsl --shutdown` during normal ops | Kills all containers; keepalive must restart everything |
| `Start-Visa2026Compose.ps1` for recovery | Starts app + SQL together → SQL **4060** race |
| Docker Desktop on server | Extra WSL distros; same instability, not supported |
| Leaving `FORCE_XAF_DB_UPDATE=true` | Slow restarts; more time for WSL to die mid-update |

### 1.7 App updates (when stack is stable)

```powershell
# Prefer SQL-first script after changing image tag:
# Edit C:\visa2026\.env.prod APP_IMAGE_TAG=...
wsl -d Ubuntu -u root -- bash -lc "cd /mnt/c/visa2026 && docker compose -p visa2026-prod --env-file .env.prod pull app && docker compose -p visa2026-prod --env-file .env.prod up -d app"
powershell -File C:\visa2026-deploy\Set-OnPremWslPortProxy.ps1
```

---

## 2. Linux cutover outline

Same application images and compose file; **no WSL**, **no portproxy**. Docker binds port 80 on the Linux host directly.

### 2.1 Choose target

| Target | Best when | Deploy from |
|--------|-----------|-------------|
| **DigitalOcean droplet** | Already used; public IP; ops comfortable with SSH | `droplet-scripts/update-prod.ps1` |
| **Company Linux VM** | LAN-only; IT provides Ubuntu 22.04/24.04 VM | Manual compose (same files as droplet) |

Both use:

- `docker-compose.prod.yml` (repo root)
- `.env.prod` (same variables as on-prem)
- Project name: `visa2026-prod`
- Images: `webapia/visa2026:${APP_IMAGE_TAG}`, `mcr.microsoft.com/mssql/server:2025-latest`

### 2.2 Cutover phases

```text
[1] Backup SQL on Windows Server (mandatory if any prod data exists)
[2] Prepare Linux host (Docker Engine + compose plugin)
[3] Copy compose + .env.prod + optional .bak
[4] Start stack on Linux (SQL first or full compose)
[5] Verify HTTP + login
[6] Point users to new URL/IP; keep old server read-only until sign-off
[7] Decommission Windows stack (optional)
```

### 2.3 Phase 1 — Backup database on current server

On **Windows Server** (WSL running):

```powershell
powershell -File C:\visa2026-deploy\Repair-OnPremVisa2026Stack.ps1
```

In WSL (replace password from `.env.prod`):

```bash
wsl -d Ubuntu -u root -- bash -lc '
SA_PASSWORD="$(grep -E "^SA_PASSWORD=" /mnt/c/visa2026/.env.prod | cut -d= -f2- | tr -d "\r")"
DB_NAME="$(grep -E "^DB_NAME=" /mnt/c/visa2026/.env.prod | cut -d= -f2- | tr -d "\r")"
docker exec visa2026-prod-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -C \
  -Q "BACKUP DATABASE [$DB_NAME] TO DISK = N\"/var/opt/mssql/data/${DB_NAME}.bak\" WITH INIT, COMPRESSION"
'
```

Copy backup to a safe path:

```powershell
wsl -d Ubuntu -u root -- docker cp visa2026-prod-sqlserver-1:/var/opt/mssql/data/Visa2026DbProd.bak C:/visa2026/Visa2026DbProd.bak
```

Copy `C:\visa2026\Visa2026DbProd.bak` to your PC or the Linux host.

See also [PRODUCTION_DEPLOYMENT_RUNBOOK.md](./PRODUCTION_DEPLOYMENT_RUNBOOK.md).

### 2.4 Phase 2 — Linux host prerequisites

**Ubuntu 22.04/24.04 VM** (minimum similar to [ON_PREM_PREREQUISITES.md](./ON_PREM_PREREQUISITES.md): 8 GB RAM, 4 vCPU, 100 GB disk).

```bash
# Docker Engine + compose (official docs)
sudo apt-get update
sudo apt-get install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
# ... follow https://docs.docker.com/engine/install/ubuntu/
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
sudo usermod -aG docker $USER
```

**Firewall (LAN VM):** allow inbound TCP **80** (and **22** for SSH).

**Outbound:** Docker Hub (`webapia/visa2026`), MCR (`mcr.microsoft.com`).

### 2.5 Phase 3 — Deploy on Linux

```bash
sudo mkdir -p /opt/visa2026
cd /opt/visa2026
# Copy: docker-compose.prod.yml, .env.prod, Visa2026DbProd.bak (if restoring)
nano .env.prod   # SA_PASSWORD, DEVEXPRESS_LICENSEKEY, DB_NAME, APP_PORT=80
```

**Greenfield** (empty DB, same as first on-prem start):

```bash
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml up -d
# Optional one-shot in .env.prod: FORCE_XAF_DB_UPDATE=true, then remove after login
```

**Restore from backup** (keep existing data):

```bash
# Start SQL only
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml up -d sqlserver
# Wait until SQL accepts connections, then restore (adjust paths/password)
docker cp Visa2026DbProd.bak visa2026-prod-sqlserver-1:/var/opt/mssql/data/
docker exec -it visa2026-prod-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -C \
  -Q "RESTORE DATABASE [Visa2026DbProd] FROM DISK = N'/var/opt/mssql/data/Visa2026DbProd.bak' WITH REPLACE"
docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml up -d app
```

**Verify:**

```bash
docker compose -p visa2026-prod ps
curl -s -o /dev/null -w "%{http_code}\n" http://127.0.0.1/LoginPage
```

### 2.6 Phase 4 — DigitalOcean droplet (if using existing prod)

From your **Windows admin PC** (repo clone):

1. Ensure `~/visa2026` on droplet has `.env.prod` and compose (or use sync scripts).
2. Upload backup if migrating data (SCP `.bak` to droplet, restore as in §2.5).
3. Run:

```powershell
cd droplet-scripts
.\update-prod.ps1
```

Uses SSH to pull `webapia/visa2026` and restart app; see [DIGITAL_OCEAN_DEPLOYMENT.md](../DIGITAL_OCEAN_DEPLOYMENT.md) and [visa2026-droplet-prod-deploy](../.cursor/skills/visa2026-droplet-prod-deploy/SKILL.md).

**Never** run `fresh-install.ps1` / `fresh-install.sh` on prod unless you intend to **wipe the database volume**.

### 2.7 Phase 5 — Cutover checklist

| Step | Action |
|------|--------|
| 1 | Backup verified (`.bak` size, test restore on staging if possible) |
| 2 | Linux/droplet serves `http://<new-host>/LoginPage` (200/302) |
| 3 | Login works (`Admin` or real users) |
| 4 | Critical flows smoke-tested (org, application, report) |
| 5 | DNS or LAN bookmark updated for users |
| 6 | `FORCE_XAF_DB_UPDATE` off on new host |
| 7 | Old Windows server: stop compose or power off after rollback window |

### 2.8 Comparison

| | Windows Server + WSL | Linux VM / Droplet |
|--|----------------------|-------------------|
| Compose file | `docker-compose.prod.yml` | Same |
| Port 80 to LAN | Portproxy + firewall | Direct bind + firewall |
| Main risk | WSL **Stopped** | Host patching / disk |
| Ops scripts | `scripts/on-prem/` | `droplet-scripts/` |
| Docker Desktop | No | No (Engine on Linux) |

---

## 3. Recommendation

1. **New on-prem prod:** [§2 Linux](#2-linux-cutover-outline) + [ON_PREM_LINUX_SERVER.md](./ON_PREM_LINUX_SERVER.md) — do not start new Windows Server + WSL deploys.
2. **Existing WSL host only:** [§1 Harden checklist](#1-harden-windows-server-keepalive-checklist) short term; plan [§2 cutover](#2-linux-cutover-outline) when stable LAN HTTP is required.
3. **Cloud:** Existing **droplet** path (`droplet-scripts/`); migrate DB with `.bak` restore.

---

## 4. Script reference (`C:\visa2026-deploy\`)

| Script | Use |
|--------|-----|
| `Repair-OnPremVisa2026Stack.ps1` | **Primary recovery** — keepalive + SQL-first + portproxy |
| `Register-Visa2026WslKeepAliveTask.ps1` | One-time task registration |
| `Monitor-OnPremWslStack.ps1` | 2-minute stability watch |
| `Set-OnPremWslPortProxy.ps1` | After reboot / LAN HTTP fails |
| `Set-OnPremForceXafDbUpdate.ps1` | `-Disable` after first login |
| `Start-OnPremWslPersistent.ps1` | Called by repair / keepalive task |
| `remote-compose-sql-up.sh` | In `C:\WslDocker-Setup\` — SQL before app |
