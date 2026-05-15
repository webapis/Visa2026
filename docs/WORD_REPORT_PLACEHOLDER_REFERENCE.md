# Word Report Placeholder Reference

> **Reference guide for manually designing Word templates**  
> Lists all available placeholders from `Application`, `ApplicationItem`, `Registration`, and `BusinessTrip` business objects.

## How to Use This Reference

### Placeholder Syntax

| Data Source | Syntax | Example |
|-------------|--------|---------|
| **Header data** (single values) | `{{ds.PropertyName}}` | `{{ds.ApplicationDateText}}` |
| **Row data** (inside table loops) | `{{.PropertyName}}` | `{{.Person_FullName}}` |
| **Conditional** | `{?{ds.Condition}}...{{/}}` | `{?{ds.ShowUrgency}}{{ds.Urgency_NameTm}}{{/}}` |

### Table Loop Pattern

```
{{#ds.rows}}
{{.RowNumber}}. {{.Person_LastName}} {{.Person_FirstName}} — {{.Passport_Number}}
{{/ds.rows}}
```

**Important:** `{{#ds.rows}}` and `{{/ds.rows}}` must each be in **their own paragraph**.

---

## Application — Header Placeholders (`{{ds.*}}`)

Use these for letter headers, dates, counts, and application-level data.

### Application Identity

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{ds.ApplicationNumber}}` | `string` | Application number only |
| `{{ds.AppNumberPrefix}}` | `string` | Prefix (e.g., "A-", "B-") |
| `{{ds.FullApplicationNumber}}` | `string` | Full formatted number |
| `{{ds.Year}}` | `int` | Application year |
| `{{ds.Month}}` | `int` | Application month |
| `{{ds.ApplicationDateText}}` | `string` | Application date as **dd.MM.yyyy** (use in Word templates) |
| `{{ds.ApplicationDate}}` | `DateTime` | Raw date — merge may show locale date/time; prefer **`ApplicationDateText`** |

### Company & Signatory

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{ds.Company_Code}}` | `string` | Company code |
| `{{ds.Application_CompanyHead_FullName}}` | `string` | Company head name |
| `{{ds.Application_CompanyHead_PositionTm}}` | `string` | Company head position (Turkmen) |
| `{{ds.Application_CompanyHead_PassportNumber}}` | `string` | Signatory passport (if expat) |
| `{{ds.Application_CompanyHead_PassportAuthority}}` | `string` | Signatory passport authority |
| `{{ds.Application_CompanyHead_PassportIssueDateText}}` | `string` | Signatory passport issue date |
| `{{ds.Application_CompanyHead_PassportLine}}` | `string` | Combined: number, authority, date |
| `{{ds.Representative_FullName}}` | `string` | Representative name |
| `{{ds.Representative_PassportLine}}` | `string` | Representative passport line |
| `{{ds.Representative_Phone}}` | `string` | Representative phone |
| `{{ds.Application_CompanyAddress}}` | `string` | Company address |
| `{{ds.Application_CompanyRegistryAddressLine}}` | `string` | Tax info + address + phone |

### Application Type & Category

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{ds.Category}}` | `string` | Category (Employee/Family/Both) |
| `{{ds.ApplicationType_Name}}` | `string` | Application type name |
| `{{ds.ApplicationType_ShowUrgency}}` | `bool` | Flag for urgency visibility |
| `{{ds.Urgency_NameTm}}` | `string` | Urgency level (Turkmen) |

### Visa Configuration

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{ds.VisaPeriod_NameTm}}` | `string` | Visa period (Turkmen) |
| `{{ds.VisaCategory_NameTm}}` | `string` | Visa category (Turkmen) |
| `{{ds.VisaType_NameTm}}` | `string` | Visa type (Turkmen) |

### Project & Ministry

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{ds.ProjectContract_Description}}` | `string` | Project contract description |
| `{{ds.ProjectContract_Ministry_RecipientBlock}}` | `string` | Ministry address block |
| `{{ds.ProjectContract_Ministry_RecipientBlock_Line1}}` | `string` | Ministry line 1 (computed) |
| `{{ds.ProjectContract_Ministry_RecipientBlock_Line2}}` | `string` | Ministry line 2 (computed) |
| `{{ds.ProjectContract_Ministry_RecipientBlock_HasLine2}}` | `bool` | Has second line |
| `{{ds.ProjectContract_Ministry_FormOfAddress}}` | `string` | Ministry form of address |

### Migration Service

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{ds.MigrationService_NameTm}}` | `string` | Migration service name (Turkmen) |
| `{{ds.Application_MigrationServiceCode}}` | `string` | Migration service code |

### Business Trip

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{ds.BusinessTripStartDate}}` | `string` | Start date dd.MM.yyyy |
| `{{ds.BusinessTripEndDate}}` | `string` | End date dd.MM.yyyy |
| `{{ds.BusinessTripStartDateText}}` | `string` | Start date (formatted) |
| `{{ds.BusinessTripEndDateText}}` | `string` | End date (formatted) |
| `{{ds.BusinessTripDurationDays}}` | `int?` | Duration in days |
| `{{ds.BusinessTripPurpose_NameTm}}` | `string` | Purpose (Turkmen) |
| `{{ds.FromCityName}}` | `string` | From city |
| `{{ds.FromRegionName}}` | `string` | From region |
| `{{ds.FromRegionName_Genitive}}` | `string` | From region (genitive case) |
| `{{ds.FromCityName_Ablative}}` | `string` | From city (ablative case) |
| `{{ds.ToCityName}}` | `string` | To city |
| `{{ds.ToRegionName}}` | `string` | To region |
| `{{ds.ToRegionName_Genitive}}` | `string` | To region (genitive case) |
| `{{ds.ToCityName_Dative}}` | `string` | To city (dative case) |
| `{{ds.MovementPermitLocation_NameTm}}` | `string` | Movement permit location |
| `{{ds.BorderZoneLocation_NameTm}}` | `string` | Border zone location |

### Person Counts (Turkmen Words)

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{ds.TotalPersonCount}}` | `int` | Total count |
| `{{ds.TotalPersonCountText}}` | `string` | Count in Turkmen words |
| `{{ds.CancelPersonCount}}` | `int` | Cancel count |
| `{{ds.CancelPersonCountText}}` | `string` | Cancel count in words |
| `{{ds.CancelWPCount}}` | `int` | Cancel WP count |
| `{{ds.CancelWPCountText}}` | `string` | WP count in words |
| `{{ds.CancelInvCount}}` | `int` | Cancel invitation count |
| `{{ds.CancelInvCountText}}` | `string` | Invitation count in words |
| `{{ds.FamilyMember_Relationship_NameTm}}` | `string` | Family relationships (genitive list) |

---

## ApplicationItem — Row Placeholders (`{{.*}}`)

Use these **inside** `{{#ds.rows}}` … `{{/ds.rows}}` loops for person/item tables.

### Person Information

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.RowNumber}}` | `int` | Sequential row number |
| `{{.Person_FullName}}` | `string` | Full name |
| `{{.Person_LastName}}` | `string` | Last name |
| `{{.Person_FirstName}}` | `string` | First name |
| `{{.Person_MiddleName}}` | `string` | Middle name |
| `{{.Person_DateOfBirth}}` | `DateTime?` | Date of birth |
| `{{.Person_DateOfBirthText}}` | `string` | DOB as dd.MM.yyyy |
| `{{.Person_BirthPlace}}` | `string` | Birth place |
| `{{.Person_GenderTm}}` | `string` | Gender (Turkmen) |
| `{{.Person_NationalityCode}}` | `string` | Nationality code |
| `{{.Person_NationalityTm}}` | `string` | Nationality (Turkmen) |
| `{{.Person_CountryOfBirthCode}}` | `string` | Country of birth code |
| `{{.Person_CountryOfBirthTm}}` | `string` | Country of birth (Turkmen) |
| `{{.Person_ForeignAddress}}` | `string` | Foreign address |
| `{{.Person_Photo}}` | `byte[]` | Photo (binary) |

### Position

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.Position_PositionTm}}` | `string` | Position name (Turkmen) |
| `{{.Position_DepartmentTm}}` | `string` | Department (Turkmen) |

### Passport (Current)

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.Passport_Number}}` | `string` | Passport number |
| `{{.Passport_PersonalNumber}}` | `string` | Personal number |
| `{{.Passport_Authority}}` | `string` | Issuing authority |
| `{{.Passport_IssueDate}}` | `DateTime?` | Issue date |
| `{{.Passport_IssueDateText}}` | `string` | Issue date dd.MM.yyyy |
| `{{.Passport_ExpirationDate}}` | `DateTime?` | Expiration date |
| `{{.Passport_ExpirationDateText}}` | `string` | Expiration dd.MM.yyyy |
| `{{.Passport_CountryCode}}` | `string` | Country code |
| `{{.Passport_CountryTm}}` | `string` | Country (Turkmen) |

### Passport (Previous)

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.PreviousPassport_Number}}` | `string` | Previous passport number |
| `{{.PreviousPassport_PersonalNumber}}` | `string` | Previous personal number |
| `{{.PreviousPassport_Authority}}` | `string` | Previous authority |
| `{{.PreviousPassport_IssueDateText}}` | `string` | Previous issue date |
| `{{.PreviousPassport_ExpirationDateText}}` | `string` | Previous expiration |
| `{{.PreviousPassport_CountryCode}}` | `string` | Previous country code |
| `{{.PreviousPassport_CountryTm}}` | `string` | Previous country (Turkmen) |

### Visa (Current)

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.Visa_Number}}` | `string` | Visa number |
| `{{.Visa_IssueDate}}` | `DateTime?` | Visa issue date |
| `{{.Visa_IssueDateText}}` | `string` | Issue date dd.MM.yyyy |
| `{{.Visa_StartDate}}` | `DateTime?` | Visa start date |
| `{{.Visa_StartDateText}}` | `string` | Start date dd.MM.yyyy |
| `{{.Visa_ExpirationDate}}` | `DateTime?` | Visa expiration |
| `{{.Visa_ExpirationDateText}}` | `string` | Expiration dd.MM.yyyy |
| `{{.Visa_IssuedPlaceTm}}` | `string` | Issued place (Turkmen) |
| `{{.Visa_CategoryTm}}` | `string` | Visa category (Turkmen) |
| `{{.Visa_TypeTm}}` | `string` | Visa type (Turkmen) |

### Address of Residence

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.Address_FullAddress}}` | `string` | Full address |
| `{{.Address_Type}}` | `string` | Address type |
| `{{.Address_StartDate}}` | `DateTime?` | Start date |
| `{{.Address_StartDateText}}` | `string` | Start dd.MM.yyyy |
| `{{.Address_ExpirationDate}}` | `DateTime?` | Expiration date |
| `{{.Address_ExpirationDateText}}` | `string` | Expiration dd.MM.yyyy |
| `{{.Address_RegionTm}}` | `string` | Region (Turkmen) |
| `{{.Address_CityTm}}` | `string` | City (Turkmen) |

### Work Permit

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.WorkPermit_Number}}` | `string` | WP number |
| `{{.WorkPermit_StartDateText}}` | `string` | Start date dd.MM.yyyy |
| `{{.WorkPermit_ExpirationDate}}` | `DateTime?` | Expiration date |
| `{{.WorkPermit_ExpirationDateText}}` | `string` | Expiration dd.MM.yyyy |
| `{{.WorkPermit_ASNumber}}` | `string` | AS number |
| `{{.WorkPermit_WorkPermittedLocations}}` | `string` | Permitted locations |
| `{{.PreviousWorkPermit_Number}}` | `string` | Previous WP number |
| `{{.PreviousWorkPermit_ExpirationDateText}}` | `string` | Previous WP expiration |

### Invitation

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.Invitation_Number}}` | `string` | Invitation number |
| `{{.Invitation_StartDateText}}` | `string` | Start dd.MM.yyyy |
| `{{.Invitation_ExpirationDateText}}` | `string` | Expiration dd.MM.yyyy |
| `{{.PreviousInvitation_Number}}` | `string` | Previous invitation number |
| `{{.PreviousInvitation_StartDateText}}` | `string` | Previous start |
| `{{.PreviousInvitation_ExpirationDateText}}` | `string` | Previous expiration |

### Contract

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.Contract_Salary}}` | `decimal?` | Salary amount |
| `{{.Contract_SalaryText}}` | `string` | Salary formatted (#,##0.00) |
| `{{.Contract_StartDateText}}` | `string` | Contract start dd.MM.yyyy |
| `{{.Contract_ExpirationDateText}}` | `string` | Contract end dd.MM.yyyy |
| `{{.Contract_PeriodFallbackText}}` | `string` | Fallback text (no visa) |
| `{{.Salary_CurrencyCode}}` | `string` | Currency code |

### Education

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.Education_GraduationYear}}` | `int?` | Graduation year |
| `{{.Education_LevelTm}}` | `string` | Education level (Turkmen) |
| `{{.Education_InstitutionName}}` | `string` | Institution name |
| `{{.Education_SpecialtyTm}}` | `string` | Specialty (Turkmen) |

### Medical Record

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.MedicalRecord_Number}}` | `string` | Document number |
| `{{.MedicalRecord_IssueDate}}` | `DateTime?` | Issue date |
| `{{.MedicalRecord_ExpirationDate}}` | `DateTime?` | Expiration date |
| `{{.MedicalRecord_ExpirationDateText}}` | `string` | Expiration dd.MM.yyyy |

### Signatory (Application Level)

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.CompanyHead_FullName}}` | `string` | Signatory name |
| `{{.CompanyHead_PositionTm}}` | `string` | Signatory position |
| `{{.CompanyHead_PassportNumber}}` | `string` | Signatory passport |
| `{{.CompanyHead_PassportAuthority}}` | `string` | Signatory authority |
| `{{.CompanyHead_PassportIssueDateText}}` | `string` | Signatory passport date |
| `{{.CompanyHead_PassportLine}}` | `string` | Combined line |

### Application Reference

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.Application_FullNumber}}` | `string` | Full application number |
| `{{.Application_VisaPeriod_NameTm}}` | `string` | Visa period (Turkmen) |
| `{{.Application_VisaCategory_NameTm}}` | `string` | Visa category (Turkmen) |
| `{{.Application_BorderZoneLocation_NameTm}}` | `string` | Border zone location |
| `{{.Application_DateText}}` | `string` | Application date dd.MM.yyyy |
| `{{.Application_SponsorName}}` | `string` | Sponsor company name |
| `{{.Application_SponsorSignatory}}` | `string` | Sponsor authorized signatory |

---

## Registration — Row Placeholders (`{{.*}}`)

Use for check-in/check-out registration reports.

### Person Information

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.RowNumber}}` | `int` | Row number |
| `{{.Person_FullName}}` | `string` | Full name |
| `{{.Person_LastName}}` | `string` | Last name |
| `{{.Person_FirstName}}` | `string` | First name |
| `{{.Person_MiddleName}}` | `string` | Middle name |
| `{{.Person_BirthPlace}}` | `string` | Birth place |
| `{{.Person_DateOfBirth}}` | `DateTime?` | Date of birth |
| `{{.Person_DateOfBirthText}}` | `string` | DOB dd.MM.yyyy |
| `{{.Person_GenderTm}}` | `string` | Gender (Turkmen) |
| `{{.Person_MaritalStatusTm}}` | `string` | Marital status (Turkmen) |
| `{{.Person_NationalityCode}}` | `string` | Nationality code |
| `{{.Person_NationalityTm}}` | `string` | Nationality (Turkmen) |
| `{{.Person_CountryOfBirthCode}}` | `string` | Country of birth code |
| `{{.Person_CountryOfBirthTm}}` | `string` | Country of birth (Turkmen) |
| `{{.Person_ForeignAddress}}` | `string` | Foreign address |
| `{{.Person_ForeignAddressCountryCode}}` | `string` | Foreign address country code |
| `{{.Person_ForeignAddressCountryTm}}` | `string` | Foreign address country (Turkmen) |
| `{{.Person_IsEmployee}}` | `bool` | Is employee flag |
| `{{.Person_RelationshipTm}}` | `string` | Relationship (Turkmen) |
| `{{.Person_SponsoringEmployeeFullName}}` | `string` | Sponsoring employee name |
| `{{.Person_SponsoringEmployeePositionTm}}` | `string` | Sponsoring employee position |
| `{{.Person_Photo}}` | `byte[]` | Photo |

### Passport

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.Passport_Number}}` | `string` | Passport number |
| `{{.Passport_IssueDateText}}` | `string` | Issue date dd.MM.yyyy |
| `{{.Passport_ExpirationDate}}` | `DateTime?` | Expiration date |
| `{{.Passport_ExpirationDateText}}` | `string` | Expiration dd.MM.yyyy |
| `{{.Passport_CountryCode}}` | `string` | Country code |
| `{{.Passport_CountryTm}}` | `string` | Country (Turkmen) |

### Visa

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.Visa_Number}}` | `string` | Visa number |
| `{{.Visa_IssueDate}}` | `DateTime?` | Issue date |
| `{{.Visa_IssueDateText}}` | `string` | Issue dd.MM.yyyy |
| `{{.Visa_StartDate}}` | `DateTime?` | Start date |
| `{{.Visa_StartDateText}}` | `string` | Start dd.MM.yyyy |
| `{{.Visa_ExpirationDate}}` | `DateTime?` | Expiration date |
| `{{.Visa_ExpirationDateText}}` | `string` | Expiration dd.MM.yyyy |
| `{{.Visa_IssuedPlaceTm}}` | `string` | Issued place (Turkmen) |
| `{{.Visa_CategoryTm}}` | `string` | Visa category (Turkmen) |
| `{{.Visa_TypeTm}}` | `string` | Visa type (Turkmen) |

### Travel / Movement

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.Travel_Date}}` | `DateTime?` | Travel date |
| `{{.Travel_DateText}}` | `string` | Travel date dd.MM.yyyy |
| `{{.Travel_PurposeOfTravelTm}}` | `string` | Purpose (Turkmen) |
| `{{.Travel_CheckPointTm}}` | `string` | Checkpoint (Turkmen) |

### Address

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.Address_FullAddress}}` | `string` | Full address |
| `{{.Address_RegionTm}}` | `string` | Region (Turkmen) |
| `{{.Address_CityTm}}` | `string` | City (Turkmen) |

### Position

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.Position_PositionTm}}` | `string` | Position (Turkmen) |
| `{{.Position_DepartmentTm}}` | `string` | Department (Turkmen) |

### Application Reference

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.Application_FullNumber}}` | `string` | Full application number |
| `{{.Application_MigrationServiceCode}}` | `string` | Migration service code |
| `{{.Application_DateText}}` | `string` | Application date dd.MM.yyyy |
| `{{.Application_RegistrationDateText}}` | `string` | Registration date dd.MM.yyyy |

### Signatory

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.CompanyHead_PositionTm}}` | `string` | Signatory position (Turkmen) |
| `{{.CompanyHead_FullName}}` | `string` | Signatory full name |

---

## BusinessTrip — Row Placeholders (`{{.*}}`)

Use for business trip sanawy and letters.

### Person Information

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.Person_LastName}}` | `string` | Last name |
| `{{.Person_FirstName}}` | `string` | First name |
| `{{.Person_DateOfBirthText}}` | `string` | DOB dd.MM.yyyy |
| `{{.Person_BirthPlace}}` | `string` | Birth place |
| `{{.Person_GenderTm}}` | `string` | Gender (Turkmen) |
| `{{.Person_NationalityCode}}` | `string` | Nationality code |

### Passport

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.Passport_Number}}` | `string` | Passport number |
| `{{.Passport_ExpirationDateText}}` | `string` | Expiration dd.MM.yyyy |

### Position

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.Position_NameTm}}` | `string` | Position (Turkmen) |

### Visa

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.Visa_NumberAndType}}` | `string` | Visa number + category |
| `{{.Visa_StartDateText}}` | `string` | Start dd.MM.yyyy |
| `{{.Visa_ExpirationDateText}}` | `string` | Expiration dd.MM.yyyy |

### Address

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.Address_FullAddress}}` | `string` | Current address |
| `{{.BusinessTripAddress_FullAddress}}` | `string` | Business trip address |

### Signatory (Application Level)

| Placeholder | Type | Description |
|-------------|------|-------------|
| `{{.Application_CompanyHead_FullName}}` | `string` | Company head name |
| `{{.Application_CompanyHead_PositionTm}}` | `string` | Company head position |

---

## Common Patterns

### Simple Letter Header (L1/L2 Family)

```
{{ds.FullApplicationNumber}}
{{ds.ApplicationDateText}}
{{ds.ProjectContract_Ministry_RecipientBlock}}

Hormatly {{ds.ProjectContract_Ministry_FormOfAddress}}!
```

### Signatory Block

```
{{ds.Application_CompanyHead_PositionTm}}              {{ds.Application_CompanyHead_FullName}}
```

### Table with Row Numbers

```
{{#ds.rows}}
{{.RowNumber}}. {{.Person_LastName}} {{.Person_FirstName[0]}}. {{.Passport_Number}}
{{/ds.rows}}
```

### Conditional Urgency

```
{?{ds.ApplicationType_ShowUrgency}}Artykmaç çalt okatmak üçin — {{ds.Urgency_NameTm}}{{/}}
```

---

## Source Files

| Business Object | File Path |
|-----------------|-----------|
| `Application` | `Visa2026.Module/BusinessObjects/Application.cs` |
| `ApplicationItem` | `Visa2026.Module/BusinessObjects/ApplicationItem.cs` |
| `Registration` | `Visa2026.Module/BusinessObjects/Registration.cs` |
| `BusinessTrip` | `Visa2026.Module/BusinessObjects/BusinessTrip.cs` |

## Related Documentation

- `docs/WORD_REPORT_GENERATION_IDEA.md` — Full architecture and workflow
- `.cursor/skills/visa2026-word-reports/SKILL.md` — Skill documentation
- `.cursor/skills/visa2026-word-reports/reference.md` — Layout families and typography
