---
name: visa2026-on-prem-windows-server
description: >-
  On-prem Windows Server for Visa2026 using ONLY scripts/on-prem/*.ps1 (strict allowlist): SSH setup,
  Docker Engine prep (WSL2), SSH connection from PC, compose at C:\visa2026. Never use
  droplet-scripts or scripts/local. Skill docs: learnings.md, reference-ssh-and-docker-prep.md.
disable-model-invocation: false
---

# Visa2026: Windows Server — step-by-step (prereq → SSH → Docker → compose)

## Script allowlist (strict)

When this skill runs commands or recommends automation, use **only** these files under [`scripts/on-prem/`](../../../scripts/on-prem/):

| Script | Step |
|--------|------|
| [Test-OnPremServerPrerequisites.ps1](../../../scripts/on-prem/Test-OnPremServerPrerequisites.ps1) | 0 |
| [Install-WindowsOpenSshServer.ps1](../../../scripts/on-prem/Install-WindowsOpenSshServer.ps1) | 1 |
| [Install-WslDockerEngine.ps1](../../../scripts/on-prem/Install-WslDockerEngine.ps1) | 2 |
| [Start-Visa2026Compose.ps1](../../../scripts/on-prem/Start-Visa2026Compose.ps1) | 3 |
| [Set-OnPremForceXafDbUpdate.ps1](../../../scripts/on-prem/Set-OnPremForceXafDbUpdate.ps1) | 3 (one-shot DB updaters) |

Manifest: [scripts/on-prem/README.md](../../../scripts/on-prem/README.md)

### Forbidden for this skill (do not run or suggest)

- `scripts/local/**` (Docker Desktop workstation, `Build-DockerImages.ps1`, `Set-ForceXafDbUpdate.ps1`, lifecycle-docker, etc.)
- `droplet-scripts/**` (Linux droplet SSH deploy)
- Any other `scripts/*.ps1` outside `scripts/on-prem/`

### Allowed without being repo scripts

- Windows/OpenSSH client tools on the admin PC: `ssh`, `scp`, `Test-NetConnection`
- Windows on the server: `wsl.exe`, `notepad`, `Restart-Computer` (when the runbook requires reboot)
- Inline `wsl -d Ubuntu ... docker compose ...` **only** for read-only checks (ps/logs) if no on-prem script exists yet; prefer `Test-OnPremServerPrerequisites.ps1` first

## Goal (four steps)

| Step | What you get |
|------|----------------|
| **0** | Server **matches prerequisites** (OS, RAM, CPU, disk) |
| **1** | **SSH** from your PC → server PowerShell |
| **2** | **Docker Engine** in WSL 2 (Ubuntu) on the server |
| **3** | **`docker compose`** running Visa2026 at `C:\visa2026` |

**Docs:** [ON_PREM_WINDOWS_SERVER.md](../../../docs/ON_PREM_WINDOWS_SERVER.md) · **Commands:** [reference.md](./reference.md) · **SSH + Docker prep + client connection (detail):** [reference-ssh-and-docker-prep.md](./reference-ssh-and-docker-prep.md) · **Try/error:** [learnings.md](./learnings.md)

### Chat openers

- `@.cursor/skills/visa2026-on-prem-windows-server/` — **Step by step**: check prereqs, SSH, Docker, compose.
- **Configure SSH on Windows Server** (sshd, firewall, keys).
- **Prepare Windows Server for Docker Engine** (WSL, Ubuntu, systemd).
- **Connect to server from my PC** using SSH (`ssh`, `scp`, Test-NetConnection).
- Walk me through **prerequisite check** then **SSH** to `<ip>`.

### Approval mode

If the user asks **step-by-step with OK**: propose **one** check or install command, wait for **OK**, then **verify** before the next step. Read-only checks (prereq script, `Test-NetConnection`, `wsl -l -v`) need no OK unless user asked for strict approval on everything.

---

## Prerequisites target (~10 users)

| Resource | Minimum | Recommended |
|----------|---------|-------------|
| OS | Windows Server 2019/2022 x64 | Server 2022 |
| RAM | 8 GB | 16 GB |
| CPU | 2 cores | 4 cores |
| Disk free (`C:`) | 100 GB | 200 GB |
| LAN | Inbound **22** (SSH), **80** (app) | |

**Not used on server:** Docker Desktop, native Windows SQL. **Not used by this skill:** `droplet-scripts/`, `scripts/local/`.

---

## Three skills this runbook teaches

Agents must know all three before compose deploy. Full prose: **[reference-ssh-and-docker-prep.md](./reference-ssh-and-docker-prep.md)**.

### A) Configure SSH on Windows Server

| What | How |
|------|-----|
| Install **sshd** | `Install-WindowsOpenSshServer.ps1` (Win32-OpenSSH if DISM broken) |
| Auto-start + port **22** | Script starts service, firewall rule |
| Remote **PowerShell** | Script sets `DefaultShell` in `HKLM:\SOFTWARE\OpenSSH` |
| Optional **key auth** | `C:\Users\<user>\.ssh\authorized_keys` + `icacls` |
| Optional **sshd_config** | `C:\ProgramData\ssh\sshd_config` → `Restart-Service sshd` |

**Verify (server):** `Get-Service sshd`, `Get-NetTCPConnection -LocalPort 22 -State Listen`

### B) Prepare Windows Server for Docker Engine

| What | How |
|------|-----|
| **WSL optional component** | `wsl.exe --install --no-distribution` → **reboot** |
| **Ubuntu** on WSL **2** | `wsl --install Ubuntu` or `Install-WslDockerEngine.ps1` |
| **systemd** in WSL | `/etc/wsl.conf` → `systemd=true`, `wsl --shutdown` |
| **Docker CE + compose plugin** | `Install-WslDockerEngine.ps1` inside Ubuntu |
| **Not** Docker Desktop | Engine runs only under `wsl -d Ubuntu` |

**Verify:** `wsl -l -v` (VERSION 2), `systemctl is-system-running` → `running`, `docker run hello-world`

### C) Establish SSH connection from your PC

| What | How |
|------|-----|
| Test LAN + port | `Test-NetConnection <ip> -Port 22` |
| Login | `ssh <windows-user>@<ip>` (accept host key first time) |
| Optional alias | `~/.ssh/config` → `Host visa2026-onprem` |
| Copy scripts | `scp ... C:/visa2026-deploy/` |
| Remote Docker | `ssh user@ip "wsl -d Ubuntu -u root -- docker ps"` |

**Verify:** remote PowerShell prompt; `ssh user@ip "hostname"` returns server name

---

## Step 0 — Check server parameters

**Where:** Server, **Administrator** PowerShell (RDP/console or SSH after Step 1).

**Run once per phase** (repeat after SSH / WSL / Docker / compose to see what is still missing):

```powershell
cd C:\visa2026-deploy
.\Test-OnPremServerPrerequisites.ps1
```

**From your PC** (also tests LAN reachability to SSH):

```powershell
.\Test-OnPremServerPrerequisites.ps1 -ServerIp 10.100.128.25
```

(Script must be on the PC copy, or run prereq on server and `Test-NetConnection` separately on PC.)

### Pass criteria (Step 0 baseline)

- [ ] OS is Windows Server; RAM/CPU/disk **PASS** or acceptable **WARN**
- [ ] No **FAIL** on items you have not installed yet (expected FAIL: sshd, WSL, Docker until later steps)

After Steps 1–3, re-run: expect **PASS** on sshd, WSL, Docker, compose files.

**If FAIL:** see [learnings.md](./learnings.md) — do not skip to compose.

---

## Step 1 — Setup SSH (server config + client connection)

Implements **§ A** and **§ C** above. Details: [reference-ssh-and-docker-prep.md Part 1 & 3](./reference-ssh-and-docker-prep.md).

### 1.1 Configure SSH on the server

```powershell
cd C:\visa2026-deploy
.\Install-WindowsOpenSshServer.ps1
```

**Verify on server:**

```powershell
Get-Service sshd
Get-NetTCPConnection -LocalPort 22 -State Listen
```

Expect: **Running**, **Listen** on 22.

### 1.2 Establish connection from your PC

```powershell
Test-NetConnection -ComputerName <server-ip> -Port 22
ssh <windows-user>@<server-ip>
```

Expect: `TcpTestSucceeded : True`, remote **PowerShell** prompt. First login: type `yes` to trust host key; enter **Windows password**.

Optional: `~/.ssh/config` entry — see [reference-ssh-and-docker-prep.md § 3.4](./reference-ssh-and-docker-prep.md).

### 1.3 Copy deploy bundle (PC, after SSH works)

```powershell
scp -r .\scripts\on-prem\ <user>@<server-ip>:C:/visa2026-deploy/
scp .\docker-compose.prod.yml <user>@<server-ip>:C:/visa2026-deploy/
scp .\.env.prod.example <user>@<server-ip>:C:/visa2026-deploy/
```

### Step 1 checklist

- [ ] `Install-WindowsOpenSshServer.ps1` completed (`OK: Port 22 is listening`)
- [ ] `ssh user@server` from PC works
- [ ] `Test-OnPremServerPrerequisites.ps1` → **PASS** sshd + TCP 22

**Stuck?** [learnings.md](./learnings.md) (Win32-OpenSSH, no sshd.exe, paste errors).

---

## Step 2 — Prepare server + install Docker Engine (WSL)

Implements **§ B** above. Details: [reference-ssh-and-docker-prep.md Part 2](./reference-ssh-and-docker-prep.md).

### 2.1 WSL component (server, first time only)

```powershell
wsl.exe --install --no-distribution
Restart-Computer -Force
```

**Verify after reboot:**

```powershell
wsl --status
```

No `WSL_E_WSL_OPTIONAL_COMPONENT_REQUIRED`.

### 2.2 Ubuntu + systemd + Docker (server)

```powershell
cd C:\visa2026-deploy
.\Install-WslDockerEngine.ps1
```

Complete Ubuntu username/password if prompted.

**If Ubuntu already installed** (only Docker missing):

```powershell
.\Install-WslDockerEngine.ps1 -SkipWslInstall -SkipSystemdConfig
```

**Verify:**

```powershell
wsl -l -v
wsl -d Ubuntu -u root -- systemctl is-system-running
wsl -d Ubuntu -u root -- docker run --rm hello-world
```

Expect: **Ubuntu** VERSION **2**, systemd **running**, **Hello from Docker!**

### Step 2 checklist

- [ ] `wsl -l -v` shows Ubuntu (WSL 2)
- [ ] `hello-world` succeeds
- [ ] `Test-OnPremServerPrerequisites.ps1` → **PASS** Docker + Compose plugin

**Stuck?** [learnings.md](./learnings.md) (SkipWslInstall, long apt, no distro).

---

## Step 3 — Setup docker compose (Visa2026)

### 3.1 Deploy files (server)

```powershell
New-Item -ItemType Directory -Path C:\visa2026 -Force
Copy-Item C:\visa2026-deploy\docker-compose.prod.yml C:\visa2026\
Copy-Item C:\visa2026-deploy\.env.prod.example C:\visa2026\.env.prod
notepad C:\visa2026\.env.prod
```

Required: `SA_PASSWORD`, `DEVEXPRESS_LICENSEKEY`.

### 3.2 Start stack (server)

```powershell
cd C:\visa2026-deploy
.\Start-Visa2026Compose.ps1 -Pull -OpenHttpFirewall
```

### 3.3 Verify

**On server:**

```powershell
.\Test-OnPremServerPrerequisites.ps1
wsl -d Ubuntu -u root -e bash -lc "cd /mnt/c/visa2026 && docker compose -p visa2026-prod ps"
```

**From PC (over SSH):**

```powershell
ssh <user>@<server-ip> "wsl -d Ubuntu -u root -e bash -lc 'cd /mnt/c/visa2026 && docker compose -p visa2026-prod ps'"
```

**Browser:** `http://<server-ip>` — login smoke test.

### Step 3 checklist

- [ ] `app` and `sqlserver` **Up** in `docker compose ps`
- [ ] HTTP works on LAN
- [ ] Prereq script: no **FAIL**

---

## Remote management (after Step 1)

```text
PC  --ssh-->  Windows PowerShell  --wsl-->  docker compose
```

Daily commands: [reference.md § Remote Docker from PC](./reference.md).

---

## Agent workflow (deterministic)

1. Read [learnings.md](./learnings.md) if user reports errors.
2. Use **only** [scripts/on-prem/](../../../scripts/on-prem/) for automation — never `scripts/local/` or `droplet-scripts/`.
3. **Step 0** → `Test-OnPremServerPrerequisites.ps1`.
4. **Step 1a** → `Install-WindowsOpenSshServer.ps1`.
5. **Step 1b** → `Test-NetConnection`, `ssh`, `scp` of **`scripts/on-prem/*.ps1`** to `C:\visa2026-deploy\`.
6. **Step 2** → `wsl --install --no-distribution`, reboot, `Install-WslDockerEngine.ps1`.
7. **Step 3** → `Start-Visa2026Compose.ps1`; schema one-shot → `Set-OnPremForceXafDbUpdate.ps1 -Enable` then `-Disable`.
8. Re-check `Test-OnPremServerPrerequisites.ps1` after steps 1–3.
9. [reference-ssh-and-docker-prep.md](./reference-ssh-and-docker-prep.md) for narrative SSH/Docker/connection detail (commands only, no foreign scripts).
10. One command block per message if user wants approval mode.
11. Update [ON_PREM_WINDOWS_SERVER.md § Reference deployment status](../../../docs/ON_PREM_WINDOWS_SERVER.md) per host.

---

## Investigation map (quick)

| Signal | Step | Action |
|--------|------|--------|
| Prereq RAM/disk WARN | 0 | Proceed if acceptable; plan upgrade |
| sshd FAIL | 1 | `Install-WindowsOpenSshServer.ps1` |
| PC port 22 FAIL | 1 | Firewall / IT / sshd |
| WSL component FAIL | 2 | `wsl --install --no-distribution`, reboot |
| Docker FAIL | 2 | `Install-WslDockerEngine.ps1` |
| compose files WARN | 3 | Copy to `C:\visa2026`, edit `.env.prod` |

Details: [learnings.md](./learnings.md).

---

## Continuous improvement

After each server reaches a stable step: **append** [learnings.md](./learnings.md). **Promote** to this file after 2+ servers hit the same issue.

---

## Example host status

**`10.100.128.25`:** Step 0–2 largely done (SSH, Ubuntu, systemd); confirm Step 2 `hello-world`; then Step 3 compose.
