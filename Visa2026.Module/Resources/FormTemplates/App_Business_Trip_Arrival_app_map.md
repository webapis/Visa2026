# Report Map — App_Business_Trip_Arrival_app.jpg

**Status:** ✅ Complete

---

## Report Identity

| Field | Value |
|---|---|
| Class Name | `AppBusinessTripArrivalReport` |
| Registered Name | `"App Business Trip Arrival Report"` |
| Display Name (Tm) | `"Iş Sapary — Geliş"` |
| Reference Image | `App_Business_Trip_Arrival_app.jpg` |

---

## Data

| Field | Value |
|---|---|
| Data Type | `Application` |
| Base Class | `AppGroupEBaseReport` |
| Visibility Criteria | `[ApplicationType.Name] = 'App_Business_Trip_Arrival'` |
| Shared vs Per-Type | Per-type |
| Background Rule | Company letterhead — loaded automatically by `AppBaseReport` at runtime via `Company.Code` |

---

## Page Setup

| Property | Value | Source |
|---|---|---|
| Orientation | Portrait | Inherited from `AppBaseReport` |
| Paper | A4 | Inherited |
| Margins | L=100F, R=100F, T=50F, B=100F | Inherited |
| Printable width | 626.7717F | Inherited |

---

## Band Map

| Band | HeightF | Source |
|---|---|---|
| TopMargin | 50F | Inherited |
| PageHeader | 200F | Inherited — app number + date labels |
| Detail | ~560F | Mostly inherited; derived adds Body3, repositions Body2 |
| ReportFooter | 50F | Inherited — signatory labels |
| BottomMargin | 100F | Inherited |

---

## Control Map — PageHeader

| Control | Source | Value / Expression | Notes |
|---|---|---|---|
| `xrPictureBoxBackground` | Inherited | Company letterhead image | Loaded at runtime by `AppBaseReport` |
| `xrLabelAppNumber` | Inherited | `[FullApplicationNumber]` | Bold, top-left |
| `xrLabelAppDate` | Inherited | `[ApplicationDate]` format `{0:dd.MM.yyyy} ý.` | Bold, below number |

---

## Control Map — Detail

| Control | Type | LocationFloat | SizeF | Source | Value / Expression | Notes |
|---|---|---|---|---|---|---|
| `xrLabelRecipient` | XRLabel | (220F, 218F) | (406.7717F, 80F) | Inherited | `[MigrationService_NameTm]` | Bold, right-aligned, CanGrow |
| `xrRichBody1` | XRRichText | (0F, 313F) | (626.7717F, 80F) | **Derived sets RTF** | See body text below | CanGrow; Y fixed by base |
| `xrRichBody3` | XRRichText | (0F, 401F+gap) | (626.7717F, 40F) | **New in derived** | Maksady paragraph (see below) | CanGrow; Y = Body1.Bottom + 8F |
| `xrRichBody2` | XRRichText | (0F, Body3.Bottom+8F) | (626.7717F, 80F) | Inherited; **repositioned** | `RtfResponsibility` (static) | CanGrow; must override Y |

---

### Body1 RTF (Notification Paragraph)

RTF paragraph — justified, first-line indent 0.5″ (fi720), Times New Roman 15pt.
Dynamic values embedded as `[FieldName]` inline; bold phrases use `\b...\b0`.

**Text structure:**
```
Hatymyzyň goşundysynda görkezilen sanawdaky
**[TotalPersonCountText] ([TotalPersonCount]) sany**
daşary ýurt raýatlynyň
**[BusinessTripStartDateText]-den [BusinessTripEndDateText]-ne**
çenli **[BusinessTripDurationDays] gün** möhlet bilen
**[ToRegionName_Genitive] [ToCityName_Dative]**
iş saparyna **gelendigini** size habar berýäris.
```

---

### Body3 RTF (Maksady Paragraph)

RTF paragraph — justified, first-line indent 0.5″, Times New Roman 15pt.

**Text structure:**
```
Maksady-[BusinessTripPurpose_NameTm]
```

`"Maksady-"` is bold inline; the purpose text follows it on the same line.

---

### Body2 RTF (Responsibility — Inherited)

Static. `AppBaseReport.RtfResponsibility` — the shared clause:
```
Daşary ýurt raýatynyň Türkmenistana gelmeginiň, onda bolmagynyň we ondan
gitmeginiň düzgünlerini berjaý etmegine jogapkärçiligi kompaniýamyz öz üstüne alýar.
```

---

## Control Map — ReportFooter

| Control | Source | Value / Expression | Notes |
|---|---|---|---|
| `xrLabelSignatoryPosition` | Inherited | `[CompanyHead_PositionTm]` | Bold, left half |
| `xrLabelSignatoryFullName` | Inherited | `[CompanyHead_FullName]` | Bold, right half |

---

## Ignored Elements

| Element visible in image | Reason ignored |
|---|---|
| Company logo (top right) | Part of background letterhead image — not a separate control |
| Stamp / round seal | Handwritten / physical stamp — do not replicate |
| Footer address/website text | Part of background letterhead image |
| Handwritten signature | Physical signature — do not replicate |

---

## Required BO Properties

All on `Application`:

| Property | Exists? | Notes |
|---|---|---|
| `FullApplicationNumber` | ✅ | PageHeader, inherited |
| `ApplicationDate` | ✅ | PageHeader, inherited |
| `MigrationService_NameTm` | ✅ | Recipient label, inherited |
| `TotalPersonCount` | ✅ | Inline in body |
| `TotalPersonCountText` | ✅ | Inline in body (Turkmen word form) |
| `ToRegionName_Genitive` | ✅ | "Ahal welaýatynyň" |
| `ToCityName_Dative` | ✅ | "Akbugdaý etrabyna" |
| `BusinessTripStartDateText` | ❌ **Missing** | Add: `$"{BusinessTripStartDate:dd.MM.yyyy}"` |
| `BusinessTripEndDateText` | ❌ **Missing** | Add: `$"{BusinessTripEndDate:dd.MM.yyyy}"` |
| `BusinessTripDurationDays` | ❌ **Missing** | Add: `(int?)((BusinessTripEndDate - BusinessTripStartDate)?.TotalDays + 1)` (inclusive) |
| `BusinessTripPurpose_NameTm` | ✅ | `BusinessTripPurpose?.Name` |
| `CompanyHead_PositionTm` | ✅ | Footer, inherited |
| `CompanyHead_FullName` | ✅ | Footer, inherited |
