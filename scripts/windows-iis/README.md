# scripts/windows-iis — native Windows Server (IIS, no Docker)

**Runbook:** [docs/ON_PREM_WINDOWS_IIS.md](../../docs/ON_PREM_WINDOWS_IIS.md)

**Agent skill:** [visa2026-windows-iis-deploy](../../.cursor/skills/visa2026-windows-iis-deploy/SKILL.md) · [learnings.md](../../.cursor/skills/visa2026-windows-iis-deploy/learnings.md)

Use when the company host is **Windows Server** and **Docker / WSL is not acceptable**. The app runs as **ASP.NET Core** behind **IIS**; SQL Server runs **on Windows** (same machine or separate).

**Not this folder:** `scripts/linux/` (Docker on Ubuntu), `scripts/legacy/on-prem-windows/` (WSL + Docker), `droplet-scripts/` (cloud).

## Scripts

| Script | Where to run | Purpose |
|--------|----------------|---------|
| `Publish-Visa2026ForIis.ps1` | Dev PC or build agent | `dotnet publish` → `dist/visa2026-iis-<version>/` |
| `Deploy-Visa2026IisRemote.ps1` | Dev PC (SSH) | Full deploy to `visa2026-onprem` |
| `Install-SqlServerExpress.ps1` | Windows Server | SQL Server 2022 Express (`SQLEXPRESS`) + create DB |
| `Configure-SqlExpressSaLogin.ps1` | Windows Server | After **manual** SQL install: mixed mode, `sa`, create DB |
| `Restore-Visa2026SqlBackup.ps1` | Windows Server | Restore `.bak` into `Visa2026DbProd` (`WITH REPLACE`) |
| `Install-Visa2026ServerPrerequisites.ps1` | Windows Server | IIS + .NET 8 Hosting Bundle |
| `Install-Visa2026IisSite.ps1` | Windows Server | Create app pool, site, WebSockets |
| `Configure-Visa2026Production.ps1` | Windows Server | `appsettings.Production.json` from `C:\visa2026\.env.prod` |
| `Set-Visa2026AppPoolEnvironment.ps1` | Windows Server | App pool env via **appcmd** |
| `Update-Visa2026Database.ps1` | Windows Server | `Visa2026.Blazor.Server.exe --updateDatabase` |
| `Run-Visa2026DbUpdateOnServer.ps1` | Windows Server | DB update using publish folder + env json |
| `Diagnose-Port80.ps1` | Windows Server | Port 80 listener + IIS sites + Docker ports |
| `Set-Visa2026IisAutoStart.ps1` | Windows Server | Auto-start Visa2026 after reboot; SQL before IIS; move Default Web Site off :80 |
| `Register-Visa2026IisBootTask.ps1` | Windows Server | Scheduled task **Visa2026-IisAfterBoot** (recycle after SQL on boot) |
| `Get-Visa2026IisStartupError.ps1` | Windows Server | Diagnose 500.30 / startup failures |

## Quick start (workstation)

```powershell
cd C:\path\to\Visa2026
.\scripts\windows-iis\Publish-Visa2026ForIis.ps1 -Zip -OpenOutputFolder
```

Copy the publish folder (or zip) to the server, e.g. `C:\inetpub\visa2026`.

## Server layout

```text
C:\inetpub\visa2026\          # publish output (site physical path)
C:\visa2026\.env.prod         # SA_PASSWORD, DEVEXPRESS_LICENSEKEY, DB_NAME
C:\visa2026-deploy\iis\       # scripts copied from repo
C:\visa2026\backups\          # SQL .bak files
C:\ProgramData\Visa2026\
  DataProtection-Keys\        # persist across app pool recycle (env var)
```

Set **app pool** environment variables (use `Set-Visa2026AppPoolEnvironment.ps1` or IIS Manager):

- `ASPNETCORE_ENVIRONMENT` = `Production`
- `DEVEXPRESS_LICENSEKEY` = (same value as Docker `.env.prod`)
- `ASPNETCORE_DATA_PROTECTION_KEYS` = `C:\ProgramData\Visa2026\DataProtection-Keys`

Connection string: **`Server=localhost\SQLEXPRESS`** in `appsettings.Production.json` (from `Configure-Visa2026Production.ps1`).

First deploy: run database update before opening the site to users:

```powershell
C:\visa2026-deploy\iis\Run-Visa2026DbUpdateOnServer.ps1 -ForceUpdate
```

Restore prod data from backup:

```powershell
C:\visa2026-deploy\iis\Restore-Visa2026SqlBackup.ps1 -BackupPath C:\visa2026\backups\<file>.bak
C:\visa2026-deploy\iis\Run-Visa2026DbUpdateOnServer.ps1
```

