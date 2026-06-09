# visa2026-windows-iis-deploy — reference commands

**Runbook:** [docs/ON_PREM_WINDOWS_IIS.md](../../../docs/ON_PREM_WINDOWS_IIS.md)

**Slot manifest:** [Visa2026-IisSlots.ps1](../../../scripts/windows-iis/Visa2026-IisSlots.ps1)

## Deployment slots (one Windows Server)

| Slot | Port | Smoke URL (LAN) | Site | App pool | Publish | Env | DB |
|------|------|-----------------|------|----------|---------|-----|-----|
| **Production** | 80 | `http://<server>/LoginPage` | `Visa2026-Prod` | `Visa2026-Prod` | `C:\inetpub\visa2026-prod` | `C:\visa2026\env\prod.env` | `Visa2026DbProd` |
| **Staging** | 8080 | `http://<server>:8080/LoginPage` | `Visa2026-Staging` | `Visa2026-Staging` | `C:\inetpub\visa2026-staging` | `C:\visa2026\env\staging.env` | `Visa2026DbStaging` |
| **Demo** | 8081 | `http://<server>:8081/LoginPage` | `Visa2026-Demo` | `Visa2026-Demo` | `C:\inetpub\visa2026-demo` | `C:\visa2026\env\demo.env` | `Visa2026DbDemo` |

**Shared on server:**

| Path | Purpose |
|------|---------|
| `C:\visa2026-deploy\iis\` | Scripts from repo |
| `C:\visa2026\backups\{prod,staging,demo}\` | SQL `.bak` per slot |
| `C:\ProgramData\Visa2026\DataProtection-Keys-{Prod,Staging,Demo}\` | Auth cookies per slot |
| `C:\visa2026\.env.prod` | Legacy secrets file (can seed `env\*.env`) |

**SQL:** `localhost\SQLEXPRESS` — three databases on one instance.

---

## Dev PC — publish

```powershell
cd C:\path\to\Visa2026
.\scripts\windows-iis\Publish-Visa2026ForIis.ps1 -Zip -OpenOutputFolder
```

---

## Dev PC — deploy one slot (SSH)

```powershell
# Production (default)
.\scripts\windows-iis\Deploy-Visa2026IisRemote.ps1 -Profile Production

# Staging / demo
.\scripts\windows-iis\Deploy-Visa2026IisRemote.ps1 -Profile Staging -ForceUpdate
.\scripts\windows-iis\Deploy-Visa2026IisRemote.ps1 -Profile Demo -EnableForceXafDbUpdate -ForceUpdate
```

Copy scripts only:

```powershell
scp -r scripts/windows-iis/*.ps1 visa2026-onprem:C:/visa2026-deploy/iis/
scp -r scripts/windows-iis/env/*.example visa2026-onprem:C:/visa2026-deploy/iis/env/
```

---

## Server — greenfield (all slots)

```powershell
cd C:\visa2026-deploy\iis

.\Install-SqlServerExpress.ps1
# OR: .\Configure-SqlExpressSaLogin.ps1

.\Install-Visa2026ServerPrerequisites.ps1

# Create prod.env / staging.env / demo.env from legacy .env.prod if present
.\Install-Visa2026IisSlots.ps1 -SourceEnvFile C:\visa2026\.env.prod

# Copy same publish build into each inetpub folder (or deploy slots separately from dev PC)

.\Run-Visa2026DbUpdateOnServer.ps1 -Profile Production -ForceUpdate
.\Run-Visa2026DbUpdateOnServer.ps1 -Profile Staging -ForceUpdate
.\Run-Visa2026DbUpdateOnServer.ps1 -Profile Demo -ForceUpdate

.\Set-Visa2026IisSlotsAutoStart.ps1
.\Diagnose-Port80.ps1
```

---

## Server — app update (one slot)

```powershell
C:\Windows\System32\inetsrv\appcmd stop apppool Visa2026-Prod
# Copy new publish into C:\inetpub\visa2026-prod (keep appsettings + keys)
C:\visa2026-deploy\iis\Configure-Visa2026Production.ps1 -Profile Production
C:\visa2026-deploy\iis\Set-Visa2026AppPoolEnvironment.ps1 -Profile Production
C:\visa2026-deploy\iis\Run-Visa2026DbUpdateOnServer.ps1 -Profile Production
C:\Windows\System32\inetsrv\appcmd start apppool Visa2026-Prod
```

Replace `Production` / `Visa2026-Prod` / `visa2026-prod` with **Staging** or **Demo** as needed.

---

## Server — restore `.bak` (production)

```powershell
C:\visa2026-deploy\iis\Restore-Visa2026SqlBackup.ps1 -Profile Production -BackupPath C:\visa2026\backups\prod\<file>.bak
C:\visa2026-deploy\iis\Run-Visa2026DbUpdateOnServer.ps1 -Profile Production
```

---

## Health checks

**On server:**

```powershell
sc query MSSQL`$SQLEXPRESS
C:\Windows\System32\inetsrv\appcmd list site
Invoke-WebRequest http://127.0.0.1/LoginPage -UseBasicParsing | Select-Object StatusCode
Invoke-WebRequest http://127.0.0.1:8080/LoginPage -UseBasicParsing | Select-Object StatusCode
Invoke-WebRequest http://127.0.0.1:8081/LoginPage -UseBasicParsing | Select-Object StatusCode
```

**From workstation (`10.100.128.25` example):**

```powershell
curl.exe -s -o NUL -w "%{http_code}" http://10.100.128.25/LoginPage
curl.exe -s -o NUL -w "%{http_code}" http://10.100.128.25:8080/LoginPage
curl.exe -s -o NUL -w "%{http_code}" http://10.100.128.25:8081/LoginPage
```

---

## Migration from legacy single site

Old layout: site **`Visa2026`**, path **`C:\inetpub\visa2026`**, env **`C:\visa2026\.env.prod`**.

Suggested cutover on `10.100.128.25`:

1. `Install-Visa2026IisSlots.ps1 -SourceEnvFile C:\visa2026\.env.prod`
2. **Prod:** restore/copy prod data → `Visa2026DbProd`; publish → `visa2026-prod`; smoke `:80`
3. **Demo:** if current single site used demo DB, copy publish to `visa2026-demo`; smoke `:8081`
4. **Staging:** greenfield `Visa2026DbStaging`; smoke `:8080`
5. Stop legacy site: `appcmd stop site Visa2026`
6. `Set-Visa2026IisSlotsAutoStart.ps1`

Scripts accept **`-Profile Legacy`** for the old paths during transition.

---

## FORCE_XAF_DB_UPDATE (per slot)

```powershell
# Enable on app pool (deploy script: -EnableForceXafDbUpdate)
# Remove after successful update:
C:\visa2026-deploy\iis\Remove-Visa2026ForceXafDbUpdate.ps1 -Profile Demo
```

---

## Legacy Docker/WSL on same host

```powershell
wsl -d Ubuntu -u root -- docker stop visa2026-prod-app-1 visa2026-prod-sqlserver-1
netsh interface portproxy delete v4tov4 listenaddress=0.0.0.0 listenport=80
```
