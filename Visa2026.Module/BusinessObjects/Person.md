# Business Object: Person

## 1. Purpose

`Person` is the **single concrete** record for everyone in the system: **employees** (`IsEmployee = true`) and **family members** (`IsEmployee = false`). One table (`People`), one detail view pattern, with **Appearance** rules that show or hide employee-only vs family-only fields.

There are **no** separate `Employee` / `FamilyMember` business object types in the current model (legacy inheritance was consolidated).

Each person holds **identity** data, **project contract** scope, **document histories** (passports, visas on passports, work permits, etc.), and links to [`ApplicationItem`](ApplicationItem.md) lines on applications.

**Registration** is **not** stored on `Person`; check-in/out and travel fields live on [`ApplicationItem`](ApplicationItem.md) when `ApplicationType.ShowRegistrations` is true.

---

## 2. Inheritance and interfaces

- Base: `BaseObject`
- `ISoftDelete` — soft delete
- `IOptionalDetailFields` — gear toggle (`ShowOptionalFields`) for optional child collections (documents, images, etc.)

---

## 3. Identity and classification

| Property | Type | Notes |
|----------|------|--------|
| `FirstName`, `LastName`, `MiddleName` | `string` | Required when active (`!IsDeleted`). |
| `FullName` | `string` | `[NotMapped]` computed; default display property. |
| `PersonalNumber` | `string` | **Canonical** national/civil ID (not per-passport). Unique among active persons except sentinel `0`. |
| `DateOfBirth` | `DateTime` | Required when active. `ImmediatePostData`; drives `Age` and child marital status default. |
| `Age` | `int` | `[NotMapped]` read-only. |
| `BirthPlace` | `string` | Required when active. |
| `CountryOfBirth` | `Country` | Required when active. |
| `Gender` | `Gender` | Lookup; required when active. |
| `MaritalStatus` | `MaritalStatus` | Auto-set to child status when under 18. |
| `Nationality` | `Country` | Required when active. |
| `ForeignAddress`, `ForeignAddressCountry` | `string`, `Country` | Required for **employees** when active. |
| `ProjectContract` | `ProjectContract` | Required when active; scopes person to a project. |
| `Photo` | `byte[]` | Cropped/resized on save for mail-merge dimensions. |
| `IsArchived` | `bool` | Optional archive flag. |
| `IsEmployee` | `bool` | **Discriminator:** employee vs family member. Not user-editable in UI (`Browsable(false)`). |
| `Subcontractor` | `Subcontractor` | Required when active (caption **Company (Subcontractor)**). Replaces legacy `IsSubcontractorEmployee`. |

### Employee-only (`Appearance` when `!IsEmployee`)

| Property | Notes |
|----------|--------|
| `Email` | Format validation. |
| `HireDate` | |
| `WorkPermitItems`, `FamilyMembers`, `PositionHistory`, `EmployeeContracts`, `Salaries`, `WorkDuties` | Collections (see below). |
| `VisaApplicationFamilyMembersText` | Manual visa PDF family block; custom editor. See [`docs/VISA_FAMILY_MEMBERS_TEXT_EDITOR.md`](../../docs/VISA_FAMILY_MEMBERS_TEXT_EDITOR.md). |

### Family-member-only (`Appearance` when `IsEmployee`)

| Property | Notes |
|----------|--------|
| `SponsoringEmployee` | `Person` with `IsEmployee = true`. |
| `Relationship` | Required on save when `RequiresRelationshipOnSave` (exemptions for manual visa text on sponsor — see `Person.cs`). |

---

## 4. Effective “current” documents — `PersonCurrentItems`

**Persisted `Person.Current*` FK columns were removed.** Code that needs “the person's current passport / visa / work permit / …” must use [`PersonCurrentItems`](PersonCurrentItems.cs):

| Resolver | Rule (summary) |
|----------|----------------|
| `GetCurrentPassport` | Latest passport by `IssueDate` (non-deleted). |
| `GetCurrentVisa(person, asOf?)` | Visas on all passports; **effective on** `asOf` (default today); not cancelled; latest by start/issue date. |
| `GetCurrentEducation` | Highest `GraduationYear` (parsed as int). |
| `GetCurrentMedicalRecord` | Latest by issue date. |
| `GetCurrentAddressOfResidence` | Open period by dates; **undated** lodging rows still resolve. |
| `GetCurrentInvitationItem` / `GetCurrentRejectionItem` | Latest by parent document date. |
| `GetCurrentWorkPermitItem` | Latest by `StartDate`. |
| `GetCurrentPositionHistory` / `GetCurrentEmployeeContract` / `GetCurrentSalary` | Open period or latest start. |
| `GetCurrentWorkDuty` | Latest work duty row. |

`PersonCurrentItems.ResolveFromSource` supports sync/PDF paths that still reference property names like `CurrentPassport` on `ApplicationItem` or `WorkPermitItem`.

[`ApplicationItem.ApplyCurrentFieldsFromSelectedPerson`](ApplicationItem.cs) copies these effective values onto application lines (respecting `ApplicationType.Show*` flags).

**Visas:** there is **no** `Person.Visas` collection. Visas belong to [`Passport`](Passport.cs); use `Passports` → `Visa` children.

---

## 5. Collections

| Collection | Item type | Notes |
|------------|-----------|--------|
| `Educations` | `Education` | Aggregated. |
| `Passports` | `Passport` | Aggregated; visas nested on passport. |
| `MedicalRecords` | `MedicalRecord` | Aggregated. |
| `AddressesOfResidence` | `AddressOfResidence` | Aggregated. |
| `Documents` | `PersonDocument` | Optional supporting files. |
| `Images` | `FamilyMemberImage` | Optional; often hidden until gear toggle. |
| `WorkPermitItems` | `WorkPermitItem` | Aggregated; read-only list on person. |
| `FamilyMembers` | `Person` | Employee's dependents (`SponsoringEmployee` inverse). |
| `PositionHistory` | `EmployeePositionHistory` | Employee careers. |
| `EmployeeContracts` | `EmployeeContract` | |
| `Salaries` | `EmployeeSalary` | |
| `WorkDuties` | `WorkDuty` | Visit purpose / work duty descriptions. |
| `InvitationItems` | `InvitationItem` | Read-only on person detail. |
| `RejectionItems` | `RejectionItem` | Read-only on person detail. |
| `TravelHistories` | `TravelHistory` | Travel log (distinct from registration on `ApplicationItem`). |
| `ApplicationItems` | `ApplicationItem` | Read-only; lines on applications referencing this person. |

**Removed:** `Registrations` collection and `Registration` entity. Use registration fields on **`ApplicationItem`**.

---

## 6. Business rules

- **`PersonalNumber`:** trimmed on save; uniqueness rule `Person_PersonalNumberUniqueAmongActive` (case-insensitive; `0` allowed for multiple “unknown” IDs).
- **`DateOfBirth`:** under 18 → default marital status **Çaga**; clearing when adult.
- **`OnCreated`:** default nationality, country of birth, foreign address country, gender, marital status from lookups.
- **`OnSaving` (employee):** default `VisaApplicationFamilyMembersText` via `VisaFamilyMemberLinesHelper` when empty.
- **`OnSaving`:** `Photo` processed to 3:4 passport ratio for templates.
- **Family vs manual visa text:** if sponsor has manual `VisaApplicationFamilyMembersText` and no sibling family row with `Relationship`, family member may be exempt from relationship requirement (`IsExemptFromRelationshipWhenManualVisaFamily`).
- **Soft delete:** `IsDeleted` / `DateDeleted` / `DeletedBy`; inactive persons skip most required-field criteria.

---

## 7. UI and navigation

- **Navigation:** `Lookup/Person` (concrete list/detail for all persons).
- **Employee vs family:** same `Person` DetailView; fields toggled by `IsEmployee` (set at create/import, not flipped casually in UI).
- **Optional collections:** `ShowOptionalFields` gear reveals low-priority tabs (documents, images, etc.) — `IOptionalDetailFields`.
- **Importer:** `Persons` sheet uses `Person` entity; upsert often by `Email` (employees). See `Visa2026.DataImporter/SCENARIO_GUIDE.md`.

---

## 8. Related docs

| Topic | Location |
|-------|----------|
| Application line items | [`ApplicationItem.md`](ApplicationItem.md) |
| Application header | [`APPLICATION.md`](APPLICATION.md) |
| Manual visa family lines | [`docs/VISA_FAMILY_MEMBERS_TEXT_EDITOR.md`](../../docs/VISA_FAMILY_MEMBERS_TEXT_EDITOR.md) |
| Legacy removals (`Company` on person, etc.) | [`docs/DEPRECATED.md`](../../docs/DEPRECATED.md) |
| Passport `PersonalNumber` | Prefer **`Person.PersonalNumber`** (passport field legacy) |
