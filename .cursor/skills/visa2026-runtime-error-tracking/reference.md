# Runtime error catalog — Visa2026.Blazor.Server

Stable codes for triage and `ApplicationRuntimeLog.ErrorCode`.  
**Explicit (Phase 3a):** emitters call `LogErrorWithCode` / `LogWarningWithCode` with `ApplicationRuntimeLogErrorCodes`.  
**Fallback:** provider infers code from message text when `{ErrorCode}` is absent.

**Deployments in scope:** Visual Studio local + IIS on-prem prod — see [environments.md](./environments.md).

Severity: **P0** Critical · **P1** Error · **P2** Warning · **P3** UI-only

---

## Infrastructure and startup

| Code | Sev | Emitter | When | Typical cause | Logged |
|------|-----|---------|------|---------------|--------|
| `INFRA-DB-001` | P0 | XAF `CheckCompatibility` / EF | App startup | SQL down, wrong connection string, firewall | Yes (often unhandled) |
| `INFRA-DB-002` | P1 | `BlazorApplication` DB update / `Startup` | Startup schema update | SQL timeout; updater failure; stale MSBuild lock (retried) | Yes (Phase 3d) |
| `INFRA-CFG-001` | P0 | `Startup` / JWT config | Startup | Missing `DefaultConnection` or `IssuerSigningKey` | Crash |
| `INFRA-BATCH-SCHEMA-001` | P2 | `BatchWorkerSchemaGate` | Before workers run | Batch tables/columns missing; XAF update still running | `LogWarning` |
| `INFRA-TEMPLATE-SEED-001` | P1 | `UserReportTemplateSeedGate` | Startup | Template seed / embed failure | `LogError` |
| `INFRA-LOOKUP-SYNC-001` | P1 | `LookupCatalogSyncUpdater` | Startup updater | Bad/missing catalog JSON | Yes (Phase 3d) |
| `INFRA-FORCE-DB-UPDATE` | P2 | `Startup` build step | Startup when env set | Informational — full updater run | `Console.WriteLine` |

---

## Background workers

| Code | Sev | Emitter | When | Typical cause | Logged |
|------|-----|---------|------|---------------|--------|
| `PDF-WORKER-LOOP-001` | P0 | `PdfGenerationBatchWorkerService` | Worker loop | Unhandled exception outside batch | `LogError` |
| `PDF-BATCH-001` | P1 | `PdfGenerationBatchWorkerService` | Per batch | Template, mapping, ZIP, SQL, disk | `LogError` + `ErrorMessage` on batch |
| `PDF-BATCH-WAIT-001` | P2 | `PdfGenerationBatchWorkerService` | Worker wait | Schema not ready | `LogWarning` |
| `PDF-BATCH-FLAGS-001` | P2 | `PdfGenerationBatchWorkerService` | Per batch | Uninitialized Include* flags | `LogWarning` + auto-fix |
| `WORD-WORKER-LOOP-001` | P0 | `WordReportGenerationBatchWorkerService` | Worker loop | Unhandled exception | `LogError` |
| `WORD-BATCH-001` | P1 | `WordReportGenerationBatchWorkerService` | Per batch | Missing application, report gen failure | `LogError` + `ErrorMessage` |
| `WORD-BATCH-WAIT-001` | P2 | `WordReportGenerationBatchWorkerService` | Worker wait | Schema not ready | `LogWarning` |
| `TEMP-CLEANUP-001` | P1 | `TempFileCleanupService` | Scheduled | File lock, permissions | `LogError` |

---

## Document / PDF pipelines

| Code | Sev | Emitter | When | Typical cause | Logged |
|------|-----|---------|------|---------------|--------|
| `PDF-FILL-001` | P1 | `PdfFormFillerService` | PDF generation | Template not found | `LogError` |
| `PDF-FILL-002` | P1 | `PdfFormFillerService` | PDF generation | No AcroForm fields | `LogError` |
| `PDF-FILL-003` | P1 | `PdfFormFillerService` | Field fill | Bad value / image data | `LogError` |
| `PDF-FILL-004` | P1 | `PdfFormFillerService` | Fill/merge | Unexpected exception | `LogError` |
| `PDF-FILL-005` | P2 | `PdfFormFillerService` | Field/image | Skipped or fallback | `LogWarning` |
| `PDF-MAP-001` | P2 | `PdfMappingHelper` | Mapping | NULL field skipped | `LogWarning` |
| `PDF-MAP-002` | P1 | `PdfMappingHelper` | Dynamic mapping | Exception per key | `LogError` |
| `ZIP-PACK-001` | P2 | `ApplicationSupportingDocumentsPacker` | ZIP build | Missing file, merge skip | `LogWarning` |
| `DOC-MERGE-001` | P2 | `ApplicationItemDocumentCopyPdfMerger` | Copy PDF merge | Rasterize/merge issue | `LogWarning` |
| `PDF-TEMPLATE-001` | P1 | `PdfGenerationBatchWorkerService` | Batch | `PdfSettings:TemplatePath` missing / file not found | throw → `LogError` |

---

## Framework and HTTP

| Code | Sev | Emitter | When | Typical cause | Logged |
|------|-----|---------|------|---------------|--------|
| `HTTP-UNHANDLED-001` | P1 | ASP.NET exception middleware | HTTP request | Unhandled controller/middleware exception | Framework `LogError` |
| `BLAZOR-CIRCUIT-001` | P1 | Blazor Server circuits | User session | Unhandled in component/XAF view | Framework `LogError` |
| `EF-SQL-001` | P1 | EF Core | DB operation | Constraint, timeout, invalid column | Often in stack trace |
| `HTTP-401-001` | P3 | `AuthenticationController` | JWT login | Wrong credentials | **No** — `401` only |
| `HTTP-400-FEEDBACK-001` | P3 | `UserFeedbackController` | Submit | Validation | **No** — `400` only |

---

## Module controllers (selective)

| Code | Sev | Emitter | When | Typical cause | Logged |
|------|-----|---------|------|---------------|--------|
| `MAILMERGE-001` | P2 | `ShowMailMergeController` | Mail merge | Cache/service missing | Yes (Phase 3d) |
| `MAILMERGE-002` | P1 | `ShowMailMergeController` | Visibility criteria | Criteria eval exception | Yes (Phase 3d) |
| `USER-REPORT-001` | P3 | `UserReportTemplateController` | Extract/validate | Template/placeholder issue | **UI only** |

---

## UI-only (not in stdout today)

| Code | Sev | Emitter | When | Typical cause | Logged |
|------|-----|---------|------|---------------|--------|
| `XAF-VALIDATION-001` | P3 | XAF Validation | Save | Rule violation | No |
| `XAF-FRIENDLY-001` | P3 | `UserFriendlyException` | Action | Business rule message | No |
| `UI-DOC-COPIES-001` | P3 | Document copies component + preview dialog | Package/preview | Enqueue or preview failure | Yes (Phase 3c) |
| `UI-REPORT-PKG-001` | P3 | Report package component + preview dialog | Resminamalar package | Enqueue/preview failure | Yes (Phase 3c) |
| `UI-CULTURE-001` | P3 | `UserCultureController` | After logon | Circuit teardown | Swallowed |

**Phase 3c:** `IApplicationErrorReporter` wired for document copies and Resminamalar report package (`ReportUiErrors` in appsettings).

**Phase 3d:** mail merge controller, lookup catalog sync updater, and `CheckCompatibility` startup failures use explicit codes (`ApplicationRuntimeLogStartupCapture` flushes pre-DI rows in `Startup.Configure`).

---

## Swallowed / low signal

| Code | Sev | Emitter | When | Notes |
|------|-----|---------|------|-------|
| `E2E-JS-DISC-001` | P3 | E2E selector controllers | Test hooks | `JSDisconnectedException` ignored |
| `CONSOLE-QUICKCODE-001` | P2 | `ApplicationTypeQuickCodePropertyEditor` | Dev UI | `Console.WriteLine` only |

---

## Cause → where to look

| Symptom | First check | Then |
|---------|-------------|------|
| `PDF batch failed` | `PdfGenerationBatches.ErrorMessage`, batch worker log | Template path, mappings, attachments |
| `Resminamalar batch failed` | `WordReportGenerationBatches.ErrorMessage` | Application exists, report definitions |
| `Batch schema` warnings | XAF DB version, `FORCE_XAF_DB_UPDATE` | `BatchWorkerSchemaGate`, ModuleUpdaters |
| `fail:` + stack | Full stack category | Unhandled path — add handler or reporter |
| User error, empty logs | reference § UI-only | Reproduce; UI component or XAF validation |
| `Invalid column name` | Schema drift | [visa2026-lifecycle-docker](../visa2026-lifecycle-docker/SKILL.md) |

---

## Future `ApplicationRuntimeLog` row shape

Maps 1:1 to [RUNTIME_ERROR_TRACKING_PLAN.md](../../../docs/RUNTIME_ERROR_TRACKING_PLAN.md) §2.1: `ErrorCode`, `Severity`, `Category`, `Message`, `StackTrace`, `UserName`, `CorrelationId`, `RequestPath`, `OccurredAtUtc`, `RelatedBatchId`, `SentryEventId`.

---

## Sentry (Phase 4)

| Setting | Purpose |
|---------|---------|
| `Sentry:Enabled` | Master switch (default `false`) |
| `Sentry:Dsn` | Project DSN — set via env/user-secrets on prod; never commit |
| `Sentry:BridgeRuntimeLog` | Send persisted runtime log rows to Sentry |
| `Sentry:BridgeWarnings` | Include Warning severity rows when bridged |

When enabled, `ApplicationRuntimeLog.SentryEventId` holds the Sentry event id for cross-linking in the dashboard.

---

## Cursor agent loop (Phase 5)

| Piece | Path / command |
|-------|----------------|
| Inbox (dev) | `.cursor/runtime-errors/inbox/{id}.json` |
| Skill loop | [agent-fix-loop.md](./agent-fix-loop.md) |
| Cursor hooks | `.cursor/hooks.json` (`sessionStart`, `stop`) |
| CLI | `dotnet run --project tools/RuntimeLogResolution -- list-open` |
| Mark fixed | `... mark-fixed --id {guid} --notes "..."` |

Resolution statuses: `Open`, `InProgress`, `Fixed`, `Ignored` on `ApplicationRuntimeLog.ResolutionStatus`.
