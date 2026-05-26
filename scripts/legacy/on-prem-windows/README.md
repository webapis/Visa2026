# scripts/legacy/on-prem-windows — Windows Server + WSL (deprecated)

> **New on-prem prod:** [scripts/linux/](../../linux/README.md) · [setup-docker-engine](../../.cursor/skills/setup-docker-engine/SKILL.md) · [ON_PREM_LINUX_SERVER.md](../../docs/ON_PREM_LINUX_SERVER.md).

PowerShell/bash scripts for **Windows Server 2019/2022 + WSL 2** only. Copy to `C:\visa2026-deploy\` and `C:\WslDocker-Setup\` on the host.

**Runbook:** [docs/legacy/ON_PREM_WINDOWS_SERVER.md](../../docs/legacy/ON_PREM_WINDOWS_SERVER.md)

## Skills

| Skill | Scripts |
|-------|---------|
| [**legacy-on-prem-windows-setup**](../../.cursor/skills/legacy-on-prem-windows-setup/SKILL.md) | `Test-OnPremServerPrerequisites.ps1`, `Install-WslDockerEngine.ps1 -SkipDockerInstall` |
| [**setup-openssh-server**](../../.cursor/skills/setup-openssh-server/SKILL.md) (Win32 only) | `Install-WindowsOpenSshServer.ps1`, `Repair-WindowsOpenSshServer.ps1` |
| WSL Docker/compose (archived) | `Install-WslDockerEngine.ps1`, `Start-Visa2026Compose.ps1`, recovery scripts below |

## Allowlist — legacy-on-prem-windows-setup

| Script | Purpose |
|--------|---------|
| `Test-OnPremServerPrerequisites.ps1` | Hardware/OS/WSL/systemd audit |
| `Install-WslDockerEngine.ps1` | WSL + Ubuntu + systemd — **`-SkipDockerInstall`** |

## WSL Docker / compose (legacy; use Linux path for new hosts)

| Script | Purpose |
|--------|---------|
| `Install-WslDockerEngine.ps1` | **`-SkipWslInstall -SkipSystemdConfig`** (Docker only) |
| `Install-WslDockerEngine-Offline.ps1` | Offline `.deb` Docker install |
| `reference-docker-offline-install.md` | Prepare offline packages |
| `Start-Visa2026Compose.ps1` | Pull/up Visa2026 |
| `Repair-OnPremVisa2026Stack.ps1` | WSL keepalive + SQL-first + portproxy |
| `Register-Visa2026WslKeepAliveTask.ps1` | Boot/minute scheduled tasks |
| `Start-OnPremWslPersistent.ps1` | Hidden `wsl sleep infinity` |
| `Set-OnPremWslPortProxy.ps1` | LAN port 80 → WSL IP |
| `Monitor-OnPremWslStack.ps1` | Stability watch |
| `Set-OnPremForceXafDbUpdate.ps1` | One-shot `FORCE_XAF_DB_UPDATE` |
| `remote-compose-sql-up.sh` | Copy to `C:\WslDocker-Setup\` — SQL before app |
| `install-docker-engine.sh` | Docker in WSL Ubuntu (called by install script) |

Copy **`docker-compose.restart.override.yml`** from [scripts/linux/](../../linux/docker-compose.restart.override.yml) into `C:\visa2026\` if needed.

## Allowlist — setup-openssh-server (Win32 only)

| Script | Purpose |
|--------|---------|
| `Install-WindowsOpenSshServer.ps1` | Install/start `sshd`, firewall |
| `Repair-WindowsOpenSshServer.ps1` | Connection reset / domain ACLs |
| `Setup-OnPremSshAuthorizedKey.ps1` | `administrators_authorized_keys` |

**Maturity:** [on-prem-deploy/MATURITY.md](../../.cursor/skills/on-prem-deploy/MATURITY.md)
