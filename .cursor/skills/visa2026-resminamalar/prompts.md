# Prompts — visa2026-resminamalar

Copy-paste into Cursor chat to steer the agent toward the **Resminamalar** report package dialog (catalog, preview, ZIP batch, UX). Reference the skill explicitly:

**`@visa2026-resminamalar`** or **`@.cursor/skills/visa2026-resminamalar`**

**In scope:** Application / ApplicationItem **Resminamalar** dialog, readiness, preview PDF, batch toast, empty template list, seed gate, permissions, gear toggle.

**Out of scope:** new `.docx`/`.xlsx` layout, maps, placeholder tokens, merge row builders → **`@visa2026-user-report-templates`**

**Agent should:** read **`learnings.md`** first; append after **verified** fixes ([**MATURITY.md**](./MATURITY.md)).

---

## Quick start

| You want… | Copy this |
|-----------|-----------|
| **Orient me** | `@visa2026-resminamalar Explain Resminamalar v2 — Application vs ApplicationItem entry, catalog, preview, ZIP batch. Point to key files.` |
| **Bug / something broken** | `@visa2026-resminamalar Resminamalar [symptom]. Read learnings.md and Scenarios first, then triage and fix.` |
| **UX improvement** | `@visa2026-resminamalar Improve Resminamalar UX: [describe change]. Follow UX checklist in SKILL.md and update localization if needed.` |
| **After we fixed it** | `@visa2026-resminamalar Append learnings.md for [short title] — verified fix: [one line]. Promote to Scenarios if this happened before.` |
| **Wrong skill? (merge error)** | `@visa2026-user-report-templates Batch failed: '{{ds.rows.RowNo}}' could not be replaced on Sanaw — not catalog UI.` |

---

## Empty catalog / seeding

- `@visa2026-resminamalar User Report Template list is empty after restart — check UserReportTemplateSeedGate and seed logs.`
- `@visa2026-resminamalar Resminamalar dialog shows no reports but templates exist in DB — visibility and Application vs Item scope.`
- `@visa2026-resminamalar Templates never seeded on first deploy — trace UserReportTemplateUpdater vs Startup.Configure gate.`
- `@visa2026-resminamalar DEBUG: confirm embedded templates from Resources/Templates re-seed on every startup.`

---

## Catalog & visibility

- `@visa2026-resminamalar Borcnama shows in User Report Template admin but not in Resminamalar for App_Inv — triage visibility rules.`
- `@visa2026-resminamalar GT-15 template missing from catalog — check project contract links and application type.`
- `@visa2026-resminamalar Resminamalar action disabled on Application detail — NoApplicableReports / catalog TotalCount.`
- `@visa2026-resminamalar Item ListView Resminamalar disabled — selection validation (same application, item scope).`

---

## Readiness, gap confirm, Check chip

- `@visa2026-resminamalar Officers think Check chip blocks download — explain gap confirm vs hard batch failure.`
- `@visa2026-resminamalar Reduce false-positive readiness hints for [template name] in dry-run evaluator.`
- `@visa2026-resminamalar Hide readiness hint lines by default; keep gear toggle behaviour documented.`

---

## Preview vs ZIP / batch

- `@visa2026-resminamalar Preview works but Download package ZIP fails — same ApplicationWordReportEntryGenerator path; check worker logs.`
- `@visa2026-resminamalar ZIP missing files I checked — SelectedReportKeysJson and batch worker selection.`
- `@visa2026-resminamalar ApplicationItem Resminamalar ZIP wrong item count — SelectedApplicationItemIdsJson and per-item Word output.`
- `@visa2026-resminamalar Resminamalar batch toast never completes — WordReportGenerationBatchWorkerService triage.`
- `@visa2026-resminamalar Invalid column name WordReportGenerationBatches — BatchWorkerSchemaGate / FORCE_XAF_DB_UPDATE.`

**Merge / token errors in worker log** (route to template skill):

- `@visa2026-user-report-templates Resminamalar ZIP: '{{…}}' could not be replaced for template [Sanaw|Forma 16|…] — row builder / merge, not dialog.`

---

## Preview PDF / Office conversion

- `@visa2026-resminamalar Preview PDF blank or fails for Excel template [name] — Office preview converter path.`
- `@visa2026-resminamalar Preview PDF works for Word but not xlsx — ApplicationWordReportOfficePreviewPdfConverter.`
- `@visa2026-resminamalar Download Word from preview header returns wrong filename — file access / catalog entry.`

---

## Permissions & Edit template

- `@visa2026-resminamalar Extract placeholders security error from Resminamalar Edit template link — UserReportPlaceholder permissions.`
- `@visa2026-resminamalar Edit template link not shown — gear toggle, UserReportTemplateEditAccess, Write permission.`
- `@visa2026-resminamalar Users role cannot open Reports → User Report Template — Updater officer permissions.`

---

## UX / UI changes

- `@visa2026-resminamalar Add [control/label/behaviour] to ApplicationReportPackageComponent — Application scope only.`
- `@visa2026-resminamalar Same Resminamalar UX change on ApplicationItem ListView host — shared component.`
- `@visa2026-resminamalar Add localization for ApplicationReportPackage.[Key] and regenerate GenerateModelLocalization.`
- `@visa2026-resminamalar Match Document copies dialog pattern for [preview|selection|footer] in Resminamalar.`
- `@visa2026-resminamalar Default all templates unchecked on first open / remember last selection — discuss UX then implement.`

---

## Verify & commit

- `@visa2026-resminamalar Fix [issue], dotnet build, describe manual test on Application + ApplicationItem Resminamalar.`
- `@visa2026-resminamalar commit after verify — Resminamalar [short description]. Include learnings.md if we verified a new incident.`

---

## Experience loop (make the skill smarter)

- `@visa2026-resminamalar Read learnings.md before starting — we're debugging [symptom] again.`
- `@visa2026-resminamalar Same root cause as [date/entry title] — promote to Scenarios in SKILL.md per MATURITY.md.`
- `@visa2026-resminamalar Document officer-facing change in APPLICATION_REPORT_PACKAGE.md and append learnings.`

---

## General activation keywords

Paste any of these if `@` skill picker is unavailable:

- `Resminamalar` / `report package dialog` / `ApplicationReportPackage`
- `WordReportGenerationBatch` / `Resminamalar batch` / `visaWordBatchToast`
- `UserReportTemplateSeedGate` / `empty User Report Template list`
- `ApplicationWordReportPackageCatalog` / `ApplicationReportPackageComponent`
- `visa2026-resminamalar skill`

---

## Full prompt template — bug fix

```
@visa2026-resminamalar

Resminamalar issue:
- Where: [Application detail | ApplicationItem ListView]
- Symptom: [empty catalog | preview fail | ZIP fail | Check chip confusion | …]
- Template(s): [name if known]
- What I tried: [restart | re-extract | …]

Read learnings.md + Scenarios first. Classify: dialog/batch vs merge (user-report-templates).
Fix with preview/ZIP parity. Append learnings.md after verified fix.
```

---

## Full prompt template — UX change

```
@visa2026-resminamalar

Resminamalar UX change:
- Entry: [Application | ApplicationItem | both]
- Desired behaviour: [describe officer workflow]
- Keep: preview and ZIP same generator; gear hidden by default unless specified

Update ApplicationReportPackageComponent (+ localization/model if needed).
Update docs/APPLICATION_REPORT_PACKAGE.md if officer-visible.
dotnet build Visa2026.slnx -c Debug.
```

---

## Related skills

| Task | Skill |
|------|--------|
| Embed/register `.docx`/`.xlsx`, maps, placeholders, Sanaw/RowNo merge | **`@visa2026-user-report-templates`** |
| Role denied / navigation | **`@visa2026-security-access`** |
| Docker deploy, schema drift, FORCE_XAF_DB_UPDATE | **`@visa2026-lifecycle-docker`** |
| Document copies dialog (parallel UX pattern) | **`docs/APPLICATION_ITEM_DOCUMENT_COPIES.md`** |
| Commit after build | **`@commit-after-verify`** |
