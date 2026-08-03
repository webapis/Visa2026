# windows-docker-desktop scripts

| Script | Purpose |
|--------|---------|
| [Prepare-Visa2026DesktopPilot.ps1](./Prepare-Visa2026DesktopPilot.ps1) | Copy compose/env helpers to a client-style folder (pilot or staging) |

Examples:

```powershell
.\Prepare-Visa2026DesktopPilot.ps1
.\Prepare-Visa2026DesktopPilot.ps1 -TargetDir 'E:\visa2026-staging' -ProjectName visa2026-staging -DbName visa2026_staging_docker -AppPort 9080 -PgHostPort 5434
```

On-prem `.25` staging layout (created 2026-08-03): `E:\visa2026-staging` — needs Docker installed before `compose up`.

Runbook: [../../docs/ON_PREM_WINDOWS_DOCKER_DESKTOP.md](../../docs/ON_PREM_WINDOWS_DOCKER_DESKTOP.md)  
Checklist: [../../docs/windows-docker-desktop/PILOT_CHECKLIST.md](../../docs/windows-docker-desktop/PILOT_CHECKLIST.md)
