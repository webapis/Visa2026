# Map: App_Change_Inv_item

**Report class:** `AppChangeInvItemReport`  
**Target type:** `ApplicationItem`  
**Reference image:** `App_Change_Inv_item.jpg`  
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
| `xrHdrNo` / `xrCellNo` | № | 20 | `sumRecordNumber()` + XRSummary |
| `xrHdrFamiliyasy` / `xrCellFamiliyasy` | Familiýasy | 70 | `[Person_LastName]` |
| `xrHdrAdy` / `xrCellAdy` | Ady | 60 | `[Person_FirstName]` |
| `xrHdrDoglanSenesi` / `xrCellDoglanSenesi` | Doglan senesi we ýurdy | 90 | `[Person_DateOfBirthText] + Char(10) + [Person_CountryOfBirthTm] + '/' + [Person_BirthPlace]` |
| `xrHdrJynsy` / `xrCellJynsy` | Jynsy | 42 | `[Person_GenderTm]` |
| `xrHdrRayatlygy` / `xrCellRayatlygy` | Raýatlygy | 42 | `[Person_NationalityCode]` |
| `xrHdrPasport` / `xrCellPasport` | Pasport belgisi we möhleti | 85 | `[Passport_Number] + Char(10) + [Passport_ExpirationDateText]` |
| `xrHdrBilimi` / `xrCellBilimi` | Bilimi we okan ýeri | 95 | `[Education_LevelTm] + Char(10) + [Education_InstitutionName]` |
| `xrHdrHunari` / `xrCellHunari` | Bilimine görä hünäri | 95 | `[Education_SpecialtyTm]` |
| `xrHdrWezipesi` / `xrCellWezipesi` | Wezipesi | 120 | `[Position_PositionTm]` |
| `xrHdrTmSalgysy` / `xrCellTmSalgysy` | Türkmenistandaky salgysy | 145 | `[Address_FullAddress]` |
| `xrHdrDasarySalgysy` / `xrCellDasarySalgysy` | Daşary ýurtdaky salgysy | 135 | `[Person_ForeignAddress]` |
| `xrHdrBarjakSerhet` / `xrCellBarjakSerhet` | Barjak serhet ýakasy | 130.291 | `[WorkPermit_WorkPermittedLocations]` |

**Weight total:** 20+70+60+90+42+42+85+95+95+120+145+135+130.291 = **1129.291** ✓

---

## Table Properties

| Property | Header row | Data row |
|---|---|---|
| Font | TNR 6pt Bold | TNR 7pt |
| TextAlignment | MiddleCenter | MiddleCenter |
| WordWrap | true | true |
| CanGrow | — | true |
| Multiline | — | true on: DoglanSenesi, Pasport, Bilimi, Hunari, Wezipesi, TmSalgysy, DasarySalgysy, BarjakSerhet |
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
