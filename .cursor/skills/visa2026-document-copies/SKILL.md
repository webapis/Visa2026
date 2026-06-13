---
name: visa2026-document-copies
description: >-
  ApplicationItem Document copies dialog: linked scan readiness, per-slot preview,
  application form inline download, package options, gap confirm, PdfGenerationBatch enqueue,
  PDF toast (PdfBatchToastHost). Successor to hidden Generate PDF / My PDF Jobs. Use for
  document copies bugs, preview UX, package download, scan merge preview, readiness gaps —
  not for XFA PdfFormMapping field rules (visa2026-pdf-form-mapping) or Resminamalar Word
  reports (visa2026-resminamalar). Always read learnings.md first; append after verified
  fixes; prompts.md.
disable-model-invocation: false
---

# Visa2026 — Document copies (ApplicationItem)

**User prompts:** [prompts.md](./prompts.md) (`@visa2026-document-copies`).

## Agent workflow (every task — mandatory)

1. **Read** [learnings.md](./learnings.md) (**## Entries**, newest first) and **Scenarios** below.
2. **Classify** — dialog / scans / preview / batch enqueue (**this skill**) vs filled form field empty/wrong (**[pdf-form-mapping](../visa2026-pdf-form-mapping/SKILL.md)**) vs batch worker/schema (**[lifecycle-docker](../visa2026-lifecycle-docker/SKILL.md)**).
3. **Fix** with minimal diff; respect **Module vs Blazor** boundaries (see [reference.md](./reference.md)).
4. **Verify** — `dotnet build Visa2026.slnx -c Debug`; manual: scan preview, application form download (no popup), package → PDF toast.
5. **Record** — append [learnings.md](./learnings.md) after **verified** fix ([MATURITY.md](./MATURITY.md)).
6. **Promote** — same root cause twice → update **Scenarios** or **Triage**.

## Canonical doc

**[`docs/APPLICATION_ITEM_DOCUMENT_COPIES.md`](../../../docs/APPLICATION_ITEM_DOCUMENT_COPIES.md)** — officer workflow, v1→v2, architecture, file map.

**Related skills (do not duplicate):**

| Topic | Skill |
|-------|--------|
| XFA field empty/wrong, `PdfFormMapping`, `PdfMappingHelper`, template swap | [`.cursor/skills/visa2026-pdf-form-mapping/SKILL.md`](../visa2026-pdf-form-mapping/SKILL.md) |
| Resminamalar UX patterns (row progress CSS) — Word batch, not PDF ZIP | [`.cursor/skills/visa2026-resminamalar/SKILL.md`](../visa2026-resminamalar/SKILL.md) |
| `PdfGenerationBatch` worker failures, Docker, schema drift | [`.cursor/skills/visa2026-lifecycle-docker/SKILL.md`](../visa2026-lifecycle-docker/SKILL.md) |
| Slot eligibility / diploma packaging rules (domain) | [`docs/APPLICATION_DIPLOMA_PACKAGE_PLAN.md`](../../../docs/APPLICATION_DIPLOMA_PACKAGE_PLAN.md) |

**Long reference:** [reference.md](./reference.md). **Experience log:** [learnings.md](./learnings.md). **Maturity:** [MATURITY.md](./MATURITY.md).

---

## Scenarios (promoted from learnings — check first)

| Symptom | First step | Likely owner |
|---------|------------|--------------|
| Package queue cast error (`String` → `ApplicationItem`) | `ItemKeyType` = `typeof(Guid)` in enqueue; worker `ResolveKeyType` for legacy rows | **This skill** |
| Footer shows batch progress / Download ZIP in dialog | Removed by design — check `PdfBatchToastHost` only | **This skill** |
| Application form opens second preview popup | Should download inline in component — no `OpenFilledApplicationFormAsync` popup | **This skill** |
| Preview row flash too fast | `MinimumPreviewProgressDuration` (1.5s); Resminamalar CSS classes | **This skill** |
| Dialog missing scans but files exist in DB | `ApplicationItemLinkedDocumentsResolver` + eligibility (diploma plan §1.2–§1.3) | **This skill** |
| Preview OK, ZIP missing slot | Package options flags vs `ApplicationItemDocumentCopiesPackageSlotRules` vs packer | **This skill** |
| Application form downloads but **fields empty/wrong** | Mapping logs, `PdfFormMapping`, `PdfMappingSourceGate` | **pdf-form-mapping** |
| `Invalid column name` on `PdfGenerationBatch` | Updaters, `FORCE_XAF_DB_UPDATE` | **lifecycle-docker** |
| Resminamalar / Word ZIP issue | Not document copies | **resminamalar** |

---

## Scope (this skill)

| In scope | Out of scope |
|----------|----------------|
| **`ApplicationItemDocumentCopiesComponent`** dialog UI | Resminamalar / Word reports |
| **`ApplicationItemDocumentCopiesPreviewDialog`** (scan slots only) | Authoring XFA PDF template layout |
| Readiness, gap confirm, package options, gear toggle | New `PdfFormMapping` rules (admin BO) |
| Preview merge for **linked scans** (`ApplicationItemDocumentCopyPdfMerger`) | Spire XFA fill internals (`PdfFormFillerService`) |
| Enqueue via `ApplicationItemPdfBatchEnqueueService` | Changing ZIP packer semantics without parity check |
| `visaPdfBatchToast.setCurrentBatchId` + PDF toast progress | Inline footer batch progress bar (toast only) |
| Application form **download** path (inline, row progress) | Second modal after application form Preview |

---

## Entry point

| Where | Action |
|-------|--------|
| **`ApplicationItem`** ListView (multi-select) | **Document copies** (`ApplicationItemDocumentCopiesController`) |

Generate PDF / My PDF Jobs are **hidden** — document copies is the supported path.

---

## Officer workflow (v2)

1. Select line(s) → **Document copies** → slot readiness list.
2. Optional **gear** — per-file / missing-line detail.
3. **Preview** scan slot → PDF modal (iframe); **Application form** → inline download (no second modal).
4. **Download package** → optional gap confirm → `PdfGenerationBatch` → **PDF generation toast** (not footer progress).

---

## Parity rules (do not violate)

1. **ZIP parity:** `ApplicationItemDocumentPackageOptions` ↔ `PdfBatchEnqueueOptions` ↔ worker ↔ `ApplicationSupportingDocumentsPacker`.
2. **Enqueue keys:** `ItemKeyType` = **`typeof(Guid)`**, not `ApplicationItem`.
3. **Preview vs package:** preview = synchronous row progress; package = async job + toast — **no dialog footer progress bar**.
4. **Application form:** `ApplicationFilledFormPdfGenerator` — no Spire merge for preview/download; worker still writes `PDF_Form/` in ZIP.
5. **Resminamalar UX reuse:** `app-report-package__*` progress + `ApplicationReportPackage.Preview.Downloading` for generating label.

---

## Triage (symptom → first check)

| Symptom | First check |
|---------|-------------|
| Dialog empty / no slots | Selection count; resolver output; `ApplicationItemDocumentCopiesListPropertyEditor` refresh |
| Preview fails for scan slot | `ApplicationItemDocumentFileAccess.TryGetMergedSlotPdf`; merger logs |
| Application form download fails | Error in footer; then **pdf-form-mapping** if generation succeeds but fields wrong |
| Gap confirm wrong | `ApplicationItemDocumentCopiesReadinessSummary` + current include flags only |
| Package never completes | Worker log, `PdfBatchToastHost`, `/api/PdfBatches/my-latest` — **lifecycle-docker** |
| Wrong files in ZIP | Batch options on row vs dialog options; packer notes `PACKAGING_NOTES.txt` |

---

## UX / UI changes (checklist)

1. **Blazor:** `ApplicationItemDocumentCopiesComponent.razor`, `ApplicationItemDocumentCopiesPreviewDialog.razor`.
2. **CSS:** `wwwroot/css/site.css` — `.app-item-doc-copies*`; reuse `app-report-package__*` for preview progress.
3. **Localization:** `ApplicationItemDocumentCopies.*` in `UiStrings.messages.json` → regenerate via `tools/GenerateModelLocalization`.
4. **Docs:** update [`docs/APPLICATION_ITEM_DOCUMENT_COPIES.md`](../../../docs/APPLICATION_ITEM_DOCUMENT_COPIES.md) if officer-visible behaviour changes.
5. **Verify:** single + multi line; partial gaps; application form PDF vs ZIP; package toast.

---

## Build / verify

```powershell
dotnet build Visa2026.slnx -c Debug
```

Manual: ListView → Document copies → preview one scan → application form Preview (download only, no popup) → Download package → toast ZIP.

---

## Recording experience

| After verified fix | Action |
|--------------------|--------|
| Dialog, preview scans, package enqueue, readiness, toast registration | Append [learnings.md](./learnings.md) |
| Filled form field empty/wrong | Append [pdf-form-mapping/learnings.md](../visa2026-pdf-form-mapping/learnings.md) + **Cross-skill** |
| Worker / schema / Docker | **lifecycle-docker** learnings or skill |
| Same root cause **2+** times | Promote **Scenarios** ([MATURITY.md](./MATURITY.md)) |

**Do not** append speculative fixes. **Do not** delete old learnings entries.
