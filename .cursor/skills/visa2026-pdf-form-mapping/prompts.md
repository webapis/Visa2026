# Prompts — visa2026-pdf-form-mapping

Copy-paste into Cursor chat. Reference:

**`@visa2026-pdf-form-mapping`** or **`@.cursor/skills/visa2026-pdf-form-mapping`**

**In scope:** `PdfFormMapping`, `PdfMappingHelper`, `PdfMappingSourceGate`, Spire XFA fill, template, updaters.

**Out of scope:** Document copies dialog layout → **`@visa2026-document-copies`** · scan attachments → **document-copies**

**Agent should:** read **`learnings.md`** first; append after verified fixes ([**MATURITY.md**](./MATURITY.md)).

---

## Quick start

| You want… | Copy this |
|-----------|-----------|
| **Orient me** | `@visa2026-pdf-form-mapping Explain XFA fill pipeline — PdfFormMapping → PdfMappingHelper → PdfFormFillerService → consumers.` |
| **Empty/wrong field** | `@visa2026-pdf-form-mapping PDF field [PdfFieldKey or label] empty/wrong for ApplicationItem — triage mapping and gate.` |
| **New mapping** | `@visa2026-pdf-form-mapping Add PdfFormMapping for [field] from ApplicationItem path [path] — follow updater patterns.` |
| **After verified fix** | `@visa2026-pdf-form-mapping Append learnings.md — verified fix for [title].` |
| **Wrong skill (UI only)** | `@visa2026-document-copies Document copies download works but officer wants UX change — not mapping.` |

---

## Empty / wrong fields

- `@visa2026-pdf-form-mapping Field empty but Person.LastName exists — PdfFieldKey, path, null navigation, Debug logs.`
- `@visa2026-pdf-form-mapping Mapping skipped for application type [code] — PdfMappingSourceGate and ApplicationType Show* flags.`
- `@visa2026-pdf-form-mapping Dropdown shows wrong value — ChoiceList raw XFA code vs ResolveRawValue.`
- `@visa2026-pdf-form-mapping Expression mapping fails — criteria syntax and gate on expression string.`

---

## Template & infrastructure

- `@visa2026-pdf-form-mapping Swap or update Visa_Application_TM_QR_08.pdf embedded template — remap PdfFieldKey values.`
- `@visa2026-pdf-form-mapping PdfSettings TemplatePath not found in Docker — embedded fallback and XFA_PDF_Integration Linux notes.`
- `@visa2026-pdf-form-mapping Filled PDF shows Please wait after merge — forbidden MergeFiles on XFA.`

---

## Updaters & seed

- `@visa2026-pdf-form-mapping Add seeded PdfFormMapping row in PdfFormMappingUpdater for [field].`
- `@visa2026-pdf-form-mapping OrganizationPdfFormMappingUpdater / lookup mapping sync issue after deploy.`

---

## Consumers (verify all)

- `@visa2026-pdf-form-mapping Fix mapping — verify Document copies application form download AND batch PDF_Form folder in ZIP.`
- `@visa2026-pdf-form-mapping ApplicationFilledFormPdfGenerator error ApplicationItemDocumentCopies.GenerateForm.Error — triage filler/template.`
