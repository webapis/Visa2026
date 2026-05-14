# User-Defined Report Templates

This directory contains seed templates for the User Report Template feature.

## How to Add a New Seed Template

1. **Create the .docx file** with `{{placeholder}}` syntax
2. **Place it in this folder** (e.g., `MyTemplate.docx`)
3. **Set Build Action** = `Embedded Resource` in file properties
4. **Register in UserReportTemplateUpdater.cs**:

```csharp
EnsureTemplateExists(
    templateName: "My Template Display Name",
    description: "What this template is for",
    resourceName: "Visa2026.Module.Resources.Templates.MyTemplate.docx",
    boType: UserReportBoType.Application,
    applicabilityMode: ApplicabilityMode.AllTypes,  // Or SpecificTypes/DataDriven
    visibilityCriteria: "[ApplicationType.Name] = 'SomeType'",  // Optional
    sortOrder: 100
);
```

## Available Placeholders

See `docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md` for available properties.

### Common Placeholders
- `{{ApplicationNumber}}` — Application number
- `{{ApplicationDate}}` — Application date
- `{{CompanyHead.FullName}}` — Company head name
- `{{CompanyHead.PositionTm}}` — Position in Turkmen
- `{{#ApplicationItems}}` — Loop over application items
- `{{.Person.FullName}}` — Inside loop: person name
- `{{.Passport.Number}}` — Inside loop: passport number

## Template Behavior

- **DEBUG builds**: Template files are always overwritten on startup
- **Production**: Only created if missing (preserves user edits to metadata)
- Users can modify the template record (name, description, visibility) but the file content only updates in DEBUG
