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
