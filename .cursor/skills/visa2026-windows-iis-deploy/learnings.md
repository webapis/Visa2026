# visa2026-windows-iis-deploy — learnings (append-only)

Read before IIS deploy/update work on a company Windows Server. **Append** verified fixes only; do not edit or delete older entries.

**Promotion:** repeated patterns move into [SKILL.md](./SKILL.md) §6 or [docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md](../../../docs/DEPLOYMENT_LIFECYCLE_EXPERIENCE.md) §8. Template: [on-prem-deploy/MATURITY.md](../on-prem-deploy/MATURITY.md).

---

## Entries

### 2026-05-26 — Manual SQL Express + sa login (ENJ18VWSPVIZE2 / 10.100.128.25)

- **Symptom:** `Run-Visa2026DbUpdateOnServer.ps1` / `--updateDatabase` fails with `Login failed for user 'sa'` (SqlException 18456).
- **Try:** `Configure-Visa2026Production.ps1` with `-SqlServer 'localhost\SQLEXPRESS'` (connection string was correct).
- **Test:** Windows auth connected; `IS_SRVROLEMEMBER('sysadmin')` = 0 for `ENJ18VWSPVIZE2\Administrator`; `sa` existed but `is_disabled=1`; mixed mode was Windows-only until registry `LoginMode=2`.
- **Fix:** [Configure-SqlExpressSaLogin.ps1](../../../scripts/windows-iis/Configure-SqlExpressSaLogin.ps1) — registry mixed mode, single-user `-m` bootstrap to grant sysadmin + enable `sa` with password from `C:\visa2026\.env.prod`. `sqlcmd` was not on PATH; script uses **System.Data.SqlClient**.
- **Prevent:** After **manual** SQL Express setup, always run `Configure-SqlExpressSaLogin.ps1` before first DB update. Prefer `Install-SqlServerExpress.ps1` for silent install with `SECURITYMODE=SQL`.

### 2026-05-26 — Port 80 blocked by WSL portproxy (10.100.128.25)

- **Symptom:** `appcmd start site Visa2026` → `80070020` file/port in use; `netstat` shows PID on port 80 as `svchost` / service **iphlpsvc**.
- **Try:** Stop Default Web Site (already stopped); Docker app container stopped.
- **Test:** `netsh interface portproxy show all` showed `0.0.0.0:80` → WSL IP `172.25.x.x:80` (leftover from Docker/WSL deploy).
- **Fix:** `netsh interface portproxy delete v4tov4 listenaddress=0.0.0.0 listenport=80` then `appcmd start site Visa2026`.
- **Prevent:** After cutover from WSL/Docker to IIS, run [Diagnose-Port80.ps1](../../../scripts/windows-iis/Diagnose-Port80.ps1) and remove stale portproxy rules.

### 2026-05-26 — Restore prod .bak to SQLEXPRESS (10.100.128.25)

- **Symptom:** Greenfield DB update succeeded but operators need **prod users/data** from Docker SQL backup.
- **Try:** Upload `visa2026-prod.bak.*` (~650 MB) via `scp` to `C:\visa2026\backups\`.
- **Test:** [Restore-Visa2026SqlBackup.ps1](../../../scripts/windows-iis/Restore-Visa2026SqlBackup.ps1) with `RESTORE … WITH REPLACE` + `MOVE` to Express default data paths; then `Run-Visa2026DbUpdateOnServer.ps1` (exit 0); `/LoginPage` HTTP 200.
- **Fix:** Stop app pool during restore; post-restore DB update aligns schema with published app (e.g. 1.0.0.239).
- **Prevent:** Take a fresh `.bak` before each IIS app update when data matters.

### 2026-05-26 — HTTP 500.30 after reboot (SQL not ready) (10.100.128.25)

- **Symptom:** `/LoginPage` returns **HTTP Error 500.30** — ASP.NET Core app failed to start. Visa2026 site **Started** (not IIS welcome page).
- **Test:** `MSSQL$SQLEXPRESS` was **Stopped** while **W3SVC** / app pool already **Running**; after SQL started, manual exe run succeeded.
- **Fix:** `Set-Visa2026IisAutoStart.ps1` — start SQL if stopped; `sc config W3SVC depend= …/MSSQL$SQLEXPRESS`; **recycle** app pool after SQL up. **`Register-Visa2026IisBootTask.ps1`** — scheduled task **Visa2026-IisAfterBoot** (startup + 2 min) runs auto-start script again.
- **Prevent:** Run both scripts once on server; on 500.30 after reboot wait 2–3 min or recycle app pool after SQL is Running.

### 2026-05-26 — After reboot, IIS welcome page instead of Visa2026 (10.100.128.25)

- **Symptom:** `http://10.100.128.25/` shows default **IIS Windows Server** welcome page after server restart.
- **Try:** Start Visa2026 site manually.
- **Test:** `appcmd list site` — **Default Web Site** was **Started** on `*:80`; **Visa2026** was **Stopped**.
- **Fix:** [Set-Visa2026IisAutoStart.ps1](../../../scripts/windows-iis/Set-Visa2026IisAutoStart.ps1) — `serverAutoStart:true` on Visa2026, move Default Web Site binding to `127.0.0.1:8080`, stop Default, start Visa2026.
- **Prevent:** Run auto-start script after install; re-run if Default Web Site is re-enabled by Windows updates.

### 2026-05-26 — App pool environment over SSH (10.100.128.25)

- **Symptom:** `Set-Visa2026AppPoolEnvironment.ps1` failed when using IIS PowerShell provider (`IIS:\`) over SSH.
- **Fix:** Script uses **`appcmd.exe set config`** for app pool environment variables (`DEVEXPRESS_LICENSEKEY`, `ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_DATA_PROTECTION_KEYS`).
- **Prevent:** Use appcmd on Server Core / SSH sessions; avoid WebAdministration provider unless interactive IIS manager session.

### 2026-06-01 — VisaExtensionStatus_ListView error on greenfield demo DB (10.100.128.25)

- **Symptom:** Login OK; **Application Error** opening `VisaExtensionStatus_ListView` on **`Visa2026DbDemo`**.
- **Test:** `View_VisaExtensionStatus` missing or invalid; `SqlViewsUpdater` used `) latest_ap AND at.Name IN (...)` instead of **`WHERE`** (SQL parse error). Updater uses `ignoreExceptions: true`, so `CREATE VIEW` failed silently.
- **Fix (code):** `SqlViewsUpdater.cs` — `WHERE` for visa extension / transfer / cancel-ext / cancellation views (match `View_WorkPermitExtensionStatus` pattern).
- **Related:** `-ForceUpdate` failed with **`Invalid column name 'IsDeleted'`** on greenfield — `SoftDeleteColumnsCleanupUpdater` `DELETE` was compile-validated despite `IF COL_LENGTH`; fixed with **`sp_executesql`** for purge. App pool **`FORCE_XAF_DB_UPDATE=true`** (not in `.env.prod`) caused **500.30** on startup — remove with [Remove-Visa2026ForceXafDbUpdate.ps1](../../../scripts/windows-iis/Remove-Visa2026ForceXafDbUpdate.ps1).
- **Deploy:** Republish → copy `C:\inetpub\visa2026` → `Run-Visa2026DbUpdateOnServer.ps1 -ForceUpdate` (exit 0) → `appcmd start apppool Visa2026`. Verify: `sys.views` has `View_VisaExtensionStatus`; `SELECT COUNT(*)` returns 0 on empty demo.
- **Prevent:** Do not leave `FORCE_XAF_DB_UPDATE` on the app pool after greenfield update; use normal DB update on release unless schema drift.

### 2026-06-01 — Multi-slot IIS (prod :80 / staging :8080 / demo :8081) (10.100.128.25)

- **Problem:** Single site `Visa2026` + `Set-Visa2026EnvDbName.ps1` made it easy to point prod URL at demo DB by mistake.
- **Fix:** [Visa2026-IisSlots.ps1](../../../scripts/windows-iis/Visa2026-IisSlots.ps1) + `Install-Visa2026IisSlots.ps1` — three sites/pools/publish folders/env files/DBs on one SQL Express instance.
- **Ports:** Production **80**, Staging **8080**, Demo **8081**. Default Web Site moved to **`127.0.0.1:8090`** (not 8080) via `Set-Visa2026IisSlotsAutoStart.ps1`.
- **Deploy:** `Deploy-Visa2026IisRemote.ps1 -Profile Production|Staging|Demo`; DB update `Run-Visa2026DbUpdateOnServer.ps1 -Profile …`.
- **Migration:** Legacy `C:\inetpub\visa2026` → copy to slot folders; stop old `Visa2026` site; `-Profile Legacy` during transition.
