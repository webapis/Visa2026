# Import property maps — Mermaid sources

VISA2015 (`10.100.128.15` / `VISA2015`) → Visa2026 business-object field maps.

Canonical mapping: [`Visa2026.DataImporter/legacy/visa2014/field-maps/`](../../../Visa2026.DataImporter/legacy/visa2014/field-maps/) · layer 1 [`table-mappings.yaml`](../../VISA2014_MIGRATION/table-mappings.yaml) · skill [visa2014-to-visa2026-import](../../../.cursor/skills/visa2014-to-visa2026-import/SKILL.md).

| File | Purpose |
|------|---------|
| `person-property-map.mmd` | Legacy Person (+ Passport / lookup joins) → `Person.cs` properties |
| `person-gaps-and-defaults.mmd` | Relocated, dropped, and Visa2026-only Person members |
| `passport-property-map.mmd` | Legacy Passport (+ type / issued-country joins) → `Passport.cs` properties |
| `passport-gaps-and-defaults.mmd` | PersonalNumber/Citizenship relocated to Person, number dedupe, PassportCopy file wave |
| `visa-property-map.mmd` | Legacy Visa (+ type / category / issued-place / border-zone / Passport) → `Visa.cs` properties |
| `visa-gaps-and-defaults.mmd` | ASNumber vs PIA ProcessNumber, issuing-app corrections, GöçürmeNusga file wave |
| `education-property-map.mmd` | Legacy Education (+ level / institution / specialty / country joins) → `Education.cs` properties |
| `education-gaps-and-defaults.mmd` | Dropped dates, catalog seed (not lookup-table import), file wave, Visa2026-only Education members |
| `employee-position-history-property-map.mmd` | Legacy `WorkHistoryOfEmployee` (+ Position / Department / Person.MiddleName) → `EmployeePositionHistory.cs` |
| `employee-position-history-gaps-and-defaults.mmd` | Dropped Company/Imp* flags, derived EndDate, ActualPosition find-or-create, permit-supplement |
| `employee-salary-property-map.mmd` | Legacy `Employee` + `Salary.Detail` + current work-history start → `EmployeeSalary.cs` |
| `employee-salary-gaps-and-defaults.mmd` | Dropped Employee flags, unparseable amount skips, fixed USD, current-snapshot EndDate |
| `address-of-residence-property-map.mmd` | Legacy AOR + Address + Region/SaherEtrap/DocumentOfAddress → `AddressOfResidence.cs` |
| `address-of-residence-gaps-and-defaults.mmd` | Dropped StartDate, site catalog seed, PIA inference sub-pass, file wave TBD |
| `application-profile-property-map.mmd` | Legacy ApplicationType DISTINCT → tenant JSON → `ApplicationProfile.cs` (templates) |
| `application-profile-gaps-and-defaults.mmd` | Not an instance wave; nested Word files and approval-leg versions are separate catalogs |
| `application-profile-instance-property-map.mmd` | Legacy `dbo.Application` + `IRegistration_Data` → `ApplicationProfileInstance.cs` |
| `application-profile-instance-gaps-and-defaults.mmd` | Header-only scope, child BOs later, E:33/E:55 skip, tenant catalogs first |
| `application-profile-instance-app-inv-property-map.mmd` | `App_Inv` only (`E:0:0:na:na`) → instance header; `--application-type App_Inv` |
| `application-profile-instance-app-inv-gaps-and-defaults.mmd` | Not `App_Inv_And_WP` / `App_Inv_FM`; no WP; invitation/roster/progress later |

Preview: open a `.mmd` in Cursor with a Mermaid preview, or paste into [mermaid.live](https://mermaid.live).

**Do not** treat `dbo.Person.IDNumber` as `Person.PersonalNumber` — that column is employer/subcontractor text. Personal number comes from the canonical `dbo.Passport` row.

**Passport:** do **not** collapse to one document per Person. `PassportIssuedPlace` → `Authority`. Citizenship is not on Visa2026 Passport (Person.Nationality). `Passport.PersonalNumber` is a hidden copy — Person wave owns the PN. Scans are `dbo.PassportCopy` in a later file wave (`PassportDocuments` still empty on this local run).

**Visa:** do **not** treat legacy `Visa.ProcessNumber` as the stamp process string — that Guid is `LegacyPersonInApplicationProfileInstanceOid`. Stamp **Işlenen belgisi** is `ASNumber` → `Visa.ProcessNumber`. Issuing application / invitation FKs stay null until later `--correct-visa2014-*` passes. Scans are inline `GöçürmeNusga` (`VisaDocument` still empty on this local run).

**Education:** do **not** import `dbo.EducationInstitution` / `dbo.Speciality` as BOs — seed `NameTm` from live `.15` DISTINCT. The legacy specialty FK column is `Education.Spcialty` (typo) → `dbo.Speciality`. Diploma scans are on `dbo.PassportCopy` (Education FK), not on `dbo.Education`.

**EmployeePositionHistory:** legacy table is `dbo.WorkHistoryOfEmployee`; the person FK is `Employee`, not `Person`. Do **not** copy `Person.MiddleName` onto `Person.MiddleName` in Visa2026 — it is the company job title and maps to `ActualPosition` on the current/latest history row. `EndDate` is derived (next `StartDateOnThisPosition` per person). Position/Department catalogs are seeded, not imported as tables.

**EmployeeSalary:** one row per `dbo.Employee` (same Oid as Person). Amount comes from free-text `Salary.Detail` (normalized); Currency is fixed **USD**. StartDate is MAX work-history start, not a salary table date. Skip empty/unparseable Detail — that is transform skip, not FailedCount.

**AddressOfResidence:** geography lives on `dbo.Address`, not on the AOR header. City table is `dbo.SaherEtrap` (Unicode S-cedilla). `DocumentOfAddress` is a type discriminator (Lojman/Patent/myhmanhana), not a scan. Lodging/Hotel/Hospital are catalog lookups from AddressLine. PIA inference runs automatically at the end of this wave.

**ApplicationProfile (templates):** seed **before** ApplicationProfileInstance. Path is Wave 0 Excel sign-off → Wave 1 `application-profile.calik-energi.json` → `ApplicationProfileTenantCatalogSeedUpdater` — **not** `--import-visa2014 --entity ApplicationProfile`. Officer UI “profile templates” = `ApplicationProfile` rows. Nested Word/Excel files are `ApplicationProfileTemplate` (separate generate script). Instance import sets `ApplicationProfile` via `ResolveApplicationProfile(ApplicationType, ProjectContract)`.

**ApplicationProfileInstance:** legacy table is **`dbo.Application`**, not a table named ApplicationProfileInstance. Numbering is on `IRegistration_Data`. Upsert on legacy **Oid**, never merge duplicate `ManualApplicationNumber` groups. PersonInApplication / Invitation / WorkPermit / Progress are later waves. Run tenant catalog generation before this header import. `IsManualEntry` is always true so auto-numbering does not rewrite legacy numbers. Requires **ApplicationProfile** rows already in the target DB.

**App_Inv (first type-filtered instance wave):** composite `E:0:0:na:na` only (employee invitation, `InvitationAndWorkPermitRequired = 0`). Do **not** include `App_Inv_And_WP` (`E:0:1`) or `App_Inv_FM` (`F:0`). CLI `--application-type App_Inv` filters after transform. `VisaType` infers **BS1**. Profile Code is `get_invitation`. This wave is header-only; Invitation / roster / progress stay later and use the App_Inv id-map.
