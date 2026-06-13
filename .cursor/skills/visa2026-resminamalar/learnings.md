# Learnings (append-only): Resminamalar

Purpose: **catalog, seed gate, batch worker, preview, permissions, dialog UX** — not template token design.

**Read before every Resminamalar task:** skim **## Entries** (newest first).  
**Maturity loop & promotion rules:** [MATURITY.md](./MATURITY.md).

**Template merge / placeholders:** [visa2026-user-report-templates/learnings.md](../visa2026-user-report-templates/learnings.md).

**After a verified fix:** append one entry using the template below. **Do not** edit or delete prior entries.

```markdown
### YYYY-MM-DD — <short title> (<Application | ApplicationItem | seed | batch>)

- **Symptom**:
- **Try**:
- **Test**:
- **Root cause**:
- **Fix**:
- **Prevent**:
- **Cross-skill**: resminamalar | user-report-templates | security-access | lifecycle-docker | —
```

---

## Entries

### 2026-06-06 — Sanaw preview failed from ApplicationItem Resminamalar (ItemRows)

- **Symptom**: **Sanaw** / **Sanaw_uzt.docx** — Extract + Validate OK; Resminamalar **Preview** shows “Preview could not be generated”; dry-run falsely warns `RowNo` empty.
- **Try**: Reproduce from **ApplicationItem** ListView → **Resminamalar** → Preview **Sanaw** (ApplicationItem root, `{{#ds.rows}}`).
- **Test**: Preview PDF opens; ZIP contains one **Sanaw** docx with all selected lines; no `RowNo` readiness hint.
- **Root cause**: `UsesPerItemWordOutput` treated every ApplicationItem Word template as one file per person. Sanaw merge then used labor-contract row keys instead of `BuildSanawyRowDictionary`. Dry-run read `RowNo` off `ApplicationItem` (synthetic merge key, not a BO property).
- **Fix**: `UserReportMergeDataHelper.UsesSingleDocumentItemList` → Sanawy lists generate once via `Application` + selected items; `BuildSingleItemRowsForTemplate` for true per-item templates (Contract, Forma 16); dry-run skips `RowNo`/`RowNumber`.
- **Prevent**: ItemRows **list** templates (Sanaw, Sanaw_ckl) vs **per-person** templates (Contract) — preview and ZIP must share `ApplicationWordReportEntryGenerator` only; see user-report-templates for row builders.
- **Cross-skill**: user-report-templates

### 2026-06-06 — Empty User Report Template list after deploy (seed)

- **Symptom**: **Reports → User Report Template** shows “No data”; Resminamalar dialog empty.
- **Try**: Confirm `UserReportTemplate` row count in DB; restart app; check console for seed log.
- **Test**: After fix, console shows `User report template seed completed (N template(s)…)`; ListView populated.
- **Root cause**: `UserReportTemplateUpdater` runs during XAF `CheckCompatibility()` in `Startup.AddBuildStep` **before** `XafApplication.ServiceProvider` exists → seed skipped; DB version still advanced → updater not re-run on next launch.
- **Fix**: `UserReportTemplateSeedGate.EnsureSeeded` in `Startup.Configure` after DI is built; shared `EnsureLinkIndexesAndSeedTemplates` on updater.
- **Prevent**: Any seed logic needing scoped services must run from gate or when `ServiceProvider` is confirmed non-null; log to console on defer/skip.
- **Cross-skill**: —

### 2026-06-06 — Code-backed system reports removed (catalog)

- **Symptom**: N/A (intentional removal).
- **Try**: —
- **Test**: Catalog keys are only `user:{Guid}`; no System/Custom section headers.
- **Root cause**: Ministry outputs moved to **`Resources/Templates/`** user seeds; `IWordReportDefinition` removed.
- **Fix**: Catalog and generator only emit **`user:{Guid}`** keys.
- **Prevent**: Use **`visa2026-resminamalar`** + **`visa2026-user-report-templates`**, not **`visa2026-word-reports`**.
- **Cross-skill**: —

### 2026-06-06 — Extract placeholders security error (Edit template)

- **Symptom**: “Saving UserReportPlaceholder is prohibited by security rules” on Extract.
- **Try**: Reproduce from **User Report Template** detail with Users role.
- **Test**: Extract completes; placeholder grid repopulates.
- **Root cause**: Users role lacked delete on child placeholders; Extract replaces rows.
- **Fix**: Full CRUD on `UserReportPlaceholder` in `Updater.cs`; `UserReportTemplateController` uses non-secured object space after edit check.
- **Prevent**: Maintenance actions that delete/recreate child rows need matching permissions or non-secured OS pattern.
- **Cross-skill**: security-access

### 2026-06-06 — Readiness warnings vs ZIP failure (UX)

- **Symptom**: Officers thought **Check** chip blocked export.
- **Try**: Download package with only Warning rows checked — confirm gap dialog vs worker error.
- **Test**: ZIP succeeds after confirm; hard failure only in batch worker log (DocxTemplater).
- **Root cause**: Dry-run hints are advisory; gap confirm is optional cancel only.
- **Fix**: Document in APPLICATION_REPORT_PACKAGE; triage table in resminamalar skill.
- **Prevent**: Distinguish catalog warnings from worker error logs when triaging.
- **Cross-skill**: user-report-templates (when log shows token replace error)
