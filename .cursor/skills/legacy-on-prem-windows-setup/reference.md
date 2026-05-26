# legacy-on-prem-windows-setup — command reference

**Prerequisites (hardware/software):** [docs/ON_PREM_PREREQUISITES.md](../../../docs/ON_PREM_PREREQUISITES.md)

Skill: [SKILL.md](./SKILL.md) · Runbook: [docs/legacy/ON_PREM_WINDOWS_SERVER.md](../../../docs/legacy/ON_PREM_WINDOWS_SERVER.md) · **Maturity:** [on-prem-deploy/MATURITY.md](../on-prem-deploy/MATURITY.md)

**Blockers before Docker Engine:** [SKILL.md § Scenarios](./SKILL.md#scenarios-that-hinder-docker-engine-setup)

## Step 0 — Prerequisite script

**On server:**

```powershell
cd C:\visa2026-deploy
.\Test-OnPremServerPrerequisites.ps1
```

**From admin PC (+ port 22 reachability):**

```powershell
.\Test-OnPremServerPrerequisites.ps1 -ServerIp 10.100.128.25
```

**Before compose (stricter — use in setup-docker-engine phase):**

```powershell
.\Test-OnPremServerPrerequisites.ps1 -RequireDocker -RequireDeployFiles
```

Exit code **0** = no FAIL.

## Manual hardware check

```powershell
systeminfo | findstr /B /C:"OS Name" /C:"Total Physical Memory"
Get-CimInstance Win32_ComputerSystem | Select-Object NumberOfLogicalProcessors
Get-CimInstance Win32_LogicalDisk -Filter "DeviceID='C:'" | Select-Object @{N='FreeGB';E={[math]::Round($_.FreeSpace/1GB,1)}}
whoami; $env:USERPROFILE
```

## Step 1 — WSL bootstrap

```powershell
wsl.exe --install --no-distribution
Restart-Computer -Force
```

After reboot:

```powershell
cd C:\visa2026-deploy
.\Install-WslDockerEngine.ps1 -SkipDockerInstall
```

Ubuntu exists, fix systemd only:

```powershell
.\Install-WslDockerEngine.ps1 -SkipWslInstall -SkipDockerInstall
```

## WSL idle timeout (production)

```powershell
"[wsl2]`nvmIdleTimeout=-1" | Set-Content -Path C:\Users\<user>\.wslconfig -Encoding ascii
wsl --shutdown
wsl -d Ubuntu -u root -- echo WSL_OK
```

## Verify WSL ready for Docker install

```powershell
wsl -l -v
wsl -d Ubuntu -u root -- systemctl is-system-running
```

Expect: **VERSION 2**, **Running**, **running**.

## Next skill

[setup-docker-engine](../setup-docker-engine/SKILL.md) — `Install-WslDockerEngine.ps1 -SkipWslInstall -SkipSystemdConfig` and compose.
