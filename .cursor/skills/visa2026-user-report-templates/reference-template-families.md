# User report template families (Word)

Canonical matrix for **which BO validates placeholders** vs **how Resminamalar builds output**.  
Agents must **confirm with the user** — do not infer from filename or `ApplicationItem` root alone.

## Two axes (do not conflate)

| Axis | Stored today | Meaning |
|------|----------------|---------|
| **A — Validation root** | **`UserReportTemplate.RootBoType`** (`Application` / `ApplicationItem`) | Which type **`Validate Placeholders`** checks (`PropertyExists` on `Application` vs `ApplicationItem`). |
| **B — Word merge layout** | **Not in DB yet** (filename + updater description + skill checklist) | How **`UserReportGenerator`** binds data when **Resminamalar** runs on an **`Application`** (one zip entry, one or many logical “copies” inside the `.docx`). |

**Today:** Resminamalar always calls `GenerateAsync(template, application, …)` — merge root is **`Application`**, except item-only APIs not used in the zip.

## Word merge layouts (axis B)

| Layout ID | Resminamalar output | Word structure | Validation root (typical) | Bind model at runtime |
|-----------|---------------------|----------------|---------------------------|------------------------|
| **`AppScalar`** | **One** `.docx` per application; **one** logical document | `{{ds.FullApplicationNumber}}`, `{{ds.Application_CompanyHead_FullName}}`, … | **`Application`** | Scalars on `Application` |
| **`ItemRows`** | **One** `.docx` per application; **one section per `ApplicationItem`** (often `{{:PageBreak}}` between) | `{{#ds.rows}}` … `{{ds.rows.Person_FullName}}` or `{{.Person_FullName}}` … `{{/ds.rows}}` | **`ApplicationItem`** | `data["rows"]` = list of row dicts from all active items |
| **`ItemRoster`** | **One** `.docx`; **table/list** of all items (may share one page or break per row) | `{{#ds.ApplicationItems}}` … `{{.Person_FullName}}`, `{{IMAGE:Person_Photo}}` … `{{/ds.ApplicationItems}}` | **`Application`** | `data["ApplicationItems"]` = `ApplicationItemPhotoMergeRow` or dict rows |
| **`ItemScalar`** | **One** `.docx` for **one** item (intended; **not** default zip behavior) | `{{ds.Person_FullName}}`, `{{ds.Passport_Number}}`, … | **`ApplicationItem`** | Root object = single `ApplicationItem` |

### Layout vs root BO — common mistake

- **`RootBoType = ApplicationItem`** does **not** imply `{{ds.Person_FullName}}` at document root for Resminamalar.
- **`ItemRows`** templates almost always use **`ApplicationItem`** root **and** `{{#ds.rows}}` / `{{ds.rows.*}}` because the zip merges from **`Application`** + synthetic **`rows`**.

## Filename convention (hint only)

**New seeds** should embed the layout in the filename so humans and agents can spot mismatches. Legacy names stay as-is (see **Legacy seeds**).

| Filename pattern | Layout ID | Validation root |
|------------------|-----------|-----------------|
| `*_App.docx` or `App_*.docx` | `AppScalar` | `Application` |
| `*_ItemRows.docx` or `ItemRows_*.docx` | `ItemRows` | `ApplicationItem` |
| `*_ItemRoster.docx` or `ItemRoster_*.docx` | `ItemRoster` | `Application` |
| `*_ItemOne.docx` or `ItemOne_*.docx` | `ItemScalar` | `ApplicationItem` |

Examples for new files:

- `Forma_16_ItemRows.docx` — registration Form 16, one page per person in one file.
- `Hasaba_almak_App.docx` — letter headers only on `Application`.
- `Employee_Photo_ItemRoster.docx` — already shipped as `Employee_Photo_Roster_Sample.docx` (`ItemRoster`).

**Agent rule:** If filename layout token ≠ tokens found in `.docx` (Extract) or ≠ user-stated layout → **stop** and ask.

## Legacy seeds (no rename required)

| File | Layout ID | Root BO (seed) | Notes |
|------|-----------|----------------|-------|
| `Borcnama.docx` | `ItemRows` | `ApplicationItem` | `{{#ds.rows}}`, `{{ds.rows.*}}`, page break |
| `Contract_uzt.docx`, `Contract_Inv.docx` | `ItemRows` | `ApplicationItem` | Contract family |
| `Sanaw_uzt.docx` | `ItemRows` | `ApplicationItem` | Sanawy row dict |
| `hasaba_almak_hat.docx` | `AppScalar` | `Application` | Letter, `{{ds.*}}` on application |
| `GT-15_*.docx` | `AppScalar` | `Application` | Ministry letters |
| `Employee_Photo_Roster_Sample.docx` | `ItemRoster` | `Application` | `{{#ds.ApplicationItems}}` |
| `Forma_16.docx` | **TBD** — confirm with user | Likely `ApplicationItem` + **`ItemRows`** for Resminamalar | Static sample today; not registered |

## Agent checklist — ask before embed/register

Copy to chat; **wait for explicit answers** (do not assume):

1. **Layout ID:** `AppScalar` | `ItemRows` | `ItemRoster` | `ItemScalar`
2. **Validation root (`RootBoType`):** `Application` | `ApplicationItem` — must match layout table above unless user accepts a documented exception.
3. **Resminamalar:** one `.docx` in the zip with all people vs (future) one file per item — today only **one entry per template name**.
4. **Placeholder style in Word:** confirm after **Extract** — e.g. `#ds.rows` present? `ds.rows.*` vs `{{.}}`? `ApplicationItems` loop?
5. **Photos:** yes/no → `{{IMAGE:…}}` inside loop row only.

If the user only provides a filename, map the pattern table → propose layout → **get confirmation**.

## Consistency checks (after Extract)

| Layout ID | Must find in `.docx` | Must not rely on |
|-----------|----------------------|------------------|
| `AppScalar` | `{{ds.<ApplicationField>}}` | `{{#ds.rows}}` as primary data (unless user wants hybrid) |
| `ItemRows` | `{{#ds.rows}}` / `{{/ds.rows}}` | Bare `{{ds.Person_FullName}}` for per-person body in zip |
| `ItemRoster` | `{{#ds.ApplicationItems}}` | `{{ds.ApplicationItems.Person_FullName}}` (use `{{.Person_FullName}}`) |
| `ItemScalar` | `{{ds.<ApplicationItemField>}}` without row loop | Resminamalar zip without code change |

## Phase 2 (optional product work)

Mirror **`ExcelMergeMode`**: add **`WordMergeLayout`** on **`UserReportTemplate`**, updater parameter, UI field, and generator assert (layout vs extracted tokens). Filename remains documentation; DB field becomes source of truth.

Until then: record layout in **`EnsureTemplateExists` `description`** (e.g. `Word layout: ItemRows`) and follow this reference.
