# Prompts — visa2026-document-copies

Copy-paste into Cursor chat. Reference:

**`@visa2026-document-copies`** or **`@.cursor/skills/visa2026-document-copies`**

**In scope:** Document copies dialog, scan preview, package download, readiness, gap confirm, PDF toast.

**Out of scope:** XFA field mapping → **`@visa2026-pdf-form-mapping`** · Word Resminamalar → **`@visa2026-resminamalar`**

**Agent should:** read **`learnings.md`** first; append after verified fixes ([**MATURITY.md**](./MATURITY.md)).

---

## Quick start

| You want… | Copy this |
|-----------|-----------|
| **Orient me** | `@visa2026-document-copies Explain Document copies v2 vs hidden Generate PDF — entry points, preview, package, toast.` |
| **Bug / broken UX** | `@visa2026-document-copies Document copies [symptom]. Read learnings.md and Scenarios first.` |
| **UX improvement** | `@visa2026-document-copies Improve Document copies UX: [change]. Match Resminamalar progress pattern where applicable.` |
| **After verified fix** | `@visa2026-document-copies Append learnings.md for [title] — verified: [one line].` |
| **Wrong skill (PDF fields)** | `@visa2026-pdf-form-mapping Application form PDF field [X] empty in Document copies — mapping/gate, not dialog UI.` |

---

## Preview & progress

- `@visa2026-document-copies Scan slot Preview fails — TryGetMergedSlotPdf / merger triage.`
- `@visa2026-document-copies Preview row progress should match Resminamalar — app-report-package__ classes.`
- `@visa2026-document-copies Application form Preview must download inline — no second preview modal.`

---

## Package download & toast

- `@visa2026-document-copies Download package queued but toast never updates — PdfBatchToastHost / setCurrentBatchId.`
- `@visa2026-document-copies Remove or fix footer batch progress — should be toast only (see learnings).`
- `@visa2026-document-copies Package enqueue Invalid cast ApplicationItem — ItemKeyType Guid.`
- `@visa2026-document-copies ZIP missing slots officer expected — options flags vs packer vs readiness rules.`

---

## Readiness & gaps

- `@visa2026-document-copies Gap confirm shows incorrectly for [slot] — ReadinessSummary + package options.`
- `@visa2026-document-copies Slot shows no files but attachment exists — resolver + APPLICATION_DIPLOMA_PACKAGE_PLAN eligibility.`
- `@visa2026-document-copies Gear toggle / missing-line summary UX improvement.`

---

## Refactor / maintain

- `@visa2026-document-copies Keep ApplicationItemDocumentPackageOptions in parity with PdfBatchEnqueueOptions after [change].`
- `@visa2026-document-copies Add localization for new Document copies string ApplicationItemDocumentCopies.*`
- `@visa2026-document-copies Update APPLICATION_ITEM_DOCUMENT_COPIES.md after officer-visible change.`
