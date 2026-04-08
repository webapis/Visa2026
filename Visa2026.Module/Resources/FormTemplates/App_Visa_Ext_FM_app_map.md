# Report Map: App_Visa_Ext_FM_app

**Status:** ✅ Implemented

---

## 1. Report Identity

| Property | Value |
|---|---|
| Class Name | `AppVisaExtFMReport` |
| Registered Name | `"App Visa Ext FM Report"` |
| Display Name (Tm) | `"Wiza Möhletini Uzaltmak FM — Ýüztutma"` |
| Inherits From | `AppBaseReport` |
| Data Type | `Application` |
| ApplicationType | `App_Visa_Ext_FM` |
| Visibility Criteria | `[ApplicationType.Name] = 'App_Visa_Ext_FM'` |
| Reference Image | `Resources/FormTemplates/App_Visa_Ext_FM_app.jpg` |

---

## 2. Differences vs AppInvFMReport

Nearly identical structure. Only body3 (request paragraph) differs:

| Element | AppInvFMReport | AppVisaExtFMReport |
|---|---|---|
| body1 | Static "Berkarar..." intro | Same ✅ |
| body2 | Static company partnership + `[Company.Name]` | Same ✅ |
| body3 phrase | `[VisaPeriod_NameTm]` bilen `[VisaCategory_NameTm]` **çakylyk** resmileşdirilmegine | `[VisaPeriod_NameTm]` `[VisaCategory_NameTm]` **wizalaryny** resmileşdirilmegine |
| body4 | Static responsibility | Same ✅ |
| Attachments | `[TotalPersonCount]-pasport kopiýalary` | Same ✅ |

---

## 3. Page Setup

Identical to `AppBaseReport` — inherited automatically.

---

## 4. Band Map

| Band | HeightF | Contents |
|---|---|---|
| Detail | ~680F | All content controls |

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

### xrRichBody1 — Static intro paragraph 1 (Berkarar döwlet)
| Property | Value |
|---|---|
| Type | `XRRichText` |
| X, Y | 0F, 230F |
| W × H | 626.77F × 80F |
| CanGrow | true |
| Content | Same static RTF as `AppInvFMReport.xrRichBody1` |

RTF content:
```
Berkarar döwletimiziň bagtyýarlyk döwründe \b Hormatly Prezidentimiziň\b0  täýsyz tagallalary netijesinde ýurdumyzyň elektroenergetika pudagynda birnäçe iri taslamalar durmuşa geçirilýär.
```

---

### xrRichBody2 — Static intro paragraph 2 (company partnership)
| Property | Value |
|---|---|
| Type | `XRRichText` |
| X, Y | 0F, 318F |
| W × H | 626.77F × 80F |
| CanGrow | true |
| Content | Same static RTF as `AppInvFMReport.xrRichBody2` — `[Company.Name]` inline |

RTF content:
```
Şunuň bilen baglylykda, elektroenergetika pudagyny köp ýyllardan bäri hyzmatdaşy bolup gelýän "[Company.Name]" kompaniýasy tarapyndan birnäçe taslamalar amala aşyrylýar.
```

---

### xrRichBody3 — Request paragraph (FM visa extension)
| Property | Value |
|---|---|
| Type | `XRRichText` |
| X, Y | 0F, 406F |
| W × H | 626.77F × 120F |
| CanGrow | true |
| Content | RTF — justified, first-line indent 0.5" |

Dynamic fields:
- `[Company.Name]` — company name inline
- `[SponsoringEmployee_FullName]` — bold
- `[SponsoringEmployee_PositionTm]` — bold
- `[TotalPersonCount]` — bold
- `[TotalPersonCountText]` — bold
- `[FamilyMember_Relationship_NameTm]` — FM relationship, genitive (e.g. "adamsynyň we kakasynyň")
- `[VisaPeriod_NameTm]` — bold (e.g. "6 aý")
- `[VisaCategory_NameTm]` — bold (e.g. "köp gezeklik")
- Static suffix: **" wizalaryny"** (bold, after VisaCategory)

RTF content:
```
Türkmenistandaky çäklerinde amala aşyrylýan taslamalar utgaşdyrmak boýunça "[Company.Name]" kompaniýasyna degişli hünärmeniň (\b [SponsoringEmployee_FullName] - [SponsoringEmployee_PositionTm]\b0 ) maşgala agzalaryna ýagny, hatymyzyň goşundysynda görkezilen sanawdaky \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany daşary ýurt raýatyna [FamilyMember_Relationship_NameTm] \b [VisaPeriod_NameTm] [VisaCategory_NameTm] wizalaryny\b0  resmileşdirilmegine ýardam bermegiňizi Sizden haýyş edýäris.
```

---

### xrRichBody4 — Static responsibility paragraph
| Property | Value |
|---|---|
| Type | `XRRichText` |
| X, Y | 0F, 534F |
| W × H | 626.77F × 70F |
| CanGrow | true |
| Content | Same static RTF as all other reports |

---

### xrLabelAttachments — Attachment list
| Property | Value |
|---|---|
| Type | `XRLabel` |
| X, Y | 0F, 612F |
| W × H | 626.77F × 60F |
| Font | Times New Roman 15pt |
| Alignment | TopLeft |
| Multiline / WordWrap / CanGrow | true |
| Binding | `BeforePrint / Text / expression` |

Expression (same as AppInvFMReport):
```
'Goşundy:   1. ' + [TotalPersonCount] + '-pasport kopiýalary,' + Char(10) + '           2. Goşundy (' + [TotalPersonCount] + '-daşary ýurt raýatynyň maglumaty)'
```

---

## 6. Required BO Properties

All already exist on `Application.cs` — no new NotMapped properties needed.

| Field | Property |
|---|---|
| Ministry recipient block | `ProjectContract_Ministry_RecipientBlock` |
| Ministry salutation | `ProjectContract_Ministry_FormOfAddress` |
| Company name | `Company.Name` (direct navigation) |
| Sponsoring employee full name | `SponsoringEmployee_FullName` |
| Sponsoring employee position | `SponsoringEmployee_PositionTm` |
| FM relationship (genitive) | `FamilyMember_Relationship_NameTm` |
| Person count (integer) | `TotalPersonCount` |
| Person count (text) | `TotalPersonCountText` |
| Visa period name (Tm) | `VisaPeriod_NameTm` |
| Visa category name (Tm) | `VisaCategory_NameTm` |
| Urgency name (Tm) | `Urgency_NameTm` |

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
AddPredefinedReport<AppVisaExtFMReport>("App Visa Ext FM Report", typeof(Application), isInplaceReport: true);
CreateReportVisibility("App Visa Ext FM Report", "Wiza Möhletini Uzaltmak FM — Ýüztutma", typeof(Application), "[ApplicationType.Name] = 'App_Visa_Ext_FM'");
```
