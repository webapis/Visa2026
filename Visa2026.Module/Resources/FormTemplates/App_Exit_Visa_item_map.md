# Report Map — App_Exit_Visa_item.jpg

**Status:** ✅ Implemented — `AppExitVisaItemReport`

---

## Report Identity

| Field | Value |
|---|---|
| Class Name | `AppExitVisaItemReport` |
| Base Class | `AppItemInvSanawBaseReport` → `AppItemBaseReport` → `XtraReport` |
| Registered Name | `"App Exit Visa Item Report"` |
| Display Name (Tm) | `"Çykyş Wiza — Sanawy"` |
| Reference Image | `App_Exit_Visa_item.jpg` |
| ApplicationType Name | `App_Exit_Visa` |

---

## Data

| Field | Value |
|---|---|
| Data Type | `ApplicationItem` |
| Registration Target | `typeof(ApplicationItem)` |
| Visibility Criteria | `[Application.ApplicationType.Name] = 'App_Exit_Visa'` |
| Background Rule | None — clean white page (landscape sanawy) |

---

## Page Setup

Inherited from `AppItemInvSanawBaseReport` (Landscape A4):

| Property | Value |
|---|---|
| Orientation | Landscape |
| Paper | A4 |
| Margins | L=20F, R=20F, T=50F, B=50F |
| PageWidthF | 1169.291F |
| PageHeightF | 826.7717F |
| Printable width | **1129.291F** |

---

## What AppItemInvSanawBaseReport Provides (do not redeclare)

14-column "Daşary ýurt raýatlarynyň sanawy" table — all columns, layout, signatory footer, title. See `AppItemInvSanawBaseReport.Designer.cs`.

**Only one column differs from the base:**

| Column | Base expression | Exit Visa override |
|---|---|---|
| Möhleti we gezekligi | `[Application_VisaPeriod_NameTm] + ' ' + [Application_VisaCategory_NameTm]` | `[Visa_StartDateText] + Char(10) + [Visa_ExpirationDateText] + Char(10) + '(' + [Visa_Number] + ') ' + [Visa_CategoryTm]` |

All other 13 columns unchanged.

---

## Derived Override (constructor only)

```csharp
// xrCellMohleti — exit visa shows actual visa dates + number + category
// rather than the application-level period + category stored on Application
this.xrCellMohleti.ExpressionBindings.Clear();
this.xrCellMohleti.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
    "[Visa_StartDateText] + Char(10) + [Visa_ExpirationDateText] + Char(10) + '(' + [Visa_Number] + ') ' + [Visa_CategoryTm]"));
```

---

## Required BO Properties

All on `ApplicationItem`. Properties used by the base are already confirmed. Only the override needs verification:

| Property | Exists? | Source |
|---|---|---|
| `Visa_StartDateText` | ✅ | `ApplicationItem` (NotMapped) |
| `Visa_ExpirationDateText` | ✅ | `ApplicationItem` (NotMapped) |
| `Visa_Number` | ✅ | `ApplicationItem` (NotMapped) |
| `Visa_CategoryTm` | ✅ | `ApplicationItem` (NotMapped) |

---

## ReportsUpdater.cs entry

```csharp
AddPredefinedReport<AppExitVisaItemReport>("App Exit Visa Item Report", typeof(ApplicationItem), isInplaceReport: true);
CreateReportVisibility("App Exit Visa Item Report", "[Application.ApplicationType.Name] = 'App_Exit_Visa'");
```

---

## Ignored Elements

| Element | Reason |
|---|---|
| Round stamp / seal | Physical stamp — do not replicate |
| Handwritten signature | Physical — do not replicate |
