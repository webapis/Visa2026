# Map: App_Border_Zone_Permission_item

**Report class:** `AppBorderZonePermissionItemReport`  
**Target type:** `ApplicationItem`  
**Reference image:** `App_Border_Zone_Permission_item.jpg`  
**Page:** A4 Landscape, Margins 20F left/right, 50F top, 50F bottom  
**Printable width:** 1129.291F

---

## Layout Overview

```
TopMargin       (50F)     — inherited from AppItemBaseReport
PageHeader      (75F)     — title label (30F) + column header table (45F)
Detail          (80F+)    — data row table, CanGrow
ReportFooter    (80F)     — signatory block (repositioned for landscape)
BottomMargin    (60F)     — inherited from AppItemBaseReport
```

---

## PageHeader Controls

| Control | X | Y | Width | Height | Font | Notes |
|---|---|---|---|---|---|---|
| `xrLabelTitle` | 0F | 5F | 1129.291F | 22F | TNR 10pt Bold, MiddleCenter | "Daşary ýurt raýatlarynyň sanawy" |
| `xrTableHeader` | 0F | 30F | 1129.291F | 45F | — | Column header row |

---

## Column Header / Data Cell Map

| Control Name | Header Text | Weight | Data Expression |
|---|---|---|---|
| `xrHdrNo` / `xrCellNo` | № | 25 | `RowNumber()` |
| `xrHdrFamiliyasy` / `xrCellFamiliyasy` | Familiýasy | 75 | `[Person_LastName]` |
| `xrHdrAdy` / `xrCellAdy` | Ady | 65 | `[Person_FirstName]` |
| `xrHdrDoglanSenesi` / `xrCellDoglanSenesi` | Doglan senesi we ýeri | 85 | `[Person_DateOfBirthText] + Char(10) + [Person_CountryOfBirthTm] + '/' + [Person_BirthPlace]` |
| `xrHdrJynsy` / `xrCellJynsy` | Jynsy | 40 | `[Person_GenderTm]` |
| `xrHdrRayatlygy` / `xrCellRayatlygy` | Raýatlygy | 45 | `[Person_NationalityCode]` |
| `xrHdrPasport` / `xrCellPasport` | Pasport belgisi we möhleti | 85 | `[Passport_Number] + Char(10) + [Passport_ExpirationDateText]` |
| `xrHdrWezipesi` / `xrCellWezipesi` | Wezipesi | 120 | `[Position_PositionTm]` |
| `xrHdrMohleti` / `xrCellMohleti` | Möhleti we gezekligi | 100 | `[WorkPermit_Number] + Char(10) + 'WP' + Char(10) + [WorkPermit_StartDateText] + Char(10) + [WorkPermit_ExpirationDateText]` |
| `xrHdrTmSalgysy` / `xrCellTmSalgysy` | Türkmenistanaky salgysy | 215 | `[Address_FullAddress]` |
| `xrHdrSerhet` / `xrCellSerhet` | Barjak serhet ýakasy | 274.291 | `[Application_BorderZoneLocation_NameTm]` |

**Weight total:** 25+75+65+85+40+45+85+120+100+215+274.291 = **1129.291** ✓

---

## Table Properties

| Property | Header row | Data row |
|---|---|---|
| Font | TNR 7pt Bold | TNR 7pt |
| TextAlignment | MiddleCenter | MiddleCenter |
| WordWrap | true | true |
| CanGrow | — | true |
| Multiline | — | true on: DoglanSenesi, Pasport, Mohleti, Wezipesi |
| Borders | All | Left+Right+Bottom |
| BorderWidth | 0.5F | 0.5F |
| BorderColor | Black | Black |
| BackColor | Transparent | Transparent |

---

## ReportFooter (repositioned for landscape)

| Control | X | Y | Width | Height | Alignment | Expression |
|---|---|---|---|---|---|---|
| `xrLabelSignatoryPosition` | 0F | 40F | 564F | 20F | MiddleCenter Bold | `[CompanyHead_PositionTm]` |
| `xrLabelSignatoryFullName` | 565F | 40F | 564.291F | 20F | MiddleCenter Bold | `[CompanyHead_FullName]` |

---

## Differences from AppItemInvSanawBaseReport (14-col base)

This report drops 3 columns present in the standard 14-column sanawy:
- Bilimi we okan ýeri (Education level + institution)
- Bilimine görä hünäri (Education specialty)
- Daşary ýurtdaky salgysy (Foreign address)

The freed column space is redistributed to Wezipesi, Möhleti, Türkmenistanaky salgysy, and Barjak serhet ýakasy.

The "Möhleti we gezekligi" column uses work permit data (`WorkPermit_Number`, `WorkPermit_StartDateText`, `WorkPermit_ExpirationDateText`) rather than visa period/category.
