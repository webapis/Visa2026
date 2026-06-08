# Visa2026.Blazor.Server — runtime logging and errors

Reference for what `Visa2026.Blazor.Server` emits at runtime (stdout / IIS logs), what is **not** logged, and how to triage production issues.

Related:

- [ENVIRONMENTS.md](./ENVIRONMENTS.md) — Docker compose, `ASPNETCORE_ENVIRONMENT`, `FORCE_XAF_DB_UPDATE`
- [DEBUGGING_DOCKER_DEPLOYMENTS.md](./DEBUGGING_DOCKER_DEPLOYMENTS.md) — Droplet vs local, log pipelines, incident checklist
- [RUNTIME_ERROR_TRACKING_PLAN.md](./RUNTIME_ERROR_TRACKING_PLAN.md) — planned central DB store + real-time hook
- [`.cursor/skills/visa2026-runtime-error-tracking/`](../.cursor/skills/visa2026-runtime-error-tracking/SKILL.md) — error catalog, severity, triage skill

---

## Solution roles (quick context)

| Project | Role |
|---------|------|
| **Visa2026.Blazor.Server** | Runnable host (XAF Blazor UI, Web API). Logs go here in production. |
| **Visa2026.Module** | Domain logic; many services are registered in the Blazor host and log through injected `ILogger<T>`. |

---

## How logging is wired

- **Framework:** `Host.CreateDefaultBuilder` in `Visa2026.Blazor.Server/Program.cs` — standard ASP.NET Core logging (console provider in Docker).
- **No** Serilog, NLog, or HTTP request logging middleware (`UseHttpLogging` is not configured).
- **API:** `Microsoft.Extensions.Logging.ILogger<T>` injected into services and controllers.

### Configuration files

| File | Used when |
|------|-----------|
| `appsettings.json` | Base levels for all environments |
| `appsettings.Development.json` | Development only (more verbose; `DetailedErrors`) |
| *(no `appsettings.Production.json`)* | Production uses base + env overrides |

Base levels (`appsettings.json`):

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.AspNetCore": "Warning"
  }
}
```

Docker production overrides (`docker-compose.prod.yml` / `docker-compose.yml`):

| Variable | Level | Effect |
|----------|-------|--------|
| `Logging__LogLevel__Default` | Information | App code (`Visa2026.*`) Information and above |
| `Logging__LogLevel__Microsoft` | Warning | Suppresses most framework noise; **Error still passes** |
| `Logging__LogLevel__DevExpress` | Information | Some XAF / DevExpress messages |

Development adds `DevExpress.ExpressApp: Information` and per-controller Debug (e.g. `ApplicationTypeSelectionController`).

### Where logs go

| Deploy | Output |
|--------|--------|
| **Visual Studio (F5)** | **Output** window → Visa2026.Blazor.Server; `DetailedErrors` in Development |
| **IIS on-prem prod** | Optional stdout: `C:\inetpub\visa2026\logs\stdout_*.log` — `scripts/windows-iis/Enable-Visa2026StdoutLog.ps1` |
| **Docker** | Container stdout → `json-file` driver (10 MB × 3 files). `docker compose logs app` |

**Primary runtime error tracking scope:** VS local + IIS prod — [`.cursor/skills/visa2026-runtime-error-tracking/environments.md`](../.cursor/skills/visa2026-runtime-error-tracking/environments.md).

---

## What is `LogError`?

`LogError` is a method on `ILogger<T>` (`Microsoft.Extensions.Logging`). It writes at **Error** severity.

```csharp
logger.LogError(ex, "PDF batch failed. BatchId={BatchId}", batchId);
```

- **Optional exception** — message plus stack trace when `ex` is passed.
- **Explicit** — only runs where code calls it; not automatic for every user-visible error.
- **Included in production** — `Default=Information` and `Microsoft=Warning` both allow Error-level lines.

Other common levels: `LogDebug`, `LogInformation`, `LogWarning`, `LogCritical`.

---

## Exception handling (production vs development)

In `Startup.cs`:

- **Development:** `UseDeveloperExceptionPage()` — detailed error page in browser.
- **Production:** `UseExceptionHandler("/Error")` + `UseHsts()`.

**Unhandled HTTP exceptions** are logged by ASP.NET Core’s exception handler middleware at **Error** before redirecting to `/Error`. There is no dedicated `Error.cshtml` in the repo; logging still happens even if the error page is generic.

**Blazor circuit failures** (unhandled exceptions in a live session) are logged under `Microsoft.AspNetCore.Components.Server.Circuits` at Error level.

---

## What is logged at runtime

### 1. Startup and infrastructure

| Kind | Example | Logged? |
|------|---------|---------|
| SQL connection / schema | DB unreachable, login failed, missing table | Often yes — unhandled or XAF `CheckCompatibility` failure |
| DB update timeout | SQL timeout during schema update | Retried in `BlazorApplication` (kills stale MSBuild); may not log if retry succeeds |
| Missing config | No connection string; no `Authentication:Jwt:IssuerSigningKey` | Startup crash (`ArgumentNullException` / `InvalidOperationException`) |
| `FORCE_XAF_DB_UPDATE` | Full ModuleUpdater run | `Console.WriteLine` info line in `docker logs` |
| Batch schema gate | Batch tables/columns not ready | `LogWarning` — retries until XAF DB update finishes |
| User report template seed | Seed failure | `LogError` in `UserReportTemplateSeedGate` |
| Lookup / org updaters (Module) | Catalog sync failure at startup | `Tracing.Tracer.LogError` (when DevExpress logging is Information) |

Key files: `Startup.cs`, `BlazorApplication.cs`, `BatchWorkerSchemaGate.cs`, `UserReportTemplateSeedGate.cs`.

### 2. Background workers (most common explicit app logs)

Registered in `Startup.cs`:

| Service | LogError examples | LogWarning examples |
|---------|-------------------|---------------------|
| `PdfGenerationBatchWorkerService` | Loop crash; PDF batch failed (`BatchId=…`) | Batch tables not ready; all attachment flags false |
| `WordReportGenerationBatchWorkerService` | Loop crash; Resminamalar batch failed | Batch tables not ready |
| `TempFileCleanupService` | Cannot delete temp files | — |

Batch failures also set `ErrorMessage` on the batch row (UI toast / batch API) in addition to server logs.

### 3. PDF / ZIP / document pipelines

Triggered from UI; executed via Module services registered in the host:

| Service | Typical messages |
|---------|------------------|
| `PdfFormFillerService` | Template missing, no form, field/image failures — `LogError` / `LogWarning` |
| `PdfMappingHelper` | Missing mapping, dynamic mapping exception |
| `ApplicationSupportingDocumentsPacker` | Missing attachment, merge skipped, ZIP write failed — mostly `LogWarning` |
| `ApplicationItemDocumentCopyPdfMerger` | Merge/rasterize issues — `LogWarning` |

### 4. Module controllers (selective)

| Location | Logged? |
|----------|---------|
| `ShowMailMergeController` | `LogWarning` / `LogError` for mail-merge visibility |
| `UserReportTemplateController` | **No** — `ShowMessage` in UI only on extract/validate failure |

### 5. Framework unhandled crashes

Anything not caught in application code:

- Middleware / controller / API unhandled exceptions
- EF Core / `SqlException` on unhandled DB calls
- Blazor component or XAF view exceptions in an active circuit

Look for `fail:` lines and `Microsoft.AspNetCore.*` categories in `docker compose logs`.

---

## What is usually **not** logged

| Kind | Behavior |
|------|----------|
| XAF **validation** failures | Shown in UI dialog |
| **`UserFriendlyException`** | User message only (e.g. missing PDF template on Generate PDF) |
| **Save/delete** rule violations | XAF message box |
| **Blazor component** errors | Inline `_error` / status text (document copies, report package, preview) |
| **Failed login** | `401 Unauthorized` from JWT API — no explicit `LogError` |
| **User feedback** validation | `400 BadRequest` from `UserFeedbackController` — caught, not logged |
| **Swallowed exceptions** | e.g. `UserCultureController` after logon; `JSDisconnectedException` in E2E hooks |

**Practical rule:** users can see an error in the browser that never appears in stdout. Check the screen, batch `ErrorMessage` in DB/API, or Sentry (if added later) — not only `docker logs`.

---

## Runtime error categories (summary)

### LogWarning — degraded but continuing

- Batch schema not ready yet (retry)
- PDF field skipped, attachment missing, merge skipped
- ZIP packer partial failure

### LogError — failure needing attention

- Background worker loop exception
- Whole PDF or Word batch failed
- PDF form fill catastrophic failure
- Temp file cleanup failure
- Template seed failure
- Unhandled framework exception

### UI-only — common at runtime, quiet in logs

- Validation, friendly exceptions, enqueue/preview failures in components

---

## Useful grep patterns

### Visual Studio (local)

Use the **Output** window filter; or run from repo root with the same profile as F5:

```powershell
dotnet run --project Visa2026.Blazor.Server/Visa2026.Blazor.Server.csproj --launch-profile "Visa2026 - LocalDB"
```

### IIS on-prem prod

```powershell
Get-ChildItem C:\inetpub\visa2026\logs -Filter stdout_* | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | Get-Content -Tail 100
```

Or: `Enable-Visa2026StdoutLog.ps1`, `Get-Visa2026IisStartupError.ps1` — see [visa2026-windows-iis-deploy](../.cursor/skills/visa2026-windows-iis-deploy/SKILL.md).

### Docker (optional)

```powershell
docker compose -p visa2026-dev --env-file .env.dev -f docker-compose.dev.yml logs app --since 1h
```

| Pattern | Meaning |
|---------|---------|
| `PdfGenerationBatchWorkerService` | PDF batch worker |
| `WordReportGenerationBatchWorkerService` | Resminamalar / Word batch worker |
| `PDF batch failed` | Failed document-copy / PDF batch |
| `Resminamalar batch failed` | Failed Word report batch |
| `User report template seed failed` | Startup template seed |
| `Batch schema column ensure failed` | Schema gate retry |
| `fail:` | ASP.NET Core unhandled exception |
| `SqlException` | Database error in stack trace |

---

## Gaps and possible improvements

Not implemented today. **Planned in-repo:** [RUNTIME_ERROR_TRACKING_PLAN.md](./RUNTIME_ERROR_TRACKING_PLAN.md) (`ApplicationRuntimeLog` BO + `ILoggerProvider` + SignalR). See also [DEBUGGING_DOCKER_DEPLOYMENTS.md](./DEBUGGING_DOCKER_DEPLOYMENTS.md) §7:

1. Structured logging (JSON) + request correlation ID
2. Central log pipeline (Loki, Seq, Datadog, etc.) or error tracking (Sentry)
3. Dedicated `/Error` Razor page
4. Explicit `LogError` on selected UI-only catch blocks where ops visibility is needed

---

## Key source files

| Area | Path |
|------|------|
| Host entry | `Visa2026.Blazor.Server/Program.cs` |
| Pipeline + workers registration | `Visa2026.Blazor.Server/Startup.cs` |
| PDF batch worker | `Visa2026.Blazor.Server/Services/PdfGenerationBatchWorkerService.cs` |
| Word batch worker | `Visa2026.Blazor.Server/Services/WordReportGenerationBatchWorkerService.cs` |
| Schema / seed gates | `Visa2026.Blazor.Server/Services/BatchWorkerSchemaGate.cs`, `UserReportTemplateSeedGate.cs` |
| PDF fill | `Visa2026.Module/Services/PdfFormFillerService.cs` |
| Docker log levels | `docker-compose.prod.yml`, `docker-compose.yml` |
