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
