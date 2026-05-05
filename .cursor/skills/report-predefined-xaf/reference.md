# Reference: predefined report creation (Visa2026)

This file is intentionally “copy/paste friendly” for repeated report work. Canonical rules remain in:

- `Visa2026.Module/Reports/REPORT_GENERATION_GUIDE.md`
- `Visa2026.Module/Reports/REPORT_STANDARDS.md`
- `Visa2026.Module/Reports/REPORTS.md`

## `_map.md` skeleton (minimum sections)

```markdown
# <ReferenceImageFilename> — Map

## Status
📋 Draft | ✅ Agreed | ✅ Implemented

## Report Identity
- Reference image: `Resources/FormTemplates/<image>.jpg`
- Map file: `Resources/FormTemplates/<image>_map.md`
- ApplicationType.Name: `<App_Inv | ...>`
- Report class: `<ClassName>`
- Registered name: `<Registered Name>`
- Display name (Tm): `<...>`

## Data
- Data type: `<Application | ApplicationItem | Registration | BusinessTrip>`
- Base class: `<AppBaseReport | AppItemBaseReport | AppRegBaseReport | Group base | XtraReport>`
- Visibility criteria: `<criteria string>`
- Shared across types?: `<no | yes (list types)>`

## Page Setup
- Paper: A4
- Orientation: Portrait/Landscape
- Margins: `<per standards or inherited>`

## Band Map
| Band | HeightF | Source (Inherited/Local) | Notes |
|---|---:|---|---|
| Detail |  |  |  |

## Control Map — Detail
| Control | Type | LocationFloat (X,Y) | SizeF (W,H) | Source (Inherited/Static/Bound/Expression) | Value / Expression | Notes |
|---|---|---|---|---|---|---|

## Ignored Elements
- `<element>` — `<why ignored>`

## Required BO Properties
| Property | BO | Exists? | Notes |
|---|---|---:|---|
```

## Visibility criteria patterns (ReportsUpdater)

```csharp
// Application
"[ApplicationType.Name] = 'App_Inv'"

// ApplicationItem
"[Application.ApplicationType.Name] = 'App_Inv'"

// Registration
"[Application.ApplicationType.Name] = 'App_Reg_Check_In'"

// Shared across multiple ApplicationTypes (example)
"[Application.ApplicationType.Name] In ('App_Inv', 'App_Inv_According_to_WP')"

// Contract-specific (only when layout differs per contract)
"[ApplicationType.Name] = 'App_Inv' AND [ProjectContract.Code] = 'TAPI'"
```

## Turkmen QA quick checks

Use `Visa2026.Module/Reports/REPORT_STANDARDS.md` §14 as the source of truth. The most common checks:

- Genitive suffix must end with `ň` (never `n`)
- No Turkish `ğ`
- Verify all required chars: `ş ý ö ä ü ç ň`
- Post-code: scan all `.Text`, `.Rtf`, and expression strings

