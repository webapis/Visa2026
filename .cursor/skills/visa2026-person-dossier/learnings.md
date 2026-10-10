# Person dossier — learnings (append-only)

Newest entries at the **bottom**. Read before dossier work; append after verified fixes.

## 2026-07-30 - Screen | Paper without stealing the preview slot

**Ask:** Preview the director PDF layout without losing Document copies on the right.

**Decision:** Toolbar Screen | Paper. Paper renders `PersonDossierDocumentHtmlBuilder.BuildFragment` inside A4 chrome on the dossier page — not a preview-slot PDF occupant.

**Prevent:** Do not `Open*` a PDF in `#visa-preview-slot` for Paper mode.

## 2026-07-30 - Director export folder keys

**Symptom:** Visas nested under Passports/ in ZIP because copies catalog nests them.

**Fix:** `PersonExportPacker.FolderKeyByRecordType` for director layout; leaf names prefer `RecordLabel` over merger upload filenames.

## 2026-07-31 - Staged loading panel

**Ask:** Progressive feedback while dossier prepares (was plain "Loading dossier...").

**Fix:** `LoadingMessage` + `LoadingProgressPercent` on model; stages in `PersonDossierPropertyEditor.LoadAsync` with `Task.Delay(16)` before resolve; skeleton in `person-dossier.css`. Dashboard hand-off uses indeterminate bar when `_localLoading` (report-dashboard).

**Prevent:** Synchronous resolve before first paint leaves officers on empty chrome.

### 2026-07-31 — Open dossier as ListView row icon (like Document copies)

- **Ask**: Remove Open dossier from Person ListView toolbar; per-row dossier icon instead.
- **Fix**: `Person.DossierListLink` + `PersonDossierListViewColumnUpdater` (index 1); `PersonDossierListLinkClickController` + JS/`PersonDossierNavigationHelper`; `PersonDossierController` DetailView-only. Document copies column shifted to index 2.
- **Prevent**: Do not keep both toolbar and row entry for the same Person ListView action.
- **Cross-skill**: person-dossier | person-document-copies

### 2026-07-31 — Dossier ListView icon missing (CustomizeElement clash)

- **Symptom**: Dossier column showed only a bullet; Copies pill worked (sometimes). Click did nothing useful.
- **Root cause**: `PersonDossierListLinkClickController` and `PersonDocumentCopiesListLinkClickController` each deferred-reapplied `GridModel.CustomizeElement` and reset to their own previous handler, wiping the other column’s CSS class / data attributes.
- **Fix**: Single `PersonListViewActionLinksController` styles both columns; filled SVGs for CSS masks.
- **Prevent**: Never attach two independent CustomizeElement wrappers with deferred re-apply on the same DxGrid ListView.
- **Cross-skill**: person-dossier | person-document-copies

### 2026-07-31 — ListView icon glyphs broken + column order
- **Symptom**: Dossier/Copies pills clickable but icons looked like broken/placeholder images; columns sat after Full Name.
- **Root cause**: Mask SVGs used white fills; luminance/alpha masking turned cutouts into empty/broken-looking glyphs. FullName lacked Index so action columns felt misplaced.
- **Fix**: Monochrome black silhouettes for `person-dossier-mark.svg` / `document-copies-clip.svg`; hide cell children in CSS; column order Dossier(0) → Copies(1) → FullName(2).
- **Prevent**: CSS-mask source SVGs must be single-color opaque shapes (no white “detail” fills).

### 2026-07-31 — ListView column shift (headers vs values)
- **Symptom**: Full names under Copies header; Personal numbers under Full Name; icon + name in one cell.
- **Root cause**: `display: flex` on DxGrid data cells broke table layout; Copies column text (`•`) leaked beside icons.
- **Fix**: table-cell centering + empty link property values; `PersonListViewActionColumnsUpdater` + runtime `VisibleIndex` sync (Dossier 0, Copies 1, FullName 2).
- **Prevent**: Never `display:flex` on `.dxbl-grid` data cells; icon-only columns return `string.Empty`.

### 2026-07-31 — Dossier ListView icon click does not open dossier

- **Symptom**: Dossier icon visible and columns aligned; click did nothing (Copies preview slot still worked).
- **Root cause**: `OpenFromJs` was sync `void` from JS interop (off Blazor sync context); `ShowView` used `MainWindow` instead of the active Person ListView `Frame`.
- **Fix**: `PersonDossierListLinkBridge.OpenFromJs` → `Task` + `InvokeAsync`; `PersonDossierNavigationContext` + `PersonListViewDossierOpenBridge` route opens through `PersonListViewActionLinksController` / ListView `Frame`; fallback `PersonDossierNavigationHelper` reads context frame.
- **Prevent**: ListView row actions that call `ShowViewStrategy.ShowView` from JS must marshal with `InvokeAsync` and use `ShowViewSource(Frame, null)` — not sync void + `MainWindow` only.
- **Cross-skill**: person-dossier | preview-slot

### 2026-07-31 — Dossier opens but shows "No person selected"

- **Symptom**: ListView dossier icon opened `PersonDossierHost_DetailView` tab; body showed `No person selected.`
- **Root cause**: Blazor URL sync recreates non-persistent `PersonDossierHost` without `PersonId`; property editor loaded snapshot with `Guid.Empty`.
- **Fix**: `PersonDossierNavigationContext.PendingPersonIdValue` set on open; `PersonDossierHostViewController` restores `PersonId` on activate; `PersonDossierPropertyEditor` applies pending id and reloads when `CurrentObject` changes.
- **Prevent**: Non-persistent detail hosts opened from ListView need pending-id context + view-id controller — not only `host.PersonId` on the initial `CreateObject`.

### 2026-07-31 — PersonDossierPropertyEditor NRE on OnCurrentObjectChanged

- **Symptom**: `NullReferenceException` at `PersonDossierPropertyEditor.OnCurrentObjectChanged` line 55 (`model.IsLoading`).
- **Root cause**: `PersonDossierHostViewController` sets `View.CurrentObject` before `ComponentModel` is created; `ComponentModel` was null.
- **Fix**: Guard `ComponentModel == null` in `OnCurrentObjectChanged`, `LoadAsync`, and `QueueExport` (same as document-copies list editor).

### 2026-07-31 — ListView dossier empty while DetailView toolbar works

- **Symptom**: Person DetailView **Open dossier** loads full dossier; ListView row icon opens tab with `No person selected.`
- **Root cause**: List path used `ShowViewStrategy.ShowView(..., new ShowViewSource(frame, null))`; DetailView uses `SimpleAction` → `e.ShowViewParameters.CreatedView`. Blazor drops non-persistent `PersonId` on the ShowViewStrategy path. `AsyncLocal` pending id also did not flow from JS interop.
- **Fix**: `PersonListViewActionLinksController` opens via hidden `SimpleAction.DoExecute()` (same as `PersonDossierController`); `IPersonDossierPendingOpen` scoped service + `PersonDossierPendingOpenGate` for backup person id on host restore.
- **Prevent**: Match working DetailView navigation (`ShowViewParameters` from action execute), not raw `ShowViewStrategy` with null action, for non-persistent Blazor detail views.

### 2026-07-31 — DoExecute error 1007 (inactive Hidden)

- **Symptom**: `Unable to execute disabled or inactive action PersonListViewOpenDossier` / inactive reasons: `Hidden`.
- **Root cause**: `Active.SetItemValue("Hidden", false)` deactivates the action — any `false` in `Active` blocks `DoExecute`.
- **Fix**: Create action in ctor with `PredefinedCategory.Unspecified` (off View toolbar), leave `Active` true; never set `Active["Hidden"]=false` to “hide”.

### 2026-07-31 — Keep Person ListView tab when opening dossier

- **Symptom**: ListView dossier icon replaced the Employees (etc.) tab with Person Dossier.
- **Fix**: ListView open uses `TargetWindow.NewWindow` (DetailView toolbar can stay `Current`).

### 2026-07-31 — Screen dossier Word-like A4 page chrome

- **Symptom**: Screen mode stretched full viewport; sparse fields on ultrawide.
- **Try**: Grey desk + centered `210mm` screen sheet (Word print layout).
- **Outcome**: Rejected — horizontal table scroll, felt too paper-like.
- **Fix**: Revert Screen to flexible full-width app layout; keep A4 chrome for Paper only; table cells wrap (`white-space: normal`) so Screen has no horizontal scroll.

### 2026-07-31 — Screen centered column with Word-like side padding

- **Request**: Side gutters like Word workspace, without rigid A4 / table scroll.
- **Fix**: `.person-dossier__screen-stage` + `.person-dossier__screen-column` (`max-width: 1120px`, `padding: clamp(20px, 5vw, 72px)` gutters); Paper stays `210mm`.

### 2026-08-27 — No Start application from dossier

- **Ask**: Remove Person / Dossier Application Profile Instance create. Instances come only from Application Profile Instances lists (via-ministry picker includes Approval legs).
- **Fix**: `PersonDossierStartApplicationController` action stays `Active["Dossier"] = false`. Do not add a new create entry on dossier.
- **Prevent**: Do not re-enable **Start process…** on dossier or Person DetailView.
- **Cross-skill**: person-dossier | application-profile

### 2026-09-23 — Linux Razor `@section.` parse (Docker Hub build)

- **Symptom**: Same RZ2005/RZ1011 on `PersonDossierComponent.razor` `@section.SectionLabel` / `@section.Records.Count`.
- **Fix**: `@(section.SectionLabel)` and `@(section.Records.Count)`.
- **Prevent**: Same as person-document-copies 2026-09-23.
- **Cross-skill**: person-dossier | person-document-copies

### 2026-09-24 — Applications Status shows Seretmezlik

- **Ask**: Person dossier Applications Status was empty for people on a Seretmezlik letter; officers need the same excluded signal as People & links.
- **Fix**: `PersonDossierResolver.BuildApplications` loads `ApplicationProfileInstanceExclusionPerson` for the person (nav fallback on instance `Exclusions`). Status pill = localized `PersonDossier.Status.Excluded` (`Excluded · № {letter} · {dd.MM.yyyy}`, tk uses Seretmezlik) with `st-expiring`. Also added missing `PersonDossier.Column.ApplicationProfile` caption.
- **Officer**: Rebuild Blazor host, hard-refresh, open Francesco’s dossier → Applications → Status shows the excluded pill for Çakylyk Almak.
- **Prevent**: Do not leave Application Status as progress-only when Seretmezlik applies; Seretmezlik wins over LatestProgress for that row.
- **Cross-skill**: person-dossier | application-profile (Seretmezlik)

### 2026-09-24 — Application # opens case workspace

- **Ask**: Dossier Applications Application # should open the Application Profile Instance.
- **Fix**: Screen-mode link on the first Applications cell → `ApplicationWorkspaceOpenHelper.CreateWorkspaceView` in a new tab (`PersonDossierPropertyEditor.OpenApplicationWorkspace`). Paper/export stays plain text.
- **Officer**: Rebuild Blazor host, hard-refresh dossier, click Application # (e.g. 9/-1644).
- **Prevent**: Do not open legacy DetailView for instances — use the case workspace helper. Keep dossier open (NewWindow).
- **Cross-skill**: person-dossier | application-profile

### 2026-09-24 — InvitationItem dossier status (Valid / Used / Cancelled / Expired)

- **Ask**: Invitations Status on person dossier must show one of: Valid to {expiry}, Used (visa issued), Cancelled (cancellation instance), Expired (no visa).
- **Fix**: `PersonDossierResolver.ClassifyInvitationItem` — Cancelled → Used (`IssuedVisa` or used-id batch) → Expired → Valid to {date}. Batch `LoadUsedInvitationItemIds` when resolving the section. CSS: Used/Valid `st-approved`, Cancelled/Expired `st-expiring`.
- **Officer**: Rebuild Blazor host, hard-refresh Andy’s dossier → Invitations: unused future = Valid to …; used = Used; past unused = Expired.
- **Prevent**: Do not leave unused invitation Status empty. Do not treat Used and Expired as stackable — invitation is one state only.
- **Cross-skill**: person-dossier | IssuedDocumentLifecycle

## 2026-09-24 - Invitations Copy column (Preview / No copy + Upload)

- Screen-only **Copy** column on Invitations (PersonDossierSection.HasCopyColumn). Paper HTML is untouched.
- Copy on file (Invitation.Documents with File): **Preview** opens `OpenHeaderDocumentCopiesAsync` (Family=Invitation, `OpenPreviewOnly`) with owner `PersonDossierViewIds.DetailView`.
- No copy: amber **No copy** pill + **Upload**. With a case (Invitation.ApplicationProfileInstance) it opens `IssueIssuedHeaderSlotRequest` in edit mode (`ExistingHeaderId`), reusing the workspace panel's Upload copy. Without a case (legacy) it opens the Invitation DetailView in a new tab.
- Editor watches `IVisaPreviewSlotService.StateChanged`; when the upload occupant leaves the slot it reloads the snapshot so Preview appears. Unsubscribe in `BreakLinksToControl`.

### 2026-10-10 — Applications row uses the instance-list progress track

- **Ask**: Dossier Applications should show the same process track as the Application Profile instance list (1A / 2A / 3A).
- **Fix**: `BuildApplications` calls `ApplicationWorkspaceListProgressSteps.ApplyTo`. Screen renders `lv-progress-stepper`. Paper and the director PDF draw the same dots with print-safe tables (`PersonDossierDocumentHtmlBuilder`). The state pill is gone. Seretmezlik stays in Status only when this person is excluded, beside the track.
- **Verified**: `PersonDossierProgressTrackTests` 3 passed; Blazor host Debug build 0 errors. Live dossier was not opened in the browser in this session.
- **Prevent**: Do not put `LatestProgress.State.NameTm` back in the Applications status pill. Do not drop the stepper when Seretmezlik applies. Do not use flex CSS in the PDF fragment — RichEdit drops it.
- **Cross-skill**: person-dossier | application-profile

### 2026-10-10 — Dossier progress steps show workspace facts

- **Ask**: Each Arzalar step should show the same read-only facts as the case workspace progress track.
- **Fix**: `LoadTimelines` / `BuildTimeline` feed `ApplicationWorkspaceCaseProgressStep` into the dossier. Under each dot: state badge, date, result number, and ministry letter. Screen **View letter** uses `ProgressMinistryLetterPreviewLink` with owner `PersonDossierHost_DetailView`. Paper and the director PDF print the file name, or Missing. No advance, revert, upload, or SLA.
- **Verified**: `PersonDossierProgressTrackTests` 3 passed. Live dossier was not opened in the browser in this session.
- **Prevent**: Do not open the letter from a current step (workspace shows the letter only after the step is done). Do not add upload on the dossier. Keep `OwnerViewId` as the dossier view so the copies slot stays open.
- **Cross-skill**: person-dossier | application-profile | preview-slot

### 2026-10-10 — Office preparation file on the dossier track

- **Ask**: A finished Office preparation step should link the uploaded file, and show Missing when that file is absent.
- **Fix**: `ToDossierProgressSteps` marks a done office step with no file as missing. Screen uses **View file** (`PersonDossier.Progress.ViewFile`) for that step and **View letter** for ministry steps. The current step shows neither. Paper and the director PDF print the file name or Missing.
- **Verified**: `PersonDossierProgressTrackTests` 5 passed. Live dossier was not opened in the browser in this session.
- **Prevent**: Do not show the office file or Missing while Office preparation is still the current step. Do not add an upload box on the dossier.

### 2026-10-10 — Invitation and rejection move onto the application row

- **Ask**: Drop the separate Invitations section. After Migration service finishes, show this person's invitation or rejection in its own column. Put the application date under the application number.
- **Fix**: Applications columns are number (date underneath), profile, progress, then Issued. `ResolveIssuedOutcome` fills that column only when the migration step is no longer current or pending. A rejected migration shows the rejection number instead of the invitation. The number opens the issued copy when a file is stored. Rejections already shown on an application leave the Rejections section.
- **Verified**: `PersonDossierProgressTrackTests` 8 passed. Live dossier was not opened in the browser in this session.
- **Prevent**: Do not list every person on the application. Do not show the Issued column value while Migration service is still the current step.

### 2026-10-10 — Dossier links stay visible on Fluent Dark

- **Ask**: Preview and application links were blue and unreadable on the dark dossier.
- **Cause**: `.person-dossier__app-link` used `#1d4ed8`, and hover mixed that blue toward black.
- **Fix**: The link color is the theme accent (`--DS-color-content-primary-default-rest`), with a light blue fallback on Fluent Dark. Hover mixes toward the dossier text color.
- **Officer**: Hard-refresh. Open a person dossier. Preview, Upload, and the application name should be readable. Check light once.
- **Prevent**: Do not put `#1d4ed8` back on `.person-dossier__app-link`. Do not mix the link color toward black.

### 2026-10-10 — Application number, profile, and date share one column

- **Ask**: Remove the separate application-number column and give that width to the remaining application column. Leave the Applications section where it is.
- **Fix**: One cell holds the application number (workspace link), the profile name, and the date. Sort order stays 100, after family members.
- **Verified**: Officer confirmed the live dossier.
- **Prevent**: Do not yield Applications before passports. Do not put the date back in its own column.

### 2026-10-10 — Screen sections show five rows until Show more

- **Ask**: When a person has more than five visas, work permits, passports, applications, or other dossier rows, show only the five newest until the officer asks for the rest, and say how many remain.
- **Fix**: Screen keeps the first five rows. **Show more** opens the rest and shows the hidden count. **Show less** returns that section to five. Applications apply the limit inside the selected group. Paper and the director PDF still list every row.
- **Verified**: Officer confirmed the live dossier. Blazor host Debug build had 0 errors before the commit; the commit itself was not rebuilt.
- **Prevent**: Do not cap Paper or the director PDF. Do not reveal another five on each click.