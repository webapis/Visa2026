# Report Map: App_Change_Inv_app

**Status:** ✅ Implemented — `AppChangeInvReport`
**Inherits from:** `AppBaseReport` (directly — unique structure with DetailReportBand)

---

## Identity

| | |
|---|---|
| Class | `AppChangeInvReport` |
| Registered name | `"App Change Inv Report"` |
| Display name (Tm) | `"Çakylygy üýtgetmek — Ýüztutma"` |
| ApplicationType | `App_Change_Inv` |
| Visibility criteria | `[ApplicationType.Name] = 'App_Change_Inv'` |
| Data type | `Application` |
| Reference image | `App_Change_Inv_app.jpg` |

---

## What the base provides

`AppBaseReport` supplies:
- Dynamic letterhead background per `Company.Code`
- PageHeader: `xrLabelAppNumber` (`[FullApplicationNumber]`), `xrLabelAppDate` (`[ApplicationDate]`)
- ReportFooter: `xrLabelSignatoryPosition` (`[CompanyHead.Position.NameTm]`), `xrLabelSignatoryFullName` (`[CompanyHead.FullName]`)

This report does **not** use a group base — it has a DetailReportBand for the invitation table, which no group base supports.

---

## Detail band (application-level controls)

| Control | Type | X | Y | W | H | Value / Notes |
|---|---|---|---|---|---|---|
| `xrLabelRecipient` | XRLabel | 220F | 20F | 406.77F | 80F | Static: "Türkmenistanyň Döwlet migrasiýa gullugynyň başlygyna" — Bold, TopLeft, CanGrow, WordWrap |
| `xrRichBody1` | XRRichText | 0F | 115F | 626.77F | 100F | Request paragraph (see below) — justified, `\fi720`, CanGrow |
| `xrRichBody2` | XRRichText | 0F | 223F | 626.77F | 80F | Static responsibility paragraph (`AppBaseReport.RtfResponsibility`) |
| `xrLabelTableTitle` | XRLabel | 0F | 311F | 626.77F | 25F | Static: "Üýtgedilmeli çakylyklar:" — Bold, MiddleLeft |
| `xrTableHeader` | XRTable | 0F | 336F | 626.77F | 30F | Column headers — Bold, all borders |

**Detail.HeightF = 366F**

### xrRichBody1 — request paragraph

```
Hatymyzyň goşundysynda görkezilen sanawdaky **[TotalPersonCount] ([TotalPersonCountText]) sany**
daşary ýurt raýatynyň **pasportyny çalyşandygy** sebäpli **aşakda görkezilen** çakylyklary
täze pasportyna görä resmileşdirip bermegiňizi Sizden haýyş edýäris.
```

### xrTableHeader — column cells (XRTable, 4 cells)

| Cell | Caption | X | W | Alignment |
|---|---|---|---|---|
| colNo | № | 0F | 40F | MiddleCenter |
| colNumber | Çakylygyň belgisi | 40F | 200F | MiddleCenter |
| colStart | Resmileşdirilen senesi | 240F | 183F | MiddleCenter |
| colExpiry | Möhleti | 423F | 203.77F | MiddleCenter |

Font: Times New Roman 15pt Bold. All borders.

---

## DetailReportBand (Invitations collection)

**DataMember:** `Invitations`
**Inner Detail HeightF:** 25F per row

| Cell | X | W | Expression | Format |
|---|---|---|---|---|
| xrCellNo | 0F | 40F | `[DataSource.CurrentRowIndex] + 1` | MiddleCenter |
| xrCellNumber | 40F | 200F | `[InvitationNumber]` | MiddleCenter |
| xrCellStart | 240F | 183F | `[StartDate]` | `{0:dd.MM.yyyy}`, MiddleCenter |
| xrCellExpiry | 423F | 203.77F | `[ExpirationDate]` | `{0:dd.MM.yyyy}`, MiddleCenter |

Font: Times New Roman 15pt. All borders.

---

## Required BO properties

| Property | Source | Exists? |
|---|---|---|
| `TotalPersonCount` | `Application` | ✅ |
| `TotalPersonCountText` | `Application` | ✅ |
| `InvitationNumber` | `Invitation` (via DetailReportBand) | ✅ |
| `StartDate` | `Invitation` | ✅ |
| `ExpirationDate` | `Invitation` | ✅ |

No new NotMapped properties needed — the DetailReportBand binds directly to `Invitations`.

---

## ReportsUpdater.cs entry

```csharp
AddPredefinedReport<AppChangeInvReport>("App Change Inv Report", typeof(Application), isInplaceReport: true);
CreateReportVisibility("App Change Inv Report", "[ApplicationType.Name] = 'App_Change_Inv'");
```
