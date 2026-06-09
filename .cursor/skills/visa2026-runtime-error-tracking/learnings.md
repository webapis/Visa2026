# visa2026-runtime-error-tracking — learnings

Append-only. Record **verified** incident resolutions (symptom → root cause → fix).

## Entries

### 2026-06-09 — Event Viewer manual-only (on-prem IIS)

- **Decision:** Windows Application log stays **manual** triage (`Get-Visa2026RecentIisErrors.ps1`); no auto-pull to Cursor inbox.
- **Why:** Noisy shared log; `JSDisconnectedException` from `XafErrorBoundaryComponent` (Event ID 1000) is handled circuit teardown, not a prod defect; `ApplicationRuntimeLog` + `Pull-Visa2026RuntimeErrorsRemote.ps1` is the Agent heartbeat.
- **When to open Event Viewer:** 500.30, app pool crash, deploy startup before SQL logging runs.

<!-- Example:
### 2026-06-08 — PDF-BATCH-001 on prod
- **Symptom:** Document copies toast error; logs `PDF batch failed BatchId=...`
- **Cause:** Template path missing in container
- **Fix:** Verify embedded resource + PdfSettings:TemplatePath
- **Env:** visa2026-prod droplet
-->
