---
name: visa2026-user-report-templates
description: >-
  User report template seeds — Word (.docx) under Visa2026.Module/Resources/Templates/ and Excel (.xlsx) under
  Templates/Excel/ — embed, EnsureTemplateExists / EnsureExcelTemplateExists, placeholder lookup, ExcelMergeMode,
  Word template families (AppScalar / ItemRows / ItemRoster / ItemScalar — confirm layout with user; see
  reference-template-families.md), mandatory basename_map.md + co-located scan before any template work (see
  reference-map-contract.md), deterministic generation (docs/USER_REPORT_MAP_STANDARD.md §0–§15,
  reference-deterministic-generation.md), [NotMapped] BO properties, Word photo placeholders via WordUserReportImageInjector
  ({{IMAGE:…}}). Never edits Word/Excel layout in repo. Use for user template seeds, Resminamalar visibility,
  placeholder mapping, photo rosters, Forma_16/Borcnama-style output, or post-authoring checklist (embed,
  Extract/Validate, Active, test) after manual Word/Excel design (not code-backed Resources/App_*.docx).
disable-model-invocation: false
---

# Visa2026 — User Report Templates (embed + register seeds)

## Hard boundaries (always enforce)

### Directory — `Resources/Templates` (+ `Templates/Excel`)

| Format | Seed folder | Updater call |
|--------|-------------|--------------|
| **Word** | **`Visa2026.Module/Resources/Templates/*.docx`** | **`EnsureTemplateExists`** (Word extractor/validator) |
| **Excel** | **`Visa2026.Module/Resources/Templates/Excel/*.xlsx`** | **`EnsureExcelTemplateExists`** → same core with **`TemplateOutputFormat.Excel`** |

- **In scope:** user-report **seed** templates above, wired through **`UserReportTemplateUpdater`**. Same **`UserReportTemplate`** BO, **Extract** / **Validate**, **Resminamalar** zip (mixed `.docx` + `.xlsx`).
- **Out of scope:** Any other `.docx` under **`Visa2026.Module/Resources/`** (e.g. `App_Inv_Letter.docx`, `App_Labor_Contract_Item.docx`) — **`visa2026-word-reports`** / **`IWordReportDefinition`**. **`Visa2026.DataImporter/data.xlsx`** and other import spreadsheets.
- **Do not** add embedded resources or updater seeds outside **`Templates/`** or **`Templates/Excel/`**.

### `*_map.md` + scan (blocking — before any other skill step)

**Do not** advise placeholders, embed, register seeds, or “fix” a user template until the **map trio** exists for that basename (or the agent is **only** drafting the map from a newly supplied scan).

| File | Path |
|------|------|
| Map | **`Resources/Templates/<Basename>_map.md`** (Excel: **`Templates/Excel/<Basename>_map.md`**) |
| Scan | **`<Basename>.png`** / **`.jpg`** in the **same folder** as the map and `.docx` |
| Template | **`<Basename>.docx`** / **`.xlsx`** — authored **after** map **Approved** |

**Order:** (1) user provides scan → (2) agent writes **`<Basename>_map.md`** from scan (use **`Templates/_map_TEMPLATE.md`**) → (3) **user approves** map → (4) user authors Word/Excel → (5) embed/register.

**No scan** → stop and request it; rare **waiver** must be recorded in the map **Waiver** section and stated in chat.

Full rules: **`reference-map-contract.md`**. Legacy seeds without maps: **backfill before** material changes (see legacy table there).

### Deterministic generation (same input → same output)

All **`*_map.md`** files must follow **`docs/USER_REPORT_MAP_STANDARD.md`** (sections **§0–§15**, fixed order). Copy **`Templates/_map_TEMPLATE.md`**. No alternate map shapes.

| Requirement | Where |
|-------------|--------|
| **Exact placeholder tokens** | Map §6 — copy-paste into Word/Excel; Extract must match §7 |
| **Template family + loops** | Map §1–§2, §7 — drives `UserReportGenerator` / Excel generator |
| **Golden values** | Map §12 — transcribed once from scan; preview/QA only |
| **Map version** | Map §0 — bump on any §6/§7 change; re-Extract template |
| **Verification** | Map §11 — Extract, Validate, preview, visual compare to scan |

Details: **`reference-deterministic-generation.md`**. Agents **must not** suggest tokens that are not listed in the approved map §6.

### Layout and format — never touch

- The agent **must not** change **Word** formatting/layout in `.docx` or **Excel** sheet structure (column widths, merged cells on the data row, styles) in `.xlsx`.
- **Forbidden:** unzip/edit `word/document.xml`; rebuild `.xlsx` with **`ZipFile.CreateFromDirectory`** / **`Compress-Archive`** (breaks ClosedXML — see **`Resources/Templates/Excel/README.md`**); run **`GenerateTemplates`** on user seeds; “fix” ministry layout in the repo.
- **Allowed:** Tell the user **which placeholder token to type** in Word or Excel; user commits the binary. Agent only **embeds**, **registers**, and **maps data** in C#.

### Word vs Excel (authoring)

| | Word | Excel |
|--|------|--------|
| **Design tool** | Microsoft Word | Microsoft Excel |
| **Merge engine** | DocxTemplater (text) + **`WordUserReportImageInjector`** for `{{IMAGE:…}}` photos | ClosedXML (values + row copy only; **no** inline images in v1) |
| **Seed path** | `Resources/Templates/<Name>.docx` | `Resources/Templates/Excel/<Name>.xlsx` |
| **Manifest** | `Visa2026.Module.Resources.Templates.<file>.docx` | `Visa2026.Module.Resources.Templates.Excel.<file>.xlsx` |
| **Placeholder doc** | **`docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md`** | **`docs/EXCEL_PLACEHOLDER_REFERENCE.md`** (+ same property names) |
| **Typical root BO** | `Application` or `ApplicationItem` | **`ApplicationItem`** + **`ExcelMergeMode.ItemList`** (v1) |
| **List pattern** | `{{#ds.rows}}` in its own paragraph | `{{#ds.rows}}` on the **template data row**; `{{.Column}}` in cells |

### Word photos (`byte[]`) — custom injector (required)

**Do not** use DocxTemplater **`:img()`** or **`DocxTemplater.Images`** for user-report seeds — they are unreliable in this repo. Photos use a **two-step** pipeline:

1. **DocxTemplater** — merges text (`{{ds.*}}`, loops, page breaks).
2. **`WordUserReportImageInjector`** — after merge, replaces photo markers with inline PNG/JPEG (`Open XML`).

| Author types in Word | Data | Runtime |
|----------------------|------|---------|
| **`{{IMAGE:Person_Photo}}`** (preferred) | `byte[]` on **`ApplicationItem`** (or header key on bind model) | One marker per row in document order; empty `byte[]` → blank cell |
| Legacy `{{…Person_Photo:img(…)…}}` | same | Still detected by injector |
| Plain `{{.Person_Photo}}` | — | **Forbidden** — prints `System.Byte[]` |

**Loop templates** (`{{#ds.ApplicationItems}}` or `{{#ds.rows}}`): put **`{{IMAGE:Person_Photo}}`** in the photo table cell; set **row height / column width** in Word (e.g. portrait ~35×45 mm). **`Application`** root + **`ApplicationItems`** collection → merge uses **`ApplicationItemPhotoMergeRow`** (`UserReportGenerator.GetCollectionData`).

**Photo roster (canonical pattern)** — reference seed **`Employee_Photo_Roster_Sample.docx`**:

| Item | Value |
|------|--------|
| **Root BO** | **`Application`** (not `ApplicationItem`) |
| **Header** | `{{ds.FullApplicationNumber}}` |
| **Loop** | `{{#ds.ApplicationItems}}` … `{{/ds.ApplicationItems}}` — markers in **their own paragraph** |
| **Name column** | **`{{.Person_FullName}}`** inside the loop — **not** `{{ds.ApplicationItems.Person_FullName}}` (DocxTemplater “could not be replaced”) |
| **Photo column** | **`{{IMAGE:Person_Photo}}`** — **not** `:img()`, **not** `{{.Person_Photo}}` |
| **Multi-person** | Optional `{{:s:}}{{:PageBreak}}` between items |
| **Data** | `Person.Photo` → `ApplicationItem.Person_Photo` (`byte[]`); empty photo → blank cell after inject |

**Runtime (Resminamalar):** `WordReportBundleBuilder` loads items with **`UserReportMergeDataHelper.GetActiveApplicationItems(objectSpace, application)`** and passes them into **`UserReportGenerator.GenerateAsync(..., applicationItems)`**. **`GetCollectionData`** must use that list (not only `application.ApplicationItems` navigation — partial load caused wrong row counts / missing photo bytes). After DocxTemplater: **`WordUserReportMergeImageExtractor.FromBindData`**, fallback **`FromApplicationItems`**, then **`WordUserReportImageInjector`** whenever the template contains **`{{IMAGE:`** (placeholder rows or embedded bytes scan) — injector still runs with empty photos to **clear** literal tokens.

**Validate / Extract:** strip `:img` suffix via **`UserReportPlaceholderBindingHelper`**; row tokens like **`{{.Person_FullName}}`** on **`Application`** root validate against **`ApplicationItem`**. **`IMAGE:Person_Photo`** validates against **`ApplicationItem.Person_Photo`**.

**Preview without Blazor:** `dotnet run --project tools/PreviewWordReports -- employee-photo-roster` (sample PNGs in **`tools/PreviewWordReports/SamplePhotos/`**). Preview OK but app shows literal `{{IMAGE:…}}` → host still on old **`Visa2026.Module.dll`** — stop Blazor/VS, rebuild, restart.

**Code (Module only):** `WordUserReportImageInjector.cs`, `WordUserReportMergeImageExtractor.cs`, `ApplicationItemPhotoMergeRow.cs`, hook in **`UserReportGenerator.RenderTemplateAsync`**. **Do not** add **`DocxTemplater.Images`** for user templates.

**Docs:** **`docs/USER_TEMPLATE_AUTHOR_GUIDE.md`** (Photos), **`docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md`** (`{{IMAGE:Person_Photo}}`).

## Scope

- **User:** Authors **`.docx`** in Word or **`.xlsx`** in Excel (layout + placeholders); commits binaries under **`Templates/`** or **`Templates/Excel/`**.
- **Agent (three jobs):**
  1. **Embed + register** — **`csproj`** embedded resource + **`EnsureTemplateExists`** (Word) or **`EnsureExcelTemplateExists`** (Excel).
  2. **Update seed registration** — edit an existing updater block (metadata, applicability, **`resourceName`**, **`boType`**, **`excelMergeMode`**).
  3. **Placeholder lookup / implement** — Word/Excel reference docs; for Excel list columns also **`UserReportMergeDataHelper.BuildExcelItemListRowDictionary`** (see **Adding a missing placeholder**).

For **code-backed** Word reports (`Resources/*.docx` outside **Templates**, `IWordReportDefinition`, `GenerateTemplates`, `PreviewWordReports`), use **`visa2026-word-reports`**.

**Copy-paste chat prompts:** **`prompts.md`** in this skill folder (create/update seeds, visibility, placeholders, full template).

## Prerequisites gate (run first)

When this skill applies to basename **`<B>`**:

1. **`Templates/<B>_map.md`** (or Excel path) exists → if **no**, demand **`<B>.png`/`.jpg`** scan, then offer to create map from **`_map_TEMPLATE.md`**.
2. **`<B>.png`/`.jpg`** exists (or waiver in map) → if **no**, **stop** — no placeholder list from guessing.
3. Map **Status** is **Approved** (or user explicitly approves in chat this session) → if **Draft**, present map and wait — no `.docx` authoring instructions for production tokens.
4. Then **template family** checklist (**`reference-template-families.md`**) and Extract/validate against **`<B>.docx`** when present.

## Template family — confirm with user (blocking)

**Do not infer** layout from **`RootBoType = ApplicationItem`** alone. **`Borcnama.docx`** uses **`ApplicationItem`** validation but **`ItemRows`** merge (`{{#ds.rows}}` / `{{ds.rows.*}}`), not bare `{{ds.Person_FullName}}`.

Read **`reference-template-families.md`** for the full matrix, filename hints (`*_ItemRows.docx`, `*_App.docx`, …), legacy seed table, and Extract consistency checks.

**Before** embed, register, or telling the user which placeholders to type:

1. Present the **agent checklist** from **`reference-template-families.md`** (layout ID, root BO, Resminamalar output, placeholder style, photos).
2. **Wait for explicit user answers** — if unclear, use **`AskQuestion`** or numbered questions in chat.
3. After Word authoring, run **Extract** and verify tokens match the chosen layout (e.g. `ItemRows` must have `{{#ds.rows}}`).
4. Record layout in the seed **`description`** (e.g. `Word layout: ItemRows; one doc, page per ApplicationItem`) until a **`WordMergeLayout`** DB field exists (phase 2).

| Layout ID | One Resminamalar `.docx` contains | Typical placeholders |
|-----------|-----------------------------------|----------------------|
| **`AppScalar`** | One application letter/form | `{{ds.*}}` on **`Application`** |
| **`ItemRows`** | Same layout repeated per person (page breaks) | `{{#ds.rows}}` + `{{ds.rows.*}}` or `{{.}}` |
| **`ItemRoster`** | One table/list of all items | `{{#ds.ApplicationItems}}` + `{{.}}` / `{{IMAGE:…}}` |
| **`ItemScalar`** | One person (single-item bind) | `{{ds.*}}` on **`ApplicationItem`** — zip uses **`Application`** today; confirm with user |

## After authoring (Word / Excel) — checklist for successful generation

Use this **after** the user finishes layout and placeholders in Word or Excel. Merge only runs when the app has the file, placeholders **validate**, and visibility matches the open application.

### 1. Authoring (before leaving Office)

| Check | Word | Excel (list / ItemList) |
|-------|------|-------------------------|
| Token prefix | `{{ds.PropertyName}}` for headers | Header: `{{ds.…}}`; row cells: `{{.Column}}` |
| Lists | `{{#ds.rows}}` / `{{#ds.ApplicationItems}}` … `{{/…}}` — **start/end in their own paragraph** | `{{#ds.rows}}` on the **template data row**; optional `{{/ds.rows}}` on next row |
| Spellings | **`docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md`** | Same property names + **`docs/EXCEL_PLACEHOLDER_REFERENCE.md`** |
| Photos (Word only) | **`{{IMAGE:PropertyName}}`** in fixed-size cell — not `:img()`, not plain `{{.Person_Photo}}` | Excel v1: text only (no photo injection) |
| Root BO | Pick **`Application`** vs **`ApplicationItem`** (etc.) to match header vs row scope | Header validates on **`Application`**; row tokens on **`ApplicationItem`** |
| File format | `.docx` | `.xlsx` only (Save As from `.xls`; **do not** zip-folder package) |

If a token is not in the reference or fails validation, see **Adding a missing placeholder** (do not guess a different key).

### 2. Register the template in the app

**Path A — Repo seed (committed under `Resources/Templates/` or `Templates/Excel/`):**

1. **`Visa2026.Module.csproj`** — `<None Remove="…"/>` + `<EmbeddedResource Include="…"/>`.
2. **`UserReportTemplateUpdater.cs`** — **`EnsureTemplateExists`** or **`EnsureExcelTemplateExists`** (`boType`, `applicableApplicationTypeNames`, `excelMergeMode` for Excel, `resourceName`, `sortOrder`).
3. **`dotnet build`** `Visa2026.Module`.
4. **Run app / DB update** so the updater seeds the row (**DEBUG:** reloads embedded bytes each run; **Release:** see **Updater behavior** / **`FORCE_XAF_DB_UPDATE`** in **`docs/ENVIRONMENTS.md`**).

**Path B — Upload only (no code change):**

1. **User Report Templates** in the app → attach `.docx` / `.xlsx`.
2. Set **Root Business Object** and visibility (application types, optional project contract / criteria).

### 3. Placeholder pipeline (required)

| Step | When | Why |
|------|------|-----|
| **Extract placeholders** | New/replaced file; or tokens changed | Rebuilds the placeholder grid from `{{…}}` in the file |
| **Validate placeholders** | Always after Extract; also if **Root BO** changed | **`UserReportGenerator`** / Excel merge bind **only `IsValid` rows** — invalid keys stay empty or block a clean merge |
| **Is Active** | Before expecting Resminamalar | Inactive templates are hidden |

- **Repo seeds:** updater runs Extract + Validate when seeding (DEBUG reloads file each time).
- **UI upload:** Extract/Validate are **not** automatic on file attach — run both manually (**`docs/USER_TEMPLATE_AUTHOR_GUIDE.md`**).

### 4. Visibility (Resminamalar)

All must pass (AND):

- **Is Active**
- **Applicable application types** (empty = all types)
- Optional **project contracts** / **Visibility criteria**

Configure on the template record (UI) or via **`EnsureTemplateExists`** arguments (seeds). See **Resminamalar visibility** below.

### 5. Test generation

1. Open an **Application** (or item context for item-root templates) that matches visibility and has real data.
2. **Resminamalar** → select template → generate / download.
3. If wrong or blank: open template → placeholder grid → fix **invalid** rows or BO mapping; re-run Extract + Validate after file edits.

### 6. Common failures

| Symptom | Likely cause |
|---------|----------------|
| Not in Resminamalar zip | Inactive, wrong application type/contract/criteria, or seed not deployed |
| Blank fields | Wrong token, missing `ds.` prefix, or placeholder not **valid** after Validate |
| Loop empty / one row | Wrong collection (`rows` vs `ApplicationItems`), no non-deleted items, or Excel row rules broken |
| DocxTemplater error on `ds.ApplicationItems.Person_FullName` | Use **`{{.Person_FullName}}`** inside **`{{#ds.ApplicationItems}}`** |
| Photo shows `System.Byte[]` | Used plain **`{{.Person_Photo}}`** in Word — switch to **`{{IMAGE:Person_Photo}}`** |
| Literal **`{{IMAGE:Person_Photo}}`** in output (name/header OK) | **(1)** Running host not rebuilt after injector changes — restart Blazor. **(2)** Wrong token in DB file vs repo seed — Extract + Validate, re-upload or DEBUG seed reload. **(3)** Pre-fix build skipped injector when `photosByKey` was empty — current code always injects when template has **`{{IMAGE:`** |
| Preview has photos, Resminamalar does not | Preview uses sample PNGs; app uses **`Person.Photo`** — confirm photo bytes on person. If token is literal, restart app (see above) |
| Photo column empty (no literal token) | Person has no **`Photo`** bytes — expected; injector clears marker |
| Wrong root for roster | **`ApplicationItem`** root cannot drive **`{{#ds.ApplicationItems}}`** — use **`Application`** |
| Excel rows not copying | No `{{#ds.rows}}` on data row, merged cells on data row, wrong **`ExcelMergeMode`** |
| Old file after edit | Release DB kept previous bytes — re-upload, or DEBUG updater / **`FORCE_XAF_DB_UPDATE`** |

**Author guide (UI detail):** **`docs/USER_TEMPLATE_AUTHOR_GUIDE.md`**.

## Placeholder lookup (data mapping)

When the user asks *“which placeholder for X?”* or *“how do I bind Y?”*:

1. **Read** **`docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md`** (canonical property list). For **Excel** list templates, also **`docs/EXCEL_PLACEHOLDER_REFERENCE.md`** (loop-row rules). Prefer **exact** spellings (e.g. `{{ds.Application_CompanyHead_FullName}}`, `{{.Person_FullName}}`).
2. **Match scope to root BO** (same choice as **`boType`** on the template):
   - **`UserReportBoType.Application`** → **Application — Header** section (`{{ds.*}}`). Row tables use **`{{#ds.ApplicationItems}}`** (or another collection named in the doc) with **`{{.Field}}`** inside the loop — see **ApplicationItem — Row Placeholders**.
   - **`UserReportBoType.ApplicationItem`** with Contract-style templates → header fields still **`{{ds.*}}`** on **`Application`** when merged from an application context; per-person table rows often **`{{#ds.rows}}`** / **`{{.Person_FullName}}`** (see reference **Table Loop Pattern** and **`UserReportGenerator`** labor **`rows`**).
   - **`Registration`** / **`BusinessTrip`** roots → use the matching sections in the reference for row keys.
3. **Reply format:** state the **placeholder to type in Word**, the **data it represents** (from the reference Description column), and **syntax** (header `{{ds.…}}` vs loop `{{.…}}` vs conditional `{?{ds.…}}`).
4. If the field is **not** in the reference or **not** on the root BO, go to **Adding a missing placeholder** (do **not** silently substitute a different key).
5. **Syntax reminders** (do not replace the reference): loop markers in **their own paragraph**; always **`ds.`** prefix on top-level tokens per **`docs/USER_TEMPLATE_AUTHOR_GUIDE.md`**.

## Redundancy check (required before any new placeholder)

**Blocking rule:** Do **not** add a new `[NotMapped]` property, reference row, or `BuildLaborContractRowDictionary` key until redundancy check passes. One business fact → **one canonical** placeholder name on the template root (and one row key inside loops).

### How to check

1. **Reference doc** — Search **`docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md`** for the same meaning (description / section), not only the exact name the user suggested.
2. **Root BO file** — Grep **`Application.cs`**, **`ApplicationItem.cs`**, etc. for `[NotMapped]` and persisted members that already expose the same path (e.g. `Application_CompanyHead_FullName` vs `CompanyHead_FullName` on **`ApplicationItem`** vs nested `CompanyHead.FullName`).
3. **Nested vs flat** — If `{{ds.CompanyHead.FullName}}` already validates and merges, **do not** add `Application_CompanyHead_FullName` unless the reference already standardizes the flat name; then tell the user to use the **existing** token.
4. **Cross-type duplicates** — Header on **`Application`** vs snapshot on **`ApplicationItem`** / **`Registration`** (e.g. company head name): pick the token for the template’s **`boType`**; do not add a second Application-level alias if the reference already documents the correct one for that root.
5. **`{{#ds.rows}}`** — Compare with **`UserReportGenerator.BuildLaborContractRowDictionary`** (Contract-style Word), **`UserReportMergeDataHelper.BuildExcelItemListRowDictionary`** (Excel ministry lists / Gurlusyk / 433-ek), and **`BuildSanawyRowDictionary`**; no duplicate keys for the same column.
6. **Formatted variants** — `ApplicationDate` vs `ApplicationDateText`, `CancelPersonCount` vs `CancelPersonCountText`: only add a sibling if the user needs a **different representation** (raw vs words); never duplicate the same format under two names.

### Outcome

| Result | Action |
|--------|--------|
| **Match found** | Give the user the **canonical** `{{ds.…}}` / `{{.…}}` from the reference. **Stop** — no new C# property. |
| **Near match (alias)** | Explain both tokens map to the same data; recommend the reference spelling. Add a new alias **only** if the user explicitly needs backward compatibility for an old template name. |
| **No match** | Proceed to **Adding a missing placeholder** after confirming data source and name. |

## Adding a missing placeholder (implementation)

When redundancy check finds **no** existing token for the same data, implement in the Module (user still types the placeholder in Word themselves).

### 1. Confirm before coding

Ask or confirm:

- **What business data** should appear (source path, e.g. `CompanyHead.Position.NameTm`, computed count, formatted date).
- **Template root** (`UserReportBoType` / which BO **`ValidatePlaceholders`** uses): `Application`, `ApplicationItem`, `Registration`, or `BusinessTrip`.
- **Where in template**: header `{{ds.*}}`; Word loops `{{#ds.ApplicationItems}}` or `{{#ds.rows}}`; Excel **ItemList** — header cells on `Application`, data row with `{{#ds.rows}}` + `{{.…}}` per **`EXCEL_PLACEHOLDER_REFERENCE.md`**.
- **Exact placeholder string** the user will type (prefer names already used in **`WORD_REPORT_PLACEHOLDER_REFERENCE.md`** tables).
- **Redundancy check completed** — note which existing tokens were ruled out and why a new one is needed.

### 2. Add the BO property (typical pattern)

**Photo (`byte[]`):** prefer existing **`ApplicationItem.Person_Photo`** (via **`Person.Photo`**). Author token **`{{IMAGE:Person_Photo}}`** — no new merge formatter. For **`{{#ds.ApplicationItems}}`** on **`Application`** root, ensure **`UserReportGenerator`** supplies **`ApplicationItemPhotoMergeRow`** (already wired for that collection). Only add a new `byte[]` property if the photo is a **different** field.

On the **root BO** (or **`ApplicationItem`** for row fields), add a public property validation can find:

```csharp
/// <summary>Flattened for Word / user-report placeholders (see docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md).</summary>
[XafDisplayName("… (Word)"), VisibleInDetailView(false), VisibleInListView(false)]
[NotMapped]
public string MyPlaceholder_Name => /* map from persisted/nav graph */ ?? string.Empty;
```

- **Naming:** match the reference (`RelatedEntity_Field`, `Application_CompanyHead_FullName`, `VisaPeriod_NameTm`, …).
- **Dates:** if the template needs a **string** (e.g. `dd.MM.yyyy`), expose a `*Text` or documented string property — do not rely on raw `DateTime` formatting in Word unless the user accepts default merge formatting.
- **Counts in words:** reuse existing helpers on **`Application`** (e.g. `NumberToTurkmenWords`) when adding `*CountText` fields.

**Files:** `Visa2026.Module/BusinessObjects/Application.cs` | `ApplicationItem.cs` | `Registration.cs` | `BusinessTrip.cs` (see reference **Source Files** section).

### 3. Special: `{{#ds.rows}}` row keys

When **`RootBoType == ApplicationItem`** and the template uses **`{{#ds.rows}}`**, runtime row keys may come from **dictionary builders**, not only **`ApplicationItem`** reflection:

| Template family | Add key in |
|-----------------|------------|
| Contract / labor **Word** | **`UserReportGenerator.BuildLaborContractRowDictionary`** |
| Ministry **Excel** lists (Gurlusyk, 433-ek, Sanaw-style) | **`UserReportMergeDataHelper.BuildExcelItemListRowDictionary`** (often wraps **`BuildSanawyRowDictionary`**) |

- Add the property on **`ApplicationItem`** if row-scoped.
- **Also** add the dictionary key so Word/Excel merge receives the value.

### 4. Update the reference doc

Add **one** row to **`docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md`** (correct section). Do not list synonymous tokens; if an alias must exist, document **“Prefer `{{ds.CanonicalName}}`”** in Description rather than adding duplicate rows for the same data.

### 5. Verify

- **`dotnet build`** `Visa2026.Module`.
- After seed/updater or in UI: **Extract** + **Validate** on the template; placeholder row should be **valid** with resolved path.
- Tell the user the exact token to type in Word.

**Scope:** implement only the property (and `rows` dict entry if needed) + reference row unless the user also asked to embed/register the `.docx` / `.xlsx`.

## Other references

| Doc | When to open |
|-----|----------------|
| **`docs/USER_REPORT_MAP_STANDARD.md`** | **Canonical** map sections §0–§15 (Word + Excel) |
| **`Visa2026.Module/Resources/Templates/_map_TEMPLATE.md`** | Copy-paste starter for new `*_map.md` |
| **`.cursor/skills/visa2026-user-report-templates/reference-deterministic-generation.md`** | Same input → same output; agent enforcement |
| **`.cursor/skills/visa2026-user-report-templates/reference-map-contract.md`** | Map + scan mandatory workflow; co-located naming |
| **`.cursor/skills/visa2026-user-report-templates/reference-template-families.md`** | Layout vs root BO; filename hints; user confirmation checklist |
| **`.cursor/skills/visa2026-user-report-templates/prompts.md`** | Copy-paste chat prompts (Word + Excel) |
| **`docs/USER_TEMPLATE_AUTHOR_GUIDE.md`** | Word Extract/Validate; loop/tag placement |
| **`docs/EXCEL_PLACEHOLDER_REFERENCE.md`** | Excel list rows, `{{#ds.rows}}` on data row |
| **`docs/EXCEL_TEMPLATE_REPORTING_PLAN.md`** | Excel pipeline design, ClosedXML, unified UI |
| **`docs/WORD_REPORT_GENERATION_IDEA.md`** | Built-in Word pipeline vs user templates |
| **`Visa2026.Module/Resources/Templates/Excel/README.md`** | Excel embed checklist, spike commands, `.xlsx` pitfalls |

## Repo paths

| Path | Role |
|------|------|
| **`Visa2026.Module/Resources/Templates/<Name>.docx`** | Word user seeds |
| **`Visa2026.Module/Resources/Templates/Excel/<Name>.xlsx`** | Excel user seeds (`.xlsx` only) |
| **`Visa2026.Module/Visa2026.Module.csproj`** | `<None Remove="..."/>` + `<EmbeddedResource Include="..."/>` |
| **`Visa2026.Module/DatabaseUpdate/UserReportTemplateUpdater.cs`** | **`EnsureTemplateExists`** / **`EnsureExcelTemplateExists`** |
| **`Visa2026.Module/Services/UserReports/UserReportGenerator.cs`** | Word merge + photo injector; Contract **`{{#ds.rows}}`** |
| **`Visa2026.Module/Services/UserReports/WordUserReportImageInjector.cs`** | Post-merge **`{{IMAGE:…}}`** → inline pictures |
| **`Visa2026.Module/Services/UserReports/WordUserReportMergeImageExtractor.cs`** | Collect ordered **`byte[]`** from bind model |
| **`Visa2026.Module/Services/UserReports/ApplicationItemPhotoMergeRow.cs`** | Typed rows for **`ApplicationItems`** + photos |
| **`tools/PreviewWordReports`** | Local Word preview (`employee-photo-roster` preset) |
| **`Visa2026.Module/Services/ExcelReports/ExcelReportGenerator.cs`** | Excel merge (**`ItemList`** only in v1) |
| **`Visa2026.Module/Services/UserReports/UserReportMergeDataHelper.cs`** | **`BuildExcelItemListRowDictionary`**, shared row helpers |
| **`tools/ExcelTemplateSpike`** | Local merge tests (`test-gurlusyk`, `build-433-ek`, …) |

Manifest names: **`Visa2026.Module.Resources.Templates.<file>.docx`** | **`Visa2026.Module.Resources.Templates.Excel.<file>.xlsx`**.

---

## `EnsureTemplateExists` / `EnsureExcelTemplateExists` — create or update

Applies to **new** seeds and **editing** an existing block. Locate by **`templateName`** or **`resourceName`** in **`UserReportTemplateUpdater.UpdateDatabaseAfterUpdateSchema`**.

- **Word:** **`EnsureTemplateExists`** (word extractor/validator).
- **Excel:** **`EnsureExcelTemplateExists`** — thin wrapper that sets **`TemplateOutputFormat.Excel`** and requires **`excelMergeMode`** (v1 seeds use **`ExcelMergeMode.ItemList`**).

**Do not** add or change a seed without **explicit user confirmation** for every parameter you change (present the checklist below).

### Parameter checklist

| Parameter | Confirm / ask |
|-----------|----------------|
| **`templateName`** | Stable display name in-app (e.g. `"Gurlusyk"`, `"Contract Inv"`). **Lookup key** in DB: changing it **creates a new** row; old name may orphan. |
| **`description`** | One-line provenance + root BO + visibility summary. |
| **`resourceName`** | **`Visa2026.Module.Resources.Templates.<file>.docx`** or **`...Templates.Excel.<file>.xlsx`**. Rename file → sync **`csproj`** + on-disk path. |
| **`boType`** | **`Application`** vs **`ApplicationItem`** (etc.). Changing **`boType`** invalidates validation — user re-validates in UI. |
| **`excelMergeMode`** | **Excel only.** v1 production: **`ItemList`** (Resminamalar on Application). **`SingleItem`** reserved (generator may throw if used). |
| **`applicableApplicationTypeNames`** | **`null`** or empty → all application types. Non-empty → link rows by **`ApplicationType.Name`** (e.g. `App_Inv_And_WP`). Typos skip linking (Debug log only). |
| **`applicableProjectContractNames`** / **`applicableProjectContractNameTmContains`** | Optional project-contract filter (exact names and/or `NameTm` substring such as `GT-15`). |
| **`visibilityCriteria`** | Optional criteria string or **`null`**. |
| **`sortOrder`** | Resminamalar ordering vs other seeds. |

### Update vs create

| Situation | What to change |
|-----------|----------------|
| **Metadata only** (description, type/contract links, visibility, sort order, `boType`) | Edit the existing **`EnsureTemplateExists`** arguments in place. |
| **New/replaced file** (user edited in Word/Excel) | User replaces binary under **`Templates/`** or **`Templates/Excel/`**; agent updates **`resourceName`** only if needed — **no** layout edits. **DEBUG:** updater reloads bytes. **Release:** see **Updater behavior**. |
| **Rename template file** | User renames `.docx`/`.xlsx`; agent updates **`csproj`**, **`resourceName`**, README tables — **not** sheet/document body. |
| **Legacy `.xls`** | User must **Save As `.xlsx`** in Excel before embed (ClosedXML). |
| **Duplicate seed by mistake** | Do not add a second **`EnsureTemplateExists`** for the same file; merge into one block. |

### After parameters are confirmed

- **Create:** follow **New seed implementation** below.
- **Update:** edit the existing **`EnsureTemplateExists(...)`** block only (no duplicate call unless splitting templates intentionally).
- **`dotnet build`** `Visa2026.Module`.
- Tell user to run app / DB update (or **`FORCE_XAF_DB_UPDATE`**) so changes reach the database; in UI, **Extract + Validate** if they replaced the file outside the updater path.

---

## New seed implementation

### Word

1. **`<Name>.docx`** in **`Resources/Templates/`** (user-authored; agent does not create layout).
2. **`Visa2026.Module.csproj`:** `<None Remove="Resources\Templates\<Name>.docx" />` + `<EmbeddedResource Include="..."/>`.
3. **`UserReportTemplateUpdater`:** append **`EnsureTemplateExists(...)`** (`.GetAwaiter().GetResult()`).
4. Optional: row in **`Resources/Templates/README.md`**.

### Excel

1. **`<Name>.xlsx`** in **`Resources/Templates/Excel/`** (saved from Excel — not folder-zipped).
2. **`Visa2026.Module.csproj`:** `<None Remove="Resources\Templates\Excel\<Name>.xlsx" />` + `<EmbeddedResource Include="..."/>`.
3. **`UserReportTemplateUpdater`:** append **`EnsureExcelTemplateExists(..., excelMergeMode: ExcelMergeMode.ItemList, ...)`**.
4. Placeholders: header `{{ds.*}}` on **`Application`**; one data row with **`{{#ds.rows}}`** and **`{{.Property}}`** per column — **`docs/EXCEL_PLACEHOLDER_REFERENCE.md`**.
5. Optional: **`Resources/Templates/Excel/README.md`** + parent **`Resources/Templates/README.md`** table.
6. Smoke: **`tools/ExcelTemplateSpike`** if a command exists for that layout (see Excel README).

**Shipped Excel seeds (reference):** **Gurlusyk** (`433_gurlusyk_uzt.xlsx`), **433-ek sanawy** (`433-ek_uzt.xlsx`) — WP / visa extension application types.

## Resminamalar visibility (property-based)

Visibility is the **AND** of three optional filters (empty = no filter on that axis):

| UI / seed parameter | When empty | When set |
|---------------------|------------|----------|
| **Applicable Application Types** | All application types | Current app type must match a linked row |
| **Applicable Project Contracts** | All project contracts | Current contract must match a linked row |
| **Visibility Criteria** | No extra rule | Criteria must pass |

**`ApplicabilityMode`** is obsolete (hidden in UI); do not use it in new seeds.

- All types: **`applicableApplicationTypeNames: null`** (and no contract/criteria filters unless needed).
- Specific types: **`applicableApplicationTypeNames: new[] { "App_Inv_And_WP", ... }`**.
- GT-15 ministry letters: **`applicableProjectContractNameTmContains: "GT-15"`** (and usually **`App_Visa_and_WP_Ext`** in type names).

## Updater behavior and DB updates

- **DEBUG:** Embedded bytes (and metadata from **`EnsureTemplateExists`**) refresh every updater run for matching **`templateName`**.
- **Release:** If the template row already has a file, the updater **returns early** — **embedded `.docx`/`.xlsx` and metadata changes in code may not apply** until the row is new, file is null, or the user updates via UI. Plan prod seed changes accordingly.
- Updater not running: **`docs/ENVIRONMENTS.md`** (**`FORCE_XAF_DB_UPDATE`**).
