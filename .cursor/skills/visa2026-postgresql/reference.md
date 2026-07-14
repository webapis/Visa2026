# visa2026-postgresql — reference commands

**Skill:** [SKILL.md](./SKILL.md) · **Prompts:** [prompts.md](./prompts.md) · **Runbook:** [docs/ON_PREM_WINDOWS_IIS.md](../../../docs/ON_PREM_WINDOWS_IIS.md#dual-ef-providers-sql-server--postgresql)

Env template: [demo.env.example](../../../scripts/windows-iis/env/demo.env.example)

---

## A — Binaries zip (SSH-safe, preferred)

Verified on `10.100.128.25`: EDB GUI/exe often fails over SSH; zip + `initdb` works.

### 1. Download + extract

Use a current **PostgreSQL 16 Windows x64 binaries** zip from EnterpriseDB (or mirror). Example pattern:

```text
https://get.enterprisedb.com/postgresql/postgresql-16.<patch>-windows-x64-binaries.zip
```

On the server (elevated PowerShell):

```powershell
$dl = "C:\visa2026\downloads"
New-Item -ItemType Directory -Force -Path $dl | Out-Null
$zip = Join-Path $dl "postgresql-16-windows-x64-binaries.zip"
# Invoke-WebRequest -Uri "<binaries-zip-url>" -OutFile $zip -UseBasicParsing
$dest = "C:\PostgreSQL\16"
New-Item -ItemType Directory -Force -Path $dest | Out-Null
Expand-Archive -LiteralPath $zip -DestinationPath "$dl\pg16-expand" -Force
# Zip usually contains pgsql\ — copy contents to C:\PostgreSQL\16
Copy-Item -Path "$dl\pg16-expand\pgsql\*" -Destination $dest -Recurse -Force
```

Confirm: `C:\PostgreSQL\16\bin\psql.exe`, `initdb.exe`, `pg_ctl.exe`.

### 2. initdb (let it create the data directory)

```powershell
$env:PATH = "C:\PostgreSQL\16\bin;$env:PATH"
# Do NOT pre-create a non-empty data dir
$pwFile = "C:\visa2026\downloads\pg-pw.txt"
Set-Content -LiteralPath $pwFile -Value "<strong>" -Encoding ascii -NoNewline
& "C:\PostgreSQL\16\bin\initdb.exe" -D "C:\PostgreSQL\16\data" -U postgres -A scram-sha-256 -E UTF8 --pwfile=$pwFile
Remove-Item $pwFile -Force
```

### 3. Register + start Windows service

```powershell
& "C:\PostgreSQL\16\bin\pg_ctl.exe" register -N "postgresql-x64-16" -D "C:\PostgreSQL\16\data" -S auto
Start-Service postgresql-x64-16
Get-Service postgresql-x64-16
```

### 4. Create app database

```powershell
$env:PGPASSWORD = "<strong>"
& "C:\PostgreSQL\16\bin\createdb.exe" -h localhost -p 5432 -U postgres -E UTF8 visa2026_demo
& "C:\PostgreSQL\16\bin\psql.exe" -h localhost -p 5432 -U postgres -d visa2026_demo -c "SELECT version();"
Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
```

### 5. Optional: PATH for admins

```powershell
[Environment]::SetEnvironmentVariable(
  "Path",
  $env:Path + ";C:\PostgreSQL\16\bin",
  "Machine")
```

---

## B — EDB unattended installer (script)

```powershell
# Elevated on Windows Server; demo.env must contain PG_PASSWORD (or SA_PASSWORD fallback)
cd C:\visa2026-deploy\iis
.\Install-PostgreSqlForVisa2026.ps1 -EnvFile C:\visa2026\env\demo.env
```

Defaults in script:

| Param | Default |
|-------|---------|
| InstallerUrl | EDB `postgresql-16.9-1-windows-x64.exe` |
| InstallerPath | `C:\visa2026\downloads\postgresql-16-windows-x64.exe` |
| Service | `postgresql-x64-16` |
| Search `psql` | `C:\Program Files\PostgreSQL\**` and `C:\PostgreSQL\**` |

If the unattended exe fails under SSH → fall back to **§ A**.

---

## C — Wire Demo slot to Postgres

`C:\visa2026\env\demo.env`:

```ini
EFCORE_PROVIDER=Postgres
PG_HOST=localhost
PG_PORT=5432
PG_USER=postgres
PG_PASSWORD=<same as postgres superuser>
DB_NAME=visa2026_demo
HTTPS_ENABLED=false
```

```powershell
cd C:\visa2026-deploy\iis
.\Configure-Visa2026Production.ps1 -Profile Demo
```

Expect Npgsql CS ending with `EFCoreProvider=Postgres`.

Then deploy/update app (IIS skill):

```powershell
# From Dev PC
.\scripts\windows-iis\Deploy-Visa2026IisRemote.ps1 -Profile Demo -ForceUpdate -EnableForceXafDbUpdate
# After LoginPage OK, on server:
.\Remove-Visa2026ForceXafDbUpdate.ps1 -Profile Demo
```

---

## D — Health checks

```powershell
Get-Service postgresql*
Test-NetConnection localhost -Port 5432
$env:PGPASSWORD = "<pw>"
& "C:\PostgreSQL\16\bin\psql.exe" -h localhost -U postgres -d visa2026_demo -c "\conninfo"
& "C:\PostgreSQL\16\bin\psql.exe" -h localhost -U postgres -d visa2026_demo -c "SELECT COUNT(*) FROM information_schema.tables;"
```

Import / watch scripts often hardcode:

```text
C:\PostgreSQL\16\bin\psql.exe
```

---

## E — pg_hba / listen (only if needed)

Default local install usually allows localhost password auth. If remote tools must connect:

1. `postgresql.conf`: `listen_addresses = '*'` (or specific LAN IP) — then restart service.
2. `pg_hba.conf`: host rule for LAN subnet with `scram-sha-256`.
3. Windows Firewall inbound **TCP 5432** (only if required).

Demo IIS app uses **localhost** — skip LAN expose unless asked.