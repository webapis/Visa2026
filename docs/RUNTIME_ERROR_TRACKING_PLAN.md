# Runtime error tracking — implementation plan

Status: **Phase 1–5 implemented** (SQL persist, SignalR, error codes, retention, UI reporter, emitters, Sentry, Cursor agent inbox + resolution)  
Last updated: 2026-06-08

**Tracking scope:** **Visual Studio local** (F5 / LocalDB) and **Windows IIS on-prem production** ([visa2026-windows-iis-deploy](../.cursor/skills/visa2026-windows-iis-deploy/SKILL.md)). Docker/droplet optional later.

Canonical narrative for central error collection from `Visa2026.Blazor.Server`. Agent workflow: [`.cursor/skills/visa2026-runtime-error-tracking/SKILL.md`](../.cursor/skills/visa2026-runtime-error-tracking/SKILL.md). Per-environment triage: [environments.md](../.cursor/skills/visa2026-runtime-error-tracking/environments.md).

Related:

- [BLAZOR_SERVER_LOGGING.md](./BLAZOR_SERVER_LOGGING.md) — what is logged today (stdout)
- [DEBUGGING_DOCKER_DEPLOYMENTS.md](./DEBUGGING_DOCKER_DEPLOYMENTS.md) — Docker triage, optional Sentry/Loki
- [STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md](./STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md) — inbox UI pattern (officer alerts; different domain)

---

## 1. Problem

Today, runtime errors are mostly **ephemeral text** (Visual Studio **Output** when running locally; optional IIS **stdout** under `C:\inetpub\visa2026\logs\` on prod) or **UI-only** (XAF validation, Blazor component messages). There is no:

- Central **queryable** store (who, when, where, stack trace)
- **Real-time** ops alert when Error/Critical is persisted
- Stable **error codes** for triage and reporting

`UserFeedback` is **user-reported** bugs — not a substitute for server-side exception capture. XAF **Audit Trail** records data changes — not application faults.

---

## 2. Recommended architecture (use existing stack first)

### 2.1 Primary: custom `ILoggerProvider` → SQL table (Module BO)

**Why not only stdout / VS Output / IIS log files:** ephemeral, easy to miss, no user/correlation context, different paths per environment (VS Output vs `stdout_*.log`), hard for officers to access on IIS prod.

**Why SQL table in the app database:** both in-scope environments already use SQL Server (LocalDB dev, SQLEXPRESS prod) — one query surface for app data **and** errors; SignalR real-time works the same from VS and IIS.

**Why custom provider vs only Serilog:** Visa2026 already uses `ILogger<T>` everywhere; a provider captures **all** existing `LogError` / `LogWarning` calls without rewriting each site. Register in `Visa2026.Blazor.Server/Program.cs` or `Startup.cs`.

**Suggested BO:** `ApplicationRuntimeLog` (Module `BusinessObjects/Operations/`)

| Field | Purpose |
|-------|---------|
| `OccurredAtUtc` | When |
| `Severity` | enum: Critical, Error, Warning (store Warning optionally — config flag) |
| `ErrorCode` | stable code from catalog (e.g. `PDF-BATCH-001`) — parse or explicit at emit sites over time |
| `Category` | logger category (`Visa2026.Blazor.Server.Services.PdfGenerationBatchWorkerService`) |
| `Message` | rendered message |
| `ExceptionType` / `StackTrace` | when exception attached |
| `UserName` | from `IHttpContextAccessor` or XAF `SecuritySystem.CurrentUserName` when available |
| `CorrelationId` | middleware-generated request id |
| `RequestPath` | HTTP path when applicable |
| `MachineName` | `Environment.MachineName` |
| `DeploymentEnvironment` | enum: `LocalVisualStudio`, `IisProduction` (see environments.md) |
| `ApplicationVersion` | `AssemblyInformationalVersion` |
| `RelatedBatchId` | optional Guid when batch worker |
| `SentryEventId` | optional Sentry event id when bridged (Phase 4) |

**Persistence:** EF via non-secured or admin-only `ObjectSpace` in the provider (avoid audit recursion). **Retention:** ModuleUpdater job or scheduled cleanup (e.g. 90 days).

**Minimum level to persist:** `Error` and `Critical` in production; optional `Warning` via `ApplicationRuntimeLog:PersistWarnings=true`.

### 2.2 Real-time hook: SignalR after DB write

Reuse existing Blazor **SignalR** host (`MapBlazorHub` already wired).

```
ILoggerProvider.Log()
  → insert ApplicationRuntimeLog (async queue — do not block log thread)
  → IApplicationRuntimeLogNotifier.NotifyAsync(entry)
  → SignalR hub → admin clients (header badge / toast / Operations inbox)
```

Mirror patterns:

- `StateNotificationHeaderBadge.razor` — badge + navigate to inbox
- `PdfBatchToastHost.razor` / `WordReportBatchToastHost.razor` — toast on completion/failure

**New (host):** `ApplicationRuntimeLogHub` + `ApplicationRuntimeLogNotifier` (singleton). **UI (phase 2):** Operations → **Runtime errors** ListView (XAF) or Blazor inbox component.

### 2.3 Correlation ID middleware (host)

Add early in `Startup.Configure`:

- Generate `X-Correlation-Id` per request (or honor incoming header)
- Push to `ILogger` scope + `HttpContext.Items`
- Copy into every `ApplicationRuntimeLog` row for that request

Enables “when + where + which user action chain” without parsing stack traces.

### 2.4 Optional external tool (parallel, not replacement)

| Tool | Role | Fits Visa2026? |
|------|------|----------------|
| **Sentry** | Exception grouping, release tracking, alerts | High leverage; see [DEBUGGING_DOCKER_DEPLOYMENTS.md](./DEBUGGING_DOCKER_DEPLOYMENTS.md) §7. Complements SQL store. |
| **Serilog.Sinks.MSSqlServer** | Fast SQL sink without XAF BO | Possible shortcut; less integrated with in-app inbox/security model. |
| **Seq / Loki** | Log search UI | Good for devops; does not satisfy “dedicated DB table in app DB” alone. |

**Recommendation:** in-app SQL BO + provider is primary; **Sentry** (Phase 4) adds cross-environment grouping when `Sentry:Enabled` and DSN are set.

---

## 3. What to capture (severity policy)

| Severity | Examples | Persist? | Real-time alert? |
|----------|----------|----------|------------------|
| **Critical** | App fails to start, DB unreachable after boot, worker permanently dead | Yes | Yes (all admins) |
| **Error** | Batch failed, unhandled exception, PDF catastrophic failure, template seed failed | Yes | Yes (configurable) |
| **Warning** | Batch schema retry, ZIP partial skip, missing PDF field | Optional | No (inbox only) |
| **Information** | Batch completed, service starting | No (stdout only) | No |
| **UI-only** | XAF validation, `UserFriendlyException`, component `_error` | **Phase 3:** optional `IApplicationErrorReporter.Report()` at selected catch sites | If persisted |

Full emitter catalog: [`.cursor/skills/visa2026-runtime-error-tracking/reference.md`](../.cursor/skills/visa2026-runtime-error-tracking/reference.md).

---

## 4. Implementation phases

### Phase 1 — Persist Error/Critical (Module + host)

1. `ApplicationRuntimeLog` BO + EF mapping + `ApplicationRuntimeLogUpdater`
2. `ApplicationRuntimeLogWriter` (queued `Channel<LogEntry>` — bounded, drop-oldest on overload)
3. `ApplicationRuntimeLogLoggerProvider` + register in host
4. `CorrelationIdMiddleware`
5. Operations navigation + ListView (admin role)
6. Unit test: provider writes row on `LogError`

### Phase 2 — Real-time notify ✅

1. `IApplicationRuntimeLogNotifier` + `ApplicationRuntimeLogHub` (`/hubs/application-runtime-log`)
2. `SignalRApplicationRuntimeLogNotifier` — broadcasts after successful SQL insert
3. `RuntimeErrorAlertHost.razor` — admin-only header badge + toast; navigates to Operations → Runtime errors
4. Config: `RealtimeNotifyEnabled`, `RealtimeNotifyMinLevel` (appsettings + IIS configure script)

### Phase 3 — Coverage gaps (in progress)

**Phase 3a — Error codes ✅ (partial)**

1. `ApplicationRuntimeLogErrorCodes` + `LogErrorWithCode` / `LogWarningWithCode` extensions
2. Explicit codes on PDF/Word batch workers, schema gate, template seed, temp cleanup, `PdfFormFillerService`
3. Provider prefers structured `{ErrorCode}`; message inference kept as fallback

**Phase 3b — Retention ✅**

1. `IApplicationRuntimeLogRetention` + `EfApplicationRuntimeLogRetention` (batched `ExecuteDelete`)
2. `ApplicationRuntimeLogRetentionBackgroundService` — first run 5 min after startup, then every `RetentionCleanupIntervalHours` (default 24)
3. Config: `RetentionDays`, `RetentionCleanupIntervalHours`, `RetentionBatchSize`; set `RetentionDays` to `0` to disable purge

**Phase 3c — UI-only reporter ✅**

1. `IApplicationErrorReporter` + `ApplicationRuntimeLogErrorReporter` → same queue/background service as `ILoggerProvider`
2. `ReportUiErrors` option (default `true`); `NullApplicationErrorReporter` when runtime logging not wired on host
3. Wired: `ApplicationItemDocumentCopiesComponent`, `ApplicationItemDocumentCopiesPreviewDialog`, `ApplicationReportPackageComponent`, `ApplicationReportPackagePreviewDialog`
4. Codes: `UI-DOC-COPIES-001`, `UI-REPORT-PKG-001`

**Phase 3d — Additional emitters ✅**

1. `ShowMailMergeController` — `MAILMERGE-001` (missing action/cache), `MAILMERGE-002` (criteria eval)
2. `LookupCatalogSyncUpdater` — `INFRA-LOOKUP-SYNC-001` (manifest load, missing required catalog, per-catalog sync failure)
3. `Startup` `CheckCompatibility` catch — `INFRA-DB-002`
4. `ApplicationRuntimeLogStartupCapture` — buffers updater errors before DI; flushed in `Startup.Configure`

**Remaining Phase 3:**

1. Optional email alerts (XAF Notifications module already referenced)

### Phase 4 — Sentry ✅

1. `Sentry.AspNetCore` 5.x in Blazor.Server (`UseVisaSentry` on host builder)
2. Tags: `release`, `environment`, `correlation_id`, `error_code`, `request_path`, `batch_id`
3. `IApplicationRuntimeLogSentryBridge` — persists `SentryEventId` on `ApplicationRuntimeLog` when bridged
4. Config: `Sentry:Enabled`, `Sentry:Dsn`, `BridgeRuntimeLog`, `BridgeWarnings`, `TracesSampleRate` (default off; set DSN to enable)

### Phase 5 — Cursor agent loop ✅

1. **Resolution workflow** — `ApplicationRuntimeLogResolutionStatus`, resolution metadata on BO
2. **`IApplicationRuntimeLogResolution`** + admin ListView actions (Mark in progress / fixed / ignored)
3. **Cursor inbox bridge** — `CursorBridgeEnabled` → `.cursor/runtime-errors/inbox/{id}.json`
4. **`tools/RuntimeLogResolution`** CLI + skill [agent-fix-loop.md](../.cursor/skills/visa2026-runtime-error-tracking/agent-fix-loop.md)
5. **Cursor hooks** — `.cursor/hooks.json`: `sessionStart` (inbox context) + `stop` (auto fix prompt)

---

## 5. Files to add (expected)

| Layer | Path |
|-------|------|
| BO | `Visa2026.Module/BusinessObjects/Operations/ApplicationRuntimeLog.cs` |
| Writer | `Visa2026.Module/Services/RuntimeLogging/ApplicationRuntimeLogWriter.cs` |
| Provider | `Visa2026.Module/Services/RuntimeLogging/ApplicationRuntimeLogLoggerProvider.cs` |
| Updater | `Visa2026.Module/DatabaseUpdate/ApplicationRuntimeLogUpdater.cs` |
| Notifier | `Visa2026.Blazor.Server/Services/SignalRApplicationRuntimeLogNotifier.cs` |
| Hub | `Visa2026.Blazor.Server/Hubs/ApplicationRuntimeLogHub.cs` |
| Middleware | `Visa2026.Blazor.Server/Middleware/CorrelationIdMiddleware.cs` |
| UI | `Visa2026.Blazor.Server/Components/RuntimeErrorAlertHost.razor` |

---

## 6. Configuration (appsettings)

```json
"ApplicationRuntimeLog": {
  "Enabled": true,
  "ReportUiErrors": true,
  "PersistWarnings": false,
  "MinLevel": "Error",
  "QueueCapacity": 1000,
  "RetentionDays": 90,
  "RetentionCleanupIntervalHours": 24,
  "RetentionBatchSize": 500,
  "RealtimeNotifyEnabled": true,
  "RealtimeNotifyMinLevel": "Error",
  "CursorBridgeEnabled": true,
  "CursorBridgeLocalDevOnly": true,
  "CursorBridgeMinLevel": "Error"
}
```

```json
"Sentry": {
  "Enabled": false,
  "Dsn": "",
  "BridgeRuntimeLog": true,
  "BridgeWarnings": false,
  "TracesSampleRate": 0.0
}
```

Docker / IIS: `Sentry__Enabled=true`, `Sentry__Dsn=…` (never commit DSN). Keep `ApplicationRuntimeLog__CursorBridgeEnabled=false` on IIS prod.

---

## 7. Triage workflow (ops)

1. **Identify environment:** VS local → Output window; IIS prod → stdout + [visa2026-windows-iis-deploy](../.cursor/skills/visa2026-windows-iis-deploy/SKILL.md) if site won't start.
2. **Real-time:** admin sees badge/toast → Runtime errors ListView (Phase 2).
3. **Filter:** `DeploymentEnvironment`, `Severity`, `ErrorCode`, `OccurredAtUtc`, `UserName`.
4. **Correlate:** same `CorrelationId` = one user request chain.
5. **Cause:** [reference.md](../.cursor/skills/visa2026-runtime-error-tracking/reference.md) row + [environments.md](../.cursor/skills/visa2026-runtime-error-tracking/environments.md).
6. **Fallback:** VS Output or `Get-ChildItem C:\inetpub\visa2026\logs\stdout_*` + grep patterns in [BLAZOR_SERVER_LOGGING.md](./BLAZOR_SERVER_LOGGING.md).

---

## 8. Out of scope

- Replacing `UserFeedback` (human reports)
- Logging every XAF validation failure (noise; opt-in via reporter only)
- Storing PII beyond username — scrub passwords/tokens in provider
