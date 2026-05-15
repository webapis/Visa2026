---
name: visa2026-user-report-templates
description: >-
  User Report Template seeds only under Visa2026.Module/Resources/Templates/*.docx — embed, EnsureTemplateExists
  register/update, placeholder lookup and [NotMapped] BO properties. Never edits Word format/layout or templates outside
  Resources/Templates. Use for Templates folder seeds, placeholder mapping, or Sazakow-style updater changes.
disable-model-invocation: false
---

# Visa2026 — User Report Templates (embed + register seeds)

## Hard boundaries (always enforce)

### Directory — `Resources/Templates` only

- **In scope:** **`Visa2026.Module/Resources/Templates/*.docx`** only (user-report **seed** templates wired through **`UserReportTemplateUpdater`**).
- **Out of scope:** Any other `.docx` under **`Visa2026.Module/Resources/`** (e.g. `App_Inv_Letter.docx`, `App_Labor_Contract_Item.docx`) — those belong to **`visa2026-word-reports`** / **`IWordReportDefinition`**, not this skill.
- **Do not** add embedded resources, updater seeds, or **`EnsureTemplateExists`** calls for files outside **`Resources/Templates/`**.

### Layout and format — never touch

- The agent **must not** change Word **formatting, layout, typography, margins, tables structure, images, or body text** in any `.docx`.
- **Forbidden:** unzip/edit `word/document.xml`, run layout scripts on templates, use **`GenerateTemplates`**, or “fix” ministry layout in the repo.
- **Allowed:** Tell the user **which placeholder token to type**; the user edits the file in Word and commits the binary. The agent only references the **path** for embed/register (does not rewrite document content).

## Scope

- **User:** Authors **`Resources/Templates/<Name>.docx`** in Word (layout + placeholders).
- **Agent (three jobs):**
  1. **Embed + register** — new seed: **`csproj`** embedded resource + new **`EnsureTemplateExists(...)`** block.
  2. **Update seed registration** — change an **existing** **`EnsureTemplateExists`** call when metadata, applicability, **`resourceName`**, or **`boType`** must change (see below). Same parameter checklist as create.
  3. **Placeholder lookup / implement** — **`WORD_REPORT_PLACEHOLDER_REFERENCE.md`**; redundancy check before new **`[NotMapped]`** properties (see **Adding a missing placeholder**).

For **code-backed** Word reports (`Resources/*.docx` outside **Templates**, `IWordReportDefinition`, `GenerateTemplates`, `PreviewWordReports`), use **`visa2026-word-reports`**.

## Placeholder lookup (data mapping)

When the user asks *“which placeholder for X?”* or *“how do I bind Y?”*:

1. **Read** **`docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md`** (canonical list). Prefer its **exact** spellings in answers (e.g. `{{ds.Application_CompanyHead_FullName}}`, not a guessed variant).
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
5. **`{{#ds.rows}}`** — Compare proposed row key with **`UserReportGenerator.BuildLaborContractRowDictionary`** and existing **`ApplicationItem`** row placeholders in the reference; no duplicate keys for the same column.
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
- **Where in Word**: header `{{ds.*}}`, collection loop `{{#ds.ApplicationItems}}` + `{{.…}}`, or Contract-style `{{#ds.rows}}` + `{{.…}}`.
- **Exact placeholder string** the user will type (prefer names already used in **`WORD_REPORT_PLACEHOLDER_REFERENCE.md`** tables).
- **Redundancy check completed** — note which existing tokens were ruled out and why a new one is needed.

### 2. Add the BO property (typical pattern)

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

### 3. Special: `{{#ds.rows}}` (Contract / labor seeds)

When **`RootBoType == ApplicationItem`** and the template uses **`{{#ds.rows}}`**, runtime row keys come from **`UserReportGenerator.BuildLaborContractRowDictionary`** — not only from **`ApplicationItem`** reflection.

- Add the property on **`ApplicationItem`** if it is row-scoped.
- **Also** add the same dictionary key in **`BuildLaborContractRowDictionary`** so merge receives the value.

### 4. Update the reference doc

Add **one** row to **`docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md`** (correct section). Do not list synonymous tokens; if an alias must exist, document **“Prefer `{{ds.CanonicalName}}`”** in Description rather than adding duplicate rows for the same data.

### 5. Verify

- **`dotnet build`** `Visa2026.Module`.
- After seed/updater or in UI: **Extract** + **Validate** on the template; placeholder row should be **valid** with resolved path.
- Tell the user the exact token to type in Word.

**Scope:** implement only the property (and `rows` dict entry if needed) + reference row unless the user also asked to embed/register the `.docx`.

## Other references

| Doc | When to open |
|-----|----------------|
| **`docs/USER_TEMPLATE_AUTHOR_GUIDE.md`** | Extract/Validate in UI; loop/tag placement rules |
| **`docs/WORD_REPORT_GENERATION_IDEA.md`** | Built-in Word pipeline vs user templates |

## Repo paths

| Path | Role |
|------|------|
| **`Visa2026.Module/Resources/Templates/<Name>.docx`** | Where the user’s finished file lives |
| **`Visa2026.Module/Visa2026.Module.csproj`** | `<None Remove="..."/>` + `<EmbeddedResource Include="..."/>` |
| **`Visa2026.Module/DatabaseUpdate/UserReportTemplateUpdater.cs`** | **`EnsureTemplateExists`** calls |
| **`Visa2026.Module/Services/UserReports/UserReportGenerator.cs`** | **`ApplicationItem`** root + synthetic **`{{#ds.rows}}`** behavior (Contract family) |

Default manifest name: **`Visa2026.Module.Resources.Templates.<FileName>.docx`**.

---

## `EnsureTemplateExists` — create or update

Applies to **new** seeds and **editing** an existing block (e.g. **Sazakow (seed)**). Locate the call in **`UserReportTemplateUpdater.UpdateDatabaseAfterUpdateSchema`** by **`templateName`** or **`resourceName`**.

**Do not** add or change a seed without **explicit user confirmation** for every parameter you change (present the checklist below).

### Parameter checklist

| Parameter | Confirm / ask |
|-----------|----------------|
| **`templateName`** | Stable display name in-app (e.g. `"Sazakow (seed)"`). **Lookup key** in DB: changing it **creates a new** `UserReportTemplate` row; old name may orphan. Rename only if user accepts DB cleanup or one-time migration. |
| **`description`** | One-line provenance + root BO + visibility summary. |
| **`resourceName`** | **`Visa2026.Module.Resources.Templates.<exact filename>.docx`**. If the **file is renamed**, update **`csproj`** embedded path and on-disk name to match. |
| **`boType`** | **`Application`** vs **`ApplicationItem`** (etc.). Changing **`boType`** invalidates placeholder validation — user must re-validate tokens in Word. |
| **`applicabilityMode`** | **`AllTypes`**, **`SpecificTypes`**, or **`DataDriven`**. **`AllTypes`** → **`applicableApplicationTypeNames: null`**. |
| **`applicableApplicationTypeNames`** | Full list of **`ApplicationType.Name`** when **`SpecificTypes`** (e.g. add/remove `App_Visa_and_WP_Ext`). Typos skip linking (Debug log only). |
| **`visibilityCriteria`** | Criteria string or **`null`**. |
| **`sortOrder`** | Resminamalar ordering vs other seeds. |

### Update vs create

| Situation | What to change |
|-----------|----------------|
| **Metadata only** (description, applicability, visibility, sort order, `boType`) | Edit the existing **`EnsureTemplateExists`** arguments in place. |
| **New/replaced `.docx`** (user edited file in Word) | User replaces **`Resources/Templates/<Name>.docx`**; agent updates **`resourceName`** / updater only if needed — **no** content edits. **DEBUG:** updater reloads bytes each run. **Release:** see **Updater behavior**. |
| **Rename template file** | User renames `.docx`; agent updates **`csproj`**, **`resourceName`**, optional **`README.md`** — **not** the document body. |
| **Duplicate seed by mistake** | Do not add a second **`EnsureTemplateExists`** for the same file; merge into one block. |

### After parameters are confirmed

- **Create:** follow **New seed implementation** below.
- **Update:** edit the existing **`EnsureTemplateExists(...)`** block only (no duplicate call unless splitting templates intentionally).
- **`dotnet build`** `Visa2026.Module`.
- Tell user to run app / DB update (or **`FORCE_XAF_DB_UPDATE`**) so changes reach the database; in UI, **Extract + Validate** if they replaced the file outside the updater path.

---

## New seed implementation

1. **`<Name>.docx`** already in **`Visa2026.Module/Resources/Templates/`** (user-authored and committed; agent does not create layout).
2. **`Visa2026.Module.csproj`:** `<None Remove="Resources\Templates\<Name>.docx" />` + `<EmbeddedResource Include="..."/>`.
3. **`UserReportTemplateUpdater`:** append **`EnsureTemplateExists(...)`** (`.GetAwaiter().GetResult()` like existing seeds).
4. Optional: **`Resources/Templates/README.md`** shipped table.

## Applicability quick reference

- **`ApplicabilityMode.AllTypes`** → **`applicableApplicationTypeNames: null`**.
- **`ApplicabilityMode.SpecificTypes`** → non-null array of **`ApplicationType.Name`** values.
- **`VisibilityCriteria`** — criteria popup style; often **`null`** for seeds.

## Updater behavior and DB updates

- **DEBUG:** Embedded bytes (and metadata from **`EnsureTemplateExists`**) refresh every updater run for matching **`templateName`**.
- **Release:** If the template row already has a file, **`EnsureTemplateExists` returns early** — **`.docx` and metadata changes in code may not apply** until the row is new, file is null, or the user updates via UI. Plan prod seed changes accordingly.
- Updater not running: **`docs/ENVIRONMENTS.md`** (**`FORCE_XAF_DB_UPDATE`**).
