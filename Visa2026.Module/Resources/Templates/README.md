# User-Defined Report Templates

This directory contains **seed** `.docx` / `.xlsx` files for the **User Report Template** feature. Each file must be an **Embedded Resource** in `Visa2026.Module.csproj` and registered in **`DatabaseUpdate/UserReportTemplateUpdater.cs`**.

## Mandatory trio per report (map-first)

Before designing or registering **`<Basename>.docx`**, commit three co-located artifacts with the **same basename**:

| File | Example | Purpose |
|------|---------|---------|
| **Scan** | `Forma_16.png` | Photo/scan of the real official document |
| **Map** | `Forma_16_map.md` | Contract: layout, template family, placeholders, Resminamalar output — **from the scan** |
| **Template** | `Forma_16.docx` | User-authored Word file **after** map is **Approved** |

- **All maps use the same format:** **`docs/USER_REPORT_MAP_STANDARD.md`** (sections §0–§15). Copy **`_map_TEMPLATE.md`**.
- **Determinism:** map §6 exact tokens + §12 golden values + §11 verification → same input, same output.
- Agent: **`.cursor/skills/visa2026-user-report-templates/`** (`reference-map-contract.md`, `reference-deterministic-generation.md`)
- Excel maps/scans: same pattern under **`Templates/Excel/`** (e.g. `433_gurlusyk_uzt_map.md` + `433_gurlusyk_uzt.png` + `433_gurlusyk_uzt.xlsx`).

Legacy seeds may lack maps — backfill before major changes.

## Word template families (filename + layout)

**Root BO** on the template record is **not** enough to choose placeholders. Also pick a **merge layout** (how Resminamalar fills the file):

| Layout | Filename hint (new seeds) | Root BO | Word tokens |
|--------|---------------------------|---------|-------------|
| Application letter | `*_App.docx` | `Application` | `{{ds.*}}` |
| One form per person in one file | `*_ItemRows.docx` | `ApplicationItem` | `{{#ds.rows}}` … `{{ds.rows.*}}` … `{{/ds.rows}}` |
| Table of all items | `*_ItemRoster.docx` | `Application` | `{{#ds.ApplicationItems}}` … `{{.Field}}` |
| Single item only | `*_ItemOne.docx` | `ApplicationItem` | `{{ds.*}}` (item fields) |

Full matrix, legacy filenames (`Borcnama.docx`, `Forma_16.docx`), and agent checklist: **`.cursor/skills/visa2026-user-report-templates/reference-template-families.md`**.

## How to Add a New Seed Template

1. **Create the .docx** in Microsoft Word (Save As `.docx`). Do **not** build `.docx` with PowerShell `Compress-Archive` — ZIP entry paths must use forward slashes (`word/document.xml`) or Open XML / placeholder extract will see an empty document.
2. **Confirm layout** with the team (see template families above) — then use DocxTemplater syntax with the **`ds`** model — **`docs/USER_TEMPLATE_AUTHOR_GUIDE.md`**.
3. **Place it in this folder** (e.g., `MyTemplate.docx`).
4. **Register in `Visa2026.Module.csproj`**: add `<None Remove="Resources\Templates\MyTemplate.docx" />` and `<EmbeddedResource Include="Resources\Templates\MyTemplate.docx" />`.
5. **Register in `UserReportTemplateUpdater.UpdateDatabaseAfterUpdateSchema`**: call `EnsureTemplateExists(...)` with the manifest name below.

**Embedded resource name** (default root namespace = `Visa2026.Module`):

`Visa2026.Module.Resources.Templates.<FileName>.docx`

Example — all application types (no type/contract/criteria filters):

```csharp
EnsureTemplateExists(
    extractor,
    validator,
    templateName: "My Template",
    description: "What this template is for",
    resourceName: "Visa2026.Module.Resources.Templates.MyTemplate.docx",
    boType: UserReportBoType.Application,
    applicableApplicationTypeNames: null,
    visibilityCriteria: null,
    sortOrder: 100)
    .GetAwaiter()
    .GetResult();
```

Restrict to named application types:

```csharp
applicableApplicationTypeNames: new[] { "App_Inv_And_WP" },
```

GT-15 project contracts (`NameTm` contains `GT-15`):

```csharp
applicableApplicationTypeNames: new[] { "App_Visa_and_WP_Ext" },
applicableProjectContractNameTmContains: "GT-15",
```

## Shipped seeds (summary)

| File | Template name | Root BO | Visibility |
|------|----------------|---------|------------|
| `Employee_Photo_Roster_Sample.docx` | **Employee photo roster (sample)** | Application (`{{#ds.ApplicationItems}}`) | All application types |
| `Forma_16.docx` | **Forma 16** | `ApplicationItem` / **`ItemRows`** — **`Forma_16_map.md`** (Approved) | Registration types (check-in/out, ext, info-change) |

**Preview without the app:** `dotnet run --project tools/PreviewWordReports -- employee-photo-roster` (demo PNGs in `tools/PreviewWordReports/SamplePhotos/`). Photo column uses `{{IMAGE:Person_Photo}}`; images are injected after DocxTemplater merge (`WordUserReportImageInjector`). **Troubleshooting** (literal `{{IMAGE:…}}`, preview vs app): **`docs/USER_TEMPLATE_AUTHOR_GUIDE.md`** (Photos) and **`.cursor/skills/visa2026-user-report-templates/prompts.md`** (*Employee photo roster — experience*).
| `Borcnama.docx` | **Borcnama** | ApplicationItem (`{{#ds.rows}}`) | All application types |
| `Contract_uzt.docx` | **Contract** | ApplicationItem | Types: visa/WP extension family |
| `Contract_Inv.docx` | **Contract Inv** | ApplicationItem | `App_Inv_And_WP` only |
| `GT-15_Sazakow_uzt.docx` | **GT-15_Sazakow_uzt** | Application | `App_Visa_and_WP_Ext` + GT-15 contracts |
| `GT-15_Elyasow_uzt.docx` | **GT-15_Elyasow_uzt** | Application | same |
| `GT-15_Elyasow_ckl.docx` | **GT-15_Elyasow_ckl** | Application | **`App_Inv_And_WP`** + GT-15 contracts; **`GT-15_Elyasow_ckl_map.md`** (Approved) |
| `GT-15_MINSTROY_uzt.docx` | **GT-15_MINSTROY_uzt** | Application | same |
| `Sanaw_uzt.docx` | **Sanaw** | ApplicationItem | `App_Visa_and_WP_Ext` |
| `hasaba_almak_hat.docx` | **Hasaba almak hat** | Application | `App_Reg_Check_In` |
| `Excel/433_gurlusyk_uzt.xlsx` | **Gurlusyk** | ApplicationItem | WP / visa extension types |
| `Excel/433-ek_uzt.xlsx` | **433-ek sanawy** | ApplicationItem | same |

## Available placeholders

See **`docs/USER_TEMPLATE_AUTHOR_GUIDE.md`** and **`docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md`**.

## Template behavior

- **DEBUG:** Updater reloads embedded bytes on each run.
- **Release:** Existing rows keep user-edited files until recreated or updated in UI — see **`docs/ENVIRONMENTS.md`** (`FORCE_XAF_DB_UPDATE`).
