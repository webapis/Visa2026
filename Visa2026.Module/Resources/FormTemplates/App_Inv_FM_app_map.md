# Report Map: App_Inv_FM_app

**Status:** ✅ Implemented

---

## 1. Report Identity

| Property | Value |
|---|---|
| Class Name | `AppInvFMReport` |
| Registered Name | `"App Inv FM Report"` |
| Display Name (Tm) | `"Çakylyk — FM Ýüztutma"` |
| Inherits From | `AppBaseReport` |
| Data Type | `Application` |
| ApplicationType | `App_Inv_FM` |
| Visibility Criteria | `[ApplicationType.Name] = 'App_Inv_FM'` |
| Reference Image | `Resources/FormTemplates/App_Inv_FM_app.jpg` |

---

## 2. Differences vs AppInvReport

This report is nearly identical to `AppInvReport` (`App_Inv`). Differences:

| Element | AppInvReport | AppInvFMReport |
|---|---|---|
| Intro paragraphs | None | 2 extra static paragraphs before request para |
| Request paragraph | Contract context + person count + visa period + visa category | Company name + person count + employee name+position inline |
| Visa category field | `[VisaCategory_NameTm]` | Hardcoded "köp gezeklik" (⚠️ confirm) |
| Visa period field | `[VisaPeriod_NameTm]` | Hardcoded "wiza möhletine görä" (⚠️ confirm) |
| Employee reference | Not present | `[SponsoringEmployee_FullName]` + `[SponsoringEmployee_PositionTm]` — new NotMapped |

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
| Detail | ~780F | All content controls |

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

### xrRichBody1 — Static intro paragraph 1 (Berkarar döwlet)
| Property | Value |
|---|---|
| Type | `XRRichText` |
| X, Y | 0F, 230F |
| W × H | 626.77F × 80F |
| CanGrow | true |
| Content | Static RTF — justified, first-line indent 0.5", bold on "Hormatly Prezidentimiziň" |

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
| Content | Static RTF — justified, first-line indent 0.5", `[Company.Name]` inline |

RTF content:
```
Şunuň bilen baglylykda, elektroenergetika pudagyny köp ýyllardan bäri hyzmatdaşy bolup gelýän "[Company.Name]" kompaniýasy tarapyndan birnäçe taslamalar amala aşyrylýar.
```

---

### xrRichBody3 — Request paragraph (person count + employee reference)
| Property | Value |
|---|---|
| Type | `XRRichText` |
| X, Y | 0F, 406F |
| W × H | 626.77F × 120F |
| CanGrow | true |
| Content | RTF — justified, first-line indent 0.5", bold on count + employee name/position |

Dynamic fields used:
- `[Company.Name]` — company name
- `[TotalPersonCount]` — bold
- `[TotalPersonCountText]` — bold
- `[FamilyMember_Relationship_NameTm]` — relationship of FM to sponsoring employee, genitive form (new NotMapped)
- `[SponsoringEmployee_FullName]` — bold (new NotMapped)
- `[SponsoringEmployee_PositionTm]` — bold (new NotMapped)
- `[VisaPeriod_NameTm]` — already exists
- `[VisaCategory_NameTm]` — already exists (replaces hardcoded "köp gezeklik")

RTF content:
```
Türkmenistandaky çäklerinde amala aşyrylýan taslamalar utgaşdyrmak boýunça "[Company.Name]" kompaniýasyna degişli hünärmeniň maşgala agzalaryna ýagny, hatymyzyň goşundysynda görkezilen sanawdaky \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany daşary ýurt raýatyna [FamilyMember_Relationship_NameTm] (\b [SponsoringEmployee_FullName] - [SponsoringEmployee_PositionTm]\b0 ) \b [VisaPeriod_NameTm]\b0  bilen \b [VisaCategory_NameTm]\b0  çakylyk resmileşdirilmegine ýardam bermegiňizi Sizden haýyş edýäris.
```

> **Note on `[FamilyMember_Relationship_NameTm]`**: `Person.Relationship.NameTm` should store the genitive form of the relationship label (e.g., "aýalynyň", "çagasynyň", "kakasynyň") so it reads naturally in the sentence. The Relationship lookup data must be seeded accordingly.

---

### xrRichBody4 — Responsibility paragraph (static)
| Property | Value |
|---|---|
| Type | `XRRichText` |
| X, Y | 0F, 534F |
| W × H | 626.77F × 70F |
| CanGrow | true |
| Content | Same static RTF as `AppInvReport.xrRichBody3` |

---

### xrLabelAttachments — Attachment list
| Property | Value |
|---|---|
| Type | `XRLabel` |
| X, Y | 0F, 612F |
| W × H | 626.77F × 60F |
| Font | Times New Roman 15pt |
| Alignment | TopLeft |
| Multiline / WordWrap | true |
| Binding | `BeforePrint / Text / 'Goşundy:   1. ' + [TotalPersonCount] + '-pasport kopiýalary,' + Char(10) + '           2. Goşundy (' + [TotalPersonCount] + '-daşary ýurt raýatynyň maglumaty)'` |

---

## 6. Required New NotMapped Properties (Application.cs)

These do not exist yet — must be added before code generation:

```csharp
[XafDisplayName("FM Relationship (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
[NotMapped]
public string FamilyMember_Relationship_NameTm =>
    ApplicationItems?.FirstOrDefault()?.Person?.Relationship?.NameTm;

[XafDisplayName("Sponsoring Employee Full Name"), VisibleInDetailView(false), VisibleInListView(false)]
[NotMapped]
public string SponsoringEmployee_FullName =>
    ApplicationItems?.FirstOrDefault()?.Person?.SponsoringEmployee?.FullName;

[XafDisplayName("Sponsoring Employee Position (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
[NotMapped]
public string SponsoringEmployee_PositionTm =>
    ApplicationItems?.FirstOrDefault()?.Person?.SponsoringEmployee?.CurrentPositionHistory?.Position?.NameTm;
```

> These navigate through the first ApplicationItem. Valid for FM invitation applications where all items share the same sponsoring employee.

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
AddPredefinedReport<AppInvFMReport>("App Inv FM Report", typeof(Application), isInplaceReport: true);
CreateReportVisibility("App Inv FM Report", "[ApplicationType.Name] = 'App_Inv_FM'");
```

---

## 9. Resolved Decisions

| # | Question | Decision |
|---|---|---|
| 1 | Relationship word (was "adamsynyň") | Dynamic — `[FamilyMember_Relationship_NameTm]` from `Person.Relationship.NameTm`; lookup data must store genitive form |
| 2 | Visa category (was "köp gezeklik") | Dynamic — `[VisaCategory_NameTm]` |
| 3 | Y positions | Adjusted for 2 extra intro paragraphs (tighter spacing to fit A4) |
