# Map: App_Cancel_Inv_WP_item

**Report class:** `AppCancelInvWPItemReport`  
**Target type:** `ApplicationItem`  
**Reference image:** `App_Cancel_Inv_WP_item.jpg`  
**Page:** A4 Portrait (default from AppItemBaseReport), Margins 20F left/right, 50F top, 60F bottom  
**Printable width:** 786.7717F

---

## Layout Overview

```
TopMargin       (50F)     — inherited from AppItemBaseReport
PageHeader      (82F)     — title label (27F) + column header table (55F)
Detail          (70F+)    — data row table, CanGrow
ReportFooter    (50F)     — signatory block (default portrait positions)
BottomMargin    (60F)     — inherited from AppItemBaseReport
```

---

## PageHeader Controls

| Control | X | Y | Width | Height | Font | Notes |
|---|---|---|---|---|---|---|
| `xrLabelTitle` | 0F | 5F | 786.7717F | 20F | TNR 10pt Bold, MiddleCenter | "Daşary ýurt raýatlarynyň sanawy" |
| `xrTableHeader` | 0F | 27F | 786.7717F | 55F | — | Column header row |

---

## Column Header / Data Cell Map

| Control Name | Header Text | Weight | Data Expression |
|---|---|---|---|
| `xrHdrNo` / `xrCellNo` | № | 18 | `sumRecordNumber()` + XRSummary |
| `xrHdrASNo` / `xrCellASNo` | AS-№ | 60 | `[WorkPermit_Number]` |
| `xrHdrTassykNama` / `xrCellTassykNama` | Tassyk-nama belgisi | 47 | `[WorkPermit_ASNumber]` |
| `xrHdrFamiliyasy` / `xrCellFamiliyasy` | Familiýasy | 52 | `[Person_LastName]` |
| `xrHdrAdy` / `xrCellAdy` | Ady | 44 | `[Person_FirstName]` |
| `xrHdrDoglanSenesi` / `xrCellDoglanSenesi` | Doglan senesi we şurdy | 68 | `[Person_DateOfBirthText] + Char(10) + [Person_CountryOfBirthTm] + '/' + [Person_BirthPlace]` |
| `xrHdrPasport` / `xrCellPasport` | Pasport belgisi | 60 | `[Passport_Number] + Char(10) + [Passport_ExpirationDateText]` |
| `xrHdrHunari` / `xrCellHunari` | Hünäri we bilimi | 115 | `[Education_LevelTm] + ', ' + [Position_PositionTm]` |
| `xrHdrHereket` / `xrCellHereket` | Hereket edýän çägi | 72 | `[WorkPermit_WorkPermittedLocations]` |
| `xrHdrRugsat` / `xrCellRugsat` | Rugsat edilen möhleti | 52 | `[WorkPermit_ExpirationDateText]` |
| `xrHdrCakylyk` / `xrCellCakylyk` | Çakylyk belgisi | 57 | `[Invitation_Number]` |
| `xrHdrResmilesen` / `xrCellResmilesen` | Çakylygyň resmileşdirilen senesi | 63 | `[Invitation_StartDateText]` |
| `xrHdrMohletTamam` / `xrCellMohletTamam` | Çakylygyň möhleti tamamlanýan sene | 78.7717 | `[Invitation_ExpirationDateText]` |

**Weight total:** 18+60+47+52+44+68+60+115+72+52+57+63+78.7717 = **786.7717** ✓

---

## Table Properties

| Property | Header row | Data row |
|---|---|---|
| Font | TNR 6pt Bold | TNR 6pt |
| TextAlignment | MiddleCenter | MiddleCenter |
| WordWrap | true | true |
| CanGrow | — | true |
| Multiline | — | true on: DoglanSenesi, Pasport, Hunari, Hereket |
| Padding | — | PaddingInfo(3, 3, 2, 2) |
| Borders | All | Left+Right+Bottom |
| BorderWidth | 0.5F | 0.5F |
| BorderColor | Black | Black |
| BackColor | Transparent | Transparent |

---

## New NotMapped Properties Added to ApplicationItem.cs

| Property | Source |
|---|---|
| `WorkPermit_ASNumber` | `CurrentWorkPermitItem?.ASNumber` |
| `WorkPermit_WorkPermittedLocations` | `CurrentWorkPermitItem?.WorkPermittedLocations` |
| `Invitation_Number` | `CurrentInvitationItem?.Invitation?.InvitationNumber` |
| `Invitation_StartDateText` | `CurrentInvitationItem?.Invitation?.StartDate` (dd.MM.yyyy) |
| `Invitation_ExpirationDateText` | `CurrentInvitationItem?.Invitation?.ExpirationDate` (dd.MM.yyyy) |
