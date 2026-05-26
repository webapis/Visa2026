# visa2026-windows-iis-deploy — reference commands

**Runbook:** [docs/ON_PREM_WINDOWS_IIS.md](../../../docs/ON_PREM_WINDOWS_IIS.md)

**Server layout:**

| Path | Purpose |
|------|---------|
| `C:\inetpub\visa2026\` | IIS publish root |
| `C:\visa2026\.env.prod` | `SA_PASSWORD`, `DEVEXPRESS_LICENSEKEY`, `DB_NAME` |
| `C:\visa2026-deploy\iis\` | Copied scripts from repo |
| `C:\visa2026\backups\` | SQL `.bak` files |
| `C:\ProgramData\Visa2026\DataProtection-Keys\` | Auth cookie keys |

**Connection string server part:** `localhost\SQLEXPRESS` (named instance).

---

## Dev PC — publish

```powershell
cd C:\path\to\Visa2026
.\scripts\windows-iis\Publish-Visa2026ForIis.ps1 -Zip -OpenOutputFolder
```

Copy `dist\visa2026-iis-<version>\` (or zip) to the server.

---

## Dev PC — full remote deploy (SSH)

Requires `~/.ssh/config` host (e.g. `visa2026-onprem`) and scripts on server under `C:\visa2026-deploy\iis\`.

```powershell
.\scripts\windows-iis\Deploy-Visa2026IisRemote.ps1 -SshHost visa2026-onprem
```

---

## Dev PC — copy scripts / backup to server

```powershell
scp -r scripts/windows-iis/*.ps1 visa2026-onprem:C:/visa2026-deploy/iis/
scp C:\path\to\visa2026-prod.bak visa2026-onprem:C:/visa2026/backups/
```

---

## Server — greenfield (Administrator PowerShell)

```powershell
cd C:\visa2026-deploy\iis

# SQL (silent install OR manual install + fix sa)
.\Install-SqlServerExpress.ps1
# OR after manual SQL Express:
.\Configure-SqlExpressSaLogin.ps1

.\Install-Visa2026ServerPrerequisites.ps1
.\Install-Visa2026IisSite.ps1

# After publish files are in C:\inetpub\visa2026:
.\Configure-Visa2026Production.ps1 -PublishPath C:\inetpub\visa2026 -EnvFile C:\visa2026\.env.prod -SqlServer 'localhost\SQLEXPRESS'
.\Set-Visa2026AppPoolEnvironment.ps1
.\Run-Visa2026DbUpdateOnServer.ps1 -ForceUpdate

# Port 80 / IIS start
.\Diagnose-Port80.ps1
# If portproxy shows 0.0.0.0:80 -> WSL IP:
netsh interface portproxy delete v4tov4 listenaddress=0.0.0.0 listenport=80

C:\Windows\System32\inetsrv\appcmd start apppool Visa2026
C:\Windows\System32\inetsrv\appcmd start site Visa2026
```

---

## Server — app update

```powershell
C:\Windows\System32\inetsrv\appcmd stop apppool Visa2026
# Copy new publish into C:\inetpub\visa2026 (preserve appsettings.Production.json + DataProtection-Keys)
C:\visa2026-deploy\iis\Run-Visa2026DbUpdateOnServer.ps1
C:\Windows\System32\inetsrv\appcmd start apppool Visa2026
C:\Windows\System32\inetsrv\appcmd start site Visa2026
```

---

## Server — restore `.bak`

```powershell
C:\visa2026-deploy\iis\Restore-Visa2026SqlBackup.ps1 -BackupPath C:\visa2026\backups\visa2026-prod.bak
C:\visa2026-deploy\iis\Run-Visa2026DbUpdateOnServer.ps1
```

---

## Health checks

**On server:**

```powershell
sc query MSSQL`$SQLEXPRESS
C:\Windows\System32\inetsrv\appcmd list site Visa2026
Invoke-WebRequest http://127.0.0.1/LoginPage -UseBasicParsing | Select-Object StatusCode
```

**From workstation:**

```powershell
curl.exe -s -o NUL -w "%{http_code}" http://10.100.128.25/LoginPage
```

Expect **200** (first load after recycle may be slow).

---

## SQL backup (server)

Use SSMS or `BACKUP DATABASE` with path under `C:\visa2026\backups\`. Example pattern (adjust name/password via `.env.prod` on server only):

```sql
BACKUP DATABASE [Visa2026DbProd]
TO DISK = N'C:\visa2026\backups\Visa2026DbProd-manual.bak'
WITH INIT, COMPRESSION;
```

---

## Legacy Docker/WSL on same host

Stop containers so they do not compete for port **80** or confuse SQL:

```powershell
wsl -d Ubuntu -u root -- docker stop visa2026-prod-app-1 visa2026-prod-sqlserver-1
```

Remove port proxy if IIS cannot bind port 80 (see greenfield above).
