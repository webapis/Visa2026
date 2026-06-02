# Word / Excel Report Placeholder Reference

> **Source of truth:** [`Visa2026.Module/BusinessObjects/Application.cs`](../Visa2026.Module/BusinessObjects/Application.cs) and [`Visa2026.Module/BusinessObjects/ApplicationItem.cs`](../Visa2026.Module/BusinessObjects/ApplicationItem.cs).  
> Every property listed here exists on one of those types (report flatteners are `[NotMapped]` and hidden from the normal detail UI unless noted).  
> Update this document when you add or rename report placeholders on those BOs.

**Not covered here:** keys built only inside code-backed [`IWordReportDefinition`](../Visa2026.Module/Services/WordReports/) merge dictionaries (for example split ministry address lines). See [`docs/WORD_REPORT_GENERATION_IDEA.md`](WORD_REPORT_GENERATION_IDEA.md) and per-template `*_map.md` under `Visa2026.Module/Resources/FormTemplates/`.

Excel user templates use the **same property names**; see [`docs/EXCEL_PLACEHOLDER_REFERENCE.md`](EXCEL_PLACEHOLDER_REFERENCE.md).

---

## How to Use This Reference

### Template root

| Root business object | `{{ds.*}}` binds to | Row loop collection | `{{.*}}` binds to |
|----------------------|---------------------|---------------------|-------------------|
| **Application** | `Application` | `ApplicationItems` (often exposed as `rows` in merge data) | each **ApplicationItem** |
| **ApplicationItem** | that **ApplicationItem** | nested lists if the report defines them | same item |

On **ApplicationItem-root** templates, header aliases `VisaPeriod_NameTm` and `VisaCategory_NameTm` mirror `Application_VisaPeriod_NameTm` / `Application_VisaCategory_NameTm`.

### Placeholder syntax

| Data | Syntax | Example |
|------|--------|---------|
| Header / scalar | `{{ds.PropertyName}}` | `{{ds.ApplicationDateText}}` |
| Row (inside loop) | `{{.PropertyName}}` | `{{.Person_FullName}}` |
| Conditional | `{?{ds.Property}}…{{/}}` | `{?{ds.Urgency_NameTm}}Artykmaç — {{ds.Urgency_NameTm}}{{/}}` |
| Photo (Word only) | `{{IMAGE:Person_Photo}}` | Post-merge injection — see below |

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

| Property | Type | Notes |
|----------|------|--------|
| `ApplicationNumber` | `string` | Number only |
| `AppNumberPrefix` | `string` | Prefix (e.g. `A-`) |
| `FullApplicationNumber` | `string` | Formatted number |
| `Year` | `int` | Application year |
| `Month` | `int` | Application month |
| `ApplicationDate` | `DateTime` | Raw date — prefer text for Word |
| `ApplicationDateText` | `string` | **dd.MM.yyyy** |

### Company and signatory (application level)

| Property | Type | Notes |
|----------|------|--------|
| `Company_Code` | `string` | From company profile |
| `Application_Company_Name` | `string` | Company name |
| `Application_Company_Address` | `string` | Company address |
| `Application_Company_PhoneNumber` | `string` | Phone |
| `Application_Company_Email` | `string` | Email |
| `Application_CompanyHead_FullName` | `string` | Authorized signatory name |
| `Application_CompanyHead_PositionTm` | `string` | Signatory position (Turkmen) |

Passport lines for the signatory are on **ApplicationItem** (`CompanyHead_Passport*`), not on **Application**.

### Application type, urgency, visa (header)

| Property | Type | Notes |
|----------|------|--------|
| `ApplicationType_Name` | `string` | Application type display name |
| `Urgency_NameTm` | `string` | Urgency (Turkmen); empty when not set |
| `VisaPeriod_NameTm` | `string` | Visa period (Turkmen) |
| `VisaCategory_NameTm` | `string` | Visa category (Turkmen) |

There is no `VisaType_NameTm` or `Category` flattener on **Application** — use navigations in the UI or item-level visa fields in row loops.

### Project and ministry

| Property | Type | Notes |
|----------|------|--------|
| `ProjectContract_Description` | `string` | Project description |
| `ProjectContract_Ministry_RecipientBlock` | `string` | Ministry address block |
| `ProjectContract_Ministry_FormOfAddress` | `string` | Ministry salutation |

### Migration service

| Property | Type | Notes |
|----------|------|--------|
| `MigrationService_NameTm` | `string` | Migration service (Turkmen) |

Migration **code** is on **ApplicationItem** as `Application_MigrationServiceCode`.

### Family / sponsor (application level)

| Property | Type | Notes |
|----------|------|--------|
| `FamilyMember_Relationship_NameTm` | `string` | Genitive list of FM relationships on the application |
| `SponsoringEmployee_FullName` | `string` | From first application line |
| `SponsoringEmployee_PositionTm` | `string` | Sponsor position (Turkmen) |

### Business trip (application header)

| Property | Type | Notes |
|----------|------|--------|
| `BusinessTripStartDate` | `DateTime?` | Raw start date |
| `BusinessTripEndDate` | `DateTime?` | Raw end date |
| `BusinessTripStartDateText` | `string` | **dd.MM.yyyy** |
| `BusinessTripEndDateText` | `string` | **dd.MM.yyyy** |
| `BusinessTripDurationDays` | `int?` | Inclusive day count |
| `BusinessTripPurpose_NameTm` | `string` | Purpose (Turkmen) |
| `FromCityName` | `string` | From city |
| `FromRegionName` | `string` | From region |
| `FromRegionName_Genitive` | `string` | From region (genitive) |
| `FromCityName_Ablative` | `string` | From city (ablative) |
| `ToCityName` | `string` | To city |
| `ToRegionName` | `string` | To region |
| `ToRegionName_Genitive` | `string` | To region (genitive) |
| `ToCityName_Dative` | `string` | To city (dative) |
| `MovementPermitLocation_NameTm` | `string` | Movement permit location |
| `BorderZoneLocation_NameTm` | `string` | Application-level border zone (Turkmen) |

Per-line border zone text is **ApplicationItem** `BorderZoneLocation_NameTm` / `Application_BorderZoneLocation_NameTm`.

### Person counts (Turkmen words)

| Property | Type | Notes |
|----------|------|--------|
| `TotalPersonCount` | `int` | Active application lines |
| `TotalPersonCountText` | `string` | Count in Turkmen words |
| `CancelPersonCount` | `int` | Cancel applications — line count |
| `CancelPersonCountText` | `string` | In Turkmen words |
| `CancelVisaCount` | `int` | **App_Cancel_Visa:** +1 per line for `CurrentVisa`, +1 for `NextVisa` |
| `CancelVisaCountText` | `string` | In Turkmen words |
| `CancelWPCount` | `int` | WP cancel count |
| `CancelWPCountText` | `string` | In Turkmen words |
| `CancelInvCount` | `int` | Lines with current invitation |
| `CancelInvCountText` | `string` | In Turkmen words |

---

## ApplicationItem — placeholders (`{{.*}}` or `{{ds.*}}` on item-root)

Use **`{{.Property}}`** inside `{{#ds.rows}}` / `{{#ds.ApplicationItems}}` when the root is **Application**.  
Use **`{{ds.Property}}`** when the root is **ApplicationItem**.

`RowNumber` is set at merge time (sequential index in the roster).

### Person

| Property | Type | Notes |
|----------|------|--------|
| `RowNumber` | `int` | Set by merge (not stored) |
| `Person_FullName` | `string` | |
| `Person_LastName` | `string` | |
| `Person_FirstName` | `string` | |
| `Person_MiddleName` | `string` | |
| `Person_GenderTm` | `string` | Gender (Turkmen) |
| `Person_MaritalStatusTm` | `string` | Marital status (Turkmen) |
| `Person_BirthPlace` | `string` | |
| `Person_DateOfBirth` | `DateTime?` | |
| `Person_DateOfBirthText` | `string` | **dd.MM.yyyy** |
| `Person_NationalityCode` | `string` | |
| `Person_NationalityTm` | `string` | |
| `Person_CountryOfBirthCode` | `string` | |
| `Person_CountryOfBirthTm` | `string` | |
| `Person_ForeignAddress` | `string` | |
| `Person_ForeignAddressCountryCode` | `string` | e.g. `TUR` |
| `Person_ForeignAddressWithCountry` | `string` | `code, address` |
| `Person_Photo` | `byte[]` | Use `{{IMAGE:Person_Photo}}` in Word |
| `Person_IsEmployee` | `bool` | |
| `Person_RelationshipTm` | `string` | Family relationship (Turkmen) |
| `Person_SponsoringEmployeeFullName` | `string` | |
| `Person_SponsoringEmployeePositionTm` | `string` | |

### Position

| Property | Type | Notes |
|----------|------|--------|
| `Position_PositionTm` | `string` | Current position (Turkmen) |
| `Position_DepartmentTm` | `string` | Department (Turkmen) |
| `Position_NameTm` | `string` | Alias of `Position_PositionTm` (business-trip sanawy) |

### Passport (current)

| Property | Type | Notes |
|----------|------|--------|
| `Passport_Number` | `string` | |
| `Passport_PersonalNumber` | `string` | Person or passport personal number |
| `Passport_Authority` | `string` | |
| `Passport_IssueDate` | `DateTime?` | |
| `Passport_IssueDateText` | `string` | **dd.MM.yyyy** |
| `Passport_ExpirationDate` | `DateTime?` | |
| `Passport_ExpirationDateText` | `string` | **dd.MM.yyyy** |
| `Passport_CountryCode` | `string` | |
| `Passport_CountryTm` | `string` | |

### Passport (previous)

| Property | Type | Notes |
|----------|------|--------|
| `PreviousPassport_Number` | `string` | |
| `PreviousPassport_PersonalNumber` | `string` | |
| `PreviousPassport_Authority` | `string` | |
| `PreviousPassport_IssueDate` | `DateTime?` | |
| `PreviousPassport_IssueDateText` | `string` | |
| `PreviousPassport_ExpirationDate` | `DateTime?` | |
| `PreviousPassport_ExpirationDateText` | `string` | |
| `PreviousPassport_CountryCode` | `string` | |
| `PreviousPassport_CountryTm` | `string` | |

### Visa (current / cancel stacks)

| Property | Type | Notes |
|----------|------|--------|
| `Visa_Number` | `string` | |
| `Visa_IssueDate` | `DateTime?` | |
| `Visa_IssueDateText` | `string` | |
| `Visa_StartDate` | `DateTime?` | |
| `Visa_StartDateText` | `string` | |
| `Visa_ExpirationDate` | `DateTime?` | |
| `Visa_ExpirationDateText` | `string` | |
| `Visa_IssuedPlaceTm` | `string` | |
| `Visa_CategoryTm` | `string` | |
| `Visa_TypeTm` | `string` | |
| `Visa_NumberAndType` | `string` | Number + category |
| `Visa_DurationFrequencyBlock` | `string` | Multiline: start, end, `(number)`, category — Excel wrap |
| `CancelVisa_NumberBlock` | `string` | Stacked `CurrentVisa` then `NextVisa` numbers |
| `CancelVisa_StartDateBlock` | `string` | Stacked validity starts |
| `CancelVisa_ExpirationDateBlock` | `string` | Stacked validity ends |

### Address of residence

| Property | Type | Notes |
|----------|------|--------|
| `Address_FullAddress` | `string` | |
| `Address_Type` | `string` | Enum `.ToString()` |
| `Address_StartDate` | `DateTime?` | |
| `Address_StartDateText` | `string` | |
| `Address_ExpirationDate` | `DateTime?` | |
| `Address_ExpirationDateText` | `string` | |
| `Address_RegionTm` | `string` | Currently always empty in code |
| `Address_CityTm` | `string` | Currently always empty in code |

### Travel / check-in movement (item)

| Property | Type | Notes |
|----------|------|--------|
| `Travel_Date` | `DateTime?` | |
| `Travel_DateText` | `string` | |
| `Travel_PurposeOfTravelTm` | `string` | `PurposeOfTravel.NameTm` — not Forma 16 §8 |
| `Travel_CheckPointTm` | `string` | |

### Registration / Forma 16 (item)

| Property | Type | Notes |
|----------|------|--------|
| `Registration_GelmeginMaksadyTm` | `string` | Forma 16 §8: employee → position; FM → `position-name-relationship` |

### Contract and company (item)

| Property | Type | Notes |
|----------|------|--------|
| `Contract_Salary` | `string` | Salary amount text |
| `Contract_SalaryText` | `string` | Same as `Contract_Salary` |
| `Contract_StartDateText` | `string` | Derived from current visa expiry |
| `Contract_ExpirationDateText` | `string` | Start + `Application.VisaPeriod.PdfForm_Count` months |
| `Contract_PeriodFallbackText` | `string` | When no current visa |
| `Salary_CurrencyCode` | `string` | |
| `Application_CompanyAddress` | `string` | Company address (no underscore; item-level) |

### Work duty

| Property | Type | Notes |
|----------|------|--------|
| `WorkDuty_Description` | `string` | Current work duty |

### Education

| Property | Type | Notes |
|----------|------|--------|
| `Education_GraduationYear` | `string` | |
| `Education_LevelTm` | `string` | |
| `Education_InstitutionName` | `string` | NameTm-first, fallback Name |
| `Education_SpecialtyTm` | `string` | |
| `Education_CountryCode` | `string` | |
| `Education_LevelAndInstitutionTm` | `string` | Level + institution, comma-separated |

### Work permit

| Property | Type | Notes |
|----------|------|--------|
| `WorkPermit_Number` | `string` | |
| `WorkPermit_StartDateText` | `string` | |
| `WorkPermit_ExpirationDate` | `DateTime?` | |
| `WorkPermit_ExpirationDateText` | `string` | |
| `WorkPermit_ASNumber` | `string` | |
| `WorkPermit_WorkPermittedLocations` | `string` | Line override or current WP |
| `PreviousWorkPermit_Number` | `string` | |
| `PreviousWorkPermit_ExpirationDateText` | `string` | |

### Invitation

| Property | Type | Notes |
|----------|------|--------|
| `Invitation_Number` | `string` | |
| `Invitation_StartDateText` | `string` | |
| `Invitation_ExpirationDateText` | `string` | |
| `PreviousInvitation_Number` | `string` | |
| `PreviousInvitation_StartDateText` | `string` | |
| `PreviousInvitation_ExpirationDateText` | `string` | |

### Medical record

| Property | Type | Notes |
|----------|------|--------|
| `MedicalRecord_Number` | `string` | |
| `MedicalRecord_IssueDate` | `DateTime?` | |
| `MedicalRecord_ExpirationDate` | `DateTime?` | |
| `MedicalRecord_ExpirationDateText` | `string` | |

### Application reference (from parent application)

| Property | Type | Notes |
|----------|------|--------|
| `Application_FullNumber` | `string` | |
| `Application_VisaPeriod_NameTm` | `string` | |
| `VisaPeriod_NameTm` | `string` | Alias for item-root `{{ds.*}}` |
| `Application_VisaCategory_NameTm` | `string` | |
| `VisaCategory_NameTm` | `string` | Alias for item-root `{{ds.*}}` |
| `BorderZoneLocation_NameTm` | `string` | Per-line border zone (Turkmen) |
| `Application_BorderZoneLocation_NameTm` | `string` | Same as `BorderZoneLocation_NameTm` |
| `Application_DateText` | `string` | |
| `Application_MigrationServiceCode` | `string` | |
| `Application_RegistrationDateText` | `string` | Item `RegistrationDate` |
| `Application_SponsorName` | `string` | Company name |
| `Application_SponsorSignatory` | `string` | Signatory full name |

### Signatory, representative, registry (item)

| Property | Type | Notes |
|----------|------|--------|
| `CompanyHead_FullName` | `string` | |
| `CompanyHead_PositionTm` | `string` | |
| `Application_CompanyHead_FullName` | `string` | Alias |
| `Application_CompanyHead_PositionTm` | `string` | Alias |
| `CompanyHead_PassportNumber` | `string` | |
| `CompanyHead_PassportAuthority` | `string` | |
| `CompanyHead_PassportIssueDateText` | `string` | |
| `CompanyHead_PassportLine` | `string` | Number, authority, date — one line |
| `Representative_FullName` | `string` | |
| `Representative_PassportLine` | `string` | |
| `Representative_Phone` | `string` | |
| `Application_CompanyRegistryAddressLine` | `string` | Tax info + address + phone |

### Business trip address (item)

| Property | Type | Notes |
|----------|------|--------|
| `BusinessTripAddress_FullAddress` | `string` | Trip destination address on the line |

### PDF visa application (XFA) — item

| Property | Type | Notes |
|----------|------|--------|
| `Pdf_FamilyMembersAggregateText` | `string` | Household list for TM visa PDF |
| `Pdf_SpouseLastName` | `string` | |
| `Pdf_SpouseFirstName` | `string` | |
| `Pdf_SpouseAdditional` | `string` | Middle name + DOB |
| `Pdf_AccompanyingFullName` | `string` | |
| `Pdf_AccompanyingNationalityCode` | `string` | |
| `Pdf_AccompanyingDetail1` | `string` | Relationship label |
| `Pdf_AccompanyingDetail2` | `string` | DOB |
| `Pdf_AccompanyingDetail3` | `string` | Passport number |
| `Pdf_AccompanyingDetail4` | `string` | Personal number |

### Şahsy kagyz / FM sanawy — item

| Property | Type | Notes |
|----------|------|--------|
| `SahsyKagyz_FamilyStatusText` | `string` | `sahsy_kagyz.docx` maşgala ýagdaýy line |
| `FM_EducationLevelTm` | `string` | FM column: Çaga/Orta or employee education |
| `FM_SpecialtyTm` | `string` | FM column: bilimine görä hünär |
| `FM_WezipesiTm` | `string` | FM column: wezipesi (employee position or sponsor line) |

---

## Common patterns

### Letter header

```
{{ds.FullApplicationNumber}}
{{ds.ApplicationDateText}}
{{ds.ProjectContract_Ministry_RecipientBlock}}

Hormatly {{ds.ProjectContract_Ministry_FormOfAddress}}!
```

### Signatory block (application root)

```
{{ds.Application_CompanyHead_PositionTm}}              {{ds.Application_CompanyHead_FullName}}
```

### Signatory passport (item or item-root)

```
{{.CompanyHead_PassportLine}}
```

### Table with row numbers

```
{{#ds.rows}}
{{.RowNumber}}. {{.Person_LastName}} {{.Person_FirstName[0]}}. {{.Passport_Number}}
{{/ds.rows}}
```

### Conditional urgency

```
{?{ds.Urgency_NameTm}}Artykmaç çalt okatmak üçin — {{ds.Urgency_NameTm}}{{/}}
```

---

## Source files

| Business object | File |
|-----------------|------|
| `Application` | `Visa2026.Module/BusinessObjects/Application.cs` |
| `ApplicationItem` | `Visa2026.Module/BusinessObjects/ApplicationItem.cs` |

## Related documentation

- [`docs/WORD_REPORT_GENERATION_IDEA.md`](WORD_REPORT_GENERATION_IDEA.md) — architecture and code-backed reports
- [`docs/EXCEL_PLACEHOLDER_REFERENCE.md`](EXCEL_PLACEHOLDER_REFERENCE.md) — Excel-specific notes
- [`docs/USER_TEMPLATE_AUTHOR_GUIDE.md`](USER_TEMPLATE_AUTHOR_GUIDE.md) — authoring workflow, Extract/Validate
- [`.cursor/skills/visa2026-word-reports/SKILL.md`](../.cursor/skills/visa2026-word-reports/SKILL.md) — code-backed Word reports skill
