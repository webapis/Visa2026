# Word / Excel Report Placeholder Reference

> **Source of truth:** [`Visa2026.Module/BusinessObjects/Application.cs`](../Visa2026.Module/BusinessObjects/Application.cs) and [`Visa2026.Module/BusinessObjects/ApplicationItem.cs`](../Visa2026.Module/BusinessObjects/ApplicationItem.cs).  
> Every property listed here exists on one of those types (report flatteners are `[NotMapped]` and hidden from the normal detail UI unless noted).  
> Update this document when you add or rename report placeholders on those BOs.

**Not covered here:** keys built only inside code-backed [`IWordReportDefinition`](../Visa2026.Module/Services/WordReports/) merge dictionaries (for example split ministry address lines). See [`docs/WORD_REPORT_GENERATION_IDEA.md`](WORD_REPORT_GENERATION_IDEA.md) and per-template `*_map.md` under `Visa2026.Module/Resources/FormTemplates/`.

Excel user templates use the **same property names**; see [`docs/EXCEL_PLACEHOLDER_REFERENCE.md`](EXCEL_PLACEHOLDER_REFERENCE.md).

---

## Example output column

Each table includes **Example output** — what users typically see **after merge**, not the `{{…}}` token.

| Convention | Meaning |
|------------|---------|
| Illustrative samples | Taken from ministry scans / `*_map.md` §12 golden values where available (e.g. Forma 16, şahsy kagyz, Çalık letters). |
| `(from …)` | Passthrough from master data on **Person**, **Passport**, **Visa**, lookups, etc. — your document shows whatever is stored. |
| *(empty)* | No source data → blank cell/paragraph. |
| Multiline | Line breaks in Excel/Word; see [Composite / multiline outputs](#composite--multiline-outputs) below. |

Template-specific golden QA values: **`Visa2026.Module/Resources/Templates/*_map.md`** §12. Preview without the app: `tools/PreviewWordReports`, `tools/ExcelTemplateSpike`.

---

## How to Use This Reference

### Template root

| Root business object | `{{ds.*}}` binds to | Row loop collection | `{{.*}}` binds to |
|----------------------|---------------------|---------------------|-------------------|
| **Application** | `Application` | `ApplicationItems` (often exposed as `rows` in merge data) | each **ApplicationItem** |
| **ApplicationItem** | that **ApplicationItem** | nested lists if the report defines them | same item | |

On **ApplicationItem-root** templates, header aliases `VisaPeriod_NameTm` and `VisaCategory_NameTm` mirror `Application_VisaPeriod_NameTm` / `Application_VisaCategory_NameTm`.

### Placeholder syntax

| Data | Syntax | Example token |
|------|--------|----------------|
| Header / scalar | `{{ds.PropertyName}}` | `{{ds.ApplicationDateText}}` |
| Row (inside loop) | `{{.PropertyName}}` | `{{.Person_FullName}}` |
| Conditional | `{?{ds.Property}}…{{/}}` | `{?{ds.Urgency_NameTm}}…{{/}}` |
| Photo (Word only) | `{{IMAGE:Person_Photo}}` | Inline photo in cell |

### Table loop pattern

```
{{#ds.rows}}
{{.RowNumber}}. {{.Person_LastName}} {{.Person_FirstName}} — {{.Passport_Number}}
{{/ds.rows}}
```

`{{#ds.rows}}` and `{{/ds.rows}}` must each be in **their own paragraph**. Some templates use `{{#ds.ApplicationItems}}` instead — the collection name comes from the report definition / user template seed, not from the BO.

### Photos

`Person_Photo` is `byte[]` on **ApplicationItem**. Use **`{{IMAGE:Person_Photo}}`** inside the item loop. Do not use plain `{{.Person_Photo}}` (prints `System.Byte[]`). See [`docs/USER_TEMPLATE_AUTHOR_GUIDE.md`](USER_TEMPLATE_AUTHOR_GUIDE.md).

---

## Application — placeholders (`{{ds.*}}`)

Use when the template **root** is **Application**.

### Identity and date

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `ApplicationNumber` | `string` | `120` | Number only |
| `AppNumberPrefix` | `string` | `TRM` | Prefix from numbering profile |
| `FullApplicationNumber` | `string` | `TRM-2026-120` or `1/-120` | Depends on `ApplicationNumberingProfile` |
| `Year` | `int` | `2026` | |
| `Month` | `int` | `1` | |
| `ApplicationDate` | `DateTime` | `20.01.2026 00:00:00` | Prefer text in Word |
| `ApplicationDateText` | `string` | `20.01.2026` | **dd.MM.yyyy** |

### Company and signatory (application level)

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `Company_Code` | `string` | `CLK` | From company profile |
| `Application_Company_Name` | `string` | `Çalık Enerji Sanayi ve Ticaret A.Ş. Türk kärhanasynyň Türkmenistandaky şahamçasy` | |
| `Application_Company_Address` | `string` | `Aşgabat ş., Bitarap Türkmenistan şaýoly 538` | |
| `Application_Company_PhoneNumber` | `string` | `+993 12 …` | *(from profile)* |
| `Application_Company_Email` | `string` | `info@…` | *(from profile)* |
| `Application_CompanyHead_FullName` | `string` | `Mehmet ÇIRAK` | Signatory |
| `Application_CompanyHead_PositionTm` | `string` | `Türkmenistandaky Şahamçasynyň müdiri` | |

Passport lines for the signatory are on **ApplicationItem** (`CompanyHead_Passport*`), not on **Application**.

### Application type, urgency, visa (header)

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `ApplicationType_Name` | `string` | `App_Inv_And_WP` | Internal type name |
| `Urgency_NameTm` | `string` | `Gyssagly` or *(empty)* | When urgency not set |
| `VisaPeriod_NameTm` | `string` | `6 aý` | Turkmen period label |
| `VisaCategory_NameTm` | `string` | `köp gezeklik` | |

No `VisaType_NameTm` on **Application** — use item `Visa_TypeTm` in row loops.

### Project and ministry

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `ProjectContract_Description` | `string` | `GT-15 …` | *(from contract)* |
| `ProjectContract_Ministry_RecipientBlock` | `string` | `Türkmenenergo` + address lines | Full ministry block |
| `ProjectContract_Ministry_FormOfAddress` | `string` | `Durdu Baýjanowiç` | Salutation name |

### Migration service

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `MigrationService_NameTm` | `string` | `Döwlet migrasiýa gullugy` | |

Migration **code** on items: `Application_MigrationServiceCode` → e.g. `TDMGAS`.

### Family / sponsor (application level)

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `FamilyMember_Relationship_NameTm` | `string` | `aýalynyň we çagasynyň` | Genitive list; 1 FM → `aýalynyň`; 3 → `aýalynyň, çagasynyň we oglunyň` |
| `SponsoringEmployee_FullName` | `string` | `Ali Enes Yetkin` | First application line’s sponsor |
| `SponsoringEmployee_PositionTm` | `string` | `Türkmenistandaky şahamça müdiriniň orunbasary` | |

### Business trip (application header)

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `BusinessTripStartDate` | `DateTime?` | `01.03.2026` | Raw |
| `BusinessTripEndDate` | `DateTime?` | `15.03.2026` | Raw |
| `BusinessTripStartDateText` | `string` | `01.03.2026` | **dd.MM.yyyy** |
| `BusinessTripEndDateText` | `string` | `15.03.2026` | |
| `BusinessTripDurationDays` | `int?` | `15` | Inclusive |
| `BusinessTripPurpose_NameTm` | `string` | *(lookup NameTm)* | |
| `FromCityName` | `string` | `Aşgabat şäheri` | |
| `FromRegionName` | `string` | `Aşgabat` | |
| `FromRegionName_Genitive` | `string` | `Mary welaýatynyň` | Vowel-harmony suffix |
| `FromCityName_Ablative` | `string` | `Aşgabat şäherinden` | |
| `ToCityName` | `string` | `Daşoguz şäheri` | |
| `ToRegionName` | `string` | `Daşoguz` | |
| `ToRegionName_Genitive` | `string` | `Ahal welaýatynyň` | |
| `ToCityName_Dative` | `string` | `Akbugdaý etrabyna` | |
| `MovementPermitLocation_NameTm` | `string` | *(lookup)* | |
| `BorderZoneLocation_NameTm` | `string` | `Daşoguz welaýaty` | App-level catalog text |

### Person counts (Turkmen words)

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `TotalPersonCount` | `int` | `3` | Active lines |
| `TotalPersonCountText` | `string` | `üç` | Turkmen words (`bir`, `iki`, …) |
| `CancelPersonCount` | `int` | `2` | |
| `CancelPersonCountText` | `string` | `iki` | |
| `CancelVisaCount` | `int` | `3` | 2 lines × (current + next visa) |
| `CancelVisaCountText` | `string` | `üç` | |
| `CancelWPCount` | `int` | `4` | |
| `CancelWPCountText` | `string` | `dört` | |
| `CancelInvCount` | `int` | `2` | |
| `CancelInvCountText` | `string` | `iki` | |

---

## ApplicationItem — placeholders (`{{.*}}` or `{{ds.*}}` on item-root)

Use **`{{.Property}}`** inside `{{#ds.rows}}` when the root is **Application**.  
Use **`{{ds.Property}}`** when the root is **ApplicationItem**.

`RowNumber` is set at merge time (1, 2, 3, …).

### Person

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `RowNumber` | `int` | `1` | Merge index |
| `Person_FullName` | `string` | `Yetkin Didem` | |
| `Person_LastName` | `string` | `Yetkin` | |
| `Person_FirstName` | `string` | `Didem` | |
| `Person_MiddleName` | `string` | *(optional)* | |
| `Person_GenderTm` | `string` | `Aýal` | |
| `Person_MaritalStatusTm` | `string` | `Nikaly` | |
| `Person_BirthPlace` | `string` | `Türkiye/Gaziantep` | |
| `Person_DateOfBirth` | `DateTime?` | `18.01.1977` | |
| `Person_DateOfBirthText` | `string` | `18.01.1977` | |
| `Person_NationalityCode` | `string` | `TUR` | |
| `Person_NationalityTm` | `string` | `Türkiýe` | |
| `Person_CountryOfBirthCode` | `string` | `TUR` | |
| `Person_CountryOfBirthTm` | `string` | `Türkiýe` | |
| `Person_ForeignAddress` | `string` | `Emek mahallesi … gaziantep` | |
| `Person_ForeignAddressCountryCode` | `string` | `TUR` | |
| `Person_ForeignAddressWithCountry` | `string` | `TUR, Emek mahallesi …` | |
| `Person_Photo` | `byte[]` | *(image)* | `{{IMAGE:Person_Photo}}` |
| `Person_IsEmployee` | `bool` | `false` | FM line |
| `Person_RelationshipTm` | `string` | `ayaly` | To sponsor |
| `Person_SponsoringEmployeeFullName` | `string` | `Ali Enes Yetkin` | |
| `Person_SponsoringEmployeePositionTm` | `string` | `… müdiriniň orunbasary` | |

### Position

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `Position_PositionTm` | `string` | `Taslamanyň dolandyryş müdiri` | |
| `Position_DepartmentTm` | `string` | *(department NameTm)* | |
| `Position_NameTm` | `string` | Same as `Position_PositionTm` | Business-trip alias |

### Passport (current)

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `Passport_Number` | `string` | `U36556957` | |
| `Passport_PersonalNumber` | `string` | `11402573788` | |
| `Passport_Authority` | `string` | *(authority)* | |
| `Passport_IssueDate` | `DateTime?` | `20.05.2024` | |
| `Passport_IssueDateText` | `string` | `20.05.2024` | |
| `Passport_ExpirationDate` | `DateTime?` | `20.05.2034` | |
| `Passport_ExpirationDateText` | `string` | `20.05.2034` | |
| `Passport_CountryCode` | `string` | `TUR` | |
| `Passport_CountryTm` | `string` | `Türkiýe` | |

### Passport (previous)

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `PreviousPassport_Number` | `string` | `…` | *(if set)* |
| `PreviousPassport_PersonalNumber` | `string` | `…` | |
| `PreviousPassport_Authority` | `string` | `…` | |
| `PreviousPassport_IssueDate` | `DateTime?` | `01.01.2020` | |
| `PreviousPassport_IssueDateText` | `string` | `01.01.2020` | |
| `PreviousPassport_ExpirationDate` | `DateTime?` | `01.01.2030` | |
| `PreviousPassport_ExpirationDateText` | `string` | `01.01.2030` | |
| `PreviousPassport_CountryCode` | `string` | `TUR` | |
| `PreviousPassport_CountryTm` | `string` | `Türkiýe` | |

### Visa (current / cancel stacks)

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `Visa_Number` | `string` | `A1688318` | |
| `Visa_IssueDate` | `DateTime?` | `20.01.2026` | |
| `Visa_IssueDateText` | `string` | `20.01.2026` | |
| `Visa_StartDate` | `DateTime?` | `20.01.2026` | |
| `Visa_StartDateText` | `string` | `20.01.2026` | |
| `Visa_ExpirationDate` | `DateTime?` | `06.07.2026` | |
| `Visa_ExpirationDateText` | `string` | `06.07.2026` | |
| `Visa_IssuedPlaceTm` | `string` | `Aşgabat şäher howa menzilindäki MGP` | |
| `Visa_CategoryTm` | `string` | `köp gezeklik` | |
| `Visa_TypeTm` | `string` | `FM` | |
| `Visa_NumberAndType` | `string` | `A1688318 köp gezeklik` | Space-separated |
| `Visa_DurationFrequencyBlock` | `string` | See [multiline](#composite--multiline-outputs) | Excel: enable wrap |
| `CancelVisa_NumberBlock` | `string` | See [multiline](#composite--multiline-outputs) | Current then next visa |
| `CancelVisa_StartDateBlock` | `string` | See [multiline](#composite--multiline-outputs) | |
| `CancelVisa_ExpirationDateBlock` | `string` | See [multiline](#composite--multiline-outputs) | |

### Address of residence

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `Address_FullAddress` | `string` | `Aşgabat şäheriniň 1958-nji (Andalyp) köçesi jaý-86, öý-114` | |
| `Address_Type` | `string` | `Permanent` | Enum name |
| `Address_StartDate` | `DateTime?` | `01.01.2024` | |
| `Address_StartDateText` | `string` | `01.01.2024` | |
| `Address_ExpirationDate` | `DateTime?` | *(optional)* | |
| `Address_ExpirationDateText` | `string` | `31.12.2026` | |
| `Address_RegionTm` | `string` | *(empty)* | Not populated in code today |
| `Address_CityTm` | `string` | *(empty)* | |

### Travel / check-in movement (item)

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `Travel_Date` | `DateTime?` | `20.01.2026` | |
| `Travel_DateText` | `string` | `20.01.2026` | |
| `Travel_PurposeOfTravelTm` | `string` | *(PurposeOfTravel)* | Not Forma 16 §8 |
| `Travel_CheckPointTm` | `string` | `Aşgabat … MGP` | |

### Registration / Forma 16 (item)

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `Registration_GelmeginMaksadyTm` | `string` | **Employee:** `Türkmenistandaky şahamça müdiriniň orunbasary` · **FM:** `Türkmenistandaky şahamça müdiriniň orunbasary-Ali Enes Yetkin-ayaly` | §8; dash-separated FM line |

### Contract and company (item)

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `Contract_Salary` | `string` | `5000` | Amount text from salary |
| `Contract_SalaryText` | `string` | `5000` | Same as `Contract_Salary` |
| `Contract_StartDateText` | `string` | `06.07.2026` | From current visa expiry |
| `Contract_ExpirationDateText` | `string` | `06.01.2027` | + visa period months |
| `Contract_PeriodFallbackText` | `string` | `Rugsatnamanyň başlaýan gününden 6 aý möhleti bilen güýje girer.` | When no current visa |
| `Salary_CurrencyCode` | `string` | `USD` | |
| `Application_CompanyAddress` | `string` | `Aşgabat ş., Bitarap Türkmenistan şaýoly 538` | Item-level; no underscore |

### Work duty

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `WorkDuty_Description` | `string` | *(duty text)* | |

### Education

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `Education_GraduationYear` | `string` | `2002` | |
| `Education_LevelTm` | `string` | `Ýokary` | |
| `Education_InstitutionName` | `string` | `Gündogar mediterian uniwersiteti` | NameTm-first |
| `Education_SpecialtyTm` | `string` | `elektrik-elektronika inženerçiligi` | |
| `Education_CountryCode` | `string` | `TUR` | |
| `Education_LevelAndInstitutionTm` | `string` | `Ýokary, Gündogar mediterian uniwersiteti` | Comma if both set |

### Work permit

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `WorkPermit_Number` | `string` | `WP-…` | |
| `WorkPermit_StartDateText` | `string` | `01.01.2026` | |
| `WorkPermit_ExpirationDate` | `DateTime?` | `31.12.2026` | |
| `WorkPermit_ExpirationDateText` | `string` | `31.12.2026` | |
| `WorkPermit_ASNumber` | `string` | `…` | |
| `WorkPermit_WorkPermittedLocations` | `string` | `Aşgabat; Mary` | Catalog multi-select |
| `PreviousWorkPermit_Number` | `string` | `…` | |
| `PreviousWorkPermit_ExpirationDateText` | `string` | `31.12.2025` | |

### Invitation

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `Invitation_Number` | `string` | `…` | |
| `Invitation_StartDateText` | `string` | `01.01.2026` | |
| `Invitation_ExpirationDateText` | `string` | `30.06.2026` | |
| `PreviousInvitation_Number` | `string` | `…` | |
| `PreviousInvitation_StartDateText` | `string` | `…` | |
| `PreviousInvitation_ExpirationDateText` | `string` | `…` | |

### Medical record

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `MedicalRecord_Number` | `string` | `…` | |
| `MedicalRecord_IssueDate` | `DateTime?` | `01.01.2026` | |
| `MedicalRecord_ExpirationDate` | `DateTime?` | `01.01.2027` | |
| `MedicalRecord_ExpirationDateText` | `string` | `01.01.2027` | |

### Application reference (from parent application)

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `Application_FullNumber` | `string` | `1/-120` | |
| `Application_VisaPeriod_NameTm` | `string` | `6 aý` | |
| `VisaPeriod_NameTm` | `string` | `6 aý` | Item-root alias |
| `Application_VisaCategory_NameTm` | `string` | `köp gezeklik` | |
| `VisaCategory_NameTm` | `string` | `köp gezeklik` | Item-root alias |
| `BorderZoneLocation_NameTm` | `string` | `Daşoguz welaýaty` | Per-line |
| `Application_BorderZoneLocation_NameTm` | `string` | Same as above | |
| `Application_DateText` | `string` | `20.01.2026` | |
| `Application_MigrationServiceCode` | `string` | `TDMGAS` | |
| `Application_RegistrationDateText` | `string` | `20.01.2026` | Item registration date |
| `Application_SponsorName` | `string` | `Çalık Enerji … şahamçasy` | Company |
| `Application_SponsorSignatory` | `string` | `Mehmet ÇIRAK` | |

### Signatory, representative, registry (item)

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `CompanyHead_FullName` | `string` | `Mehmet ÇIRAK` | |
| `CompanyHead_PositionTm` | `string` | `Türkmenistandaky Şahamçasynyň müdiri` | |
| `Application_CompanyHead_FullName` | `string` | `Mehmet ÇIRAK` | Alias |
| `Application_CompanyHead_PositionTm` | `string` | `… müdiri` | Alias |
| `CompanyHead_PassportNumber` | `string` | `…` | If signatory is expat |
| `CompanyHead_PassportAuthority` | `string` | `…` | |
| `CompanyHead_PassportIssueDateText` | `string` | `15.03.2020` | |
| `CompanyHead_PassportLine` | `string` | `U1234567, …, 15.03.2020ý.` | One line |
| `Representative_FullName` | `string` | `…` | |
| `Representative_PassportLine` | `string` | `…` | |
| `Representative_Phone` | `string` | `+993 …` | |
| `Application_CompanyRegistryAddressLine` | `string` | `№123 … 01.01.2020 Aşgabat ş. … +993 …` | Tax + address + phone |

### Business trip address (item)

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `BusinessTripAddress_FullAddress` | `string` | `Mary welaýaty, …` | Trip destination |

### PDF visa application (XFA) — item

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `Pdf_FamilyMembersAggregateText` | `string` | See [multiline](#composite--multiline-outputs) | Employee household |
| `Pdf_SpouseLastName` | `string` | `Erol` | |
| `Pdf_SpouseFirstName` | `string` | `Firuza` | |
| `Pdf_SpouseAdditional` | `string` | `Mine, 23.05.1985` | Middle + DOB |
| `Pdf_AccompanyingFullName` | `string` | `Nil Erol` | |
| `Pdf_AccompanyingNationalityCode` | `string` | `TUR` | |
| `Pdf_AccompanyingDetail1` | `string` | `gyzy` | Relationship |
| `Pdf_AccompanyingDetail2` | `string` | `03.07.2014` | DOB |
| `Pdf_AccompanyingDetail3` | `string` | `U…` | Passport no. |
| `Pdf_AccompanyingDetail4` | `string` | `…` | Personal no. |

### Şahsy kagyz / FM sanawy — item

| Property | Type | Example output | Notes |
|----------|------|----------------|--------|
| `SahsyKagyz_FamilyStatusText` | `string` | `ayaly-Firuza Mine Erol 23.05.1985ý. TUR., gyzy-Nil Erol 03.07.2014ý. TUR.` | Maşgala ýagdaýy |
| `FM_EducationLevelTm` | `string` | `Çaga` / `Orta` / `Ýokary` | FM under 18 → `Çaga`; adult FM → `Orta`; employee → education level |
| `FM_SpecialtyTm` | `string` | `Çaga` / `Orta` / specialty | Same age rules |
| `FM_WezipesiTm` | `string` | `Zähmeti goramak we tehniki howpsuzlyk boýunça başlyk Bóra Yolcu-ň gyzy` | FM: sponsor line; employee: position only |

---

## Composite / multiline outputs

These properties embed **line breaks** (`Environment.NewLine`). In Excel, enable **wrap text** on the column.

### `Visa_DurationFrequencyBlock` (Gurlusyk / ministry Excel)

Merged cell shows **four lines** (blank lines omitted):

```
20.01.2026
06.07.2026
(A1688318)
köp gezeklik
```

### `CancelVisa_NumberBlock` / `StartDateBlock` / `ExpirationDateBlock`

When **CurrentVisa** and **NextVisa** are both set, **same row**, stacked:

```
A1688318
B2200001
```

(Same pattern for start/end date blocks with `dd.MM.yyyy` per line.)

### `Pdf_FamilyMembersAggregateText`

One line per family member from master data:

```
Firuza Mine Erol; 23.05.1985; ayaly
Nil Erol; 03.07.2014; gyzy
```

(Manual `VisaApplicationFamilyMembersText` uses the same aggregate formatter when master list is empty.)

### Letter body with counts (application header)

Template static text + placeholders, e.g. cancel-visa letter:

```
… 1 (bir) sany daşary ýurt raýatynyň 1 (bir) sany wizasyny ýatyrmagyňyzy …
```

Uses `{{ds.CancelPersonCount}}` + `{{ds.CancelPersonCountText}}` and `{{ds.CancelVisaCount}}` + `{{ds.CancelVisaCountText}}`.

---

## Common patterns

### Letter header (merged output)

```
TRM-2026-042
20.01.2026
Türkmenenergo
…

Hormatly Durdu Baýjanowiç!
```

Tokens:

```
{{ds.FullApplicationNumber}}
{{ds.ApplicationDateText}}
{{ds.ProjectContract_Ministry_RecipientBlock}}

Hormatly {{ds.ProjectContract_Ministry_FormOfAddress}}!
```

### Signatory block

```
Türkmenistandaky Şahamçasynyň müdiri              Mehmet ÇIRAK
```

### Table row (merged)

```
1. Yetkin D. U36556957
```

### Conditional urgency

When `Urgency_NameTm` is set:

```
Artykmaç çalt okatmak üçin — Gyssagly
```

---

## Source files

| Business object | File |
|-----------------|------|
| `Application` | `Visa2026.Module/BusinessObjects/Application.cs` |
| `ApplicationItem` | `Visa2026.Module/BusinessObjects/ApplicationItem.cs` |

## Related documentation

- [`docs/WORD_REPORT_GENERATION_IDEA.md`](WORD_REPORT_GENERATION_IDEA.md) — architecture and code-backed reports
- [`docs/EXCEL_PLACEHOLDER_REFERENCE.md`](EXCEL_PLACEHOLDER_REFERENCE.md) — Excel list templates
- [`docs/USER_TEMPLATE_AUTHOR_GUIDE.md`](USER_TEMPLATE_AUTHOR_GUIDE.md) — authoring workflow, Extract/Validate
- [`docs/USER_REPORT_MAP_STANDARD.md`](USER_REPORT_MAP_STANDARD.md) — per-template §12 golden values
- [`.cursor/skills/visa2026-word-reports/SKILL.md`](../.cursor/skills/visa2026-word-reports/SKILL.md) — code-backed Word reports skill
