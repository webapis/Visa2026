---
name: visa2026-word-reports
description: >-
  Creates and maintains Visa2026 Word (.docx) reports via DocxTemplater, IWordReportDefinition,
  WordReportsController ("Resminamalar"), tools/GenerateTemplates, tools/PreviewWordReports,
  FormTemplates, layout families, and BO mapping. L2 stepped ministry letters: clone Inv+WP / Visa+WP Ext
  helpers (ministry recipient table, spacing merge, preview composites for single-digit counts). Prerequisites before template design: (1) data
  type(s) documented, (2) reference scan in FormTemplates, (3) *_map.md as build contract, (4) user
  approves map, (5) PreviewWordReports preset with dump values transcribed from that scan. See
  docs/WORD_REPORT_GENERATION_IDEA.md; report-predefined-xaf for legacy XtraReports;
  PdfFormFillerService is separate.
disable-model-invocation: false
---

# Visa2026 — Word report pipeline (DocxTemplater)

## Canonical reference

Read and follow **`docs/WORD_REPORT_GENERATION_IDEA.md`** for architecture, placeholder tables (`Application`, `ApplicationItem`, `Registration`, `BusinessTrip`), and how the Word pipeline fits next to XtraReports.

**User-seeded templates** (embedded **`Resources/Templates/*.docx`**, **`UserReportTemplateUpdater`**, **`UserReportTemplate`**): use **`visa2026-user-report-templates`** — same DocxTemplater **`ds`** model; different shipping and registration path than **`IWordReportDefinition`**.

## Prerequisites for starting Word report design

All of the following must be satisfied **before** authoring or regenerating **`Resources/*.docx`**, **`GenerateTemplates`** layout code, or new **`IWordReportDefinition`** merge dictionaries for a report that targets a ministry real document:

1. **Data type (business object)** — It is clear **which type(s)** supply data for the report (from **`Visa2026.Module/BusinessObjects/`**): almost always **`Application`** as root for **Resminamalar**, plus **row type** if any (`ApplicationItem`, `Registration`, `BusinessTrip`, or a documented exception). This is recorded in **`FormTemplates/<basename>_map.md`** (see **`reference.md` → Map document checklist** → **Data type**). Do not guess placeholder sources.
2. **Scanned image of the real document** — The **reference scan** (ministry / signed sample / official output) is present under **`Visa2026.Module/Resources/FormTemplates/`** with a **stable filename**. The map **must reference** that file. If the user waives a scan (rare), they must state it in chat and the map must document the typography source. **No scan + no waiver → stop** layout work.
3. **`*_map.md` is the build contract** — **`FormTemplates/<basename>_map.md`** exists (**create if missing**). It holds **all metadata** needed to build the actual Word template: identity, data types, reference image(s), layout/bands or letter structure, **field contract** (placeholders ↔ BO paths ↔ `ds.*` / `{{.…}}`), static vs dynamic, and notes (e.g. Word vs XtraReport). The **implemented template and `*ReportDef` must match this map**.
4. **User approves the map** — The agent presents the map and **waits for explicit user approval** (“approved”, “LGTM”, or equivalent). **Word template / generator / def work starts only after approval.** Material binding or placeholder changes → **update the map** and **re-approve** (or a one-line “minor fix only” confirm if the user allows).
5. **Preview with scan-derived dump data** — **Together with** the Word template, add or update a **`tools/PreviewWordReports`** preset that fills **dynamic** placeholders. **Dump values must be taken from the same reference scan** (transcribe visible text from the scan image into the preset so the preview resembles the real document). Use **longer stress-test strings** only where the map explicitly allows or after layout is verified. **Production** downloads do not use yellow highlighting; preview output is under **`PreviewWordReports/.../out/*_preview.docx`**.

**Blocking rule:** Phases **0–4** of the predetermined workflow must complete (including **prerequisite 4**) before **Phase 5** implementation. Treat the approved **`_map.md`** plus **`WORD_REPORT_GENERATION_IDEA.md`** and BO sources as the binding contract.

## Word vs XtraReports — alternative implementation, same reports

The Word pipeline (**Resminamalar**, **`WORD_REPORT_GENERATION_IDEA.md`**) is an **alternative way to produce the same ministry-facing outputs** that were (or could have been) built as DevExpress **XtraReports** in **`Visa2026.Module/Reports/`**. It is **not** a mandate to delete or rewrite every XAF report overnight.

- **Reality:** Roughly **all** of the reports the team cares about were **first designed as XtraReports**; the ongoing effort is to **redesign the same report** using Word + DocxTemplater (layout in **`Resources/*.docx`**, data in **`*ReportDef`**).
- **When redesigning:** Use the **existing XtraReport** (`*.Designer.cs`, ExpressionBindings, **`_map.md`** if present) as a **cross-check** for **which fields are dynamic** and how they were bound—then re-express that in **`IWordReportDefinition`** + template placeholders. **`report-predefined-xaf`** is the skill for **editing legacy XtraReport code**; **`visa2026-word-reports`** is for **Word delivery** of the same form.
- **Coexistence:** XtraReports can remain in the product until Word output is accepted for that application type; avoid duplicating conflicting business rules—prefer one source of truth on the BOs.

## Business object data anchors (no guessing)

Filled Word reports **always map template keys to data** drawn from domain types under **`Visa2026.Module/BusinessObjects/`**. Do **not** invent placeholder names or row sources—confirm against these types and **`WORD_REPORT_GENERATION_IDEA.md`** property tables.

| Type | Source file | Typical use in Word pipeline |
|------|-------------|------------------------------|
| **`Application`** | `Application.cs` | **Root** for **`WordReportsController`**: `GenerateAsync` receives the current **`Application`**. Header `{{ds.*}}` fields (dates, numbers, ministry text, company head, aggregates, project contract strings) almost always come from here or values computed in `*ReportDef` from `Application`. |
| **`ApplicationItem`** | `ApplicationItem.cs` | Per-person data: **`Person_*`**, **`Passport_*`**, **`WorkPermit_*`**, **`Invitation_*`**, addresses, signatories, etc. Used as **each row** in `FillListForm` (sanawy, item tables) or merged into a **single** `FillForm` when generating **one document per person**. |
| **`Registration`** | `Registration.cs` | Registration / check-in/out flows: person + travel + application snapshot fields. Row source when the report is a **list of registrations** on the application. |
| **`BusinessTrip`** | `BusinessTrip.cs` | Business-trip sanawy and related letters: row source **`application.BusinessTrips`**. |

**Controller contract today:** **`WordReportsController`** runs on **`DetailView`** with **`TargetObjectType = typeof(Application)`** — implementations assume **`Application`** is the root. If a future report must run from another view object, **do not assume**—ask the user and design an explicit controller/entry point.

### Missing information — stop and ask (prevent false implementation)

If **any** of the following is unclear from chat + repo, **ask the user before coding** (templates, defs, or presets):

1. **Header vs rows** — Which `{{ds.*}}` keys come from **`Application`** only vs from **each row object** (`{{.FieldName}}`)?
2. **Row collection** — Are rows **`ApplicationItems`**, **`Registrations`**, **`BusinessTrips`**, a **filtered** subset (e.g. non-deleted only), or a **single** item repeated in a loop inside `GenerateAsync`?
3. **One file vs many** — One combined `.docx` per application vs **one `.docx` per person** (or per trip) in a zip? (Pattern: multiple `FillForm` calls vs one `FillListForm`.)
4. **Property existence** — Does each template token match an **`[NotMapped]`** or persisted property (or a documented computed string in the def)? If the scan needs a **new** field, ask whether to add it to the BO / item type first.
5. **Ordering** — Required **sort order** for rows (e.g. by `Person_LastName`, `RowNumber`, trip date)?
6. **`IsApplicable`** — Exact guard (e.g. “only if `BusinessTrips.Any()`”, “only if at least one non-deleted item”)?
7. **Ambiguous similar reports** — If two existing defs differ subtly, which one is the **authoritative** pattern for this task?

After answers are recorded (or explicitly confirmed from context), implement. **Never** silently substitute a different collection or property path “because it looked close.”

## Layout families — categorize before you design

Reports are grouped by **layout similarity** so fonts, margins, and structure stay consistent and steps repeat. Each new Word report **must** map to a **family code** (or a documented new one).

| Code | Short name | When to use |
|------|------------|-------------|
| **L1** | Simple portrait letter | Migration-service block, justified body, standard signatory (`MakeSimpleLetterTemplate`). |
| **L2** | Ministry / Group A letter | Recipient block, urgency, greeting, attachments pattern (`MakeGroupALetterTemplate`). **Stepped ministry table header** (№/date, addressee table, optional urgency): clone **`MakeAppInvAndWPLetterTemplate`** / **`MakeAppVisaAndWPExtLetterTemplate`** + shared `FormalCompanyLetterLayout` / `AppendMinistry*` helpers — see **L2 — stepped ministry letters** below. |
| **L3** | Letter variant | Other portrait letters in `GenerateTemplates` with same **1800/1440** margin habit but custom blocks. |
| **T1** | Landscape sanawy | Multi-column personnel table; `{{#ds.rows}}` in table; header repeat / row split rules. |
| **F1** | Statutory item form | Tighter margins, underlines, mixed title/body/caption sizes (Borçnama-style). |
| **F2** | Sectioned item contract | Numbered sections, long static + merge fields (labor contract). |
| **T2** | Compact item table | Single main table, row loop, map-driven column widths (cancel/change/border items). |

**Letter category (L1–L3)** — company → organization, closed by the company head:

- **Body:** **`reference.md` → Letter category** — 720 twips first-line indent, justified, no leading spaces in literals; responsibility = `FormalCompanyLetterLayout.ResponsibilityPlain` (same text as `AppBaseReport.RtfResponsibility`).
- **Signatory block:** **`reference.md` → Signatory block (company head)** — borderless two-column table; **capacity / position** left (`{{ds.Application_CompanyHead_PositionTm}}`), **name** right (`{{ds.Application_CompanyHead_FullName}}`); both **15 pt bold**, top-aligned, with standard column widths and spacing from `FormalCompanyLetterLayout` + `AppendSignatoryLetter`. **Do not** hand-roll a different signatory layout for new L1–L3 letters unless the **FormTemplates** scan documents an exception.

**L2 — stepped ministry letters (`App_Inv_And_WP_Letter`, `App_Visa_And_WP_Ext_Letter`, close siblings)** — reuse this pattern before inventing a new header/body:

- **Clone** `MakeAppInvAndWPLetterTemplate` / `MakeAppVisaAndWPExtLetterTemplate` in **`tools/GenerateTemplates/Program.cs`**: `AppendMinistrySteppedHeaderWithUrgency` (pass **`conditionalUrgency: true`** when XAF hides urgency for Group C), `AppendMinistryRecipientBlockRightColumnTable`, shared **`FormalCompanyLetterLayout`** `InvAndWP*` spacing and **`AppInvAndWPPrintableWidthTwips`** for tables/signatory width.
- **Recipient data** — in **`*ReportDef`**, derive `ProjectContract_Ministry_RecipientBlock_Line1` / `_Line2` / `_HasLine2` via **`MinistryRecipientBlockFormatter.SplitIntoAddressLines`**; templates use **`{{ds.*}}`** only (no static ministry names).
- **Addressee block on the right** — full-width borderless **two-column** table: **wide spacer** + **address column width capped** (see **`reference.md` → Ministry addressee block**); both lines **`w:jc left`** inside the address cell so they share one left edge. Avoid two separate **right**-justified paragraphs (staggered left edges).
- **Single-page / no blank page 2** — **never** rely on an empty `<w:p>` only for vertical gap; Word still lays out ~one line per empty paragraph. Put gaps in **`w:spacing/@w:after`** on the previous real paragraph (e.g. merge header→salutation gap into the **urgency** paragraph; merge pre-signatory gap into the **last Goşundy** line).
- **Group C urgency** — template: `{?{ds.ApplicationType_ShowUrgency}}{{ds.Urgency_NameTm}}{{/}}`; def must supply **`ApplicationType_ShowUrgency`** and **`Urgency_NameTm`**.
- **Preview yellow** — if a numeric `ds` field can render as a **single digit** inside static Turkmen (e.g. Goşundy counts), add **phrase-level** composites in **`AddSingleDataComposites`** (`PreviewWordReports/Program.cs`) so highlights still match; do not lower global minimum match length.
- **Done criteria** — confirm **runtime** Resminamalar output (not only preview); record acceptance in **`review-status.md`** and the report’s **`*_map.md` / word_map** (see **`learnings.md`** for `App_Visa_And_WP_Ext_Letter` examples).

**Full matrix** (margins in twips, half-point sizes, example defs): **`reference.md`** in this folder.

**Standard defaults** (unless the **FormTemplates** scan clearly differs):

- **Font family:** Times New Roman everywhere in generated OOXML.
- **Page:** A4 portrait **11906 × 16838** twips; **T1** uses landscape with the same page size swapped in section props.
- **Do not** invent new margin or font sizes for each report—**clone the family** in `tools/GenerateTemplates/Program.cs`, then adjust only what the scan requires.

**Patternized design steps** (after family is chosen):

1. **Classify** — Record family code in the task / PR / `learnings.md` entry.
2. **Clone** — Start from the matching `Make*` helper or column array (same file).
3. **Scan diff** — Change only what the ministry image demands (spacing, one-off heading size).
4. **Bind** — Placeholders and `FillForm` / `FillListForm` shape unchanged by family rules.
5. **Preview** — `PreviewWordReports` + yellow merge check + side-by-side with scan.

## Review, refactor, and redesign (initialized templates in `Resources/`)

Most Word **`.docx`** are already generated and embedded under **`Visa2026.Module/Resources/`** (see **`Visa2026.Module.csproj`** `<EmbeddedResource Include="Resources\*.docx" />` and **`reference.md`** → *Inventory*). Use this skill to **audit** them and **standardize** layout and bindings—not to rewrite everything blindly in one pass.

### Start every review batch with the user

| | |
|---|---|
| **Ask** | Which **batch** should we tackle first (by **layout family**, by **ApplicationType**, or by **explicit file list**)? May we change **only** typography/layout to match `reference.md`, or also **placeholder keys** / `*ReportDef` behavior? Any reports **frozen** (no touch)? |
| **Need (response)** | User sets **priority**, **scope**, and **compatibility** (e.g. must keep existing downloaded filenames and zip behavior). |
| **Then** | Apply the per-template checklist below only within that scope. |

Work in **small batches** (e.g. all **L1** registration letters, then **T1** sanawy) so diffs stay reviewable. Append **`learnings.md`** after each batch.

**Tracker:** In **`review-status.md`**, set rows for this batch to **In review** when starting and **Completed** (or **Blocked** + note) when done. Keep **Last updated** and **Notes** current so the team sees what is left.

### Per-template checklist (refactor / redesign)

For each **`Resources\*.docx`** in scope:

1. **Trace** — Find **`IWordReportDefinition`** that references this template and the **`GenerateTemplates/Program.cs`** section that produces it (if code-generated). No orphan templates.
2. **Family** — Assign **L1–L3 / T1 / F1 / F2 / T2**; record deviations from **`reference.md`** (margins, `w:sz`). Plan to **align** to family baseline where the **FormTemplates** scan allows. For **L1–L3**, confirm **body** and **signatory block** match **`reference.md` → Letter category** (including **Signatory block**) unless the map documents a scan-specific exception.
3. **Scan + map parity** — Open **`FormTemplates/`** scan + **`*_map.md`**; if map is missing for this report, **create** it (**Prerequisites** + **`reference.md` → Map document checklist**) and obtain **user approval** before large redesigns. Cross-check legacy **XtraReport** if applicable. Run **`PreviewWordReports`**; compare layout to scan and **yellow** regions to map-listed dynamics.
4. **Binding audit** — List every `{{ds.…}}` / `{{.…}}` in the template; confirm each key is populated in the `*ReportDef` from **`Application` / `ApplicationItem` / `Registration` / `BusinessTrip`** (see **Missing information**). Remove dead placeholders or add missing dictionary entries after user confirms.
5. **Preview preset** — Ensure **`tools/PreviewWordReports/Program.cs`** has a preset with **stress-test** strings (long names, long addresses) for this template.
6. **Redesign** — Refactor **`GenerateTemplates`** to share **`Make*`** helpers where scans are structurally identical; avoid copy-paste drift. Regenerate **`.docx`** into **`Resources/`** and verify paths (see **`learnings.md`**).
7. **Verify** — `dotnet build Visa2026.slnx -c Debug`; user sign-off on sample output vs scan.

### When “redesign” means ministry form changed

Treat as **new scan**: Phase 2 (FormTemplates) + full layout diff; update **`_map.md`** first when columns change; then regenerate template and re-run binding audit.

## Predetermined workflow (ask, response, then act)

For **new** Word reports, **new** `IWordReportDefinition` classes, or **material** template changes (layout, new placeholders, applicability), follow these **phases in order**. Each **Ask** must get a **user response** before unlocking the next gate—**do not guess** ministry layout, `ApplicationType` names, **or which `Application` / `ApplicationItem` / `Registration` / `BusinessTrip` fields** feed each placeholder (see **Missing information** above).

Use Cursor **AskQuestion** when a step is naturally multiple-choice; otherwise ask in plain language and **stop until the user replies**. If the user already answered in the same turn, treat that as the response and state what you assumed.

**Exception:** Trivial fixes (e.g. one typo in static text, single obvious placeholder rename) need only a short confirm—“Confirm this is the only change?”—then implement.

### Phase 0 — Context (agent, no blocking question)

- Skim **`learnings.md`** (## Entries).
- Open **`review-status.md`** — see which templates are **Pending**, **In review**, **Completed**, or **Blocked**; do not duplicate work without checking.
- Use **`docs/WORD_REPORT_GENERATION_IDEA.md`** for placeholder tables and architecture.
- Open the relevant BO(s): **`Application.cs`**, **`ApplicationItem.cs`**, **`Registration.cs`**, **`BusinessTrip.cs`** — confirm properties exist for every field you plan to bind before writing templates or defs.

### Phase 1 — Identity, layout family, and goal

| | |
|---|---|
| **Ask** | Which report is this? If it **replaces or parallels** an existing **XtraReport**, which class name (`Visa2026.Module/Reports/…`)? Desired **output** (letter / per-item form / sanawy table)? Which **layout family** (**L1–L3**, **T1**, **F1**, **F2**, **T2** — see **Layout families** above) does it match? If none fit, can we define a new code and row in **`reference.md`**? Which **`ApplicationType.Name`** values should it apply to? **New** or change to existing `*ReportDef` / template? |
| **Need (response)** | User confirms purpose, **family code**, applicability, and pattern (or approves agent proposal by similarity to an existing report). |
| **Then** | Continue to **Phase 2** (scan + map). **Do not** implement templates or defs here—wait for **Phase 4** map approval. |

### Phase 2 — FormTemplates pack: scan + `*_map.md` + data types

| | |
|---|---|
| **Ask** | (1) **Basename** for map + assets (e.g. `App_Inv_And_WP_app`). (2) Is the **reference scan** already in **`FormTemplates/`**? Filename? If not, user adds/commits it or **waives** in writing. (3) Does **`FormTemplates/<basename>_map.md`** exist? If not, agent **drafts** it in the same turn or next, using **`reference.md` → Map document checklist**. |
| **Need (response)** | **Scan** in repo with stable name **or** waiver. **Map** file exists (or user approves agent-created draft). Map includes mandatory **Data type(s)** section (root + row types). **No template/generator coding** until Phase 4 approval. |
| **Then** | Continue to **Phase 3** (refine `FillForm` / rows / keys; update **`_map.md`**). Then **Phase 4 (map approval)**. If no scan and no waiver: **stop** until scan path is resolved. |

### Phase 3 — Data shape and business objects

| | |
|---|---|
| **Ask** | (1) **`FillForm`** only, **`FillListForm`** only, or **both** (e.g. header + rows)? (2) **Row source**: `Application.ApplicationItems`, `Application.Registrations`, `Application.BusinessTrips`, or other—**filter/sort**? (3) **Per-application vs per-row output**: single file, or one `.docx` per item/trip? (4) **Header keys**: list the main `Application` fields (or confirm “same as report X”). (5) **Row keys**: confirm they match **`ApplicationItem`** / **`Registration`** / **`BusinessTrip`** property names used in **`WORD_REPORT_GENERATION_IDEA.md`**. (6) **`IsApplicable`** rule? (7) Any **new** field needed on a BO for this scan? |
| **Need (response)** | User confirms each point **or** approves the agent’s written binding spec (quoted from an existing `*ReportDef`). If anything is still unknown, **ask again**—do not implement with guessed bindings. |
| **Then** | **Reflect** answers in **`FormTemplates/<basename>_map.md`** (data types + placeholder table). Proceed to **Phase 4**; **implement** `*ReportDef` dictionaries only in **Phase 5** after map approval. |

### Phase 4 — User approval of the map (blocking)

| | |
|---|---|
| **Ask** | Please **review** **`FormTemplates/<basename>_map.md`**: data types, reference scan name, placeholder ↔ property table, static vs dynamic, any Word-vs-XtraReport typography notes. **Approve** to proceed with `.docx` / `GenerateTemplates` / `*ReportDef`, or list edits. |
| **Need (response)** | **Explicit approval** of the map (or revised map + second approval). **No implementation** of Phase 5 until this gate clears. Trivial follow-ups (typo in static Turkmen in map only) may be bundled with a one-line “confirm” if the user allows. |
| **Then** | If bindings or placeholders change materially later, **update the map** and **re-confirm** before merge. |

### Phase 5 — Implementation

Execute **Workflow: add a new Word report** below (steps 0–10): template / `GenerateTemplates`, `csproj`, `*ReportDef`, `Startup.cs`, `PreviewWordReports` preset, regenerate, build. **Trace every placeholder** to the approved map and BO types.

### Phase 6 — Verification (user sign-off)

| | |
|---|---|
| **Ask** | Does **`PreviewWordReports`** output (or app download) **match the scan** side-by-side? In the preview file, are **yellow** regions exactly the BO-driven fields you expect? For one-page forms, does everything fit **one A4** with longest realistic text? Anything still wrong? |
| **Need (response)** | User confirms OK **or** lists fixes; iterate Phase 5–6 until accepted. |
| **Then** | Treat user acceptance as the layout gate before merge. |

### Phase 7 — Learnings

Append **`learnings.md`** (see **Continuous improvement**).

---

## Continuous improvement — read `learnings.md`, then append

This skill **accumulates experience** in **`learnings.md`** (same folder as this file).

1. **Before** substantial work on a Word report (new template, new `*ReportDef`, or non-trivial layout change): **read** **`learnings.md`** — skim **## Entries** for related templates, DocxTemplater table loops, `GenerateTemplates`, or `PreviewWordReports` issues.
2. **After** the task is complete (user sign-off or merge-ready): **append** a new dated entry using the template at the top of **`learnings.md`** (symptom, root cause, fix, prevent). One entry per report batch is enough; split if multiple unrelated lessons.
3. If the same lesson appears **again**, consider **promoting** a short bullet into this **`SKILL.md`** so every run sees it without opening the log.

Do not rewrite or delete past entries — **append only**.

## FormTemplates — scans, maps, static vs dynamic (design and review)

Everything under **`Visa2026.Module/Resources/FormTemplates/`** is the **ministry reference pack** for Word (and historically XtraReports): **scanned real documents** (or official exports) plus **`*_map.md`** contracts. Use this folder **whenever** you design, compare, or review a Word report—not only for layout. See **Prerequisites for starting Word report design** above.

### Scanned images (`.png`, `.jpg`, etc.)

- **Role:** **Visual authority** for structure and typography: static text, spacing, alignment, and what the final output should look like. Dynamic slots are inferred from the map + BO data, not from OCR of the scan.
- **Naming:** Prefer a stable basename shared with the map (e.g. `App_Inv_And_WP_app.jpg` with `App_Inv_And_WP_app_map.md`).
- **Review:** Side-by-side with **`PreviewWordReports`** or production output: static blocks should match; **yellow** preview highlights should cover **only** intended **dynamic** regions.

### `*_map.md` files (required for new design; create if missing)

- **Role:** **Binding and design contract**: identity, **data type(s)** (which BO(s) feed the report), reference scan filename(s), **placeholder ↔ property** table, static vs dynamic regions, column widths / bands where relevant, and notes when Word output intentionally differs from XtraReport RTF.
- **Not optional for new reports:** If **`FormTemplates/<basename>_map.md`** is missing, **create it** (use **`reference.md` → Map document checklist**) **before** writing `GenerateTemplates` sections or embedding new **`Resources/*.docx`**.
- **User approval:** The map is **approved by the user** before implementation (**Phase 4**). The shipped template and `*ReportDef` must **implement the approved map**; drift requires a map update and re-approval (or explicit user OK for minor fixes).

**Non-negotiable:** When a scan exists for a report, the shipped **`.docx`** must **visually match** that scan (within agreed tolerances). When **no** scan is in repo, **prompt the user** for the official image/PDF before locking layout (or document a written waiver in the map).

**Workflow:**

1. **Locate or create** **`FormTemplates/<basename>_map.md`** and the **reference scan** beside it (same folder).
2. **Fill** **Data type(s)** and placeholder tables; link to **`Application` / `ApplicationItem` / …** as in **`WORD_REPORT_GENERATION_IDEA.md`**.
3. **Get user approval** of the map (**Phase 4**).
4. Use the **map** to list **dynamic** vs **static** content; confirm each dynamic field has a BO property and a template key.
5. Keep **`GenerateTemplates`** / template OOXML in sync with map **geometry** and field list.
6. **Compare** preview output to scan (layout) and to map (field completeness); use **XtraReport** + map as a **second opinion** when redesigning an existing report.

## Architecture (do not reinvent)

| Piece | Location / role |
|--------|------------------|
| Fill API | `Visa2026.Module/Services/IWordFormFillerService.cs` — `FillForm` (single record) vs `FillListForm` (header + `rows`) |
| Implementation | `Visa2026.Module/Services/WordFormFillerService.cs` — model bound as **`ds`** |
| One definition per report | `Visa2026.Module/Services/WordReports/*ReportDef.cs` implementing **`IWordReportDefinition`** |
| UI + zip download | `Visa2026.Module/Controllers/WordReportsController.cs` — **Resminamalar**; filters by `ApplicableApplicationTypeNames` + `IsApplicable`; zips when 2+ reports apply |
| Templates | `Visa2026.Module/Resources/*.docx` — **EmbeddedResource** in `Visa2026.Module.csproj` |
| Reference scans + maps | `Visa2026.Module/Resources/FormTemplates/` — scans = **static** layout/text; **`*_map.md`** = **dynamic** fields, widths, BO mapping; compare with XAF report when redesigning |
| DI | `Visa2026.Blazor.Server/Startup.cs` — register each `IWordReportDefinition` + existing `IWordFormFillerService` |

## DocxTemplater contract (runtime)

- **Single-record:** keys in the dictionary → placeholders **`{{ds.Key}}`** (see `FillForm`).
- **Tabular:** header keys → **`{{ds.Key}}`**; loop **`{{#ds.rows}}` … `{{/ds.rows}}`**; inside the loop row fields → **`{{.FieldName}}`** (see `FillListForm`).
- **Between repeated persons/items only:** **`{{:s:}}{{:PageBreak}}`** on its own paragraph (library keyword; not a raw manual page break only), as used in multi-row item templates.
- **Table templates:** `{{#ds.rows}}` and `{{/ds.rows}}` must each live in **its own Word paragraph** (`<w:p>`), not mixed with other text in the same paragraph, or literals appear instead of expansion.
- **Long tables:** header row needs **repeat on each page**; data rows should avoid ugly splits — apply the patterns documented in **WORD_REPORT_GENERATION_IDEA.md** (`tblHeader`, `cantSplit`, `RowNumber` where applicable).

## Preview tool — dump data and yellow “dynamic” fields (dev only)

**`tools/PreviewWordReports`** fills the same **`.docx`** templates as production using the **same DocxTemplater binding** as **`WordFormFillerService`** (`ds` / `ds.rows`). Presets live in **`Program.cs`**.

**Dump data source (prerequisite 5):** For a new or redesigned report, **populate each preset from the reference scan** in **`FormTemplates/`**—transcribe the visible **dynamic** values (numbers, names, dates, ministry lines, row cells, etc.) from that image into the preset dictionary so the filled preview **matches the sample document** for QA. The map’s field table tells you which scan fragments map to which `ds.*` / row keys. **Optional:** After scan parity passes, add stress-test variants (longer names, addresses) if the map or user asks for overflow QA. **Automated OCR** is not assumed unless the project adds a tool; default is **human transcription from the scan**.

**Yellow highlight (preview only):** After merge, **`ApplyDumpDataHighlights`** finds preset strings (length ≥ 2, longest-first, non-overlapping) inside each paragraph’s plain text (including `w:br` → newline) and yellow-highlights runs that overlap those spans. **Numeric** bind values are included when their string form has length ≥ 2 (single-digit counts use composites like `n (text)` / `n-daşary`). **Composite** strings (e.g. `pasporty ` + passport line, `ApplicationDate` + ` ý.`) cover split or templated fragments.

**Production:** **`WordReportsController` / `WordFormFillerService`** do **not** apply highlighting — only **`PreviewWordReports/.../out/*_preview.docx`**.

Use the preview **side-by-side with the scan**: layout and typography from the template, **yellow** on dynamic slots, literal content aligned with the scan where those fields appear on the image.

## Workflow: add a new Word report

Follow **Predetermined workflow (ask, response, then act)** above first: complete **Phases 0–4** (scan + **`*_map.md`** + data types + **explicit map approval**) before **Phase 5** implementation.

0. **Prerequisites** — Satisfy **Prerequisites for starting Word report design** (1–5) and **Phases 0–4**; **create `*_map.md` if missing** under **`FormTemplates/`** next to the reference scan.
1. **Confirm scope** — New report = Word pipeline per project decision. Do not replace working XtraReports unless explicitly requested.
2. **Name and applicability** — Choose `ApplicableApplicationTypeNames` (usually `ApplicationType.Name` strings). Implement **`IsApplicable(Application)`** for guards (e.g. non-empty child collection).
3. **Data shape** — After Phase 3 answers: mirror an existing `*ReportDef.cs`; build `Dictionary<string, object>` for header and/or per-row dictionaries with keys that **exist** on **`Application`** / **`ApplicationItem`** / **`Registration`** / **`BusinessTrip`** per **`docs/WORD_REPORT_GENERATION_IDEA.md`** and BO source files—**no orphan placeholders**.
4. **Template** — Add `.docx` under **`Visa2026.Module/Resources/`**. **Design against `FormTemplates/`** (scan + `*_map.md` when present). Use the chosen **layout family** defaults from **`reference.md`** / **`Make*`** helpers in **`tools/GenerateTemplates/Program.cs`**; prefer **code generation** (OpenXml) so layout is reviewable in git; hand-authored Word is OK if it follows the DocxTemplater paragraph rules and still matches the scan.
5. **csproj** — For each file: `<None Remove="Resources\…" />` + `<EmbeddedResource Include="Resources\…" />` (match siblings).
6. **Definition class** — New `…ReportDef.cs` implementing `IWordReportDefinition`: embedded resource name, `GetFileName`, `GenerateAsync` calling `FillForm` / `FillListForm`.
7. **DI** — One line in **`Startup.cs`**: `services.AddScoped<IWordReportDefinition, YourReportDef>();`
8. **Preview** — Add or update a preset in **`tools/PreviewWordReports/Program.cs`**. **Dump values: transcribe from the reference scan** (prerequisite 5). Run:
   - `dotnet run --project tools/PreviewWordReports/PreviewWordReports.csproj -c Debug -- list`
   - `dotnet run --project tools/PreviewWordReports/PreviewWordReports.csproj -c Debug -- <preset>`
   - Output under `tools/PreviewWordReports/bin/Debug/net8.0/out/`. **Close Word** before re-run if the file is locked.
9. **Regenerate templates** — After changing **`GenerateTemplates`**:
   - `dotnet run --project tools/GenerateTemplates/GenerateTemplates.csproj -c Debug`
   - Optional: pass specific output paths as documented in **WORD_REPORT_GENERATION_IDEA.md**.
   - Confirm updated bytes land in **this repo’s** `Visa2026.Module/Resources/` (not a path that escapes the repo tree).
10. **Learn** — Append an entry to **`learnings.md`** (see **Continuous improvement** above).

## Layout and QA

- **Scan parity:** Final check = filled preview (or production output) **next to** the **`FormTemplates`** scan; adjust until structure and typography match (including rules staying inside page margins like the original).
- **Maps:** Keep **`Visa2026.Module/Resources/FormTemplates/*_map.md`** aligned with column widths / fields that drive generators (see Business Trip sanawy comments in `GenerateTemplates/Program.cs`).
- **Pagination:** Legal one-page forms — tighten margins, line spacing, and paragraph spacing in the generator (or template) and verify with **PreviewWordReports** and longest realistic strings.
- **Turkmen copy** — Match the scan and ministry wording; static text belongs in the template or generator, not scattered in the controller.

## Build note

`dotnet build Visa2026.slnx -c Debug` after Module + host changes. If the Module DLL is locked by a running Blazor/VS host, stop the host and rebuild.
