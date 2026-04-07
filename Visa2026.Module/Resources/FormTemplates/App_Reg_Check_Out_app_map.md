# Report Map: App_Reg_Check_Out_app

**Status:** ✅ Implemented

---

## 1. Report Identity

| Property | Value |
|---|---|
| Class Name | `AppRegCheckOutReport` |
| Registered Name | `"App Reg Check Out Report"` |
| Display Name (Tm) | `"Hasapdan Çykarmak — Ýüztutma"` |
| Inherits From | `AppBaseReport` |
| Data Type | `Application` |
| ApplicationType | `App_Reg_Check_Out` |
| Visibility Criteria | `[ApplicationType.Name] = 'App_Reg_Check_Out'` |
| Reference Image | `Resources/FormTemplates/App_Reg_Check_Out_app.jpg` |

> ⚠️ **Catalog note:** The catalog had `App_Reg_Check_Out` as Reg-level only (`—:—:1`). The scanned image is App-level. Catalog updated to `1:—:1`.

---

## 2. Differences vs AppRegCheckInReport

Identical structure — only body1 RTF text differs.

| Element | AppRegCheckInReport | AppRegCheckOutReport |
|---|---|---|
| Body1 bold phrase | **Türkmenistana gelendigi sebäpli** | **Türkmenistandan gidendigi sebäpli** |
| Body1 action | hasaba almagyňyzy | hasapdan doly çykarmagyňyzy |
| Body2 | static responsibility paragraph | same static responsibility paragraph |
| Recipient | `[MigrationService_NameTm]` | `[MigrationService_NameTm]` |
| Attachments | none | none |

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
| Detail | 350F | All content controls |

---

## 5. Control Map

### xrLabelRecipient — Migration Service name, bold, right-aligned
| Property | Value |
|---|---|
| Type | `XRLabel` |
| X, Y | 313F, 30F |
| W × H | 313.77F × 100F |
| Font | Times New Roman 15pt **Bold** |
| Alignment | TopRight |
| Multiline / WordWrap | true |
| Binding | `BeforePrint / Text / [MigrationService_NameTm]` |

---

### xrRichBody1 — Request paragraph
| Property | Value |
|---|---|
| Type | `XRRichText` |
| X, Y | 0F, 155F |
| W × H | 626.77F × 80F |
| CanGrow | true |
| Content | RTF — justified, first-line indent 0.5", bold on count + "Türkmenistandan gidendigi sebäpli" |

Dynamic fields: `[TotalPersonCount]`, `[TotalPersonCountText]`

RTF content:
```
Hatymyzyň goşundysynda görkezilen sanawdaky \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany daşary ýurt raýatynyň \b Türkmenistandan gidendigi sebäpli\b0  hasapdan doly çykarmagyňyzy Sizden haýyş edýäris.
```

---

### xrRichBody2 — Static responsibility paragraph
| Property | Value |
|---|---|
| Type | `XRRichText` |
| X, Y | 0F, 243F |
| W × H | 626.77F × 80F |
| CanGrow | true |
| Content | Same static RTF as `AppRegCheckInReport.xrRichBody2` |

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
AddPredefinedReport<AppRegCheckOutReport>("App Reg Check Out Report", typeof(Application), isInplaceReport: true);
CreateReportVisibility("App Reg Check Out Report", "Hasapdan Çykarmak — Ýüztutma", typeof(Application), "[ApplicationType.Name] = 'App_Reg_Check_Out'");
```

---

## 9. Resolved Decisions

| # | Question | Decision |
|---|---|---|
| 1 | Report level | App-level (confirmed by scanned image) — catalog updated |
| 2 | Structure | Identical to AppRegCheckInReport except body1 RTF text |
