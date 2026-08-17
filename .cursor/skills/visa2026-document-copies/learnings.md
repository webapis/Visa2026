# Learnings (append-only): Document copies

Purpose: **dialog UX, scan preview, package enqueue, readiness, toast** — not XFA field mapping.

**Read before every task:** skim **## Entries** (newest first).  
**Maturity:** [MATURITY.md](./MATURITY.md).

**PDF field empty/wrong:** [visa2026-pdf-form-mapping/learnings.md](../visa2026-pdf-form-mapping/learnings.md).

**After a verified fix:** append one entry. **Do not** edit or delete prior entries.

```markdown
### YYYY-MM-DD — <short title> (preview | package | readiness | UX)

- **Symptom**:
- **Try**:
- **Test**:
- **Root cause**:
- **Fix**:
- **Prevent**:
- **Cross-skill**: document-copies | pdf-form-mapping | lifecycle-docker | resminamalar | —
```

---

## Entries

### 2026-08-17 — Application form Preview FOTO overlay missed the box (preview)

- **Symptom**: Download opened in Foxit showed the person photo in FOTO; `#visa-preview-slot` showed a blank white rectangle. Same filled XFA (e.g. Serdar Nuri Küçükkaya / B/-009).
- **Try**: Overlay already passed `Person.Photo` as a data URI. `findFotoHost` required a leaf whose `textContent` was exactly `FOTO`, then a parent ≥48×48 px. pdf.js XFA often draws that caption as SVG/grouped text, so the overlay never attached. Foxit paints `ImageField1`; pdf.js does not.
- **Test**: Stop F5, rebuild, F5, Ctrl+F5 (`?v=xfaphoto2`). Document copies → Application form Preview: photo sits in the FOTO box; header Download still filled XFA for Foxit.
- **Root cause**: Browser preview cannot rely on pdf.js to paint XFA images, nor on finding the FOTO caption in the HTML tree.
- **Fix**: Pin `.visa-xfa-preview__photo` to template coordinates on `.visa-xfa-preview__page` (ImageField1 on A4, caption 5mm excluded). Cache-bust `visa-xfa-preview.js`.
- **Prevent**: Do not search for `FOTO` text. Cache-bust after overlay/CSS edits. Leave ÇAP ET / QR to Foxit.
- **Cross-skill**: visa2026-pdf-form-mapping | visa2026-preview-slot

### 2026-08-17 — Application form Preview empty Person photo (preview)

- **Symptom**: Şahsy kagyzy preview showed name/passport filled but the FOTO box empty.
- **Try**: ImageField1 maps to Person.Photo and Spire assigns XfaImageField + XmlDatasets/XmlTemplate image XML in memory. SaveToFile drops those mutations (no XFAImages, datasets ImageField1 stays empty). pdf.js only paints `<value><image>` content.
- **Test**: `PdfFormFillerImageFieldTests` (mapping + data URI). Stop F5, rebuild, F5, Ctrl+F5. On **B/-009** Document copies → Application form Preview — FOTO shows the person photo.
- **Root cause**: Spire 12.x does not persist XFA image XML; pdf.js cannot invent the photo from an empty ImageField1.
- **Fix**: Overlay `Person.Photo` as a data URI on the FOTO widget in `visa-xfa-preview.js`.
- **Prevent**: Do not expect SaveToFile to keep XFA image datasets. Cache-bust `visa-xfa-preview.js` after overlay edits.
- **Cross-skill**: visa2026-pdf-form-mapping | visa2026-preview-slot

### 2026-08-17 — Application form browser preview via pdf.js XFA (preview)

- **Symptom**: Chrome/Edge iframe showed the XFA “Please wait / Adobe Reader” sheet (then a red preview error; then a black page with white labels). Download in Foxit/Adobe was the real Şahsy kagyzy.
- **Try**: Spire ToPdfA / SaveAsImage still snapshots the placeholder. Iframe raw XFA cannot work. pdf.js `enableXfa` + `XfaLayer.render({ xfaHtml })` (not `xfa`). Pass blob URLs, not `byte[][]`. Serve `.mjs` as `text/javascript`. Page paper is an SVG `<rect fill>` — do not skip SVG when remapping dark fills.
- **Test**: Stop F5, rebuild, F5, Ctrl+F5. On **B/-009** Document copies → Application form Preview: white paper, emblem, filled name/passport/company; header **Download** still filled XFA.
- **Root cause**: PDFium cannot paint XFA. Flattening does not replace the placeholder content stream. pdf.js XFA rectangles use `style.fill` on SVG, not CSS `background-color`.
- **Fix**: Local `wwwroot/lib/pdfjs/` + `visa-xfa-preview.js`; `DocumentCopiesInlinePreview` host div; `TryGetFilledApplicationFormPreview` returns filled XFA bytes (`XfaDocuments`); remap dark SVG rect fills to white after render. Removed `XfaPdfBrowserPreviewHelper`.
- **Prevent**: Do not iframe XFA. Do not Spire-merge filled forms. Do not rasterize for Chrome. Do not skip SVG rects when fixing paper color. Cache-bust `visa-xfa-preview.js` after edits.
- **Cross-skill**: visa2026-preview-slot | visa2026-pdf-form-mapping

### 2026-08-17 — Application form Preview in By person / By type (preview | UX)

- **Symptom**: Application form only lived in a bottom section and **Preview** downloaded the XFA instead of opening `#visa-preview-slot` like passport/visa rows.
- **Try**: Add a generated Application form row under each person and an **Application form** type section (last). Preview uses existing `OpenPreviewOnly`. Rasterize filled XFA pages for the iframe; header **Download** still returns the filled XFA (or ZIP for several people).
- **Test**: `ApplicationItemDocumentCopies*` tests (12 passed). `dotnet build` Blazor Debug. Stop F5, rebuild, F5. On **8/-009** Document copies: By person → Gabriel Application form Preview opens the side viewer; By type → Application form Preview opens selected people; Download from the preview header is the printable form.
- **Root cause**: Form preview was a download-only path so Chrome would not show empty XFA. Catalog never treated the form as a person/type row.
- **Fix**: `ApplicationItemDocumentCopiesTypeCatalog.ApplicationForm*` + person row; `DocumentCopiesInlinePreview` form branch; `XfaPdfBrowserPreviewHelper` static preview PDF (no `MergeFiles` on XFA).
- **Prevent**: Do not iframe raw XFA. Do not Spire-merge filled application forms. Do not add a second catalog in the slot.
- **Cross-skill**: visa2026-preview-slot | visa2026-pdf-form-mapping

### 2026-08-15 — Workspace Document copies By person / By type switcher

- **Symptom**: Officers needed a type-first catalog (Passport / Education / Visa …) without losing the person-first view or changing package/chips.
- **Try**: Page-header segmented **By person** (default) / **By type**. Type sections list Person | Record | Files | Status. Section Preview merges Ready files of that family for chip-selected people (`Family:{family}` → `TryGetMergedFamilyPdf`).
- **Test**: `ApplicationItemDocumentCopiesTypeCatalogTests` (4) + existing person-catalog tests (10 total). `dotnet build Visa2026.slnx -c Debug`. Stop F5, rebuild, F5. Open 8/-007 Document copies: By person unchanged; By type shows Passport/Education/Visa; Passport Preview opens both ready passports; hide a chip and that person drops from both views.
- **Root cause**: Catalog was person-only after the workspace person-first pass. No family grouping or family merge path.
- **Fix**: `ApplicationItemDocumentCopiesTypeCatalog` + `GroupByDocumentType` on the catalog; switcher on `OfficerShellCaseDocumentsTab` (not case chrome); reuse `TryBuildMergedPdfForRoster` with `familyKey + "."`.
- **Prevent**: Do not put the switcher in case header chips. Do not add person-section “preview all” or per-row checkboxes. ListView stays slot-first (`GroupByPerson`/`GroupByDocumentType` both false).
- **Cross-skill**: visa2026-preview-slot | visa2026-application-profile

### 2026-08-15 — Workspace passport/visa Preview failed when person is on multiple cases

- **Symptom**: B/-007 Document copies showed Passport K1450236 and Visa A7883333 Ready; Preview said “Could not load this file for preview.”
- **Try**: Pass the open case `ApplicationProfileInstanceId` into `TryGetMergedSlotPdf` / `TryBuildMergedPdfForRoster`. Catalog already loaded with that id; merge used `Guid.Empty` and required exactly one shared application.
- **Test**: `dotnet build` Module + Blazor Debug. Stop F5, rebuild, F5. Open B/-007 Document copies → Andy Preview on passport and visa.
- **Root cause**: Andy is on more than one imported instance. Person-scoped Preview could not pick a single shared application, so merge returned false even though files exist.
- **Fix**: Thread `ApplicationProfileInstanceId` from the workspace/slot into FileAccess, merger, form PDF, and download API.
- **Prevent**: Do not call `TryLoadSharedApplicationPeople(..., applicationId: Guid.Empty)` for a known case workspace. Empty id is only for ListView multi-select that shares one application.
- **Cross-skill**: visa2026-preview-slot | visa2026-application-profile

### 2026-08-15 — Workspace copies are linked records, not Current/Previous slots

- **Symptom**: Document copies listed ApplicationItem-era slots (Current/Previous passport, Current/Next visa, …) as Missing even when People & links showed Passport X1453316 and Visa A3303830.
- **Try**: Resolve from `ApplicationProfileInstancePersonResolvedLink` only. Label each row with the record number (`Passport {n}`, `Visa {n}`). Omit unlinked kinds. Preview slot key is `Passport.{guid}` (package include rules use the family prefix).
- **Test**: 9 Module.Tests (person catalog + linked-record slot keys). Stop F5, rebuild, F5. Open 8/-007 Document copies — Andy should show Passport X1453316, Visa A3303830, Education … — not Current/Previous/Next.
- **Root cause**: `ApplicationPersonLinkedDocumentsResolver` hydrated `ApplicationRosterMergeLine` Current* FKs and `ResolveProjection` always emitted Current/Previous slots.
- **Prevent**: Do not add Current/Previous/Next document-copy slots for Application Profile Instances. Copies follow linked records.
- **Cross-skill**: visa2026-application-profile | document-copies

### 2026-08-15 — Workspace person catalog labels after first officer pass

- **Symptom**: Person grouping worked, but names showed `ANDY PRAMASTA -`, the form card said `APPLICATIONPROFILEINSTANCE FORM`, and the footer still said “application line(s)”.
- **Try**: Keep person sections; fix display only. `DisplayPersonName` drops standalone `-` tokens (legacy empty LastName). Person titles use normal case. English catalog strings stay “Application form”. GroupByPerson footer/form summary say “people”.
- **Test**: `ApplicationItemDocumentCopiesPersonCatalogTests` (6). Blazor copy failed while F5 held DLLs (MSB3021) — stop F5, rebuild, F5 to confirm 8/-007.
- **Root cause**: Instance-rename leftover in `VisaUiMessageCatalog.g.cs`; section titles inherit `text-transform: uppercase`; imported LastName `-`.
- **Prevent**: Do not rewrite officer “Application form” to `ApplicationProfileInstance form`. Do not uppercase person names.
- **Cross-skill**: visa2026-application-profile | document-copies

### 2026-08-15 — Workspace Document copies is person-first

- **Symptom**: Case workspace Document copies was slot-first (Linked documents + Application form merged across the whole roster). Header Andy/Katie chips were display-only. Officers could not preview or package one person.
- **Try**: Header chips toggle include/exclude (default all). Catalog regroups by person (`N of M ready` + Ready/Missing + Preview). Preview/package use the filtered `Person.ID` set; a person-row Preview passes that one id into `DocumentCopiesSlotRequest` with `OpenPreviewOnly`.
- **Test**: `dotnet build Visa2026.slnx -c Debug`; `ApplicationItemDocumentCopiesPersonCatalogTests` (5). Stop F5, rebuild, F5. Open an in-process case with two people → Document copies shows a section per person. Click a header chip to hide one; Preview on a slot opens the side viewer for that person only; Download package uses the remaining people.
- **Root cause**: Catalog used `MergeBySlot` across all roster ids; chips were not wired to the tab.
- **Fix**: `ApplicationItemDocumentCopiesPersonCatalog` filter/readiness helpers; `GroupByPerson` + `PersonLines` on `ApplicationItemDocumentCopiesComponent`; workspace chips + `SelectedPersonIds` on `OfficerShellCaseDocumentsTab`. ListView/slot catalog stays slot-first.
- **Prevent**: Do not invent a new ZIP merge for arbitrary file checkboxes. Do not put a second catalog in `#visa-preview-slot` from this tab.
- **Cross-skill**: visa2026-application-profile | visa2026-preview-slot | document-copies

### 2026-08-14 — Workspace Document copies passed renamed instance id

- **Symptom**: Opening case workspace Document copies (or the preview-slot catalog) threw `InvalidOperationException`: `ApplicationItemDocumentCopiesComponent` has no property `ApplicationProfileInstanceId`.
- **Try**: Parents (`OfficerShellCaseDocumentsTab`, `DocumentCopiesSlotPanel`) already pass `ApplicationProfileInstanceId` after the instance rename; the catalog component still declared `ApplicationId`.
- **Test**: Stop F5, rebuild, open an in-process case → Document copies tab (and slot catalog) load without the parameter exception.
- **Root cause**: Blazor parameter name mismatch after `Application` → `ApplicationProfileInstance`.
- **Fix**: Rename `[Parameter] ApplicationId` → `ApplicationProfileInstanceId` on `ApplicationItemDocumentCopiesComponent`.
- **Prevent**: When renaming instance id parameters, grep both the component `[Parameter]` and every caller attribute.
- **Cross-skill**: visa2026-application-profile | document-copies

### 2026-06-06 — Inline footer batch progress removed (toast only)

- **Symptom**: Dialog footer showed Completed / 100% / Download ZIP; polling unreliable; redundant with PDF toast.
- **Try**: Compare `PdfBatchToastHost` vs dialog `fetchJson` polling on `{batchId}/status`.
- **Test**: Download package → progress and ZIP link only in bottom-right toast; footer shows subtitle + actions only (optional enqueue notice).
- **Root cause**: Duplicate progress surfaces; dialog polling added complexity without officer benefit.
- **Fix**: Removed batch polling from `ApplicationItemDocumentCopiesComponent`; keep `visaPdfBatchToast.setCurrentBatchId` on enqueue.
- **Prevent**: Package progress = toast only; document in `APPLICATION_ITEM_DOCUMENT_COPIES.md`.
- **Cross-skill**: —

### 2026-06-06 — Application form Preview — no second modal

- **Symptom**: Application form Preview opened redundant “Document preview: Application form” popup after download already started.
- **Try**: Click Application form Preview on main dialog only.
- **Test**: Row progress → browser download; no `DxPopup`; footer notice optional.
- **Root cause**: `OpenFilledApplicationFormAsync` set `_visible = true` on preview dialog after triggering download.
- **Fix**: Download inline in component via `DocumentFileAccess` + `IFileDownloader`; preview dialog scan-only.
- **Prevent**: Application form never opens preview modal; mapping issues → pdf-form-mapping skill.
- **Cross-skill**: pdf-form-mapping (if fields wrong after download)

### 2026-06-06 — Preview row progress aligned with Resminamalar

- **Symptom**: Preview clicks lacked consistent feedback on document copy rows.
- **Try**: Compare `ApplicationReportPackageComponent` preview progress markup/CSS.
- **Test**: Generating label + `app-report-package__*` indeterminate bar on active row; 1.5s minimum visible duration.
- **Root cause**: Document copies had no shared progress pattern.
- **Fix**: Reuse Resminamalar CSS classes and `ApplicationReportPackage.Preview.Downloading` message key.
- **Prevent**: UX parity changes should reuse `app-report-package__*` — do not invent parallel progress CSS.
- **Cross-skill**: resminamalar

### 2026-06-06 — Package enqueue ItemKeyType must be Guid

- **Symptom**: Package download failed with invalid cast String → ApplicationItem.
- **Try**: Inspect queued `PdfGenerationBatch.ItemKeyType` and `ItemKeysJson`.
- **Test**: Batch processes; worker resolves keys as Guid.
- **Root cause**: Enqueue stored `typeof(ApplicationItem)` instead of `typeof(Guid)`.
- **Fix**: `ApplicationItemPdfBatchEnqueueService` uses `typeof(Guid)`; worker `ResolveKeyType` treats legacy rows.
- **Prevent**: Never store BO type when keys are serialized GUID strings.
- **Cross-skill**: lifecycle-docker (if worker still fails after fix)

## 2026-07-31 - AppItem catalog uses shared sectioned chrome

**Ask:** Unify document-copies look with Person/dossier-adjacent catalog.

**Fix:** `ApplicationItemDocumentCopiesComponent` Linked documents + Application Form sections use `.doc-copies-catalog` rows; package options / Download package footer unchanged.

**Prevent:** Keep ministry ZIP semantics in this skill; catalog presentation follows PREVIEW_SLOT chrome contract.

### 2026-07-31 — Prototype A nav across all document-copies catalogs

- **Ask**: Apply Foxit-style vertical nav layout across all document copies in the project.
- **Fix**: Shared `DocumentCopiesCatalogNavIcons`; Person/Header/ApplicationItem section heads use `__section-head--nav` + Open/Close; exclusive expand (AppItem/Person multi-section); Header single Documents card collapsed by default.
- **Prevent**: Do not keep flat always-open section heads on Header/AppItem while Person has nav cards.
- **Cross-skill**: person-document-copies | document-copies | invitation-work-permit-document-copies | preview-slot

### 2026-07-31 — Dedicated Document copies brand mark (smiling paperclip)

- **Ask**: Use a dedicated icon/label for Document copies across the project (pill + smiling paperclip).
- **Fix**: `DocumentCopies.svg` (XAF ImageName), `document-copies-clip.svg` + `document-copies-brand.css`, `DocumentCopiesBrandMark`; wired toolbar actions (Person/Header/AppItem), ListView Copies pills, dossier button, slot titles.
- **Prevent**: Do not reuse `BO_FileAttachment` for document-copies entry points; use `DocumentCopies` / `.doc-copies-brand*`.
- **Cross-skill**: person-document-copies | document-copies | invitation-work-permit-document-copies | preview-slot | person-dossier
