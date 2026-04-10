# Report Map: App_Border_Zone_Permission_app

**Status:** ✅ Implemented

---

## 1. Report Identity

| Property | Value |
|---|---|
| Class Name | `AppBorderZonePermissionReport` |
| Registered Name | `"App Border Zone Permission Report"` |
| Display Name (Tm) | `"Serhet Ýaka Rugsatnama — Ýüztutma"` |
| Inherits From | `AppBaseReport` |
| Data Type | `Application` |
| ApplicationType | `App_Border_Zone_Permission` |
| Visibility Criteria | `[ApplicationType.Name] = 'App_Border_Zone_Permission'` |
| Reference Image | `Resources/FormTemplates/App_Border_Zone_Permission_app.jpg` |

---

## 2. Similarities vs AppAdditionalWPLocationReport

Nearly identical structure. Only body2 sentence differs — uses `[BorderZoneLocation_NameTm]` and "serhet ýaka wizasynyň resmileşdirilmegine" instead of "iş rugsatnamalarynyň berilmegine".

| Element | AppAdditionalWPLocationReport | AppBorderZonePermissionReport |
|---|---|---|
| Recipient | `[ProjectContract_Ministry_RecipientBlock]` | Same ✅ |
| Greeting | `[ProjectContract_Ministry_FormOfAddress]` | Same ✅ |
| body1 | `[ProjectContract_Description]` | Same ✅ |
| body2 | count + `[MovementPermitLocation_NameTm]` + "iş rugsatnamalarynyň berilmegine" | count + **`[BorderZoneLocation_NameTm]`** + "serhet ýaka wizasynyň resmileşdirilmegine" |
| body3 | Static responsibility | Same ✅ |
| Attachments | dynamic count | Same ✅ |

---

## 3. Page Setup

Identical to `AppBaseReport` — inherited automatically.

---

## 4. Band Map

| Band | HeightF | Contents |
|---|---|---|
| Detail | 580F | All content controls (same as AppAdditionalWPLocationReport) |

---

## 5. Control Map

### xrLabelRecipient — Ministry recipient block
| Property | Value |
|---|---|
| Type | `XRLabel` |
| X, Y | 220F, 20F |
| W × H | 406.77F × 80F |
| Font | Times New Roman 15pt **Bold** |
| Alignment | TopLeft |
| CanGrow / CanShrink / Multiline / WordWrap | true |
| Binding | `BeforePrint / Text / [ProjectContract_Ministry_RecipientBlock]` |

---

### xrLabelGreeting — Salutation
| Property | Value |
|---|---|
| Type | `XRLabel` |
| X, Y | 0F, 115F |
| W × H | 626.77F × 35F |
| Font | Times New Roman 15pt **Bold** |
| Alignment | MiddleCenter |
| CanGrow / WordWrap | true |
| Binding | `BeforePrint / Text / [ProjectContract_Ministry_FormOfAddress]` |

---

### xrRichBody1 — Contract reference paragraph
| Property | Value |
|---|---|
| Type | `XRRichText` |
| X, Y | 0F, 165F |
| W × H | 626.77F × 140F |
| CanGrow | true |
| Content | RTF with `[ProjectContract_Description]` inline — identical to AppAdditionalWPLocationReport |

---

### xrRichBody2 — Border zone permission request paragraph
| Property | Value |
|---|---|
| Type | `XRRichText` |
| X, Y | 0F, 313F |
| W × H | 626.77F × 100F |
| CanGrow | true |
| Content | RTF — justified, first-line indent 0.5" |

Dynamic fields:
- `[Company.Name]` — company name in guillemets (`\ldblquote ... \rdblquote`)
- `[TotalPersonCount]` — bold
- `[TotalPersonCountText]` — bold
- `[BorderZoneLocation_NameTm]` — bold (e.g. "Ahal welaýatyň Sarahs etrabyna hepdelik düzme we anna günleriniň iş wagty aralygynda desga barmak üçin")

RTF sentence:
```
Şertname esasynda, öňde goýlan wezipeleri ýetinlikli durmuşa geçirmek üçin hatymyzyň goşundysynda görkezilen «[Company.Name]» kompaniýasynyň işçi bolup \b [TotalPersonCount] ([TotalPersonCountText]) sany\b0  daşary ýurt raýatynyň \b [BorderZoneLocation_NameTm]\b0  serhet ýaka wizasynyň resmileşdirilmegine ýardam bermegiňizi Sizden haýyş edýäris.
```

---

### xrRichBody3 — Static responsibility paragraph
| Property | Value |
|---|---|
| Type | `XRRichText` |
| X, Y | 0F, 421F |
| W × H | 626.77F × 80F |
| CanGrow | true |
| Content | Identical static RTF as all other reports |

---

### xrLabelAttachments — Attachments list
| Property | Value |
|---|---|
| Type | `XRLabel` |
| X, Y | 0F, 509F |
| W × H | 626.77F × 60F |
| Font | Times New Roman 15pt |
| Alignment | TopLeft |
| Multiline / WordWrap / CanGrow | true |
| Binding | `BeforePrint / Text / expression` |

Expression (identical to AppAdditionalWPLocationReport):
```
'Goşundy: 1. Daşary ýurt raýatlarynyň sanawy-' + [TotalPersonCount] + Char(10) + '                2. ' + [TotalPersonCount] + '(' + [TotalPersonCountText] + ')- sany daşary ýurt raýatynyň maglumaty'
```

---

## 6. Required BO Properties

| Field | Property | Exists? |
|---|---|---|
| Ministry recipient block | `ProjectContract_Ministry_RecipientBlock` | ✅ |
| Ministry salutation | `ProjectContract_Ministry_FormOfAddress` | ✅ |
| Contract paragraph | `ProjectContract_Description` | ✅ |
| Company name | `Company.Name` | ✅ |
| Person count (integer) | `TotalPersonCount` | ✅ |
| Person count (text) | `TotalPersonCountText` | ✅ |
| Border zone location name | `BorderZoneLocation_NameTm` | ✅ Added this session |

---

## 7. xlsm Changes Required Before Import

1. Add **`BorderZoneLocation`** sheet to `lookup.xlsm` with columns: `Name`, `NameTm`, `Code`, `IsDefault`
   - Sample row: `Sarahs etrabyna` | `Ahal welaýatyň Sarahs etrabyna hepdelik düzme we anna günleriniň iş wagty aralygynda desga barmak üçin` | *(blank)* | `True` ✅ Added
2. Add **`ShowBorderZoneLocation`** column to `ApplicationType` sheet, set to `True` for `App_Border_Zone_Permission`

---

## 8. Ignored Elements

| Element | Reason |
|---|---|
| Date/Number block (top left) | Inherited from `AppBaseReport` |
| Company logo / letterhead | Background image inherited from `AppBaseReport` |
| Signatory block (bottom) | Inherited from `AppBaseReport` |
| Footer (address, website) | Inherited from `AppBaseReport` |
