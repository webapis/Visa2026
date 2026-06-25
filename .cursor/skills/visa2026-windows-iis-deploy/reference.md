# visa2026-windows-iis-deploy — reference commands

**Copy-paste prompts:** [user-prompts.md](./user-prompts.md)

**Runbook:** [docs/ON_PREM_WINDOWS_IIS.md](../../../docs/ON_PREM_WINDOWS_IIS.md)

**Slot manifest:** [Visa2026-IisSlots.ps1](../../../scripts/windows-iis/Visa2026-IisSlots.ps1)

## Deployment slots (one Windows Server)

Officers use **HTTPS** on every slot. HTTP on the same port may redirect via URL Rewrite (`-RedirectHttpToHttps`).

| Slot | HTTPS smoke URL (LAN) | `HTTPS_PORT` | HTTP (redirect) | Site | App pool | Publish | Env | DB |
|------|----------------------|--------------|-----------------|------|----------|---------|-----|-----|
| **Production** | `https://<server>/LoginPage` | 443 | :80 | `Visa2026-Prod` | `Visa2026-Prod` | `C:\inetpub\visa2026-prod` | `C:\visa2026\env\prod.env` | `Visa2026DbProd` |
| **Staging** | `https://<server>:8080/LoginPage` | 8080 | :8080 | `Visa2026-Staging` | `Visa2026-Staging` | `C:\inetpub\visa2026-staging` | `C:\visa2026\env\staging.env` | `Visa2026DbStaging` |
| **Demo** | `https://<server>:8081/LoginPage` | 8081 | :8081 | `Visa2026-Demo` | `Visa2026-Demo` | `C:\inetpub\visa2026-demo` | `C:\visa2026\env\demo.env` | `Visa2026DbDemo` |

**Per-slot env (required):**

```ini
HTTPS_ENABLED=true
HTTPS_PORT=443    # prod — use 8080 / 8081 for staging / demo
# TEMPLATE_EDIT_STAGING_ENABLED=true   # Resminamalar local sandbox
```

**Shared on server:**

| Path | Purpose |
|------|---------|
| `C:\visa2026-deploy\iis\` | Scripts from repo |
| `C:\visa2026\backups\{prod,staging,demo}\` | SQL `.bak` per slot |
| `C:\ProgramData\Visa2026\DataProtection-Keys-{Prod,Staging,Demo}\` | Auth cookies per slot |
| `C:\ProgramData\Visa2026\TemplateEdit\{prod,staging,demo}\` | Resminamalar template staging (local sandbox; HTTPS required) |
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

# HTTPS bindings (required — run once per slot, then on each release if cert/port changes)
.\Enable-Visa2026IisHttps.ps1 -Profile Production -HttpsPort 443 -IpAddress 10.100.128.25 -RedirectHttpToHttps
.\Enable-Visa2026IisHttps.ps1 -Profile Staging -HttpsPort 8080 -IpAddress 10.100.128.25 -RedirectHttpToHttps
.\Enable-Visa2026IisHttps.ps1 -Profile Demo -HttpsPort 8081 -IpAddress 10.100.128.25 -RedirectHttpToHttps

.\Set-Visa2026IisSlotsAutoStart.ps1
.\Enable-Visa2026IisSlotFirewall.ps1
.\Diagnose-Port80.ps1
```

---

## Server — app update (one slot)

```powershell
C:\Windows\System32\inetsrv\appcmd stop apppool Visa2026-Prod
# Copy new publish into C:\inetpub\visa2026-prod (keep appsettings + keys)
C:\visa2026-deploy\iis\Enable-Visa2026IisHttps.ps1 -Profile Production -HttpsPort 443 -RedirectHttpToHttps
C:\visa2026-deploy\iis\Configure-Visa2026Production.ps1 -Profile Production
C:\visa2026-deploy\iis\Set-Visa2026AppPoolEnvironment.ps1 -Profile Production
C:\visa2026-deploy\iis\Run-Visa2026DbUpdateOnServer.ps1 -Profile Production
C:\Windows\System32\inetsrv\appcmd start apppool Visa2026-Prod
```

Replace `Production` / `Visa2026-Prod` / `visa2026-prod` / `-HttpsPort 443` with **Staging** (`8080`) or **Demo** (`8081`) as needed.

**Officer PCs (Resminamalar Edit template):**

```powershell
.\scripts\windows-iis\Set-Visa2026TemplateEditOfficeTrust.ps1 -ServerHost 10.100.128.25
```

Run once per officer workstation after HTTPS is live; officers must browse **`https://`** URLs.

---

## Server — restore `.bak` (production)

```powershell
C:\visa2026-deploy\iis\Restore-Visa2026SqlBackup.ps1 -Profile Production -BackupPath C:\visa2026\backups\prod\<file>.bak
C:\visa2026-deploy\iis\Run-Visa2026DbUpdateOnServer.ps1 -Profile Production
```

---

## Health checks

**On server (HTTPS — primary):**

```powershell
sc query MSSQL`$SQLEXPRESS
C:\Windows\System32\inetsrv\appcmd list site
# Skip certificate validation for self-signed smoke only:
Invoke-WebRequest https://127.0.0.1/LoginPage -UseBasicParsing -SkipCertificateCheck | Select-Object StatusCode
Invoke-WebRequest https://127.0.0.1:8080/LoginPage -UseBasicParsing -SkipCertificateCheck | Select-Object StatusCode
Invoke-WebRequest https://127.0.0.1:8081/LoginPage -UseBasicParsing -SkipCertificateCheck | Select-Object StatusCode
```

**From workstation (`10.100.128.25` example):**

```powershell
curl.exe -k -s -o NUL -w "%{http_code}" https://10.100.128.25/LoginPage
curl.exe -k -s -o NUL -w "%{http_code}" https://10.100.128.25:8080/LoginPage
curl.exe -k -s -o NUL -w "%{http_code}" https://10.100.128.25:8081/LoginPage
```

(`-k` skips cert verify for self-signed smoke; officers should trust the cert instead.)

---

## Migration from legacy single site

Old layout: site **`Visa2026`**, path **`C:\inetpub\visa2026`**, env **`C:\visa2026\.env.prod`**.

Suggested cutover on `10.100.128.25`:

1. `Install-Visa2026IisSlots.ps1 -SourceEnvFile C:\visa2026\.env.prod`
2. **Prod:** restore/copy prod data → `Visa2026DbProd`; publish → `visa2026-prod`; smoke `https://<server>/LoginPage`
3. **Demo:** if current single site used demo DB, copy publish to `visa2026-demo`; smoke `https://<server>:8081/LoginPage`
4. **Staging:** greenfield `Visa2026DbStaging`; smoke `https://<server>:8080/LoginPage`
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
