# visa2026-runtime-error-tracking — learnings

Append-only. Record **verified** incident resolutions (symptom → root cause → fix).

## Entries

### 2026-07-07 — Application save circular FK on staging IIS (10.100.128.25:8080)

- **Symptom:** Generic **Application Error** on Save / Save and New for `Application` (also new-object flow). `ApplicationRuntimeLog` empty; Event Viewer shows EF `InvalidOperationException`.
- **Cause:** Today's denormalized `LatestProgressId` on `Application` + initial `ApplicationProgress` on create form a circular insert graph: `Application` → `ApplicationProgress` → `LatestProgressId` → `Application`. Staging on **1.0.0.538** (commit `e430e4db`).
- **Fix (code):** `ApplicationLatestProgressSyncHelper` — update list scalars always, but skip `LatestProgressId` link while progress row is still `IsNewObject`; `ApplicationProgressRowStateRefreshController` runs a follow-up `CommitChanges()` after first commit to persist the pointer.
- **Deploy:** Republish → `Deploy-Visa2026IisRemote.ps1 -Profile Staging` (no DB schema change beyond columns already present).
- **Verify:** Create new Application type 101 on staging; Save succeeds; `Applications.LatestProgressId` populated after save.

### 2026-06-09 — Event Viewer manual-only (on-prem IIS)

- **Decision:** Windows Application log stays **manual** triage (`Get-Visa2026RecentIisErrors.ps1`); no auto-pull to Cursor inbox.
- **Why:** Noisy shared log; `JSDisconnectedException` from `XafErrorBoundaryComponent` (Event ID 1000) is handled circuit teardown, not a prod defect; `ApplicationRuntimeLog` + `Pull-Visa2026RuntimeErrorsRemote.ps1` is the Agent heartbeat.
- **When to open Event Viewer:** 500.30, app pool crash, deploy startup before SQL logging runs.

### 2026-10-28 — Delete visa on prod (10.100.128.26) throws Application Error

- **Symptom:** Passport detail, nested visa row, Delete → generic **Application Error**. Prod app log: `InvalidOperationException` — association `Visa` / `ApplicationProfileInstanceVisas` severed; join FK is required.
- **Cause:** Skip-nav child FK was `DeleteBehavior.Restrict` (`ON DELETE RESTRICT`). Deleting a visa that is still linked to a case made EF null `VisaId` instead of removing the join row.
- **Fix (code):** Child side of `ConfigureInstanceChildSkipNav` is `ClientCascade` (database FK stays RESTRICT). `SkipNavChildDeleteDetachController` removes the join row and sticky resolved link before delete. Same path covers passport, education, and the other case-linked children.
- **Deploy:** Not on `10.100.128.26` until the next prod image. No schema change.
- **Verify:** Model test asserts `ClientCascade` on the child join FK. Re-check delete on passport `Z4480504` after that image is running.

<!-- Example:
### 2026-06-08 — PDF-BATCH-001 on prod
- **Symptom:** Document copies toast error; logs `PDF batch failed BatchId=...`
- **Cause:** Template path missing in container
- **Fix:** Verify embedded resource + PdfSettings:TemplatePath
- **Env:** visa2026-prod droplet
-->
