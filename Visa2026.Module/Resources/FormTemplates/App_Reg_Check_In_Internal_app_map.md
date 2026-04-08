# Report Map: App_Reg_Check_In_Internal_app

**Status:** ✅ Implemented

---

## 1. Report Identity

| Property | Value |
|---|---|
| Class Name | `AppRegCheckInInternalReport` |
| Registered Name | `"App Reg Check In Internal Report"` |
| Display Name (Tm) | `"Hasaba Almak (Içerki) — Ýüztutma"` |
| Inherits From | `AppBaseReport` |
| Data Type | `Application` |
| ApplicationType | `App_Reg_Check_In_Internal` |
| Visibility Criteria | `[ApplicationType.Name] = 'App_Reg_Check_In_Internal'` |
| Reference Image | `Resources/FormTemplates/App_Reg_Check_In_Internal_app.jpg` |

> ⚠️ **Catalog note:** Catalog had this as Reg-level only (`—:—:1`). Scanned image is App-level. Catalog updated to `1:—:1`.

---

## 2. Differences vs AppRegCheckInReport

Identical structure — only body1 RTF text differs.

| Element | AppRegCheckInReport | AppRegCheckInInternalReport |
|---|---|---|
| Body1 bold phrase | **Türkmenistana gelendigi sebäpli** | **ýaşaýan salgysyny [From→To] üýtgeýändigi sebäpli** |
| Body1 action | hasaba almagyňyzy | hasaba almagyňyzy (same) |
| From/To fields | not used | `[FromRegionName]`, `[FromCityName]`, `[ToRegionName]`, `[ToCityName]` |
| Body2 | static responsibility paragraph | same static responsibility paragraph |

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

### xrLabelRecipient — Migration Service name, bold, left-aligned (standard)
| Property | Value |
|---|---|
| Type | `XRLabel` |
| X, Y | 220F, 30F |
| W × H | 406.77F × 100F |
| Font | Times New Roman 15pt **Bold** |
| Alignment | TopLeft |
| Multiline / WordWrap | true |
| Binding | `BeforePrint / Text / [MigrationService_NameTm]` |

---

### xrRichBody1 — Request paragraph with internal movement details
| Property | Value |
|---|---|
| Type | `XRRichText` |
| X, Y | 0F, 155F |
| W × H | 626.77F × 90F |
| CanGrow | true |
| Content | RTF — justified, first-line indent 0.5", bold on count + movement phrase |

Dynamic fields used:
- `[TotalPersonCount]` — bold
- `[TotalPersonCountText]` — bold
- `[FromRegionName]` — bold (e.g. "Mary")
- `[FromCityName]` — bold (e.g. "Mary")
- `[ToRegionName]` — bold (e.g. "Ahal")
- `[ToCityName]` — bold (e.g. "Akbugdaý")

RTF content:
```
Hatymyzyň goşundysynda görkezilen sanawdaky \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany daşary ýurt raýatynyň \b ýaşaýan salgysyny [FromRegionName] welaýatynyň [FromCityName] etrabyndan [ToRegionName] welaýatynyň [ToCityName] etrabyna üýtgeýändigi\b0  sebäpli hasaba almagyňyzy Sizden haýyş edýäris.
```

---

### xrRichBody2 — Static responsibility paragraph (same as AppRegCheckInReport)
| Property | Value |
|---|---|
| Type | `XRRichText` |
| X, Y | 0F, 253F |
| W × H | 626.77F × 80F |
| CanGrow | true |
| Content | Same static RTF as `AppRegCheckInReport.xrRichBody2` |

---

## 6. Required New NotMapped Properties

None — all fields already exist on `Application.cs`:
- `FromCityName` → `FromCity?.Name`
- `FromRegionName` → `FromCity?.Region?.Name`
- `ToCityName` → `ToCity?.Name`
- `ToRegionName` → `ToCity?.Region?.Name`

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
AddPredefinedReport<AppRegCheckInInternalReport>("App Reg Check In Internal Report", typeof(Application), isInplaceReport: true);
CreateReportVisibility("App Reg Check In Internal Report", "Hasaba Almak (Içerki) — Ýüztutma", typeof(Application), "[ApplicationType.Name] = 'App_Reg_Check_In_Internal'");
```

---

## 9. Resolved Decisions

| # | Question | Decision |
|---|---|---|
| 1 | Report level | App-level (confirmed by scanned image) — catalog updated |
| 2 | From/To fields | Use existing NotMapped: `[FromRegionName]`, `[FromCityName]`, `[ToRegionName]`, `[ToCityName]` |
| 3 | "welaýatynyň", "etrabyndan", "etrabyna" | Static suffixes in RTF — no vowel harmony needed |
