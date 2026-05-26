# Visa2026 on company Windows Server (IIS, no Docker)

Runbook for deploying Visa2026 on **Windows Server** using **IIS**, the **ASP.NET Core Module**, and **SQL Server on Windows** — **no containers**, **no WSL**, **no Docker Engine**.

**Status:** Pilot / optional path. Prefer [ON_PREM_LINUX_SERVER.md](./ON_PREM_LINUX_SERVER.md) (Ubuntu + Docker) when Linux VMs are allowed.

**Agent skill:** [visa2026-windows-iis-deploy](../.cursor/skills/visa2026-windows-iis-deploy/SKILL.md) (deploy, update, restore, triage — read [learnings.md](../.cursor/skills/visa2026-windows-iis-deploy/learnings.md) before work on a host).

**Scripts:** [scripts/windows-iis/README.md](../scripts/windows-iis/README.md)

**Experience log:** [DEPLOYMENT_LIFECYCLE_EXPERIENCE.md](./DEPLOYMENT_LIFECYCLE_EXPERIENCE.md) §8

**Prerequisites (hardware):** [ON_PREM_PREREQUISITES.md](./ON_PREM_PREREQUISITES.md) (RAM/CPU/disk; ignore WSL/Docker rows)

**Not this path:** [legacy/ON_PREM_WINDOWS_SERVER.md](./legacy/ON_PREM_WINDOWS_SERVER.md) (WSL + Docker), [ON_PREM_LINUX_SERVER.md](./ON_PREM_LINUX_SERVER.md) (Ubuntu + Docker).

---

## Architecture

```text
LAN clients  -->  http://<server-ip>  (IIS :80 / :443)
                        |
              IIS + ASP.NET Core Module (ANCM)
                        |
              Kestrel (Visa2026.Blazor.Server.exe)
                        |
              SQL Server on Windows (local or remote)
```

| Component | Technology |
|-----------|------------|
| Web host | IIS site → `C:\inetpub\visa2026` |
| App | .NET 8 **Visa2026.Blazor.Server** (published folder) |
| Database | **SQL Server** 2019+ / Express / Standard (Windows) |
| Reports / PDF | Windows fonts (Times New Roman, etc.) — no Linux font stack |

---

## Compared to Docker on-prem

| | IIS (this doc) | Ubuntu + Docker |
|--|----------------|-----------------|
| CI artifact | **You publish** (`Publish-Visa2026ForIis.ps1`) | Hub image `webapia/visa2026` |
| SQL | Native SQL Server | SQL in Linux container |
| Updates | Replace files + recycle app pool | `docker compose pull` |
| Repo automation | Scripts in `scripts/windows-iis/` | `scripts/linux/`, skills |

---

## Server requirements

| Item | Detail |
|------|--------|
| OS | Windows Server **2019** or **2022** (x64) |
| RAM | **8 GB** minimum, **16 GB** recommended (app + SQL on one box) |
| Disk | **100+ GB** free on system drive |
| IIS | Web Server role; **WebSockets** enabled |
| Runtime | [.NET 8 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0) (includes ANCM) |
| SQL | SQL Server Express or higher; database e.g. `Visa2026DbProd` |
| Network | Inbound **TCP 80** (and **443** if HTTPS); outbound not required for runtime (build machine needs NuGet) |
| Secrets | `DEVEXPRESS_LICENSEKEY`, SQL password, JWT `IssuerSigningKey` — **never commit** |

Optional: [setup-openssh-server](../.cursor/skills/setup-openssh-server/SKILL.md) Win32 scripts under `scripts/legacy/on-prem-windows/` for remote admin.

---

## Files on the server

| Path | Purpose |
|------|---------|
| `C:\inetpub\visa2026\` | IIS site physical path (full publish output) |
| `C:\inetpub\visa2026\appsettings.Production.json` | Production config (create from example; not in git) |
| `C:\ProgramData\Visa2026\DataProtection-Keys\` | Persist auth cookies across app pool recycle |
| `C:\inetpub\visa2026\logs\` | Optional stdout logs (if configured) |

---

## Phase 0 — SQL Server Express (native on Windows)

**Do not** use SQL Server inside Docker/WSL for IIS deploy. Install **SQL Server Express** on the Windows host.

1. Ensure **`C:\visa2026\.env.prod`** exists with **`SA_PASSWORD`** and **`DB_NAME`** (same file as legacy Docker deploy).
2. On the server (Administrator PowerShell):

   ```powershell
   cd C:\visa2026-deploy\iis
   .\Install-SqlServerExpress.ps1
   ```

   Downloads **SQL Server 2022 Express** (if needed), installs instance **`SQLEXPRESS`**, sets **`sa`** password from `.env.prod`, creates **`Visa2026DbProd`**.
3. Optional: stop WSL SQL container so port **1433** is not confused with Docker:

   ```powershell
   wsl -d Ubuntu -u root -- docker stop visa2026-prod-sqlserver-1
   ```

**Migrate data from Docker/WSL:** restore a `.bak` into `localhost\SQLEXPRESS` ([ON_PREM_STABILITY_AND_CUTOVER.md](./ON_PREM_STABILITY_AND_CUTOVER.md)) — on server: `Restore-Visa2026SqlBackup.ps1` (see [scripts/windows-iis/README.md](../scripts/windows-iis/README.md)).

**Manual SQL Express install:** run `Configure-SqlExpressSaLogin.ps1` before the first DB update ([skill learnings](../.cursor/skills/visa2026-windows-iis-deploy/learnings.md)).

**Connection string (app):** `Server=localhost\SQLEXPRESS;Database=Visa2026DbProd;User Id=sa;...`

---

## Phase 1 — IIS and Hosting Bundle

1. Install **.NET 8 Hosting Bundle**; restart IIS: `iisreset`.
2. Enable **WebSockets** (Server Manager → Web Server → Application Development → WebSocket Protocol, or equivalent).
3. Create app pool **`Visa2026`**:
   - **.NET CLR version:** No Managed Code
   - **Identity:** dedicated service account (recommended)
   - **Start mode:** AlwaysRunning (optional)
   - **Idle time-out:** `0` or high value (Blazor Server keeps circuits; idle recycle disconnects users)
4. Create site **`Visa2026`**:
   - Physical path: `C:\inetpub\visa2026`
   - Binding: HTTP port **80** (HTTPS when cert is ready)
   - App pool: **Visa2026**

---

## Phase 2 — Publish (workstation or build agent)

From repo root on a machine with **.NET 8 SDK** and **`DevExpress.Key\DevExpress_License.txt`**:

```powershell
.\scripts\windows-iis\Publish-Visa2026ForIis.ps1 -Zip
```

Output: `dist\visa2026-iis-<AssemblyVersion>\` (and optional `.zip`).

Copy the **entire folder** to the server `C:\inetpub\visa2026` (robocopy, RDP, or `scp` if OpenSSH is enabled).

---

## Phase 3 — Configuration

1. Copy `appsettings.Production.json.example` → `appsettings.Production.json` on the server.
2. Set **connection string**, **JWT** secret, and verify `PdfSettings:TemplatePath` (embedded PDF fallback exists if file missing).
3. Set **app pool** environment variables (IIS → Advanced Settings → Environment Variables, or equivalent):

   | Variable | Value |
   |----------|--------|
   | `ASPNETCORE_ENVIRONMENT` | `Production` |
   | `DEVEXPRESS_LICENSEKEY` | Your DevExpress license key |
   | `ASPNETCORE_DATA_PROTECTION_KEYS` | `C:\ProgramData\Visa2026\DataProtection-Keys` |

   Optional override: `ConnectionStrings__DefaultConnection` (same as Docker compose env name).

4. Create Data Protection folder; grant app pool identity **Modify**:

   ```powershell
   New-Item -ItemType Directory -Force -Path C:\ProgramData\Visa2026\DataProtection-Keys
   icacls C:\ProgramData\Visa2026\DataProtection-Keys /grant "IIS AppPool\Visa2026:(OI)(CI)M"
   ```

5. Grant app pool **Read** on `C:\inetpub\visa2026`.

---

## Phase 4 — Database update (before users)

On the server (elevated PowerShell if needed):

```powershell
$env:DEVEXPRESS_LICENSEKEY = "your-key"
$env:ConnectionStrings__DefaultConnection = "Server=localhost;Database=Visa2026DbProd;User Id=visa2026_app;Password=...;TrustServerCertificate=True;MultipleActiveResultSets=True"

Copy-Item .\scripts\windows-iis\Update-Visa2026Database.ps1 C:\inetpub\visa2026\ -ErrorAction SilentlyContinue
cd C:\inetpub\visa2026
.\Update-Visa2026Database.ps1 -PublishPath C:\inetpub\visa2026 -ForceUpdate -Silent
```

Exit code **0** = success; **2** = already current.

**Alternative:** one-shot `FORCE_XAF_DB_UPDATE=true` in app pool env, start site once, then **remove** (slow on every start — same as Docker; see [ENVIRONMENTS.md](./ENVIRONMENTS.md)).

---

## Phase 5 — Start site and smoke test

1. Start app pool and site.
2. On server: `Invoke-WebRequest http://127.0.0.1/LoginPage -UseBasicParsing | Select-Object StatusCode`
3. From LAN: `http://<server-ip>/LoginPage` — login **Admin** / empty password (**change immediately**).
4. Verify: one read/write workflow, **PDF** generation, one **Word** report (Resminamalar), file upload.

---

## App updates (each release)

1. **Backup** SQL database (`.bak`).
2. Stop site or app pool (or deploy to `visa2026-staging` then swap path).
3. Publish new build on workstation; copy to server **except**:
   - Keep `appsettings.Production.json`
   - Keep `C:\ProgramData\Visa2026\DataProtection-Keys`
4. Run `Update-Visa2026Database.ps1` when release notes require schema change.
5. Start site; repeat smoke tests.

There is **no** `docker compose pull` — version tracking is `publish-version.txt` in the publish folder.

---

## Data import / seed (optional)

Docker uses `db-updater` (`Visa2026.DataImporter` image). On IIS:

- Publish/run **Visa2026.DataImporter** separately with the same connection string, or
- Rely on XAF **ModuleUpdaters** on first start for lookup catalogs (greenfield).

See [Visa2026.DataImporter/IMPORTING.md](../Visa2026.DataImporter/IMPORTING.md).

---

## Troubleshooting

| Symptom | Action |
|---------|--------|
| **`Login failed for user 'sa'`** | Run `Configure-SqlExpressSaLogin.ps1`; confirm `-SqlServer 'localhost\SQLEXPRESS'` in `Configure-Visa2026Production.ps1` |
| **Site won't start / port 80 in use** | `Diagnose-Port80.ps1`; delete stale `netsh interface portproxy` from WSL/Docker ([learnings](../.cursor/skills/visa2026-windows-iis-deploy/learnings.md)) |
| **502.5** / app fails to start | Hosting Bundle installed? `dotnet --list-runtimes` shows **Microsoft.AspNetCore.App 8.x** |
| **500.30** / in-process failure | Check Event Viewer → Windows Logs → Application; run `Visa2026.Blazor.Server.exe` from console in publish folder to see stderr |
| Login works then everyone logged out after recycle | Data Protection keys path missing or not writable |
| Blazor disconnects / circuits drop | Enable **WebSockets**; reduce app pool idle timeout |
| **Invalid column name** | `Update-Visa2026Database.ps1 -ForceUpdate` or one-shot `FORCE_XAF_DB_UPDATE` |
| DevExpress trial / license errors | `DEVEXPRESS_LICENSEKEY` on app pool |
| PDF / report font issues | Rare on Windows; ensure Times New Roman installed |
| Upload too large | `FileUpload:MaxRequestBodyBytes` in appsettings; IIS `maxAllowedContentLength` (default 30MB may need raise) |

**IIS request size** (if large uploads fail), in `web.config` under `<system.webServer>`:

```xml
<security>
  <requestFiltering>
    <requestLimits maxAllowedContentLength="20971520" />
  </requestFiltering>
</security>
```

(`dotnet publish` generates `web.config`; edit on server and keep across publishes if you maintain a custom template.)

---

## Pilot checklist

Use this before calling production-ready:

- [ ] Publish script succeeds on build machine
- [ ] IIS site serves `/LoginPage` on LAN
- [ ] DB update exit code 0 on fresh or restored DB
- [ ] Admin password changed
- [ ] PDF + Word report + upload tested
- [ ] App pool recycle does not log everyone out (Data Protection path verified)
- [ ] Documented connection string, backup, and update procedure for IT

---

## Related docs

- [visa2026-windows-iis-deploy](../.cursor/skills/visa2026-windows-iis-deploy/SKILL.md) — Agent deploy/update skill + [learnings.md](../.cursor/skills/visa2026-windows-iis-deploy/learnings.md)
- [ENVIRONMENTS.md](./ENVIRONMENTS.md) — `FORCE_XAF_DB_UPDATE`, logging levels
- [DEPLOYMENT_LIFECYCLE_EXPERIENCE.md](./DEPLOYMENT_LIFECYCLE_EXPERIENCE.md) — incident log (§8 IIS)
- [ON_PREM_STABILITY_AND_CUTOVER.md](./ON_PREM_STABILITY_AND_CUTOVER.md) — migrate off WSL/Docker
