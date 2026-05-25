# setup-docker-engine — command reference

Runbook: [docs/ON_PREM_WINDOWS_SERVER.md](../../../docs/ON_PREM_WINDOWS_SERVER.md)

Skill: [SKILL.md](./SKILL.md) · Install detail: [reference-docker-install.md](./reference-docker-install.md) · **Maturity:** [on-prem-windows-deploy/MATURITY.md](../on-prem-windows-deploy/MATURITY.md)

Deploy root: `C:\visa2026\` (WSL: `/mnt/c/visa2026`). Scripts: `C:\visa2026-deploy\`.

**Dependency:** [visa2026-windows-server-setup](../visa2026-windows-server-setup/SKILL.md) must be **complete** before any command below.

Prerequisites: [docs/ON_PREM_PREREQUISITES.md](../../../docs/ON_PREM_PREREQUISITES.md)

**Install / compose blockers:** [SKILL.md § Scenarios](./SKILL.md#scenarios-that-hinder-docker-engine-installation-and-compose)

---

## Gate 0 — windows-server-setup complete? (mandatory)

```powershell
cd C:\visa2026-deploy
.\Test-OnPremServerPrerequisites.ps1
```

Exit **0** required. **FAIL** on WSL/systemd → go back to **visa2026-windows-server-setup**; do **not** install Docker.

```powershell
wsl -l -v
wsl -d Ubuntu -u root -- systemctl is-system-running
```

---

## Step 1 — Docker Engine

```powershell
cd C:\visa2026-deploy
.\Install-WslDockerEngine.ps1 -SkipWslInstall -SkipSystemdConfig
```

**Direct bash (if PS wrapper hangs):**

```powershell
wsl -d Ubuntu -u root -- bash /mnt/c/WslDocker-Setup/install-docker-engine.sh
```

**Offline:** `Install-WslDockerEngine-Offline.ps1` — [reference-docker-offline-install.md](../../../scripts/on-prem/reference-docker-offline-install.md).

**Monitor install log:**

```powershell
wsl -d Ubuntu -u root -- tail -n 30 /var/log/visa-docker-install.log
```

**Verify:**

```powershell
wsl -d Ubuntu -u root -- docker run --rm hello-world
```

---

## Step 2 — Visa2026 compose

```powershell
cd C:\visa2026-deploy
.\Start-Visa2026Compose.ps1 -Pull -OpenHttpFirewall
```

**Manual:**

```powershell
wsl -d Ubuntu -u root -e bash -lc "cd /mnt/c/visa2026 && docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml pull && docker compose -p visa2026-prod --env-file .env.prod -f docker-compose.prod.yml up -d"
```

**Status / logs:**

```powershell
wsl -d Ubuntu -u root -e bash -lc "cd /mnt/c/visa2026 && docker compose -p visa2026-prod ps -a"
wsl -d Ubuntu -u root -e bash -lc "cd /mnt/c/visa2026 && docker compose -p visa2026-prod logs app --tail 100"
wsl -d Ubuntu -u root -e bash -lc "cd /mnt/c/visa2026 && docker compose -p visa2026-prod logs sqlserver --tail 50"
```

---

## App-only update

```powershell
.\Start-Visa2026Compose.ps1 -Pull -AppOnly
```

---

## FORCE_XAF_DB_UPDATE

```powershell
.\Set-OnPremForceXafDbUpdate.ps1 -Enable
.\Set-OnPremForceXafDbUpdate.ps1 -Disable
```

---

## Distro not `Ubuntu`

```powershell
wsl -l -v
.\Install-WslDockerEngine.ps1 -SkipWslInstall -SkipSystemdConfig -DistroName Ubuntu-22.04
.\Start-Visa2026Compose.ps1 -DistroName Ubuntu-22.04
```
