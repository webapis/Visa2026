# Report Map — WorkPermit_list.jpg

**Status:** ✅ Agreed

---

## Report Identity

| Field | Value |
|---|---|
| Class Name | `WorkPermitListReport` |
| Base Class | `XtraReport` (standalone — no letterhead, no AppBaseReport) |
| Registered Name | `"Work Permit List Report"` |
| Display Name (Tm) | `"Iş Rugsatnamasy — Sanawy"` |
| Reference Image | `WorkPermit_list.jpg` |
| ApplicationType Name | *(none — standalone WorkPermitItem report)* |

---

## Data

| Field | Value |
|---|---|
| Data Type | `WorkPermitItem` |
| Data Source | `CollectionDataSource` for `Visa2026.Module.BusinessObjects.WorkPermitItem` |
| Registration Target | `typeof(WorkPermitItem)` |
| Visibility Criteria | *(none — always visible for WorkPermitItem)* |
| Background Rule | None — clean white page |

---

## Page Setup

| Property | Value |
|---|---|
| Orientation | Portrait |
| Paper | A4 |
| Margins | L=50F, R=50F, T=50F, B=60F |
| PageWidthF | 826.7717F |
| PageHeightF | 1169.291F |
| Printable width | **726.7717F** |

---

## Band Map

| Band | HeightF | Contents |
|---|---|---|
| TopMargin | 50F | — |
| ReportHeader | 70F | Header text + "MAGLUMAT" label |
| PageHeader | 55F | Column header XRTable |
| Detail | 80F (CanGrow) | One data row per WorkPermitItem |
| ReportFooter | 130F | Footer note + signatory block |
| BottomMargin | 60F | — |

---

## Column Map

Total width = **726.7717F**

| Col | Header Text | WidthF | Data Expression | Notes |
|---|---|---|---|---|
| 1 | `№` | 20F | `sumRecordNumber()` | Centered |
| 2 | `F.A.A` | 78F | `[Person_FullName]` | |
| 3 | `Doglan ýyly we raýatlygy` | 62F | `[Person_DateOfBirthText] + Char(10) + [Person_NationalityCode]` | Multiline |
| 4 | `Pasport belgisi we möhleti` | 70F | `[Passport_Number] + Char(10) + [Passport_ExpirationDateText]` | Multiline |
| 5 | `Bilimi` | 52F | `[Education_LevelTm]` | |
| 6 | `Wezipesi` | 108F | `[Position_NameTm]` | CanGrow |
| 7 | `Türkmenistan-daky anyk ýaşaýan salgysy` | 135F | `[Address_FullAddress]` | CanGrow |
| 8 | `Rugsatnama belgisi we möhleti` | 72F | `[WP_Number] + Char(10) + [WP_StartDateText] + Char(10) + [WP_ExpirationDateText]` | Multiline |
| 9 | `Wiza belgisi we möhleti` | 72F | `[Visa_Number] + Char(10) + [Visa_StartDateText] + Char(10) + [Visa_ExpirationDateText]` | Multiline |
| 10 | `Bellik` | 57.7717F | *(empty)* | |

> Sum: 20+78+62+70+52+108+135+72+72+57.7717 = **726.7717F** ✓

---

## Control Map — ReportHeader

| Control | Type | LocationFloat | SizeF | Expression / Value | Notes |
|---|---|---|---|---|---|
| `xrLabelHeader` | XRLabel | (0F, 5F) | (726.7717F, 45F) | `ToString(Today(), 'dd.MM.yyyy') + ' sene boýunça ' + [Company_Name] + ' Türk kärhanasynyň Türkmenistandaky şahamçasy işleýän daşary ýurt raýatlaryň sanawy barada'` | 10pt, TopLeft, CanGrow, Multiline, WordWrap |
| `xrLabelMaglumat` | XRLabel | (0F, 52F) | (726.7717F, 16F) | `"MAGLUMAT"` | 10pt Bold, MiddleLeft |

---

## Control Map — ReportFooter

| Control | Type | LocationFloat | SizeF | Expression / Value | Notes |
|---|---|---|---|---|---|
| `xrLabelFooterNote` | XRLabel | (0F, 5F) | (726.7717F, 40F) | `"* Kärhanamyzyň hasabynda zähmet çekýän Türkmenistanyň raýatlary baradaky maglumatlaryň doly we dogry görkezilmegine jogapkärçiligi kärhanamyz öz üstüne alýar."` | 9pt, TopLeft, CanGrow, Multiline, WordWrap |
| `xrLabelSignatoryPosition` | XRLabel | (0F, 65F) | (363F, 40F) | `[CompanyHead_PositionTm]` | 10pt Bold, TopLeft, CanGrow, WordWrap |
| `xrLabelSignatoryFullName` | XRLabel | (363F, 75F) | (363.7717F, 20F) | `[CompanyHead_FullName]` | 10pt Bold, MiddleRight |

---

## Required BO Properties on `WorkPermitItem`

All `[NotMapped]` — none currently exist, all must be added.

| Property | Expression | Exists? |
|---|---|---|
| `Person_FullName` | `Person?.FullName` | ❌ Add |
| `Person_DateOfBirthText` | `$"{Person?.DateOfBirth:dd.MM.yyyy}"` | ❌ Add |
| `Person_NationalityCode` | `Person?.Nationality?.Code` | ❌ Add |
| `Education_LevelTm` | `Person?.CurrentEducation?.EducationLevel?.NameTm` | ❌ Add |
| `Passport_Number` | `Passport?.PassportNumber` | ❌ Add |
| `Passport_ExpirationDateText` | `$"{Passport?.ExpirationDate:dd.MM.yyyy}"` | ❌ Add |
| `Position_NameTm` | `CurrentPositionHistory?.Position?.NameTm` | ❌ Add |
| `Address_FullAddress` | `Person?.CurrentAddressOfResidence?.FullAddress` | ❌ Add |
| `WP_Number` | `WorkPermitNumber` | ✅ direct field — use `[WorkPermitNumber]` directly, no NotMapped needed |
| `WP_StartDateText` | `StartDate.ToString("dd.MM.yyyy")` | ❌ Add |
| `WP_ExpirationDateText` | `ExpirationDate.ToString("dd.MM.yyyy")` | ❌ Add |
| `Visa_Number` | `Person?.CurrentVisa?.VisaNumber` | ❌ Add |
| `Visa_StartDateText` | `$"{Person?.CurrentVisa?.StartDate:dd.MM.yyyy}"` | ❌ Add |
| `Visa_ExpirationDateText` | `$"{Person?.CurrentVisa?.ExpirationDate:dd.MM.yyyy}"` | ❌ Add |
| `Company_Name` | `WorkPermit?.Application?.Company?.Name` | ❌ Add |
| `CompanyHead_PositionTm` | `WorkPermit?.Application?.CompanyHead?.Position?.NameTm` | ❌ Add |
| `CompanyHead_FullName` | `WorkPermit?.Application?.CompanyHead?.FullName` | ❌ Add |

---

## Ignored Elements

| Element | Reason |
|---|---|
| Round stamp / seal | Physical stamp — do not replicate |
| Handwritten signature | Physical — do not replicate |
