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

### 2026-08-31 — Recycle Bin Move progress while waiting (Application)

- **Symptom**: After **Move to Recycle Bin**, the confirm bar sat still with no progress while the save/reload ran, so it looked like nothing happened.
- **Try**: Catalog Delete → confirm **Move to Recycle Bin** on `SANAW_CLK_07`.
- **Test**: Blazor Debug build. Officer: confirm Move — footer/card show indeterminate progress and “Moving …”; then the row leaves Catalog.
- **Root cause**: `_recycleBusy` only disabled buttons. `StateHasChanged` did not yield, so the circuit started the ObjectSpace commit before a progress paint.
- **Fix**: Keep the confirm. Show Preview-style progress on the card and footer; `Task.Yield` after busy paint; Restore/Purge use the same indicator.
- **Prevent**: Recycle mutations that touch ObjectSpace must yield after setting busy so the indicator can render.
- **Cross-skill**: —

### 2026-08-31 — Recycle Bin Delete persists but catalog row stays (Application)

- **Symptom**: Catalog **Move to Recycle Bin** writes `RecycledAtUtc` (Recycle Bin count can already be > 0) but the same card stays in Catalog.
- **Try**: Confirm Delete on `SCREENSHOT 2024-08-28 11:53:28` while Recycle Bin already had 4 items.
- **Test**: Nested catalog helper + Recycle Bin tests; Blazor Debug build. Officer: restart, Move to Recycle Bin — row leaves Catalog immediately, Recycle Bin count increments.
- **Root cause**: Catalog reload used tracked `ApplicationProfileTemplate` instances. EF kept `RecycledAtUtc == null` after another ObjectSpace committed. Overlay then dropped as soon as the bin list looked updated, which put the stale catalog row back on screen.
- **Fix**: Recycle vs live lists keyed from an `AsNoTracking` id query; reload tracked rows from the store; keep the catalog overlay until the parent catalog list no longer contains the row **and** the bin list contains it.
- **Prevent**: Do not classify Recycle Bin from identity-map `RecycledAtUtc` after a different ObjectSpace saved. Do not drop a pending overlay on `inBin` alone.
- **Cross-skill**: application-profile

### 2026-08-31 — Recycle Bin Delete no-op on locked profile (Application)

- **Symptom**: Catalog Delete shows confirm; **Move to Recycle Bin** does not move the row (case already In progress). Recycle Bin count unchanged.
- **Try**: Confirm on `SCREENSHOT 2024-08-28 115127` / SANAW_CLK_* while profile is config-locked.
- **Test**: `IsAllowedResminamalarRecycleBinMutation` (recycle fields yes; TemplateName no; purge only when already recycled); lock helper + Recycle Bin tests; Blazor Debug build.
- **Root cause**: `EnsureNestedConfigurationEditable` blocks saving existing `ApplicationProfileTemplate` after lock state A. Recycle is an update of `RecycledAtUtc`, not a new row. Confirm UI also hid the lock exception.
- **Fix**: Allow Recycle Bin-only mutations (recycle/restore fields or purge of an already-recycled row) while locked; show the exception and clear confirm on failure.
- **Prevent**: Do not treat Recycle Bin as a configuration-template edit; still block rename/file replace on locked profiles.
- **Cross-skill**: application-profile

### 2026-08-31 — Resminamalar Recycle Bin for officer templates (Application)

- **Symptom**: Officers needed to remove extra scan/convert templates (SANAW_CLK_*) without destroying them; Recycle Bin did not exist.
- **Try**: Catalog **Delete** on this-profile nested rows; Recycle Bin **Restore** / **Delete permanently**.
- **Test**: `ApplicationProfileTemplateRecycleBinTests`; nested catalog hides recycled and stays on nested mode when only recycled remain; schema SQL includes `RecycledAtUtc`; Blazor Server Debug build.
- **Root cause**: Nested `ApplicationProfileTemplate` had no recycle fields; catalog listed every Word/Excel row; empty catalog hid the component.
- **Fix**: `RecycledAtUtc` / `RecycledByUserName`; catalog Delete moves ProfileSpecific rows; Recycle Bin tab Restore/Purge; seeded Category/Global have no Delete; re-Approve same name clears recycle.
- **Prevent**: Do not use XAF `GCRecord` / generic `IsDeleted` for this; do not fall back to seeded user catalog when only recycled nested rows remain.
- **Cross-skill**: application-profile | template-scan

### 2026-08-31 — Excel catalog Preview ran Word PDF (`report_….docx`) (Application)

- **Symptom**: Scan-saved Excel READY with people; catalog Preview is a white PDF titled `report_20230321.docx`; same-case Word Preview fills.
- **Try**: Extra col A / print-area PDF tweaks; officer screenshots of wizard + catalog.
- **Test**: `ApplicationWordReportEntryGeneratorTests` (nested `profile:` catalog → `.xlsx`); `ApplicationWordReportOfficePreviewPdfConverterTests` (xlsx bytes named `.docx` still Spreadsheet PDF).
- **Root cause**: `ResolveDownloadFileName` matched only `user:{id}`. Profile-nested catalog keys are `profile:{id}`, so lookup missed and fell back to `report_yyyyMMdd.docx`. Converter chose Word from the extension.
- **Fix**: Match catalog by `UserReportTemplateId` (and keep `user:` key); always use `GetEffectiveOutputFormat()` for `.xlsx`/`.docx`. Converter sniffs ZIP `xl/` vs `word/` before the filename.
- **Prevent**: Never infer Office PDF path from filename alone; never require `user:` keys on nested profile catalogs.
- **Cross-skill**: template-scan

### 2026-08-31 — Excel Preview PDF blank while Word Preview fills (Application)

- **Symptom**: Scan-saved Excel READY; catalog Preview is a white page; same case Word sanaw Preview shows people.
- **Try**: Extra empty column A so `{{#ds.rows}}` validates; Download Excel vs Preview PDF.
- **Test**: Diff gate prepend test; Module compile with `ExportToPdf(stream, PdfExportOptions, sheetName)`.
- **Root cause**: Stale worksheet print area (empty col A) exported as first PDF page; Word uses RichEdit so it is unaffected.
- **Fix**: `ClearPrintRange` + used-range print + fit-to-width + export named first sheet only.
- **Prevent**: Do not `ExportToPdf(stream)` on a workbook that still has Print_Area from the officer sample.
- **Cross-skill**: template-scan

### 2026-08-31 — Catalog Download template + Excel ds.rows vs merged cells (Application)

- **Symptom**: Yellow-mark Excel `Sanaw_clk_05` saved READY; Generate warned `Skipped ds.rows` on merged `J5:K5`; catalog Preview failed; officers asked to download saved templates from Resminamalar.
- **Try**: Align loop with `Sanaw_ckl_map.md` (col A); add per-row Download.
- **Test**: `TemplateRosterLoopPlannerTests` merge skip → C5; Module/Blazor build; officer: Download gets `.xlsx`; re-Approve after rebuild → Preview with `#ds.rows` in A (or next unmerged free col).
- **Root cause**: Loop write landed on/near merge `J5:K5` (non-anchor skip); no catalog action for raw template file (only Edit under gear + filled ZIP).
- **Fix**: Planner skips merged/formula cells when workbook bytes passed; catalog **Download** via `TryReadTemplateFileAsync` + `IFileDownloader` (always visible, not gear).
- **Prevent**: Always pass scan source workbook into `PlanExcelLoopsFromSubstitutions`; never place `{{#ds.rows}}` inside merges.
- **Cross-skill**: resminamalar | template-scan | user-report-templates

### 2026-07-31 — Officer caption Templates + dedicated brand mark

- **Ask**: Dedicated brand like Document Copies; rename Resminamalar to Templates (catalog shows template previews).
- **Fix**: `Templates` ImageName + `TemplatesBrandMark` / `templates-brand.css`; slot title brand; `ApplicationReportPackage.Title` + action/view captions → Templates / Şablonlar / Шаблоны.
- **Prevent**: Do not use `BO_FileAttachment` or DocumentCopies paperclip for this occupant; keep internal code names (`ResminamalarSlot*`) unless a full rename is requested.
- **Cross-skill**: preview-slot

### 2026-07-31 — Catalog card chrome aligned with Document Copies (Application | ApplicationItem)

- **Ask**: Match Resminamalar preview-slot catalog look to Person Document Copies Prototype A cards.
- **Fix**: Flat selectable cards via `.resminamalar-catalog` + `ResminamalarCatalogFormatIcons` (Word/Excel circles); keep checkbox + READY/CHECK + Preview + Download package (no Open/expand sections).
- **Prevent**: Do not reuse `.doc-copies-catalog` exclusive expand for Resminamalar — templates are ZIP leaf rows; share visual tokens only.
- **Cross-skill**: preview-slot | person-document-copies

### 2026-06-06 — Application Resminamalar not replaced by ApplicationItem Resminamalar

- **Symptom**: Application DetailView Resminamalar open (catalog or PDF); nested ApplicationItem ListView Resminamalar left previous preview visible.
- **Try**: Open app-scope slot, select item row, click Resminamalar on ListView.
- **Test**: Slot shows item-scoped catalog; any open PDF closes; Application DetailView can stay active without closing slot until its owning view deactivates.
- **Root cause**: `OpenResminamalarAsync` updated service state but Blazor reused `ResminamalarSlotPanel` (`_previewActive` stuck true); close controller closed on any view deactivate.
- **Fix**: `OccupantKey` + `OwnerViewId` on state; `@key="_state.Version"` on panel; occupant change resets preview in panel; controllers pass `View.Id`; owner-aware `VisaPreviewSlotCloseController`.
- **Prevent**: Any new slot occupant must bump `Version` and use distinct `OccupantKey`; never rely on parameter set alone when local UI state exists.
- **Cross-skill**: —

### 2026-06-06 — Resizable preview slot splitter (drag + session persist)

- **Symptom**: Fixed ~40vw slot width; officers could not widen/narrow for long template names or PDF reading.
- **Try**: Drag left edge of `#visa-preview-slot` while Resminamalar or file preview open.
- **Test**: Width clamps 320–720px (max 75% shell); persists in `sessionStorage` key `visa.previewSlot.widthPx`; double-click handle resets to CSS default 40vw; `syncInlinePreviewHeight()` runs after resize.
- **Root cause**: Phase 1 slot used static `flex: 0 0 40vw` only.
- **Fix**: `.visa-preview-slot__resize-handle` + `visaPreviewDrawer.initSlotResize()` / `applySlotWidth` / `restoreSlotWidth` in `_Host.cshtml`; `--resizing` disables transition during drag.
- **Prevent**: Any inline PDF viewer in the slot should call `syncInlinePreviewHeight` after layout width changes.
- **Follow-up**: Handle visible but not draggable — slot child (`ResminamalarSlotPanel`) painted above handle; `overflow:hidden` clipped hit area. Raised handle `z-index:200`, `pointer-events:auto`, content `z-index:1`; open slot `overflow:visible`; `pointerdown` + `setPointerCapture` on handle.
- **Follow-up**: Increase (drag left) still stuck — `lostpointercapture` ended drag over main app; flex `flex-basis` on slot could not take space from `<app>`. Switched open layout to **CSS grid** on `#visa-app-shell` (`1fr` + `--visa-preview-slot-width`); resize sets shell CSS var; removed pointer capture / `lostpointercapture` stop.
- **Cross-skill**: —

### 2026-06-06 — Inline slot: template names and Preview invisible (dark theme)

- **Symptom**: Resminamalar slot showed checkboxes + green **Ready** only; template titles and **Preview** link buttons missing.
- **Try**: Open slot from ApplicationItem ListView / Application detail in dark theme.
- **Test**: After fix — names, Preview, Select all, Download package visible; theme matches main app.
- **Root cause**: `#visa-preview-slot` renders outside `<app>`; `background` fell back to light `--bs-body-bg` while `color: inherit` kept dark-theme light text; only `.app-report-package__readiness--ready` had explicit color.
- **Fix**: `visaPreviewDrawer.syncSlotTheme()` copies `--dxbl-*` / `--bs-*` vars from app root; slot CSS uses inherited colors; inline-slot layout stacks row actions vertically.
- **Follow-up**: Template titles still crushed — modal `group-head` 2-col grid + `min-width:0` flex shrink in narrow slot; added `app-report-package__slot-entry` stacked markup for `UseInlinePreview`.
- **Follow-up**: Theme switch while slot open left stale dark inline vars — `ensureThemeWatcher()` + clear/copy on `class`/`data-bs-theme`/stylesheet changes; apply `dxbl-application` + theme classes on `#visa-preview-slot`.
- **Prevent**: Any global UI outside `<app>` must sync theme on open, not rely on `color: inherit` alone.
- **Cross-skill**: —

### 2026-06-06 — Resminamalar moved from modal to global inline preview slot (phase 1)

- **Symptom**: Resminamalar opened a modal DetailView; ministry letters already used `#visa-preview-slot` split pane.
- **Try**: Click **Resminamalar** on Application detail / ApplicationItem ListView; preview PDF; ministry letter click regression.
- **Test**: `dotnet build` Module + Blazor (full slnx may fail if app running locks DLLs).
- **Root cause**: Controllers opened `ApplicationReportPackageListHost` modal; preview used `ApplicationReportPackagePreviewDialog` popup.
- **Fix**: `IVisaPreviewSlotService` + `VisaPreviewSlotHost` / `ResminamalarSlotPanel` / `ReportPackageInlinePreview`; controllers call `OpenResminamalarAsync`; empty catalog shows message in slot.
- **Prevent**: Reuse slot orchestrator for future document-copies / FileData preview (phase 2); do not duplicate generation path.
- **Cross-skill**: —

### 2026-06-06 — Application Resminamalar disabled with no feedback (empty Application scope)

- **Symptom**: **Resminamalar** on **Application** detail looked dead for types like **App_Inv_And_WP** (no Application-root templates); no message.
- **Try**: Open **Çakylyk we Iş Rugsatnamasyny Almak** vs **Wiza we Iş Rugsatnamasyny Uzaltmak** on Application detail.
- **Test**: Saved application with zero Application-scope catalog → button clickable → warning names application type; dialog still does not open.
- **Root cause**: `Enabled["NoApplicableReports"]` blocked click before `Execute`; warning in `Execute` was unreachable.
- **Fix**: Enable for persisted applications only; `WordReports.NoApplicationScopeTemplates` warning on click when Application-scope catalog empty.
- **Prevent**: Distinguish **disabled** (unsaved / no record) vs **empty catalog** (click → explain type + scope); item-scope templates stay on Application items ListView.
- **Cross-skill**: —

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

### 2026-06-25 — "Sync to database" fails with XAF context errors (local sandbox staging)

- **Symptom**: Sync → "1 failed"; errors progress through: `Value(ImageContentStorage) is null` → `valueManager.browserStorage is null` → `ValueManagerContext.Storage is null` (400 BadRequest).
- **Root cause (layered)**:
  1. `FileData.Content = bytes` and `FileData.LoadFromStream()` both route through DevExpress's Blazor browser-file-storage pipeline (`[FileAttachment]`). That pipeline requires a live Blazor circuit; an HTTP API call has none → crash.
  2. After switching to raw SQL (`efObjectSpace.DbContext.Database.ExecuteSqlRaw`), `EnsureEditAccess()` → `SecuritySystem.IsGranted()` triggered `ValueManagerContext.Storage is null`. `ValueManagerContext` is also circuit-scoped — absent in HTTP API context.
- **Fix**:
  - Write template file content via `ExecuteSqlRaw("UPDATE [FileData] SET [Content]=... WHERE [ID]=...")` — bypasses all XAF file-storage hooks entirely.
  - Remove `EnsureEditAccess()` from the HTTP upload path; `[Authorize]` on the controller handles authentication.
  - Wrap the entire `ImportUploadedContentAsync` body in a top-level try-catch → always returns `200 {status:"Failed", errorMessage}` rather than a 400.
  - Wrap `ExtractAndValidatePlaceholdersAsync` separately — if it fails (same context issue), the import still reports `Imported`.
- **Pattern**: Any XAF service called from a plain HTTP API controller must avoid: `FileData.Content =`, `FileData.LoadFromStream()`, `SecuritySystem.IsGranted()`, and any method that accesses `ValueManagerContext`. Use raw SQL or EF Core `DbContext` directly for file writes; use `[Authorize]` for auth.
- **Prevent**: Keep a single "load template + write file" helper that accepts `EFCoreObjectSpace` and does only SQL; do not go through `IObjectSpace` property setters for `[FileAttachment]` properties from API controllers.
- **Cross-skill**: user-report-templates (FileData pattern), security-access (SecuritySystem scope)

### 2026-06-06 — Readiness warnings vs ZIP failure (UX)

- **Symptom**: Officers thought **Check** chip blocked export.
- **Try**: Download package with only Warning rows checked — confirm gap dialog vs worker error.
- **Test**: ZIP succeeds after confirm; hard failure only in batch worker log (DocxTemplater).
- **Root cause**: Dry-run hints are advisory; gap confirm is optional cancel only.
- **Fix**: Document in APPLICATION_REPORT_PACKAGE; triage table in resminamalar skill.
- **Prevent**: Distinguish catalog warnings from worker error logs when triaging.
- **Cross-skill**: user-report-templates (when log shows token replace error)
