# Visa2026.PortalFiling.Worker

Separate **Playwright** Windows worker that will fill `rgstr.migration.gov.tm` from Visa2026 filing jobs.

Prototype / algorithm: [docs/prototypes/playwright-rpa-visa2026-integration-README.md](../docs/prototypes/playwright-rpa-visa2026-integration-README.md).

## Status

**Scaffold only.** Compiles and runs in `ScaffoldOnly=true` (default): loads config, logs intent, exits. No portal form fill and no Visa2026 job API yet.

## Layout

| File | Role |
|------|------|
| `Program.cs` | Generic host |
| `PortalFilingWorkerHostedService.cs` | Poll loop (stub) |
| `Visa2026ApiClient.cs` | JWT + job claim/update (stub) |
| `PortalFormFiller.cs` | Playwright Edge persistent context (stub) |
| `FilingJobModels.cs` | Job DTO + statuses |
| `appsettings.json` | Safe defaults |
| `appsettings.Local.json` | Local secrets (gitignored) — copy from `.example` |
| `samples/ready-job.sample.json` | Example payload shape |
| `install/` | Sample installer script + install README (shipped inside the zip) |

## Run

```powershell
cd Visa2026.PortalFiling.Worker
dotnet build
dotnet run
```

Install Edge browser binaries for Playwright once (filing PC):

```powershell
pwsh bin\Debug\net8.0\playwright.ps1 install msedge
```

Copy `appsettings.Local.json.example` → `appsettings.Local.json` and set Visa2026 URL/credentials locally. Never commit `appsettings.Local.json`.

Env override prefix: `PORTALFILING_` (e.g. `PORTALFILING_PortalFiling__ScaffoldOnly=false`).

## Deploy / sample installer

Ship this project **separately** from `Visa2026.Blazor.Server` (IIS/Docker). The filling PC may be used over **RDP** for attended runs; the worker still runs on that PC.

```powershell
.\scripts\local\Publish-PortalFilingWorker.ps1 -CopyToBlazorDownloads
```

Produces:

- `artifacts/portal-filing-worker/Visa2026.PortalFiling.Worker-win-x64-*.zip`
- optional copy under `Visa2026.Blazor.Server/wwwroot/downloads/portal-filing/` (zips gitignored)

On the filling PC: extract the zip, run `install\Install-PortalFilingWorker.ps1` (see `install\README-INSTALL.md`).

## Next slices

1. Visa2026 portal-filing job BO + OData/API
2. Real `Authenticate` / claim / status updates
3. Portal selectors (main form + Add citizen)
4. Officer Confirm → Save path
5. Admin download UI wrapping `/downloads/portal-filing/`