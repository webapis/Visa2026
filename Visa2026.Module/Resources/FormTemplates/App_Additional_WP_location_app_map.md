# Report Map: App_Additional_WP_location_app

**Status:** ✅ Implemented

---

## 1. Report Identity

| Property | Value |
|---|---|
| Class Name | `AppAdditionalWPLocationReport` |
| Registered Name | `"App Additional WP Location Report"` |
| Display Name (Tm) | `"Goşmaça hereket çägi — Ýüztutma"` |
| Inherits From | `AppBaseReport` |
| Data Type | `Application` |
| ApplicationType | `App_Additional_WP_location` |
| Visibility Criteria | `[ApplicationType.Name] = 'App_Additional_WP_location'` |
| Reference Image | `Resources/FormTemplates/App_Additional_WP_location_app.jpg` |

---

## 2. Similarities vs AppVisaAndWPExtReport

Same Ministry/ProjectContract recipient pattern. Key difference: body2 uses `[MovementPermitLocation_NameTm]` (new field) instead of visa period/category.

| Element | AppVisaAndWPExtReport | AppAdditionalWPLocationReport |
|---|---|---|
| Recipient | `[ProjectContract_Ministry_RecipientBlock]` | Same ✅ |
| Greeting | `[ProjectContract_Ministry_FormOfAddress]` | Same ✅ |
| body1 | `[ProjectContract_Description]` | Same ✅ |
| body2 | count + VisaPeriod + VisaCategory + "möhleti bilen uzaldylmagyna" | count + **`[MovementPermitLocation_NameTm]`** + "iş rugsatnamalarynyň berilmegine" |
| body3 | Static responsibility | Same ✅ |
| Attachments | dynamic count | Same ✅ |

---

## 3. Page Setup

Identical to `AppBaseReport` — inherited automatically.

---

## 4. Band Map

| Band | HeightF | Contents |
|---|---|---|
| Detail | ~590F | All content controls |

---

## 5. Control Map

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

### xrRichBody1 — Contract reference paragraph
| Property | Value |
|---|---|
| Type | `XRRichText` |
| X, Y | 0F, 230F |
| W × H | 626.77F × 140F |
| CanGrow | true |
| Content | Static RTF template with `[ProjectContract_Description]` inline — same as AppVisaAndWPExtReport |

---

### xrRichBody2 — Additional WP location request paragraph
| Property | Value |
|---|---|
| Type | `XRRichText` |
| X, Y | 0F, 378F |
| W × H | 626.77F × 100F |
| CanGrow | true |
| Content | RTF — justified, first-line indent 0.5" |

Dynamic fields:
- `[Company.Name]` — company name inline
- `[TotalPersonCount]` — bold
- `[TotalPersonCountText]` — bold
- `[MovementPermitLocation_NameTm]` — bold (e.g. "Aşgabat merkezi ofisimizde wezipesine degişli işleri geçirmek üçin Aşgabat şäherini")

RTF sentence:
```
Şertname esasynda, öňde goýlan wezipeleri ýetinlikli durmuşa geçirmek üçin hatymyzyň goşundysynda görkezilen "[Company.Name]" kompaniýasyna degişli bolan \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany daşary ýurt raýatyna \b [MovementPermitLocation_NameTm]\b0  iş rugsatnamalarynyň berilmegine ýardam bermegiňizi Sizden haýyş edýäris.
```

---

### xrRichBody3 — Static responsibility paragraph
| Property | Value |
|---|---|
| Type | `XRRichText` |
| X, Y | 0F, 486F |
| W × H | 626.77F × 80F |
| CanGrow | true |
| Content | Same static RTF as all other reports |

---

### xrLabelAttachments — Attachments list
| Property | Value |
|---|---|
| Type | `XRLabel` |
| X, Y | 0F, 574F |
| W × H | 626.77F × 60F |
| Font | Times New Roman 15pt |
| Alignment | TopLeft |
| Multiline / WordWrap / CanGrow | true |
| Binding | `BeforePrint / Text / expression` |

Expression:
```
'Goşundy: 1. Daşary ýurt raýatlarynyň sanawy-' + [TotalPersonCount] + Char(10) + '2. Goşundy (' + [TotalPersonCount] + '-daşary ýurt raýatynyň maglumat)'
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
| Movement permit location name | `MovementPermitLocation_NameTm` | ❌ **Must add to Application.cs** |

---

## 7. New NotMapped Property Required

```csharp
[XafDisplayName("Movement Permit Location (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
[NotMapped]
public string MovementPermitLocation_NameTm => MovementPermitLocation?.NameTm;
```

---

## 8. Ignored Elements

| Element | Reason |
|---|---|
| Date/Number block (top left) | Inherited from `AppBaseReport` |
| Company logo / letterhead | Background image inherited from `AppBaseReport` |
| Signatory block (bottom) | Inherited from `AppBaseReport` |
| Footer (address, website) | Inherited from `AppBaseReport` |
