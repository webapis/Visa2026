# scripts/on-prem — Windows Server LAN

Scripts split across **Agent skills** — do not mix allowlists.

## Skills

| Skill | Scripts |
|-------|---------|
| [**visa2026-windows-server-setup**](../../.cursor/skills/visa2026-windows-server-setup/SKILL.md) | `Test-OnPremServerPrerequisites.ps1`, `Install-WslDockerEngine.ps1` with **`-SkipDockerInstall`** |
| [**setup-docker-engine**](../../.cursor/skills/setup-docker-engine/SKILL.md) | Docker install + compose (**after** windows-server-setup) |
| [**setup-openssh-server**](../../.cursor/skills/setup-openssh-server/SKILL.md) | `Install-WindowsOpenSshServer.ps1`, `Repair-WindowsOpenSshServer.ps1` |

## Allowlist — visa2026-windows-server-setup

| Script | Purpose |
|--------|---------|
| `Test-OnPremServerPrerequisites.ps1` | Hardware/OS/WSL/systemd audit; optional `-RequireDocker` before compose |
| `Install-WslDockerEngine.ps1` | WSL + Ubuntu + systemd — **`-SkipDockerInstall`** |

## Allowlist — setup-docker-engine (blocked until windows-server-setup done)

| Script | Purpose |
|--------|---------|
| `Test-OnPremServerPrerequisites.ps1` | **Gate 0 only** — verify FAIL=0 on WSL/systemd before Docker |
| `Install-WslDockerEngine.ps1` | **`-SkipWslInstall -SkipSystemdConfig`** (Docker only; never full bootstrap) |
| `Install-WslDockerEngine-Offline.ps1` | Offline `.deb` Docker install |
| `reference-docker-offline-install.md` | Prepare offline packages |
| `Start-Visa2026Compose.ps1` | Pull/up Visa2026 |
| `Set-OnPremForceXafDbUpdate.ps1` | One-shot `FORCE_XAF_DB_UPDATE` |

## Allowlist — setup-openssh-server

| Script | Purpose |
|--------|---------|
| `Install-WindowsOpenSshServer.ps1` | Install/start `sshd`, firewall |
| `Repair-WindowsOpenSshServer.ps1` | Connection reset, domain/admin ACLs, config |
| `Test-OnPremServerPrerequisites.ps1` | Verify `sshd` / TCP 22 (optional `-ServerIp` from admin PC) |

## SSH scripts (detail)

| Script | Purpose |
|--------|---------|
| `Install-WindowsOpenSshServer.ps1` | OpenSSH Server |
| `Repair-WindowsOpenSshServer.ps1` | Fix connection reset / ACLs |

## Copy to server

`C:\visa2026-deploy\` on each host.

Prerequisites: [docs/ON_PREM_PREREQUISITES.md](../../docs/ON_PREM_PREREQUISITES.md) · Runbook: [docs/ON_PREM_WINDOWS_SERVER.md](../../docs/ON_PREM_WINDOWS_SERVER.md)

**Skill maturity (try/test/fix → learnings → promote):** [on-prem-windows-deploy/MATURITY.md](../../.cursor/skills/on-prem-windows-deploy/MATURITY.md)
