# Report Map — App_Business_Trip_Arrival_BusinessTrip.jpg / App_Business_Trip_Departure_BusinessTrip.jpg

**Status:** ✅ Complete

---

## Report Identity

| Field | Value |
|---|---|
| Class Name | `AppBusinessTripSanawReport` |
| Base Class | `XtraReport` (standalone, no AppBaseReport needed — no letterhead) |
| Registered Name | `"App Business Trip Sanaw Report"` |
| Display Name (Tm) | `"Iş Sapary — Sanawy"` |
| Reference Images | `App_Business_Trip_Arrival_BusinessTrip.jpg`, `App_Business_Trip_Departure_BusinessTrip.jpg` |

---

## Data

| Field | Value |
|---|---|
| Data Type | `BusinessTrip` |
| Registration Target | `typeof(BusinessTrip)` — inplace report (mirrors AppInvItemReport pattern) |
| Visibility Criteria | `[Application.ApplicationType.Name] In ('App_Business_Trip_Arrival', 'App_Business_Trip_Departure')` |
| Shared vs Per-Type | **Shared — one report for both ApplicationTypes** |
| Background Rule | None — clean white page, no letterhead |

---

## Page Setup

| Property | Value |
|---|---|
| Orientation | **Landscape** |
| Paper | A4 |
| Margins | L=100F, R=100F, T=50F, B=100F |
| PageWidthF | 1169.291F |
| PageHeightF | 826.7717F |
| Printable width | **969.291F** |

---

## Band Map

| Band | HeightF | Contents |
|---|---|---|
| TopMargin | 50F | — |
| ReportHeader | 55F | Centered title label |
| PageHeader | 80F | Table column header row (XRTable, all borders) |
| Detail | 70F (CanGrow) | One data row per BusinessTrip (XRTable, same column widths) |
| ReportFooter | 80F | Signatory block |
| BottomMargin | 100F | — |

---

## Column Map

Total width = **969F** (fills printable width exactly).

| Col | Header Text | WidthF | X Start | Data Expression | Notes |
|---|---|---|---|---|---|
| 1 | `№` | 28F | 0F | `sumRecordNumber()` | Centered |
| 2 | `Familiýasy` | 72F | 28F | `[Person_LastName]` | |
| 3 | `Ady` | 60F | 100F | `[Person_FirstName]` | |
| 4 | `Doglan senesi we ýeri` | 80F | 160F | `[Person_DateOfBirthText] + '\n' + [Person_BirthPlace]` | CanGrow |
| 5 | `Jynsy` | 40F | 240F | `[Person_GenderTm]` | Centered |
| 6 | `Raýatlygy` | 50F | 280F | `[Person_NationalityCode]` | Centered |
| 7 | `Pasport belgisi we möhleti` | 80F | 330F | `[Passport_Number] + '\n' + [Passport_ExpirationDateText]` | CanGrow |
| 8 | `Wezipesi` | 145F | 410F | `[Position_NameTm]` | CanGrow |
| 9 | `Möhleti we gezekligi` | 85F | 555F | `[Visa_NumberAndType] + '\n' + [Visa_StartDateText] + '\n' + [Visa_ExpirationDateText]` | CanGrow |
| 10 | `Türkmenistandaky salgysy` | 165F | 640F | `[Address_FullAddress]` | CanGrow |
| 11 | `Iş saparynda boljak salgysy` | 164F | 805F | `[BusinessTripAddress_FullAddress]` | CanGrow |

> Sum: 28+72+60+80+40+50+80+145+85+165+164 = **969F** ✓

---

## Control Map — ReportHeader

| Control | Type | LocationFloat | SizeF | Value | Notes |
|---|---|---|---|---|---|
| `xrLabelTitle` | XRLabel | (0F, 10F) | (969F, 35F) | `"Daşary ýurt raýatlarynyň sanawy"` | Bold, Times New Roman 15pt, MiddleCenter |

---

## Control Map — PageHeader

Single `XRTable` at (0F, 0F), size (969F, 80F), all borders:
- Font: Times New Roman 12pt
- TextAlignment: MiddleCenter, WordWrap: true, Multiline: true

---

## Control Map — Detail

Single `XRTable` at (0F, 0F), size (969F, 70F), all borders, same column widths as header:
- Font: Times New Roman 12pt
- CanGrow: true on table and all cells
- TextAlignment: MiddleCenter for №, Jynsy, Raýatlygy; MiddleLeft for all others
- ExpressionBinding("BeforePrint", "Text", ...) on each data cell

---

## Control Map — ReportFooter

| Control | Type | LocationFloat | SizeF | Expression | Notes |
|---|---|---|---|---|---|
| `xrLabelSignatoryPosition` | XRLabel | (0F, 10F) | (450F, 50F) | `[Application_CompanyHead_PositionTm]` | Bold, Times New Roman 15pt, TopLeft, CanGrow, WordWrap |
| `xrLabelSignatoryFullName` | XRLabel | (550F, 10F) | (419F, 28F) | `[Application_CompanyHead_FullName]` | Bold, Times New Roman 15pt, MiddleRight |

---

## Ignored Elements

| Element | Reason |
|---|---|
| Round stamp / seal | Physical stamp — do not replicate |
| Handwritten signature | Physical — do not replicate |

---

## Required BO Properties on `BusinessTrip`

All `[NotMapped]` unless noted.

| Property | Exists? | Expression |
|---|---|---|
| `Person_LastName` | ✅ | `Person?.LastName` |
| `Person_FirstName` | ✅ | `Person?.FirstName` |
| `Person_DateOfBirthText` | ✅ | `$"{Person?.DateOfBirth:dd.MM.yyyy}"` |
| `Person_BirthPlace` | ✅ | `Person?.BirthPlace` (verify field name) |
| `Person_GenderTm` | ✅ | `Person?.Gender?.NameTm` or similar |
| `Person_NationalityCode` | ✅ | `Person?.Nationality?.Code` |
| `Passport_Number` | ✅ | `CurrentPassport?.Number` |
| `Passport_ExpirationDateText` | ✅ | `$"{CurrentPassport?.ExpirationDate:dd.MM.yyyy}"` |
| `Position_NameTm` | ✅ | `Person?.CurrentPositionHistory?.Position?.NameTm` |
| `Visa_NumberAndType` | ✅ | `CurrentVisa?.Number + " " + CurrentVisa?.VisaCategory?.NameTm` (verify) |
| `Visa_StartDateText` | ✅ | `$"{CurrentVisa?.StartDate:dd.MM.yyyy}"` |
| `Visa_ExpirationDateText` | ✅ | `$"{CurrentVisa?.ExpirationDate:dd.MM.yyyy}"` |
| `Address_FullAddress` | ✅ | `CurrentAddressOfResidence?.FullAddress` |
| `BusinessTripAddress_FullAddress` | ✅ | `BusinessTripAddress?.FullAddress` |
| `Application_CompanyHead_FullName` | ✅ | `Application?.CompanyHead?.FullName` |
| `Application_CompanyHead_PositionTm` | ✅ | `Application?.CompanyHead?.Position?.NameTm` |
