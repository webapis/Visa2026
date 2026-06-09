# Runtime error tracking — deployment environments

**In scope today:** Visual Studio local + **Windows IIS on-prem production** ([visa2026-windows-iis-deploy](../visa2026-windows-iis-deploy/SKILL.md)).

**Out of scope (use other skills):** Docker dev/prod ([visa2026-lifecycle-docker](../visa2026-lifecycle-docker/SKILL.md)), DigitalOcean droplet ([visa2026-droplet-prod-deploy](../visa2026-droplet-prod-deploy/SKILL.md)).

---

## Comparison

| | **Visual Studio (local)** | **IIS production (on-prem)** |
|--|---------------------------|------------------------------|
| **Host** | `Visa2026.Blazor.Server` F5 / `dotnet run` | IIS app pool **Visa2026** → `C:\inetpub\visa2026` |
| **`ASPNETCORE_ENVIRONMENT`** | `Development` | `Production` (app pool env) |
| **Config** | `appsettings.json` + `appsettings.Development.json` + launch profile env | `appsettings.Production.json` from `Configure-Visa2026Production.ps1` |
| **SQL** | Usually **LocalDB** `(localdb)\mssqllocaldb` / `Visa2026` (see launch profiles) | **SQL Express** `localhost\SQLEXPRESS` / `Visa2026DbProd` (or `DB_NAME` in `.env.prod`) |
| **Log sink (today)** | VS **Output** window; console if CLI | IIS **stdout** files under `C:\inetpub\visa2026\logs\` (optional) |
| **Detailed errors** | `DetailedErrors: true` in Development | Generic error page; stack in logs only |
| **Deploy skill** | — | [visa2026-windows-iis-deploy](../visa2026-windows-iis-deploy/SKILL.md) |

Both run the **same** `Visa2026.Blazor.Server` assembly and emit the **same** runtime error catalog ([reference.md](./reference.md)). Differences are **where logs land** and **which database** holds app data (and future `ApplicationRuntimeLog` rows).

---

## Visual Studio local

### Launch profiles (`Properties/launchSettings.json`)

| Profile | SQL | Notes |
|---------|-----|--------|
| **Visa2026 - LocalDB** (typical) | LocalDB `Visa2026` | May set `FORCE_XAF_DB_UPDATE=true` |
| **Visa2026 - Docker SQL** | From `appsettings.Development.json` | Docker dev stack on `127.0.0.1:1433` |
| **IIS Express** | Inherited / user secrets | Less common for full XAF dev |
| **EasyTest / UI Scenarios** | Separate LocalDB databases | Test isolation |

### Where to read logs today

1. **Visual Studio** → **View → Output** → show output from **Visa2026.Blazor.Server** (or **Debug**).
2. Filter for: `fail:`, `Error`, `Warning`, service names (`PdfGenerationBatchWorkerService`, etc.).
3. **Exception Settings** / debugger breaks on thrown exceptions — useful for P0 during dev; not persisted.

### Local triage commands

```powershell
# From repo root — same host as F5 without VS
dotnet run --project Visa2026.Blazor.Server/Visa2026.Blazor.Server.csproj --launch-profile "Visa2026 - LocalDB"
```

SQL (LocalDB): connect SSMS to `(localdb)\mssqllocaldb`, database per profile.

### Planned central store (local)

When `ApplicationRuntimeLog` exists: rows go to the **same connection string as the running profile** (LocalDB `Visa2026` for default F5). Set `DeploymentEnvironment = LocalVisualStudio` on insert.

---

## IIS production (on-prem)

Canonical runbook: [docs/ON_PREM_WINDOWS_IIS.md](../../../docs/ON_PREM_WINDOWS_IIS.md).

### Paths

| Path | Purpose |
|------|---------|
| `C:\inetpub\visa2026` | Published app |
| `C:\inetpub\visa2026\appsettings.Production.json` | Connection string + logging levels (generated) |
| `C:\inetpub\visa2026\logs\stdout_*.log` | ASP.NET Core stdout (when enabled) |
| `C:\ProgramData\Visa2026\DataProtection-Keys` | App pool `ASPNETCORE_DATA_PROTECTION_KEYS` |
| `C:\visa2026\.env.prod` | Secrets (not in repo) |

### Logging levels (IIS prod)

From `Configure-Visa2026Production.ps1`:

- `Default`: Information  
- `Microsoft.AspNetCore`: Warning  
- `DevExpress`: Information  

Same effective policy as Docker prod — `LogError` is captured; most framework noise suppressed.

### Enable and read stdout logs

On the **server** (after publish):

```powershell
C:\visa2026-deploy\iis\Enable-Visa2026StdoutLog.ps1 -PublishPath C:\inetpub\visa2026
```

Or diagnose startup:

```powershell
C:\visa2026-deploy\iis\Get-Visa2026IisStartupError.ps1 -PublishPath C:\inetpub\visa2026
```

Tail latest stdout:

```powershell
Get-ChildItem C:\inetpub\visa2026\logs -Filter stdout_* | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | Get-Content -Tail 80
```

**Remote from dev PC (SSH):** use host alias `visa2026-onprem` per [visa2026-windows-iis-deploy/reference.md](../visa2026-windows-iis-deploy/reference.md).

### Windows Event Viewer (manual only)

**Policy:** do **not** auto-pull Application log into Cursor inbox or `/loop` — too noisy (shared log, DevExpress handled exceptions, `JSDisconnectedException` on tab close).

Use **manually** when the app pool fails before `ApplicationRuntimeLog` can persist (500.30, Hosting Bundle, in-process crash):

```powershell
# On server
C:\visa2026-deploy\iis\Get-Visa2026RecentIisErrors.ps1

# From dev PC
ssh visa2026-onprem "powershell -NoProfile -File C:\visa2026-deploy\iis\Get-Visa2026RecentIisErrors.ps1"
```

Typical low-signal Event ID **1000** (`.NET Runtime`): `XafErrorBoundaryComponent` + `JSDisconnectedException` — user disconnected; ignore unless reproducing a real user-blocking bug.

**Automated heartbeat:** `Pull-Visa2026RuntimeErrorsRemote.ps1` (SQL), not Event Viewer.

### IIS-specific error signals

Cross-link [visa2026-windows-iis-deploy §6 investigation map](../visa2026-windows-iis-deploy/SKILL.md):

| Signal | Often mistaken for app bug | Check |
|--------|---------------------------|--------|
| **502.5 / 500.30** | App runtime error | Hosting Bundle, stdout startup log, SQL not up |
| **Login failed for user 'sa'** | INFRA-DB-001 | `Configure-SqlExpressSaLogin.ps1`, `.env.prod` |
| **Invalid column name** | Schema drift | `Run-Visa2026DbUpdateOnServer.ps1 -ForceUpdate` |
| **HTTP 500 after reboot** | Transient | SQL Express service delay; `Set-Visa2026IisAutoStart.ps1` |

### Planned central store (IIS prod)

`ApplicationRuntimeLog` rows in **`Visa2026DbProd`** (same SQL Express as app) — queryable from XAF on prod or SSMS. `DeploymentEnvironment = IisProduction`, `MachineName` = server hostname.

**Why DB table matters more on IIS than VS:** stdout is optional, not rotated centrally, and officers do not have VS Output. In-app ListView + SignalR badge is the primary ops surface for on-prem prod.

---

## Unified triage (both environments)

1. Classify symptom → [reference.md](./reference.md) **ErrorCode** + severity.
2. **If stdout available:** grep patterns in [BLAZOR_SERVER_LOGGING.md](../../../docs/BLAZOR_SERVER_LOGGING.md).
3. **If batch failure:** `PdfGenerationBatches` / `WordReportGenerationBatches`.`ErrorMessage` in the **environment’s SQL database**.
4. **When implemented:** Operations → **Runtime errors** ListView (filter by `DeploymentEnvironment`, `OccurredAtUtc`).
5. **IIS deploy/recycle issues:** hand off to [visa2026-windows-iis-deploy](../visa2026-windows-iis-deploy/SKILL.md) — not application `LogError`.

---

## Future `ApplicationRuntimeLog.DeploymentEnvironment`

| Value | When |
|-------|------|
| `LocalVisualStudio` | `ASPNETCORE_ENVIRONMENT=Development` and not running under IIS worker |
| `IisProduction` | IIS app pool + `Production` |
| `LocalCli` | `dotnet run` without VS (optional) |
| `Docker` | Reserved — not in current tracking scope |
