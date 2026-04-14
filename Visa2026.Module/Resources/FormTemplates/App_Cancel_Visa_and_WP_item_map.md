# Map: App_Cancel_Visa_and_WP_item

**Report class:** `AppCancelVisaAndWPItemReport`  
**Target type:** `ApplicationItem`  
**Reference image:** `App_Cancel_Visa_and_WP_item.jpg`  
**Page:** A4 Landscape, Margins 20F left/right, 50F top, 50F bottom  
**Printable width:** 1129.291F

---

## Layout Overview

```
TopMargin       (50F)     — inherited from AppItemBaseReport
PageHeader      (80F)     — title label (22F) + column header table (50F)
Detail          (80F+)    — data row table, CanGrow
ReportFooter    (80F)     — signatory block (landscape positions)
BottomMargin    (50F)
```

---

## PageHeader Controls

| Control | X | Y | Width | Height | Font | Notes |
|---|---|---|---|---|---|---|
| `xrLabelTitle` | 0F | 5F | 1129.291F | 22F | TNR 10pt Bold, MiddleCenter | "Daşary ýurt raýatlarynyň sanawy" |
| `xrTableHeader` | 0F | 30F | 1129.291F | 50F | — | Column header row |

Inherited labels hidden: `xrLabelAppNumber.Visible = false`, `xrLabelAppDate.Visible = false`

---

## Column Header / Data Cell Map

| Control Name | Header Text | Weight | Data Expression |
|---|---|---|---|
| `xrHdrNo` / `xrCellNo` | № | 22 | `sumRecordNumber()` + XRSummary |
| `xrHdrASNo` / `xrCellASNo` | AS-№ | 85 | `[WorkPermit_Number]` |
| `xrHdrTassykNama` / `xrCellTassykNama` | Tassyk-nama belgisi | 68 | `[WorkPermit_ASNumber]` |
| `xrHdrFamiliyasy` / `xrCellFamiliyasy` | Familiýasy | 75 | `[Person_LastName]` |
| `xrHdrAdy` / `xrCellAdy` | Ady | 62 | `[Person_FirstName]` |
| `xrHdrDoglanSenesi` / `xrCellDoglanSenesi` | Doglan senesi we şurdy | 95 | `[Person_DateOfBirthText] + Char(10) + [Person_CountryOfBirthTm] + '/' + [Person_BirthPlace]` |
| `xrHdrPasport` / `xrCellPasport` | Pasport belgisi | 85 | `[Passport_Number] + Char(10) + [Passport_ExpirationDateText]` |
| `xrHdrHunari` / `xrCellHunari` | Hünäri we bilimi | 180 | `[Education_LevelTm] + ', ' + [Position_PositionTm]` |
| `xrHdrHereket` / `xrCellHereket` | Hereket edýän çägi | 100 | `[WorkPermit_WorkPermittedLocations]` |
| `xrHdrRugsat` / `xrCellRugsat` | Rugsat edililen möhleti | 75 | `[WorkPermit_StartDateText] + Char(10) + [WorkPermit_ExpirationDateText]` |
| `xrHdrWizaBelgisi` / `xrCellWizaBelgisi` | Wiza belgisi | 75 | `[Visa_Number]` |
| `xrHdrWizaMohleti` / `xrCellWizaMohleti` | Wiza möhleti başlanýan we tamamlanýan senesi | 207.291 | `[Visa_StartDateText] + Char(10) + [Visa_ExpirationDateText]` |

**Weight total:** 22+85+68+75+62+95+85+180+100+75+75+207.291 = **1129.291** ✓

---

## Table Properties

| Property | Header row | Data row |
|---|---|---|
| Font | TNR 7pt Bold | TNR 7pt |
| TextAlignment | MiddleCenter | MiddleCenter |
| WordWrap | true | true |
| CanGrow | — | true |
| Multiline | — | true on: DoglanSenesi, Pasport, Hunari, Hereket, Rugsat, WizaMohleti |
| Padding | — | PaddingInfo(3, 3, 2, 2) |
| Borders | All | Left+Right+Bottom |
| BorderWidth | 0.5F | 0.5F |
| BorderColor | Black | Black |
| BackColor | Transparent | Transparent |

---

## Signatory (Landscape positions)

| Label | X | Y | Width | TextAlignment |
|---|---|---|---|---|
| `xrLabelSignatoryPosition` | 0F | 40F | 564F | MiddleCenter |
| `xrLabelSignatoryFullName` | 565F | 40F | 564.291F | MiddleCenter |
