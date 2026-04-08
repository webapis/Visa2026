# Report Map: App_Visa_and_WP_Ext_app

**Status:** ✅ Implemented

---

## 1. Report Identity

| Property | Value |
|---|---|
| Class Name | `AppVisaAndWPExtReport` |
| Registered Name | `"App Visa And WP Ext Report"` |
| Display Name (Tm) | `"Wiza we Iş Rugsatnamasyny Uzaltmak — Ýüztutma"` |
| Inherits From | `AppBaseReport` |
| Data Type | `Application` |
| ApplicationType | `App_Visa_and_WP_Ext` |
| Visibility Criteria | `[ApplicationType.Name] = 'App_Visa_and_WP_Ext'` |
| Reference Image | `Resources/FormTemplates/App_Visa_and_WP_Ext_app.jpg` |

---

## 2. Differences vs AppInvAndWPReport

This report shares the same Ministry/ProjectContract recipient pattern as `AppInvAndWPReport` but is an **extension request** rather than an issuance request.

| Element | AppInvAndWPReport | AppVisaAndWPExtReport |
|---|---|---|
| Purpose | Request issuance of invitation + work permit | Request **extension** of existing visa + work permit |
| Body2 phrase | "resmileşdirilmegine ýardam bermegiňizi" | "uzaldylmagyna rugsat bermegini" |
| Person count in body2 | Once | **Twice** (once for persons, once for visas — same value) |
| VisaPeriod in body2 | `[VisaPeriod_NameTm]` möhlet bilen | `[VisaPeriod_NameTm]` inline in bold with count + category |
| Attachments section | xrLabelAttachments (Expression binding) | `xrRichAttachments` (static RTF, `[TotalPersonCount]` in item 2) |
| Recipient control type | `XRLabel` (Text binding) | `XRRichText` (Rtf binding — preserves bold/formatting stored in DB) |

---

## 3. Page Setup

Identical to `AppBaseReport` — inherited automatically:
- A4 Portrait
- Margins: Left 100F, Right 100F, Top 50F, Bottom 60F
- Letterhead background: `background_{CompanyCode}.jpg`
- Font: Times New Roman 15pt throughout

---

## 4. Band Map

| Band | HeightF | Contents |
|---|---|---|
| Detail | 590F | All content controls |

---

## 5. Control Map

Controls listed top to bottom by Y position.

### xrLabelRecipient — Ministry recipient block
| Property | Value |
|---|---|
| Type | `XRLabel` |
| X, Y | 220F, 20F |
| W × H | 406.77F × 120F |
| Font | Times New Roman 15pt **Bold** |
| Alignment | TopLeft |
| CanGrow / Multiline / WordWrap | true |
| Binding | `BeforePrint / Text / [ProjectContract_Ministry_RecipientBlock]` |
| Note | Standard §14 — XRLabel + Text binding, not XRRichText |

---

### xrLabelGreeting — Salutation line
| Property | Value |
|---|---|
| Type | `XRLabel` |
| X, Y | 0F, 185F |
| W × H | 626.77F × 35F |
| Font | Times New Roman 15pt **Bold** |
| Alignment | MiddleCenter |
| Binding | `BeforePrint / Text / [ProjectContract_Ministry_FormOfAddress]` |
| Note | Standard §19 — Bold, MiddleCenter |
| Example | "Hormatly Durdy Batjanowiç!" |

---

### xrRichBody1 — Contract reference paragraph
| Property | Value |
|---|---|
| Type | `XRRichText` |
| X, Y | 0F, 230F |
| W × H | 626.77F × 140F |
| CanGrow | true |
| Borders | None |
| Content | Static RTF template: `\qj\fi720` with `[ProjectContract_Description]` inline |
| Note | Description is plain text evaluated by XtraReports at render time; surrounding RTF provides justified/indented formatting |

---

### xrRichBody2 — Extension request paragraph
| Property | Value |
|---|---|
| Type | `XRRichText` |
| X, Y | 0F, 378F |
| W × H | 626.77F × 80F |
| CanGrow | true |
| Borders | None |
| Content | RTF — justified, first-line indent 0.5", bold on both counts + visa period + category |

Dynamic fields used:
- `[TotalPersonCount]` — bold (person count)
- `[TotalPersonCountText]` — bold (person count text)
- `[VisaPeriod_NameTm]` — bold (e.g. "1 ýyllyk" — already includes the number, no second count needed)
- `[VisaCategory_NameTm]` — bold (e.g. "Köp gezeklik")

RTF content:
```
Hatymyzyň goşundysynda görkezilen sanawdaky \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany daşary ýurt raýatyna \b [VisaPeriod_NameTm] [VisaCategory_NameTm]\b0  möhleti bilen uzaldylmagyna rugsat bermegini Sizden haýyş edýäris.
```

---

### xrRichBody3 — Static responsibility paragraph
| Property | Value |
|---|---|
| Type | `XRRichText` |
| X, Y | 0F, 466F |
| W × H | 626.77F × 80F |
| CanGrow | true |
| Borders | None |
| Content | Same static RTF as all registration reports |

---

### xrLabelAttachments — Attachments list
| Property | Value |
|---|---|
| Type | `XRLabel` |
| X, Y | 0F, 554F |
| W × H | 626.77F × 60F |
| Font | Times New Roman 15pt |
| Alignment | TopLeft |
| Multiline / WordWrap / CanGrow | true |
| Binding | `BeforePrint / Text / expression` |
| Note | Standard §16 — XRLabel + Char(10) for line break, not XRRichText |

Expression:
```
'Goşundy: 1. Daşary ýurt raýatlarynyň sanawy' + Char(10) + '2. Goşundy (' + [TotalPersonCount] + ' sany daşary ýurt raýatynyň maglumat)'
```

Dynamic field: `[TotalPersonCount]` in item 2 only.

---

## 6. Required BO Properties

| Field | BO | Property | Exists? |
|---|---|---|---|
| Ministry recipient block (RTF) | `Ministry` via `ProjectContract` | `ProjectContract_Ministry_RecipientBlock` | ✅ |
| Ministry salutation | `Ministry` via `ProjectContract` | `ProjectContract_Ministry_FormOfAddress` | ✅ |
| Contract reference paragraph | `ProjectContract` | `ProjectContract_Description` | ✅ |
| Person count (integer) | `Application` | `TotalPersonCount` | ✅ |
| Person count (text) | `Application` | `TotalPersonCountText` | ✅ |
| Visa period name (Tm) | `VisaPeriod` via `Application` | `VisaPeriod_NameTm` | ✅ |
| Visa category name (Tm) | `VisaCategory` via `Application` | `VisaCategory_NameTm` | ✅ |

---

## 7. Ignored Elements

| Element | Reason |
|---|---|
| Date/Number block (top left) | Inherited from `AppBaseReport` |
| Company logo / letterhead | Background image inherited from `AppBaseReport` |
| Signatory block (bottom) | Inherited from `AppBaseReport` |
| Footer (address, website) | Inherited from `AppBaseReport` |

---

## 8. ReportsUpdater.cs Entry

```csharp
AddPredefinedReport<AppVisaAndWPExtReport>("App Visa And WP Ext Report", typeof(Application), isInplaceReport: true);
CreateReportVisibility("App Visa And WP Ext Report", "Wiza we Iş Rugsatnamasyny Uzaltmak — Ýüztutma", typeof(Application), "[ApplicationType.Name] = 'App_Visa_and_WP_Ext'");
```

---

## 9. Resolved Decisions

| # | Question | Decision |
|---|---|---|
| 1 | Visa category | `[VisaCategory_NameTm]` is dynamic (e.g. "Köp gezeklik") |
| 2 | Visa period | `[VisaPeriod_NameTm]` is dynamic (e.g. "1 ýyllyk") — added after initial implementation |
| 3 | Person count in body2 | Appears **once** — `[VisaPeriod_NameTm]` already includes the number (e.g. "1 ýyllyk"), second count was redundant and removed |
| 4 | Recipient control | `XRLabel` + `Text` binding — standard §14. Initial implementation incorrectly used XRRichText + Rtf; corrected after standards review. |
| 5 | Attachments count | Item 2 uses `[TotalPersonCount]` (dynamic), not static "1" |
