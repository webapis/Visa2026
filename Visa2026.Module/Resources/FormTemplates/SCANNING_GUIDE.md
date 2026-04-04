# Form Reference Image Preparation Guide

This document tells you exactly what to do when scanning government form documents
for use as design references in XtraReports report creation.

---

## 1. Purpose of These Images

Scanned form images are **design references only**. They are never embedded in reports.
When you provide a scanned image, I will read it and replicate its layout in code —
placing labels, tables, and data fields at the correct positions.

The only image that becomes a report background is `background.jpg` (company letterhead),
which is already set.

---

## 2. File Format

- **Format:** `.jpg` only
- **Resolution:** 150–200 DPI — enough to read all text clearly, not so large it is slow to open
- **Color mode:** Color or grayscale — both are fine
- **Pages:** One file per page. If the form has 2 pages, provide 2 files (use `_p1`, `_p2` suffix)

---

## 3. File Naming Convention

```
{ApplicationType.Name}[_{ProjectContract.Code}]_{level}[_v{n}][_p{n}].jpg
```

### Segments

| Segment | Rule | Example |
|---|---|---|
| `{ApplicationType.Name}` | Exact name from the database — copy it exactly, preserve underscores and casing | `App_Inv`, `App_Visa_Ext_FM` |
| `[_{ProjectContract.Code}]` | ALL CAPS — **optional**. Include only when this form's layout is specific to one project contract AND differs from other contracts. Omit when the form is generic or when the layout is the same across all contracts for this ApplicationType | `_CLK`, `_TAPI` |
| `_{level}` | Always present. Use `_app` for Application-level forms, `_item` for per-person forms, `_reg` for registration/movement forms | `_app`, `_item`, `_reg` |
| `[_v{n}]` | Only include when there are 2 or more variants of this form at this level. Start from `_v0` | `_v0`, `_v1`, `_v2` |
| `[_p{n}]` | Only include when the form has more than one page | `_p1`, `_p2` |

### Examples

| Situation | Filename |
|---|---|
| Invitation form, Application level, single variant | `App_Inv_app.jpg` |
| Invitation form, per-person level, single variant | `App_Inv_item.jpg` |
| Invitation form, per-person, CLK contract only | `App_Inv_CLK_item.jpg` |
| FM visa extension, Application level, 3 variants — first | `App_Visa_Ext_FM_app_v0.jpg` |
| FM visa extension, Application level, 3 variants — second | `App_Visa_Ext_FM_app_v1.jpg` |
| FM visa extension, Application level, 3 variants — third | `App_Visa_Ext_FM_app_v2.jpg` |
| Two-page form, page 1 | `App_Inv_And_WP_app_p1.jpg` |
| Two-page form, page 2 | `App_Inv_And_WP_app_p2.jpg` |
| Two-page form, variant 0, page 1 | `App_Visa_Ext_FM_CLK_app_v0_p1.jpg` |

### How to Know Which ApplicationType.Name to Use

Check `LOOKUPS.md` at the solution root — it lists every ApplicationType with its exact `Name` value.

---

## 4. Where to Place the Files

Always place scanned reference images in:

```
Visa2026.Module/Resources/FormTemplates/
```

---

## 5. Annotation — Helping Me Identify Fields

The clean scan alone is sufficient. However, if any fields are ambiguous
(e.g. an unlabelled blank line, or several similar-looking fields), providing
an annotated companion image eliminates back-and-forth questions.

### Annotated Companion File

Name it with a `_map` suffix alongside the clean scan:

| Clean scan | Annotated companion |
|---|---|
| `App_Inv_item.jpg` | `App_Inv_item_map.jpg` |
| `App_Visa_Ext_FM_app_v0.jpg` | `App_Visa_Ext_FM_app_v0_map.jpg` |

### Annotation Color Convention

Open the scanned image in any image editor (Paint, Word, Snipping Tool, etc.)
and mark it up using the following colors:

| Color | Meaning | What I will do |
|---|---|---|
| 🟡 **Yellow highlight** | Data field — write the property name on top of the highlight | Place `XRLabel` with `ExpressionBinding` bound to that property |
| 🔴 **Red cross or red box** | Ignore this area — stamp, logo, official seal, decorative element | Skip — place no control |
| 🔵 **Blue highlight** | Static text that must appear on the report but is NOT pre-printed on the form | Place static `XRLabel` with `.Text` set |
| No marking | Pre-printed label already visible in the scan (e.g. "Familiýasy:") | Replicate as static `XRLabel` or treat as part of the visual context |

### Property Names

When writing a property name on a yellow highlight, use the exact flattened property name
from the business object. Examples:

```
Person_FullName
Person_DateOfBirthText
Passport_Number
Passport_ExpirationDateText
Application_FullNumber
CompanyHead_FullName
```

The full property list for each data type is in `REPORTS.md` under the Data Sources section.

### Example

```
┌─────────────────────────────────────────────┐
│  Familiýasy:  ___________________________   │
│               🟡 Person_FullName            │
│                                             │
│  Doglan senesi: ________________________    │
│                 🟡 Person_DateOfBirthText   │
│                                             │
│  [OFFICIAL SEAL]                            │
│       🔴                                    │
└─────────────────────────────────────────────┘
```

---

## 6. When to Include ProjectContract in the Filename

`ProjectContract` dependency is **optional**. Use the following three cases to decide
whether to include the contract code in the filename:

| Case | Situation | What to do |
|---|---|---|
| **No contract** | ApplicationType does not use ProjectContract at all | No `_{Code}` segment — use generic filename |
| **Contract shown, same layout** | ProjectContract is visible on the Application but the form looks identical regardless of which contract is selected | No `_{Code}` segment — one generic image covers all contracts |
| **Contract shown, different layout** | Each contract has its own physically different form (different field positions, different static text) | Include `_{Code}` segment — one image per contract |

**Decision test:** if you have two scanned forms for the same ApplicationType but different
contracts — compare them side by side. If the layout is structurally the same → use one
generic file without the code. If they differ → provide one file per contract with the code.

**Examples:**

| Situation | Filename |
|---|---|
| `App_Inv` — no ProjectContract | `App_Inv_item.jpg` |
| `App_Inv` — ProjectContract shown but same layout for all | `App_Inv_item.jpg` |
| `App_Inv` — CLK has its own form layout | `App_Inv_CLK_item.jpg` |
| `App_Inv` — TAPI has its own form layout | `App_Inv_TAPI_item.jpg` |

---

## 7. What NOT to Do

| Do not | Why |
|---|---|
| Name the file without the `_level` segment (e.g. `App_Inv.jpg`) | I cannot determine which data type (Application / ApplicationItem / Registration) this image belongs to |
| Use `.png`, `.pdf`, `.docx` extensions | Only `.jpg` is accepted in `FormTemplates/` |
| Place the file anywhere other than `Resources/FormTemplates/` | I will not find it |
| Use a project contract code in lowercase (e.g. `_clk`) | Must be ALL CAPS to be distinguishable from the level segment |
| Omit `_v{n}` when multiple variants exist | I will treat it as a single-variant form and will not know additional variants exist |

---

## 7. Checklist Before Handing Over an Image

- [ ] File is `.jpg`
- [ ] Resolution is 150–200 DPI and all text is clearly readable
- [ ] Filename exactly matches `{ApplicationType.Name}` from the database (check `LOOKUPS.md`)
- [ ] `_{level}` segment is present (`_app`, `_item`, or `_reg`)
- [ ] `_{ProjectContract.Code}` segment: included only if this form layout is specific to one contract AND differs from other contracts — omitted in all other cases (see Section 6)
- [ ] `_v{n}` suffix included if 2+ variants exist for this level
- [ ] `_p{n}` suffix included if the form has more than one page
- [ ] File is placed in `Resources/FormTemplates/`
- [ ] If annotated: companion `_map.jpg` is in the same folder, yellow = bound field with property name written on it, red = ignore

---

## 8. Quick Reference — ApplicationType Names

See `LOOKUPS.md` at the solution root for the complete list.
The most common ones:

| Display Name | ApplicationType.Name | Level |
|---|---|---|
| Çakylyk Almak | `App_Inv` | App + Item |
| Çakylyk Almak FM | `App_Inv_FM` | App + Item |
| Çakylyk we Iş Rugsatnamasyny Almak | `App_Inv_And_WP` | App + Item |
| Wiza Möhletini Uzaltmak FM | `App_Visa_Ext_FM` | App + Item |
| Wiza Möhletini Uzaltmak | `App_Visa_Ext` | App + Item |
| Wizany KP>Täze Pasporta Geçirmek | `App_Change_Passport` | App + Item |
| Hasaba Almak (Daşary ýurtdan) | `App_Reg_Check_In` | Reg |
| Hasapdan Çykarmak (Daşary ýurda) | `App_Reg_Check_Out` | Reg |
| Ýüztutmany Ýatyrmak | `App_Cancel_App` | App |
