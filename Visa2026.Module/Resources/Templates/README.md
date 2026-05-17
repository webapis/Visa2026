# User-Defined Report Templates

This directory contains **seed** `.docx` files for the **User Report Template** feature. Each file must be an **Embedded Resource** in `Visa2026.Module.csproj` and registered in **`DatabaseUpdate/UserReportTemplateUpdater.cs`**.

## How to Add a New Seed Template

1. **Create the .docx** using DocxTemplater syntax with the **`ds`** model — see **`docs/USER_TEMPLATE_AUTHOR_GUIDE.md`** (e.g. `{{ds.FullApplicationNumber}}`, `{{#ds.ApplicationItems}}` … `{{.Person_FullName}}`).
2. **Place it in this folder** (e.g., `MyTemplate.docx`).
3. **Register in `Visa2026.Module.csproj`**: add `<None Remove="Resources\Templates\MyTemplate.docx" />` and `<EmbeddedResource Include="Resources\Templates\MyTemplate.docx" />` (same pattern as `Contract.docx`).
4. **Register in `UserReportTemplateUpdater.UpdateDatabaseAfterUpdateSchema`**: call `EnsureTemplateExists(...)` with the manifest name below.

**Embedded resource name** (default root namespace = `Visa2026.Module`):

`Visa2026.Module.Resources.Templates.<FileName>.docx`

Example call (all application types):

```csharp
EnsureTemplateExists(
    extractor,
    validator,
    templateName: "My Template Display Name",
    description: "What this template is for",
    resourceName: "Visa2026.Module.Resources.Templates.MyTemplate.docx",
    boType: UserReportBoType.Application,
    applicabilityMode: ApplicabilityMode.AllTypes,
    applicableApplicationTypeNames: null,
    visibilityCriteria: null,
    sortOrder: 100)
    .GetAwaiter()
    .GetResult();
```

Restrict to named application types (links lookup rows by `ApplicationType.Name`):

```csharp
applicabilityMode: ApplicabilityMode.SpecificTypes,
applicableApplicationTypeNames: new[] { "App_Inv_And_WP" },
```

## Shipped seed

| File | Template name | Root BO | Applicability | Notes |
|------|----------------|---------|-----------------|-------|
| `Contract.docx` | **Contract (seed)** | **ApplicationItem** (`{{#ds.rows}}` → `Application.ApplicationItems`) | **Specific types** → `App_Visa_and_WP_Ext`, `App_WP_Ext`, `App_Visa_Ext_According_to_WP` | Labor-style row keys |
| `Contract_Inv.docx` | **Contract Inv (seed)** | **ApplicationItem** (same `rows` merge) | **Specific types** → `App_Inv_And_WP` only | Invitation + work permit |
| `433_Elyasow_uzt.docx` | **433-Elyasow (seed)** | **Application** (`{{ds.*}}`) | **Specific types** → `App_Visa_and_WP_Ext` only | Ministry letter (Elyasow layout) |
| `433_MINSTROY_uzt.docx` | **433-MINSTROY (seed)** | **Application** (`{{ds.*}}`) | **Specific types** → `App_Visa_and_WP_Ext` only | Ministry letter (MINSTROY layout) |

## Available placeholders

See **`docs/USER_TEMPLATE_AUTHOR_GUIDE.md`** and **`docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md`**.

## Template behavior

- **DEBUG**: Template **file bytes** are reloaded from the embedded resource on every DB update run (easy iteration).
- **Release**: File is loaded only when the template is **new** or **Template file** is null (metadata edits preserved).
- **`UserReportTemplateUpdater`** is registered in **`Visa2026.Module/Module.cs`** (`GetModuleUpdaters`). If updaters do not run on an existing DB, see **`docs/ENVIRONMENTS.md`** (`FORCE_XAF_DB_UPDATE`).
