# User-Defined Report Templates

This directory contains **seed** `.docx` / `.xlsx` files for the **User Report Template** feature. Each file must be an **Embedded Resource** in `Visa2026.Module.csproj` and registered in **`DatabaseUpdate/UserReportTemplateUpdater.cs`**.

## How to Add a New Seed Template

1. **Create the .docx** using DocxTemplater syntax with the **`ds`** model — see **`docs/USER_TEMPLATE_AUTHOR_GUIDE.md`** (e.g. `{{ds.FullApplicationNumber}}`, `{{#ds.ApplicationItems}}` … `{{.Person_FullName}}`).
2. **Place it in this folder** (e.g., `MyTemplate.docx`).
3. **Register in `Visa2026.Module.csproj`**: add `<None Remove="Resources\Templates\MyTemplate.docx" />` and `<EmbeddedResource Include="Resources\Templates\MyTemplate.docx" />`.
4. **Register in `UserReportTemplateUpdater.UpdateDatabaseAfterUpdateSchema`**: call `EnsureTemplateExists(...)` with the manifest name below.

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
| `Borcnama.docx` | **Borcnama** | ApplicationItem (`{{#ds.rows}}`) | All application types |
| `Contract_uzt.docx` | **Contract** | ApplicationItem | Types: visa/WP extension family |
| `Contract_Inv.docx` | **Contract Inv** | ApplicationItem | `App_Inv_And_WP` only |
| `GT-15_Sazakow_uzt.docx` | **GT-15_Sazakow_uzt** | Application | `App_Visa_and_WP_Ext` + GT-15 contracts |
| `GT-15_Elyasow_uzt.docx` | **GT-15_Elyasow_uzt** | Application | same |
| `GT-15_MINSTROY_uzt.docx` | **GT-15_MINSTROY_uzt** | Application | same |
| `Sanaw_uzt.docx` | **Sanaw** | ApplicationItem | `App_Visa_and_WP_Ext` |
| `Excel/433_gurlusyk_uzt.xlsx` | **Gurlusyk** | ApplicationItem | WP / visa extension types |
| `Excel/433-ek_uzt.xlsx` | **433-ek sanawy** | ApplicationItem | same |

## Available placeholders

See **`docs/USER_TEMPLATE_AUTHOR_GUIDE.md`** and **`docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md`**.

## Template behavior

- **DEBUG:** Updater reloads embedded bytes on each run.
- **Release:** Existing rows keep user-edited files until recreated or updated in UI — see **`docs/ENVIRONMENTS.md`** (`FORCE_XAF_DB_UPDATE`).
