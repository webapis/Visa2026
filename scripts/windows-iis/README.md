# scripts/windows-iis — native Windows Server (IIS, no Docker)

**Runbook:** [docs/ON_PREM_WINDOWS_IIS.md](../../docs/ON_PREM_WINDOWS_IIS.md)

**Agent skill:** [visa2026-windows-iis-deploy](../../.cursor/skills/visa2026-windows-iis-deploy/SKILL.md) · [learnings.md](../../.cursor/skills/visa2026-windows-iis-deploy/learnings.md)

Use when the company host is **Windows Server** and **Docker / WSL is not acceptable**. The app runs as **ASP.NET Core** behind **IIS**; SQL Server runs **on Windows** (same machine).

**Not this folder:** `scripts/linux/` (Docker on Ubuntu), `scripts/legacy/on-prem-windows/` (WSL + Docker), `droplet-scripts/` (cloud).

## Deployment slots

One server hosts **three independent IIS stacks** (see [Visa2026-IisSlots.ps1](./Visa2026-IisSlots.ps1)):

| Profile | Port | Site | Publish path | Env file | Database |
|---------|------|------|--------------|----------|----------|
| **Production** | 80 | `Visa2026-Prod` | `C:\inetpub\visa2026-prod` | `C:\visa2026\env\prod.env` | `Visa2026DbProd` |
| **Staging** | 8080 | `Visa2026-Staging` | `C:\inetpub\visa2026-staging` | `C:\visa2026\env\staging.env` | `Visa2026DbStaging` |
| **Demo** | 8081 | `Visa2026-Demo` | `C:\inetpub\visa2026-demo` | `C:\visa2026\env\demo.env` | `Visa2026DbDemo` |

Pass **`-Profile Production|Staging|Demo`** on slot-aware scripts. Env templates: [env/](./env/).

**Legacy** single site (`Visa2026` / `C:\inetpub\visa2026` / `C:\visa2026\.env.prod`) — **`-Profile Legacy`** during migration.

## Scripts

| Script | Where to run | Purpose |
|--------|----------------|---------|
| `Visa2026-IisSlots.ps1` | (dot-sourced) | Slot manifest: ports, paths, DB names |
| `Publish-Visa2026ForIis.ps1` | Dev PC | `dotnet publish` → `dist/visa2026-iis-<version>/` |
| `Deploy-Visa2026IisRemote.ps1` | Dev PC (SSH) | Deploy **one slot** (default Production) |
| `Deploy-Visa2026IisSlotRemote.ps1` | Dev PC (SSH) | Same; explicit slot deploy implementation |
| `Install-Visa2026IisSlots.ps1` | Windows Server | Create all slot sites, env files, databases |
| `Ensure-Visa2026SlotDatabases.ps1` | Windows Server | `CREATE DATABASE` for slot DBs if missing |
| `Set-Visa2026IisSlotsAutoStart.ps1` | Windows Server | Auto-start all slots; Default Web Site → `127.0.0.1:8090` |
| `Enable-Visa2026IisSlotFirewall.ps1` | Windows Server | Inbound TCP firewall for Staging `:8080` and Demo `:8081` |
| `Install-SqlServerExpress.ps1` | Windows Server | SQL Server 2022 Express (`SQLEXPRESS`) |
| `Configure-SqlExpressSaLogin.ps1` | Windows Server | After manual SQL install: mixed mode, `sa` |
| `Restore-Visa2026SqlBackup.ps1` | Windows Server | Restore `.bak` (`-Profile Production` default) |
| `Install-Visa2026ServerPrerequisites.ps1` | Windows Server | IIS + .NET 8 Hosting Bundle |
| `Install-Visa2026IisSite.ps1` | Windows Server | One site/app pool (`-Profile` or explicit paths) |
| `Configure-Visa2026Production.ps1` | Windows Server | `appsettings.Production.json` from slot env file |
| `Set-Visa2026AppPoolEnvironment.ps1` | Windows Server | App pool env via **appcmd** |
| `Update-Visa2026Database.ps1` | Windows Server | `Visa2026.Blazor.Server.exe --updateDatabase` |
| `Run-Visa2026DbUpdateOnServer.ps1` | Windows Server | DB update for one slot |
| `Diagnose-Port80.ps1` | Windows Server | Port 80 listener + IIS sites |
| `Set-Visa2026IisAutoStart.ps1` | Windows Server | Auto-start one site (used by slots script) |
| `Register-Visa2026IisBootTask.ps1` | Windows Server | Scheduled task after boot |
| `Get-Visa2026IisStartupError.ps1` | Windows Server | Diagnose 500.30 (`-Profile`) |
| `Get-Visa2026RecentIisErrors.ps1` | Windows Server | Application event log triage |
| `Test-Visa2026Startup.ps1` | Windows Server | Console startup probe (`-Profile`) |
| `Enable-Visa2026StdoutLog.ps1` | Windows Server | Enable stdout logging (`-Profile`) |
| `Set-Visa2026EnvDbName.ps1` | Windows Server | **Legacy** — change `DB_NAME` in one env file; prefer slot deploy |
| `Remove-Visa2026ForceXafDbUpdate.ps1` | Windows Server | Remove `FORCE_XAF_DB_UPDATE` from slot app pool |

## Quick start (workstation)

```powershell
cd C:\path\to\Visa2026
.\scripts\windows-iis\Deploy-Visa2026IisRemote.ps1 -Profile Production
.\scripts\windows-iis\Deploy-Visa2026IisRemote.ps1 -Profile Staging
.\scripts\windows-iis\Deploy-Visa2026IisRemote.ps1 -Profile Demo
```

## Server layout

```text
C:\inetpub\visa2026-prod\       # Production publish (:80)
C:\inetpub\visa2026-staging\    # Staging (:8080)
C:\inetpub\visa2026-demo\       # Demo (:8081)
C:\visa2026\env\
  prod.env                      # SA_PASSWORD, DEVEXPRESS_LICENSEKEY, DB_NAME
  staging.env
  demo.env
C:\visa2026-deploy\iis\         # scripts copied from repo
C:\visa2026\backups\
  prod\  staging\  demo\       # SQL .bak per slot
C:\ProgramData\Visa2026\
  DataProtection-Keys-Prod\
  DataProtection-Keys-Staging\
  DataProtection-Keys-Demo\
```

First deploy per slot:

```powershell
C:\visa2026-deploy\iis\Run-Visa2026DbUpdateOnServer.ps1 -Profile Production -ForceUpdate
```

Restore production data:

```powershell
C:\visa2026-deploy\iis\Restore-Visa2026SqlBackup.ps1 -Profile Production -BackupPath C:\visa2026\backups\prod\<file>.bak
C:\visa2026-deploy\iis\Run-Visa2026DbUpdateOnServer.ps1 -Profile Production
```
