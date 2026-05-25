# scripts/on-prem — Windows Server LAN deploy (skill-only)

PowerShell scripts used **only** by [`.cursor/skills/visa2026-on-prem-windows-server/SKILL.md`](../../.cursor/skills/visa2026-on-prem-windows-server/SKILL.md).

**Do not** point that skill at `scripts/local/`, `droplet-scripts/`, or other repo scripts.

## Allowlist

| Script | Purpose |
|--------|---------|
| `Test-OnPremServerPrerequisites.ps1` | Step 0 — OS/RAM/disk, sshd, WSL, Docker, deploy files |
| `Install-WindowsOpenSshServer.ps1` | Step 1 — OpenSSH Server (`sshd`), port 22 |
| `Install-WslDockerEngine.ps1` | Step 2 — WSL 2, Ubuntu, systemd, Docker Engine |
| `Start-Visa2026Compose.ps1` | Step 3 — pull/up Visa2026 via WSL |
| `Set-OnPremForceXafDbUpdate.ps1` | Optional — one-shot `FORCE_XAF_DB_UPDATE` + recreate app |

## Copy to server

Place all `.ps1` files in `C:\visa2026-deploy\` on each Windows Server.

Runbook: [docs/ON_PREM_WINDOWS_SERVER.md](../../docs/ON_PREM_WINDOWS_SERVER.md)
