# Report Map: App_Inv_And_WP_app

**Status:** ✅ Implemented

---

## 1. Report Identity

| Property | Value |
|---|---|
| Class Name | `AppInvAndWPReport` |
| Registered Name | `"App Inv And WP Report"` |
| Display Name (Tm) | `"Çakylyk we Iş Rugsatnamasy — Ýüztutma"` |
| Inherits From | `AppBaseReport` |
| Data Type | `Application` |
| ApplicationType | `App_Inv_And_WP` |
| Visibility Criteria | `[ApplicationType.Name] = 'App_Inv_And_WP'` |
| Reference Image | `Resources/FormTemplates/App_Inv_And_WP_app.jpg` |

---

## 2. Differences vs AppInvReport

This report is nearly identical to `AppInvReport`. One difference:

| Element | AppInvReport | AppInvAndWPReport |
|---|---|---|
| Request paragraph | visa period + visa category | visa period + hardcoded "işi gezeklik çakylyk we iş rugsatnamasyny" |
| VisaCategory field | `[VisaCategory_NameTm]` | Not used — phrase is fixed for this application type |
| Contract paragraph | `[ProjectContract_Description]` | `[ProjectContract_Description]` — same |

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
| Detail | ~640F | All content controls |

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
| Multiline / WordWrap | true |
| Binding | `BeforePrint / Text / [ProjectContract_Ministry_RecipientBlock]` |

---

### xrLabelUrgency — Urgency line
| Property | Value |
|---|---|
| Type | `XRLabel` |
| X, Y | 0F, 150F |
| W × H | 300F × 25F |
| Font | Times New Roman 15pt *Italic* |
| Alignment | MiddleLeft |
| Binding (Text) | `[Urgency_NameTm]` |
| Binding (Visible) | `[ApplicationType.ShowUrgency]` |

---

### xrLabelGreeting — Salutation
| Property | Value |
|---|---|
| Type | `XRLabel` |
| X, Y | 0F, 185F |
| W × H | 626.77F × 35F |
| Font | Times New Roman 15pt **Bold** |
| Alignment | MiddleCenter |
| WordWrap | true |
| Binding | `BeforePrint / Text / [ProjectContract_Ministry_FormOfAddress]` |

---

### xrRichBody1 — Contract context paragraph
| Property | Value |
|---|---|
| Type | `XRRichText` |
| X, Y | 0F, 230F |
| W × H | 626.77F × 140F |
| CanGrow | true |
| Content | RTF template with `[ProjectContract_Description]` inline — same as AppInvReport |

---

### xrRichBody2 — Request paragraph (person count + visa period + work permit phrase)
| Property | Value |
|---|---|
| Type | `XRRichText` |
| X, Y | 0F, 378F |
| W × H | 626.77F × 100F |
| CanGrow | true |
| Content | RTF — justified, first-line indent 0.5", bold on count + visa period + phrase |

Dynamic fields used:
- `[Company.Name]`
- `[TotalPersonCount]` — bold
- `[TotalPersonCountText]` — bold
- `[VisaPeriod_NameTm]` — bold
- `[VisaCategory_NameTm]` — bold (only the category word, e.g. "iki gezeklik" or "köp gezeklik")

Static suffix (bold, follows VisaCategory inline): **" çakylyk we iş rugsatnamasyny"**

RTF content:
```
Hatymyzyň goşundysynda görkezilen Türkiye Respublikasynyň "[Company.Name]" kompaniýasyna degişli bolan sanawdaky \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany daşary ýurt raýatyna \b [VisaPeriod_NameTm]\b0  bilen \b [VisaCategory_NameTm] çakylyk we iş rugsatnamasyny\b0  resmileşdirilmegine ýardam bermegiňizi Sizden haýyş edýäris.
```

---

### xrRichBody3 — Static responsibility paragraph
| Property | Value |
|---|---|
| Type | `XRRichText` |
| X, Y | 0F, 486F |
| W × H | 626.77F × 70F |
| CanGrow | true |
| Content | Same static RTF as `AppInvReport.xrRichBody3` |

---

### xrLabelAttachments — Attachment list
| Property | Value |
|---|---|
| Type | `XRLabel` |
| X, Y | 0F, 564F |
| W × H | 626.77F × 60F |
| Font | Times New Roman 15pt |
| Alignment | TopLeft |
| Multiline / WordWrap | true |
| Binding | `BeforePrint / Text / 'Goşundy:   1. ' + [TotalPersonCount] + '-pasport kopiýalary,' + Char(10) + '           2. Goşundy (' + [TotalPersonCount] + '-daşary ýurt raýatynyň maglumaty)'` |

---

## 6. Required New NotMapped Properties

None — all fields already exist on `Application.cs`.

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
AddPredefinedReport<AppInvAndWPReport>("App Inv And WP Report", typeof(Application), isInplaceReport: true);
CreateReportVisibility("App Inv And WP Report", "Çakylyk we Iş Rugsatnamasy — Ýüztutma", typeof(Application), "[ApplicationType.Name] = 'App_Inv_And_WP'");
```

---

## 9. Resolved Decisions

| # | Question | Decision |
|---|---|---|
| 1 | Visa category phrase | `[VisaCategory_NameTm]` is dynamic (e.g. "iki gezeklik"), " çakylyk we iş rugsatnamasyny" is static suffix |
| 2 | Contract paragraph | Same `[ProjectContract_Description]` as AppInvReport |
