# On-prem Windows Server — command reference

Canonical runbook: [docs/ON_PREM_WINDOWS_SERVER.md](../../../docs/ON_PREM_WINDOWS_SERVER.md)

Skill: [SKILL.md](./SKILL.md)

Deploy root: `C:\visa2026\` (WSL: `/mnt/c/visa2026`). Scripts: `C:\visa2026-deploy\`.

---

## Step 0 — Prerequisite check

**On server (Administrator):**

```powershell
cd C:\visa2026-deploy
.\Test-OnPremServerPrerequisites.ps1
```

**From PC (LAN test to SSH):**

```powershell
Test-NetConnection -ComputerName 10.100.128.25 -Port 22
# If script is on PC with same version:
.\Test-OnPremServerPrerequisites.ps1 -ServerIp 10.100.128.25
```

Re-run after Step 1 (SSH), Step 2 (Docker), Step 3 (compose). Exit code **0** = no FAIL lines.

**Manual checks (if no script):**

```powershell
systeminfo | findstr /B /C:"OS Name" /C:"Total Physical Memory"
Get-CimInstance Win32_LogicalDisk -Filter "DeviceID='C:'" | Select-Object DeviceID, @{N='FreeGB';E={[math]::Round($_.FreeSpace/1GB,1)}}
```

Targets: **8+ GB RAM**, **100+ GB free on C:**, Server 2019/2022.

---

## SSH — connect from your PC

```powershell
Test-NetConnection -ComputerName 10.100.128.25 -Port 22
ssh adm43419@10.100.128.25
```

Optional `~/.ssh/config`:

```sshconfig
Host visa2026-onprem
    HostName 10.100.128.25
    User adm43419
```

Password auth: use Windows password for `adm43419`. Key auth: append PC public key to server `C:\Users\adm43419\.ssh\authorized_keys` (create `.ssh` if missing).

First connection: accept host key (`yes`).

---

## Remote Docker from your PC (over SSH)

Docker runs **inside WSL**, not as a native Windows service. From your PC:

**One-shot:**

```powershell
$server = 'adm43419@10.100.128.25'
ssh $server "wsl -d Ubuntu -u root -- docker ps"
ssh $server "wsl -d Ubuntu -u root -e bash -lc 'cd /mnt/c/visa2026 && docker compose -p visa2026-prod ps'"
ssh $server "wsl -d Ubuntu -u root -e bash -lc 'cd /mnt/c/visa2026 && docker compose -p visa2026-prod logs app --tail 80'"
```

**Interactive** (stay in remote PowerShell):

```powershell
ssh adm43419@10.100.128.25
```

Then on the server:

```powershell
wsl -d Ubuntu -u root -- docker ps
wsl -d Ubuntu -u root -e bash -lc "cd /mnt/c/visa2026 && docker compose -p visa2026-prod ps"
wsl -d Ubuntu -u root -e bash -lc "cd /mnt/c/visa2026 && docker compose -p visa2026-prod logs app -f"
```

**Pull / up via remote script** (server admin rights required):

```powershell
ssh adm43419@10.100.128.25 "powershell -ExecutionPolicy Bypass -File C:/visa2026-deploy/Start-Visa2026Compose.ps1 -Pull -AppOnly"
```

**Enter WSL shell directly** (Linux prompt on server):

```powershell
ssh adm43419@10.100.128.25
wsl -d Ubuntu -u root
cd /mnt/c/visa2026
docker compose -p visa2026-prod ps
```

---

## Copy bundle to server

From dev machine (after SSH works):

```powershell
scp -r .\scripts\on-prem\ adm43419@10.100.128.25:C:/visa2026-deploy/
scp .\docker-compose.prod.yml adm43419@10.100.128.25:C:/visa2026-deploy/
scp .\.env.prod.example adm43419@10.100.128.25:C:/visa2026-deploy/
```

Adjust user/IP. Or use RDP copy to `C:\visa2026-deploy\`.

---

## Phase 1 — OpenSSH

```powershell
cd C:\visa2026-deploy
.\Install-WindowsOpenSshServer.ps1
```

Offline:

```powershell
.\Install-WindowsOpenSshServer.ps1 -ZipPath C:\Temp\OpenSSH-Win64.zip -SkipCapabilityRepair
```

Blocked execution policy:

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\Install-WindowsOpenSshServer.ps1
```

Verify from admin PC:

```powershell
Test-NetConnection -ComputerName 10.100.128.25 -Port 22
ssh adm43419@10.100.128.25
```

---

## Phase 2 — WSL component

```powershell
wsl.exe --install --no-distribution
Restart-Computer -Force
```

After reboot:

```powershell
wsl --status
wsl -l -v
```

---

## Phase 3 — Ubuntu + systemd + Docker

**First run (installs Ubuntu if needed):**

```powershell
cd C:\visa2026-deploy
.\Install-WslDockerEngine.ps1
```

Complete Ubuntu user/password prompts if shown.

**Docker only (Ubuntu + systemd already OK):**

```powershell
.\Install-WslDockerEngine.ps1 -SkipWslInstall -SkipSystemdConfig
```

Verify:

```powershell
wsl -l -v
wsl -d Ubuntu -u root -- systemctl is-system-running
wsl -d Ubuntu -u root -- docker --version
wsl -d Ubuntu -u root -- docker compose version
wsl -d Ubuntu -u root -- docker run --rm hello-world
```

Monitor long install:

```powershell
wsl -d Ubuntu -u root -- bash -c "pgrep -a apt || pgrep -a dpkg || echo done"
```

---

## Phase 4 — Visa2026 compose

```powershell
New-Item -ItemType Directory -Path C:\visa2026 -Force
Copy-Item C:\visa2026-deploy\docker-compose.prod.yml C:\visa2026\
Copy-Item C:\visa2026-deploy\.env.prod.example C:\visa2026\.env.prod
notepad C:\visa2026\.env.prod
```

Required keys: `SA_PASSWORD`, `DEVEXPRESS_LICENSEKEY`.

Start:

```powershell
cd C:\visa2026-deploy
.\Start-Visa2026Compose.ps1 -Pull -OpenHttpFirewall
```

Manual equivalent:

```powershell
wsl -d Ubuntu -u root -e bash -lc "cd /mnt/c/visa2026 && docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml pull && docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml up -d"
```

Status / logs:

```powershell
wsl -d Ubuntu -u root -e bash -lc "cd /mnt/c/visa2026 && docker compose -p visa2026-prod ps"
wsl -d Ubuntu -u root -e bash -lc "cd /mnt/c/visa2026 && docker compose -p visa2026-prod logs app --tail 100"
```

---

## App-only update

```powershell
.\Start-Visa2026Compose.ps1 -Pull -AppOnly
```

---

## FORCE_XAF_DB_UPDATE (one-shot, on-prem script only)

```powershell
cd C:\visa2026-deploy
.\Set-OnPremForceXafDbUpdate.ps1 -Enable
# After healthy start and updaters confirmed:
.\Set-OnPremForceXafDbUpdate.ps1 -Disable
```

---

## Distro name not `Ubuntu`

```powershell
wsl -l -v
.\Install-WslDockerEngine.ps1 -SkipWslInstall -DistroName Ubuntu-22.04
.\Start-Visa2026Compose.ps1 -DistroName Ubuntu-22.04
```

---

## Firewall (manual HTTP)

```powershell
New-NetFirewallRule -Name 'Visa2026-HTTP-In-TCP' -DisplayName 'Visa2026 HTTP' `
  -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 80
```
