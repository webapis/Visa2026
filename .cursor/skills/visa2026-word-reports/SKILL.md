---
name: visa2026-word-reports
description: >-
  Creates and maintains Visa2026 Word (.docx) reports via DocxTemplater, IWordReportDefinition,
  WordReportsController ("Resminamalar"), tools/GenerateTemplates, tools/PreviewWordReports,
  FormTemplates, layout families, and binding from Application, ApplicationItem, Registration,
  BusinessTrip. FormTemplates scans and *_map.md for static vs dynamic field review. Word as an
  alternative (not full replacement) to existing XAF XtraReports—redesigning the same ministry
  outputs in Docx. Asks for missing binding context. Batched Resources/*.docx refactor; review-status.md.
  See docs/WORD_REPORT_GENERATION_IDEA.md. Use report-predefined-xaf only for maintaining legacy
  XtraReports code; PdfFormFillerService is separate.
disable-model-invocation: false
---

# Visa2026 — Word report pipeline (DocxTemplater)

## Canonical reference

Read and follow **`docs/WORD_REPORT_GENERATION_IDEA.md`** for architecture, placeholder tables (`Application`, `ApplicationItem`, `Registration`, `BusinessTrip`), and how the Word pipeline fits next to XtraReports.

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
| **L2** | Ministry / Group A letter | Recipient block, urgency, greeting, attachments pattern (`MakeGroupALetterTemplate`). |
| **L3** | Letter variant | Other portrait letters in `GenerateTemplates` with same **1800/1440** margin habit but custom blocks. |
| **T1** | Landscape sanawy | Multi-column personnel table; `{{#ds.rows}}` in table; header repeat / row split rules. |
| **F1** | Statutory item form | Tighter margins, underlines, mixed title/body/caption sizes (Borçnama-style). |
| **F2** | Sectioned item contract | Numbered sections, long static + merge fields (labor contract). |
| **T2** | Compact item table | Single main table, row loop, map-driven column widths (cancel/change/border items). |

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
2. **Family** — Assign **L1–L3 / T1 / F1 / F2 / T2**; record deviations from **`reference.md`** (margins, `w:sz`). Plan to **align** to family baseline where the **FormTemplates** scan allows.
3. **Scan + map parity** — Open **`FormTemplates/`** scan (static reference) + **`_map.md`** (dynamic field contract); cross-check legacy **XtraReport** if this Word report is a redesign. Run **`PreviewWordReports`**; compare layout to scan and **yellow** regions to map-listed dynamics.
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
| **Then** | Implement using that family’s typography and margins unless the scan dictates a documented exception. |

### Phase 2 — Reference scan (FormTemplates)

| | |
|---|---|
| **Ask** | Is the **official scanned form** (or PDF) already in **`Visa2026.Module/Resources/FormTemplates/`**? If yes, **which filename**? If no, please add it or attach it—what name should we use under `FormTemplates/`? |
| **Need (response)** | Scan available in repo **or** user uploads / commits path **or** user explicitly waives (e.g. letter with no ministry scan—user states that in reply). |
| **Then** | If no scan and no waiver: **stop layout work** until Phase 2 is satisfied (see **FormTemplates** section below). |

### Phase 3 — Data shape and business objects

| | |
|---|---|
| **Ask** | (1) **`FillForm`** only, **`FillListForm`** only, or **both** (e.g. header + rows)? (2) **Row source**: `Application.ApplicationItems`, `Application.Registrations`, `Application.BusinessTrips`, or other—**filter/sort**? (3) **Per-application vs per-row output**: single file, or one `.docx` per item/trip? (4) **Header keys**: list the main `Application` fields (or confirm “same as report X”). (5) **Row keys**: confirm they match **`ApplicationItem`** / **`Registration`** / **`BusinessTrip`** property names used in **`WORD_REPORT_GENERATION_IDEA.md`**. (6) **`IsApplicable`** rule? (7) Any **new** field needed on a BO for this scan? |
| **Need (response)** | User confirms each point **or** approves the agent’s written binding spec (quoted from an existing `*ReportDef`). If anything is still unknown, **ask again**—do not implement with guessed bindings. |
| **Then** | Implement dictionaries exactly from **`Application`** / child types; mirror an existing def when possible. |

### Phase 4 — Map and field contract (when applicable)

| | |
|---|---|
| **Ask** | For sanawy / wide tables / complex forms: should we **draft or update** **`FormTemplates/*_map.md`** and pause for your approval before locking column widths and generator math? |
| **Need (response)** | User says map-first **yes** (wait on approval) **or** **no** / N/A for a simple one-page letter with no map. |
| **Then** | If map-first yes: draft map, present, **wait** before coding generator columns to match. |

### Phase 5 — Implementation

Execute **Workflow: add a new Word report** below (steps 0–10): template / `GenerateTemplates`, `csproj`, `*ReportDef`, `Startup.cs`, `PreviewWordReports` preset, regenerate, build.

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

Everything under **`Visa2026.Module/Resources/FormTemplates/`** is the **ministry reference pack** for Word (and historically XtraReports): **scanned images** plus optional **`*_map.md`** files. Use this folder **whenever** you design, compare, or review a Word report—not only for layout.

### Scanned images (`.png`, etc.)

- **Role:** **Static** ministry text, typography, rules, spacing, and overall structure. What you **do not** merge from the database should still match the scan character-for-character where required.
- **Review:** Side-by-side with **`PreviewWordReports`** or production output: static blocks should match; **yellow** preview highlights should cover **only** intended **dynamic** regions.

### `*_map.md` files

- **Role:** Machine- and human-readable **contract**: **field names**, which cells/lines are **data (dynamic)** vs **fixed (static)**, column widths, band layout, ignored chrome, and links to the BO shape. Same maps often informed **XtraReport** design; they should **drive or verify** **`GenerateTemplates`** and **`{{ds.*}}` / `{{.…}}`** placeholders.
- **Dynamic data source:** Maps list **what** is filled from **`Application` / `ApplicationItem` / …** (or equivalent labels)—use them to **audit** `*ReportDef` dictionaries and prevent wrong property bindings when **redesigning from XAF**.
- **Review:** When refactoring, open the **`_map.md`** next to the Word template and XAF report (if any); reconcile all three so dynamic fields, static text, and geometry stay aligned.

**Non-negotiable:** When a scan exists for a report, the shipped **`.docx`** must **visually match** that scan (within agreed tolerances). When **no** scan is in repo, **prompt the user** for the official image/PDF before locking layout.

**Workflow:**

1. **Locate** the scan + **`_map.md`** (if any) for this form under **`FormTemplates/`**.
2. **Missing scan** — stop and prompt (see **Phase 2**).
3. Use the **map** to list **dynamic** vs **static** content; confirm each dynamic field has a BO property and a template key.
4. Keep **`GenerateTemplates`** / template OOXML in sync with map **geometry** and field list.
5. **Compare** preview output to scan (layout) and to map (field completeness); use **XtraReport** + map as a **second opinion** when redesigning an existing report.

Once the user provides assets, store under **`FormTemplates/`** and update or create **`_map.md`** before locking design.

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

**`tools/PreviewWordReports`** fills the same **`.docx`** templates as production using the **same DocxTemplater binding** as **`WordFormFillerService`** (`ds` / `ds.rows`). Presets live in **`Program.cs`**; each preset supplies **sample strings** shaped like the dictionaries built from **`Application` / `ApplicationItem` / …** in real `*ReportDef` code.

**Not OCR:** Sample text is **not** read from the scanned image. The **layout** is aligned to **`FormTemplates/`** scans; the **dump values** are chosen by developers (long names, long registry lines, etc.) so layout can be stress-tested.

**Yellow highlight (preview only):** After merge, an **Open XML** pass (**`ApplyDumpDataHighlights`**) marks text that **equals** the preset’s sample values (string values length ≥ 4, longest-first match) with **`w:highlight`** yellow and light **`w:shd`** so you can see what will be **replaced from business object properties at runtime** versus **static** Turkmen template text. **Composite** strings (e.g. `pasporty ` + passport line) cover lines where the label stays static in the template.

**Production:** Downloads from **`WordReportsController` / `WordFormFillerService`** do **not** apply this highlighting — only files under **`PreviewWordReports/.../out/*_preview.docx`**.

Use this when comparing a filled preview **next to** the ministry scan: structure from the scan, **yellow** shows merged “slots” for BO-driven data.

## Workflow: add a new Word report

Follow **Predetermined workflow (ask, response, then act)** above first (Phases 0–4) so questions are answered before heavy implementation.

0. **Reference scan** — Per **FormTemplates** rules above: if no scan exists, **prompt the user** for one before layout work.
1. **Confirm scope** — New report = Word pipeline per project decision. Do not replace working XtraReports unless explicitly requested.
2. **Name and applicability** — Choose `ApplicableApplicationTypeNames` (usually `ApplicationType.Name` strings). Implement **`IsApplicable(Application)`** for guards (e.g. non-empty child collection).
3. **Data shape** — After Phase 3 answers: mirror an existing `*ReportDef.cs`; build `Dictionary<string, object>` for header and/or per-row dictionaries with keys that **exist** on **`Application`** / **`ApplicationItem`** / **`Registration`** / **`BusinessTrip`** per **`docs/WORD_REPORT_GENERATION_IDEA.md`** and BO source files—**no orphan placeholders**.
4. **Template** — Add `.docx` under **`Visa2026.Module/Resources/`**. **Design against `FormTemplates/`** (scan + `*_map.md` when present). Use the chosen **layout family** defaults from **`reference.md`** / **`Make*`** helpers in **`tools/GenerateTemplates/Program.cs`**; prefer **code generation** (OpenXml) so layout is reviewable in git; hand-authored Word is OK if it follows the DocxTemplater paragraph rules and still matches the scan.
5. **csproj** — For each file: `<None Remove="Resources\…" />` + `<EmbeddedResource Include="Resources\…" />` (match siblings).
6. **Definition class** — New `…ReportDef.cs` implementing `IWordReportDefinition`: embedded resource name, `GetFileName`, `GenerateAsync` calling `FillForm` / `FillListForm`.
7. **DI** — One line in **`Startup.cs`**: `services.AddScoped<IWordReportDefinition, YourReportDef>();`
8. **Preview** — Add a preset in **`tools/PreviewWordReports/Program.cs`** with representative dump rows/header (see **Preview tool** above — output gets **yellow** on merged sample values only). Run:
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
