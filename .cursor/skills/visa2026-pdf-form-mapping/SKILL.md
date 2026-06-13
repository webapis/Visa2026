---
name: visa2026-pdf-form-mapping
description: >-
  XFA visa application PDF form filling: PdfFormMapping admin BO, PdfMappingHelper,
  PdfMappingSourceGate, PdfFormFillerService (Spire.PDF), ApplicationFilledFormPdfGenerator,
  template path/embedded resource, mapping updaters. Use when PDF fields are empty/wrong,
  adding PdfFieldKey mappings, ApplicationType gating, ChoiceList raw values, template swap —
  not for Document copies dialog UX (visa2026-document-copies) or scan merge preview. Read
  learnings.md first; append after verified fixes; prompts.md.
disable-model-invocation: false
---

# Visa2026 — PDF form mapping (XFA template fill)

**User prompts:** [prompts.md](./prompts.md) (`@visa2026-pdf-form-mapping`).

## Agent workflow (every task — mandatory)

1. **Read** [learnings.md](./learnings.md) and **Scenarios** below.
2. **Classify** — mapping / fill / template (**this skill**) vs document copies UI/download path (**[document-copies](../visa2026-document-copies/SKILL.md)**) vs linked scan attachments (resolver — **document-copies**).
3. **Fix** in **Module** (`PdfFormMapping`, `PdfMappingHelper`, updaters, filler); avoid duplicating mapping in Blazor.
4. **Verify** — `dotnet build Visa2026.slnx -c Debug`; fill one `ApplicationItem` via Document copies application form or hidden Generate PDF path; check Debug logs for `PdfMappingHelper`.
5. **Record** — append [learnings.md](./learnings.md) after **verified** fix ([MATURITY.md](./MATURITY.md)).
6. **Promote** — repeat issues → **Scenarios** / **Triage**.

## Canonical docs

| Doc | Role |
|-----|------|
| [`Visa2026.Module/BusinessObjects/PdfFormMapping.md`](../../../Visa2026.Module/BusinessObjects/PdfFormMapping.md) | Admin BO, mapping modes, troubleshooting |
| [`Visa2026.Module/Services/PDF-Form-Filling.md`](../../../Visa2026.Module/Services/PDF-Form-Filling.md) | End-to-end implementation reference |
| [`Visa2026.Module/Resources/XFA_PDF_Integration.md`](../../../Visa2026.Module/Resources/XFA_PDF_Integration.md) | Spire XFA constraints (no `MergeFiles`, Linux) |

**Related skills:**

| Topic | Skill |
|-------|--------|
| Document copies dialog, application form **download UX**, scan preview | [`.cursor/skills/visa2026-document-copies/SKILL.md`](../visa2026-document-copies/SKILL.md) |
| `PdfGenerationBatch` worker / ZIP / Docker | [`.cursor/skills/visa2026-lifecycle-docker/SKILL.md`](../visa2026-lifecycle-docker/SKILL.md) |
| ApplicationType Show* visibility (gates mapping) | [`.cursor/skills/visa2026-lookup-data/SKILL.md`](../visa2026-lookup-data/SKILL.md) |
| Word / Resminamalar | [`.cursor/skills/visa2026-resminamalar/SKILL.md`](../visa2026-resminamalar/SKILL.md) — **not** XFA |

**Long reference:** [reference.md](./reference.md). **Experience log:** [learnings.md](./learnings.md). **Maturity:** [MATURITY.md](./MATURITY.md).

---

## Scenarios (check first)

| Symptom | First step | Likely owner |
|---------|------------|--------------|
| PDF field empty but BO has data | `PdfFieldKey` exact match; Property path; null intermediate object | **This skill** |
| Field empty only for some application types | `PdfMappingSourceGate` + `ApplicationType` Show* flags | **This skill** + lookup-data |
| Dropdown / ChoiceList wrong value | Raw XFA code via `ResolveRawValue`, not display label | **This skill** |
| Filled PDF shows "Please wait…" after merge | Used `PdfDocument.MergeFiles` — forbidden for XFA | **This skill** |
| Document copies application form **download works**, fields wrong | Mapping/template — **not** dialog UI | **This skill** |
| Document copies application form **download fails** | Error key from `ApplicationFilledFormPdfGenerator`; template path | **This skill** → then document-copies if UX |
| Officer says Generate PDF wrong (legacy hidden) | Same mapping path as document copies form row | **This skill** |
| Scan preview PDF wrong | Linked document merge — **document-copies**, not mapping | **document-copies** |

---

## Scope (this skill)

| In scope | Out of scope |
|----------|----------------|
| **`PdfFormMapping`** records (admin UI) | Document copies slot list / readiness UI |
| **`PdfMappingHelper`**, **`PdfMappingSourceGate`** | `ApplicationItemDocumentCopyPdfMerger` (scan previews) |
| **`PdfFormFillerService`** / Spire XFA fill | Resminamalar / DocxTemplater |
| **`ApplicationFilledFormPdfGenerator`** | Package options / gap confirm |
| Template: `Visa_Application_TM_QR_08.pdf`, `PdfSettings:TemplatePath` | Word `.docx` templates |
| Updaters: `PdfFormMappingUpdater`, org/lookup mapping updaters | `PdfGenerationBatch` worker packaging logic (except form fill step) |
| Value converters on mapping rules | New mapping storage outside existing pipeline |

---

## Consumers (same fill pipeline)

All call **`PdfMappingHelper.MapApplicationData`** + **`IPdfFormFillerService.FillForm`**:

| Entry | Location |
|-------|----------|
| Document copies — application form row | `ApplicationFilledFormPdfGenerator` ← `ApplicationItemDocumentFileAccess` |
| PDF batch worker — `PDF_Form/` folder | `PdfGenerationBatchWorkerService` |
| Legacy Generate PDF (hidden) | `ApplicationItemPdfController`, `ApplicationPdfController` |

**Rule:** fix mapping once in Module — all consumers pick it up.

---

## Triage (symptom → first check)

| Symptom | First check |
|---------|-------------|
| All fields empty | Template path / embedded resource load; filler exceptions in log |
| One field empty | Single `PdfFormMapping` row; Debug log line for that key |
| Field skipped intentionally | `PdfMappingSourceGate` — hidden slot or missing ApplicationItem link |
| Wrong date/format | Converter type on mapping rule |
| Works locally, fails Docker | `XFA_PDF_Integration.md` — GDI+ / font dependencies |
| New ministry PDF template | New embedded resource + remap all `PdfFieldKey` values |

---

## Mapping change checklist

1. Confirm **`PdfFieldKey`** from template spec (exact XFA path).
2. Choose mode: **Property** / **Expression** / **Constant**.
3. For Property/Expression: verify **`PdfMappingSourceGate`** allows path for target `ApplicationType`.
4. Add/update row via admin or **`PdfFormMappingUpdater`** (seed JSON) — follow existing updater patterns.
5. Enable **Debug** logging for `Visa2026.Module.Services` during verification.
6. Test via Document copies application form download **and** batch `PDF_Form/` if worker touched.
7. Document non-obvious gates in [`PdfFormMapping.md`](../../../Visa2026.Module/BusinessObjects/PdfFormMapping.md) if admin-facing.

---

## Build / verify

```powershell
dotnet build Visa2026.slnx -c Debug
```

Manual: one `ApplicationItem` with known data → Document copies → Application form Preview → open PDF → spot-check gated fields.

---

## Recording experience

| After verified fix | Action |
|--------------------|--------|
| Mapping, gate, filler, template, generator | Append [learnings.md](./learnings.md) |
| Officer only reported via Document copies | Note **Cross-skill: document-copies** in entry |
| ZIP worker packaging (not fill) | **lifecycle-docker** / document-copies |

**Do not** conflate scan attachment bugs with mapping bugs in learnings entries.
