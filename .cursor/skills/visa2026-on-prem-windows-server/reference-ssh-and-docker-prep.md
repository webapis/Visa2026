# SSH configuration + Windows Server Docker prep + client connection

Companion to [SKILL.md](./SKILL.md). Commands assume deploy folder `C:\visa2026-deploy\`.

**Automation:** only [scripts/on-prem/](../../../scripts/on-prem/) — see [README.md](../../../scripts/on-prem/README.md). No `scripts/local/` or `droplet-scripts/`.

---

## Part 1 — SSH configuration on Windows Server

### What you are configuring

| Item | Purpose |
|------|---------|
| **`sshd` service** | Listens on **TCP 22**, accepts remote logins |
| **Firewall** | Allow inbound **22** on the server |
| **Default shell** | Remote sessions open **PowerShell** (not `cmd`) |
| **`sshd_config`** | Optional: keys, users, hardening (advanced) |
| **`authorized_keys`** | Optional: passwordless login from your PC |

**Do not use Docker Desktop on the server.** SSH lands on **Windows**; you then run **`wsl.exe`** for Docker.

### 1.1 Automated install (recommended)

Administrator PowerShell on the server:

```powershell
cd C:\visa2026-deploy
.\Install-WindowsOpenSshServer.ps1
```

Script:

- Downloads **Win32-OpenSSH** if `sshd.exe` is missing (fixes “capability Installed but no sshd”)
- Runs `install-sshd.ps1`, registers **`sshd`** service (**Automatic**)
- Creates firewall rule **OpenSSH-Server-In-TCP** (port 22)
- Sets **DefaultShell** = PowerShell (`HKLM:\SOFTWARE\OpenSSH`)

**Verify on server:**

```powershell
Get-Service sshd
Get-NetTCPConnection -LocalPort 22 -State Listen
```

### 1.2 Manual checks if SSH still fails

```powershell
Get-Service *ssh*
Get-ChildItem C:\Windows\System32\OpenSSH\sshd.exe -ErrorAction SilentlyContinue
Get-ChildItem C:\ProgramData\ssh\sshd_config -ErrorAction SilentlyContinue
```

If `sshd.exe` only under `C:\Program Files\OpenSSH\OpenSSH-Win64\`, the install script should have registered the service — re-run the script.

Restart SSH after config changes:

```powershell
Restart-Service sshd
```

### 1.3 Optional: SSH key login (server side)

On the server, for Windows user `adm43419`:

```powershell
$sshDir = 'C:\Users\adm43419\.ssh'
New-Item -ItemType Directory -Path $sshDir -Force | Out-Null
# Paste your PC public key (one line, ssh-ed25519 or ssh-rsa) into:
notepad C:\Users\adm43419\.ssh\authorized_keys
```

Fix permissions (Administrator):

```powershell
icacls C:\Users\adm43419\.ssh\authorized_keys /inheritance:r
icacls C:\Users\adm43419\.ssh\authorized_keys /grant "adm43419:F"
icacls C:\Users\adm43419\.ssh\authorized_keys /grant "SYSTEM:F"
```

Ensure `sshd_config` allows public key auth (default on Win32-OpenSSH often includes):

```text
PubkeyAuthentication yes
```

File path: `C:\ProgramData\ssh\sshd_config`

### 1.4 Optional: `sshd_config` tweaks

Edit as Administrator:

```powershell
notepad C:\ProgramData\ssh\sshd_config
```

Common lines (only change if you understand impact):

```text
Port 22
PasswordAuthentication yes
PubkeyAuthentication yes
Subsystem powershell c:/progra~1/powershell/7/pwsh.exe -sshs -NoLogo
```

If using **Windows PowerShell 5.1** only, `Install-WindowsOpenSshServer.ps1` sets DefaultShell via registry instead.

After edit: `Restart-Service sshd`

### 1.5 IT / network for SSH

- **Inbound** on server: TCP **22**
- **LAN rule**: allow **your PC IP** → server **22** (company firewall)
- Outbound from server not required for SSH itself

---

## Part 2 — Prepare Windows Server for Docker Engine

Docker Engine for Visa2026 is **Linux Docker inside WSL 2**, not Windows containers.

### Preparation checklist (server)

| # | Task | Command / note |
|---|------|----------------|
| 1 | Meet sizing | 8+ GB RAM, 100+ GB free on `C:` — `Test-OnPremServerPrerequisites.ps1` |
| 2 | Enable WSL | `wsl.exe --install --no-distribution` |
| 3 | **Reboot** | Required after step 2 |
| 4 | Install Ubuntu | `wsl --install Ubuntu` or `Install-WslDockerEngine.ps1` |
| 5 | First Ubuntu login | Create Linux user + password when prompted |
| 6 | Enable **systemd** | `/etc/wsl.conf` → `systemd=true`, `wsl --shutdown` |
| 7 | Install Docker packages | `Install-WslDockerEngine.ps1` (apt + Docker CE) |
| 8 | Verify | `docker run hello-world` |

### 2.1 WSL optional component

```powershell
wsl.exe --install --no-distribution
```

Expected: success message + **reboot required**.

```powershell
Restart-Computer -Force
```

After reboot:

```powershell
wsl --status
```

Must **not** show `WSL_E_WSL_OPTIONAL_COMPONENT_REQUIRED`.

### 2.2 Linux distro (Ubuntu) + WSL 2

```powershell
wsl --list --online
wsl --install Ubuntu
```

Or:

```powershell
.\Install-WslDockerEngine.ps1
```

Verify:

```powershell
wsl -l -v
```

Expect: `Ubuntu` (or `Ubuntu-22.04`), **VERSION 2**, **Running** or **Stopped**.

### 2.3 systemd (required for `docker` service)

```powershell
wsl -d Ubuntu -u root bash -c "printf '%s\n' '[boot]' 'systemd=true' > /etc/wsl.conf"
wsl --shutdown
```

Wait 10 seconds, then:

```powershell
wsl -d Ubuntu -u root -- systemctl is-system-running
```

Expect: **running** (or **degraded** is often OK).

### 2.4 Docker Engine + Compose plugin

```powershell
.\Install-WslDockerEngine.ps1 -SkipWslInstall -SkipSystemdConfig
```

Needs outbound HTTPS to **download.docker.com** and (for `hello-world`) **Docker Hub**.

Verify:

```powershell
wsl -d Ubuntu -u root -- docker --version
wsl -d Ubuntu -u root -- docker compose version
wsl -d Ubuntu -u root -- docker run --rm hello-world
```

### 2.5 What is NOT part of Docker prep

- Installing **Docker Desktop** on Windows Server
- Installing **SQL Server on Windows** (SQL runs in Linux container later)
- Enabling **Windows containers** feature for Visa2026 compose

### 2.6 Outbound network (IT)

Allow from server/WSL to:

- `download.docker.com`
- `registry-1.docker.io`, `hub.docker.com`
- `mcr.microsoft.com`

---

## Part 3 — Establish SSH connection from your PC

### 3.1 Prerequisites on your PC

- Windows 10/11 with **OpenSSH Client** (optional feature), or Windows 11 built-in `ssh`
- Network route to server (same LAN or VPN)
- Credentials: **Windows username + password** for the server, or **SSH private key** matching `authorized_keys`

Check client:

```powershell
ssh -V
```

### 3.2 Test network before SSH

```powershell
Test-NetConnection -ComputerName 10.100.128.25 -Port 22
```

| Result | Meaning |
|--------|---------|
| `PingSucceeded : True`, `TcpTestSucceeded : True` | Ready to `ssh` |
| Ping OK, TCP **False** | `sshd`/firewall not ready on server — Part 1 |
| Ping **False** | Wrong IP, offline, or network block |

### 3.3 First SSH login (password)

```powershell
ssh adm43419@10.100.128.25
```

- First time: `Are you sure you want to continue connecting` → type **`yes`**
- Enter **Windows account password** for `adm43419` (typing is hidden)

Success: remote prompt like `PS C:\Users\adm43419>` or `adm43419@HOSTNAME C:\Users\adm43419>`.

### 3.4 SSH config file on your PC (optional)

File: `C:\Users\<you>\.ssh\config`

```sshconfig
Host visa2026-onprem
    HostName 10.100.128.25
    User adm43419
    # IdentityFile C:\Users\<you>\.ssh\id_ed25519
```

Connect with:

```powershell
ssh visa2026-onprem
```

### 3.5 SSH with key (optional)

On your PC (if you do not have a key yet):

```powershell
ssh-keygen -t ed25519 -f $env:USERPROFILE\.ssh\id_ed25519_visa2026 -N '""'
```

Copy public key to server `authorized_keys` (Part 1.3), then:

```powershell
ssh -i $env:USERPROFILE\.ssh\id_ed25519_visa2026 adm43419@10.100.128.25
```

### 3.6 Copy files over SSH (`scp`)

After SSH works:

```powershell
scp -r .\scripts\on-prem\ adm43419@10.100.128.25:C:/visa2026-deploy/
scp .\docker-compose.prod.yml adm43419@10.100.128.25:C:/visa2026-deploy/
```

Use forward slashes `C:/path` for OpenSSH scp on Windows Server.

### 3.7 Run remote commands without interactive session

```powershell
ssh adm43419@10.100.128.25 "hostname"
ssh adm43419@10.100.128.25 "wsl -d Ubuntu -u root -- docker ps"
```

### 3.8 Typical connection errors

| Error | Fix |
|-------|-----|
| `Connection refused` | Start `sshd`, open firewall 22 |
| `Connection timed out` | IT firewall / wrong IP |
| `Permission denied (publickey,password)` | Wrong user/password; check `authorized_keys` permissions |
| `Host key verification failed` | Server rebuilt — remove old key from `~/.ssh/known_hosts` |
| SSH works but `docker` not found | Use `wsl -d Ubuntu -u root -- docker ...` |

---

## After all three parts

1. **SSH** → manage server from PC  
2. **Docker in WSL** → `wsl ... docker compose`  
3. **Visa2026** → `Start-Visa2026Compose.ps1` and [reference.md](./reference.md)
