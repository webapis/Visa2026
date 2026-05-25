# Docker Engine install in WSL (after server prep)

Companion to [SKILL.md](./SKILL.md). **WSL/Ubuntu/systemd/prereq checks** are **Windows server prep skill**, not here.

**Scenario catalog (install failures):** [SKILL.md § Scenarios](./SKILL.md#scenarios-that-hinder-docker-engine-installation-and-compose)

## Preconditions

| Check | Command |
|-------|---------|
| WSL 2 distro | `wsl -l -v` → **VERSION 2** |
| systemd | `wsl -d Ubuntu -u root -- systemctl is-system-running` → **running** |
| Outbound HTTPS | `wsl -d Ubuntu -u root -- curl -sS -o /dev/null -w '%{http_code}\n' --connect-timeout 10 https://download.docker.com` → **200** |

## Online install (this skill)

```powershell
cd C:\visa2026-deploy
.\Install-WslDockerEngine.ps1 -SkipWslInstall -SkipSystemdConfig
```

Or direct bash:

```powershell
wsl -d Ubuntu -u root -- bash /mnt/c/WslDocker-Setup/install-docker-engine.sh
```

## Offline install

See [scripts/on-prem/reference-docker-offline-install.md](../../../scripts/on-prem/reference-docker-offline-install.md).

```powershell
.\Install-WslDockerEngine-Offline.ps1
# optional: -DebDirectory D:\transfer\docker-debs
```

## Verify

```powershell
wsl -d Ubuntu -u root -- docker --version
wsl -d Ubuntu -u root -- docker compose version
wsl -d Ubuntu -u root -- docker run --rm hello-world
```

## IT outbound (WSL)

- `download.docker.com`
- `registry-1.docker.io`, `hub.docker.com`
- `mcr.microsoft.com` (for compose SQL image)

## Not in this skill

- Docker Desktop on Windows Server
- `wsl --install`, Ubuntu first login, `.wslconfig` (server prep skill)
