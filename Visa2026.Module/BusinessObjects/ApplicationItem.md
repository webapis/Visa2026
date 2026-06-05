# Business Object: ApplicationItem

## 1. Purpose

`ApplicationItem` is a **per-person line** on an [`Application`](APPLICATION.md). Each row links one [`Person`](Person.md) (employee or family member) to the parent application and holds **context-specific** documents, travel/registration data, status flags, and report placeholders for that person.

There is **no** separate `Registration` business object and **no** `Employee` / `FamilyMember` properties on the line — only `Person`, filtered by [`ApplicationType.Category`](LookupBusinessObjects.cs) on the parent application.

---

## 2. Core persisted properties

| Property | Type | Notes |
|----------|------|--------|
| `Application` | `Application` | Required parent. Changing it runs registration defaults and visibility cleanup. |
| `Person` | `Person` | Required. `ImmediatePostData`. Data source: `AvailablePeople` (contract + type category). |
| `ApplicationItemName` | `string` | Read-only display key (`Person` + `Application.FullApplicationNumber`). |
| `CurrentPassport` | `Passport` | Required. Data source: person's passports. |
| `CurrentPositionHistory` | `EmployeePositionHistory` | Employee lines only (hidden for family members); populated from person when applicable. |
| `Registration_GelmeginMaksadyTm` | `string` (computed) | Read-only on detail when `ShowRegistrations` and person is a family member; report text for **Gelmeginiň maksady** (sponsor position–name–relationship). |

### Registration / travel (flattened on the line)

Former `Registration` child data lives on `ApplicationItem` when `ApplicationType.ShowRegistrations` is true:

| Property | Type | UI gate |
|----------|------|---------|
| `TravelDate` | `DateTime?` | `ShowRegistrations` |
| `TravelType` | `TravelType?` | `ShowRegistrations` |
| `MovementType` | `MovementType?` | `ShowRegistrations` |
| `CheckPoint` | `CheckPoint` | `ShowRegistrations` + external travel |
| `TravelNotes` | `string` | `ShowRegistrations` (optional; synced to `TravelHistory.Notes`) |
| `RegistrationDate` | `DateTime?` | `ShowRegistrations` |

`ApplyRegistrationMovementDefaults` sets travel type/movement and defaults for `App_Reg_*` types (check-in/out, internal/external).

### Business trip (per line)

| Property | Type | UI gate |
|----------|------|---------|
| `BusinessTripAddress` | `BusinessTripAddress` | `ShowBusinessTrips` (aggregated) |

Application-level business-trip dates live on `Application` (`BusinessTripStartDate`, etc.), not on a `BusinessTrips` collection.

### Comma-separated catalog fields (custom Blazor editor)

| Property | Catalog | Editor | UI gate |
|----------|---------|--------|---------|
| `BorderZoneLocation` | `BorderZoneName` | `BorderZoneMultiSelect` | `Application.ApplicationType.ShowBorderZoneLocation` (hidden on registration and other types where the catalog flag is false; default `Ýok` on create). See [`docs/COMMA_SEPARATED_MULTI_SELECT.md`](../../docs/COMMA_SEPARATED_MULTI_SELECT.md). |
| `WorkPermittedLocations` | `WorkPermittedLocationName` | `WorkPermittedLocationMultiSelect` | `ShowWorkPermittedLocations` |

`BorderZoneLocation_NameTm` is a `[NotMapped]` report alias. `Application.BorderZoneLocation` (FK lookup on the **application**) is a **different** field.

### Document links (gated by `ApplicationType.Show*` on the line)

| Property | Show* flag |
|----------|------------|
| `PreviousPassport` | `ShowPreviousPassport` |
| `CurrentVisa` / `CurrentVisaId` | `ShowCurrentVisa` |
| `NextVisa` / `NextVisaId` | `ShowNextVisa` |
| `CurrentWorkPermitItem` | `ShowCurrentWorkPermitItem` |
| `PreviousWorkPermitItem` | `ShowPreviousWorkPermitItem` |
| `CurrentInvitationItem` | `ShowCurrentInvitationItem` |
| `PreviousInvitationItem` | `ShowPreviousInvitationItem` |
| `CurrentAddressOfResidence` | `ShowCurrentAddressOfResidence` |
| `CurrentEmployeeContract` | `ShowCurrentEmployeeContract` |
| `CurrentWorkDuty` | `ShowCurrentWorkDuty` |
| `CurrentSalary` | `ShowCurrentSalary` |
| `CurrentMedicalRecord` | `ShowCurrentMedicalRecord` |
| `CurrentEducation` | `CurrentEducation` → `ShowCurrentEducation` |

Catalog flags are defined on [`ApplicationType`](LookupBusinessObjects.cs) and seeded from `DatabaseUpdate/LookupCatalogs/ApplicationTypeConfigurationCatalog.json` (deploy sync via `ApplicationTypeConfigurationUpdater`).

**Examples (not exhaustive):** `ShowCurrentSalary` is enabled for `App_Inv_According_to_WP`, `App_Inv_And_WP`, `App_Visa_Ext_According_to_WP`, `App_Visa_and_WP_Ext`, `App_WP_Ext`, and `App_Additional_WP_location`. `ShowWorkPermittedLocations` and `ShowCurrentWorkDuty` are enabled for types such as `App_Inv_And_WP`, `App_Visa_and_WP_Ext`, and `App_WP_Ext`; see the catalog for the full matrix.

### Workflow status columns (list/detail, gated)

| Property | Show* flag |
|----------|------------|
| `InvitationItemIsIssued` | `ShowInvitationItemIsIssued` |
| `WorkPermitItemIsIssued` | `ShowWorkPermitItemIsIssued` |
| `RejectionIssued` | `ShowRejectionIssued` |
| `VisaIssued` | `ShowVisaIssued` |
| `InvitationItemIsCancelled` | `ShowInvitationItemIsCancelled` |
| `IsCancelled` (work permit item) | `ShowWorkPermitItemIsCancelled` |
| `InvitationItemIsChanged` | `ShowInvitationItemIsChanged` |
| `WorkPermitItemIsChanged` | `ShowWorkPermitItemIsChanged` |
| `VisaIsCancelled` | `ShowVisaIsCancelled` |
| `VisaIsChanged` | `ShowVisaIsChanged` |

### Soft delete

`IsDeleted`, `DateDeleted`, `DeletedBy` — `ISoftDelete`.

---

## 3. Person selection and “current” document resolution

### `AvailablePeople`

- Filtered by parent `Application.ProjectContract` when set.
- Filtered by `Application.ApplicationType.Category`: `Employee` → employees only; `FamilyMember` → family members only; `Both` / null → all persons.

### `ApplyCurrentFieldsFromSelectedPerson`

When `Person` changes, the line copies **effective** child records using [`PersonCurrentItems`](PersonCurrentItems.cs) (date-effective visa selection uses `Application.ApplicationDate`):

- Always: passport, address, medical, education, invitation item.
- **Visas:** `CurrentVisa` / `NextVisa` only if the type's `ShowCurrentVisa` / `ShowNextVisa` is true.
- **Employees:** position history, contract, work duty, work permit item; if `ShowWorkPermittedLocations`, copies `WorkPermittedLocations` from the person's current work permit item.
- **Family members:** employee-only links cleared.

This mirrors intent of legacy sync rules but does **not** depend on `SyncRule` read permissions.

### `CurrentWorkPermitItem` setter

When a work permit item is chosen and `ShowWorkPermittedLocations` is true, `WorkPermittedLocations` is copied from that item.

### `ApplyVisibilityGatedReferenceFields`

Clears `CurrentVisa`, `NextVisa`, and `WorkPermittedLocations` when the parent type's flags turn them off (e.g. after application type change).

---

## 4. Visa linkage (issuing line vs target visa)

- **`Visa.IssuingApplicationItem`**: From a **visa** back to the **application line under which that visa was issued** (person + parent application). Validated against allowed issuing application types.
- **`ApplicationItem.CurrentVisa`**: From a **line** to the **visa used as input** for this procedure (extension, cancellation, registration context, etc.). Inverse: `Visa.AssociatedApplicationItems`.
- **`ApplicationItem.NextVisa`**: Optional **future** visa on the line when `ShowNextVisa` is true.

Do not confuse **issuing** (output of a prior procedure) with **target** (input/context for this procedure). See **`Visa.md`**.

`WorkPermit_WorkPermittedLocations` (report): prefers `WorkPermittedLocations` on the line, else `CurrentWorkPermitItem.WorkPermittedLocations`.

---

## 5. Report and PDF placeholders (`[NotMapped]`)

`ApplicationItem.cs` defines a large **report alias** region (`Person_*`, `Passport_*`, `Visa_*`, `WorkPermit_*`, ministry blocks, family-member PDF text, etc.). These are hidden from normal detail UI and used by XtraReports, PDF form filling, and Word/user reports.

- PDF visibility gates: `PdfMappingHelper` + `ApplicationType.Show*` (and `Person.IsEmployee` where applicable). See `Services/PDF-Form-Filling.md`.
- Word placeholders: `docs/WORD_REPORT_GENERATION_IDEA.md`, `docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md`.

---

## 6. Validation and save

### Why some registration fields are “optional” in the UI but required on save

Project-wide rule: see **`docs/OPTIONAL_DETAIL_FIELDS.md`** (design principle — user picks at create vs logic-filled / edit later).

Registration/travel members are **not** meant to be filled when the user first creates an `ApplicationItem` on the detail form. **`ApplyRegistrationMovementDefaults`**, sync rules, and related business logic set them when the line is created or when `Person` / `Application` changes (e.g. `TravelDate = Today`, default `CheckPoint`, `TravelType` / `MovementType` from application type).

The **gear** hides optional registration fields (`RegistrationDate`, `TravelType`, `MovementType`, `TravelNotes`) on first open so the form stays focused on person and document links; officers **edit that data later**. **`TravelDate`** and **`CheckPoint`** stay **always visible** on registration types (Travel group) because they are required in the UI — `[ExcludeFromOptionalDetailFields]`, not gear-scoped. **`[RuleRequiredField]`** on `TravelDate` and external `CheckPoint` still applies on **save**; defaults from `ApplyRegistrationMovementDefaults` populate them when the line is created.

Document links on the main tab follow the opposite pattern for many application types: **required when the type shows the field**, because the user must pick the correct passport/visa/work-permit context when adding the line.

- **Always required:** `Application`, `Person`, `CurrentPassport`.
- **Required when visible (application type `Show*` flags):** document links (`PreviousPassport`, `CurrentVisa`, `NextVisa`, work permit and invitation items, address, medical, education), `BorderZoneLocation`, `WorkPermittedLocations` — each uses `[RuleRequiredField(TargetCriteria = …)]` matching the field's `[Appearance]` hide rule.
- **Registration / travel:** **`TravelDate`** and **`CheckPoint`** are required when shown (`ShowRegistrations`; external for `CheckPoint`); always visible in the Travel group — `[ExcludeFromOptionalDetailFields]`, defaults in `ApplyRegistrationMovementDefaults`. **Gear:** `RegistrationDate`, `TravelType`, `MovementType`, `TravelNotes` are optional on save and hidden when the gear is off. Travel purpose on registration lines is **`CurrentPositionHistory`** (not a separate purpose-of-travel lookup). Application-type `[Appearance]` still gates the registration block (`ShowRegistrations`). `BusinessTripAddress` and workflow status columns use `[ExcludeFromOptionalDetailFields]`.
- **Employee-only lines:** `CurrentPositionHistory`, `CurrentSalary`, `CurrentWorkPermitItem`, `CurrentWorkDuty` are required only when `Person.IsEmployee` and the field is shown (hidden for family members via `PersonIsFamilyMemberCriteria`).
- **Education on registration:** `CurrentEducation` is hidden and not required on all registration application lines (`RegistrationApplicationItemContextCriteria`), including employees — `ShowCurrentEducation` in the catalog does not apply there.
- **Business trip:** when `ShowBusinessTrips`, `IsBusinessTripAddressValid` requires `BusinessTripAddress.City` and non-empty `FullAddress`.
- **Unique person per application:** `IsPersonUniqueInApplication`.
- **`OnSaving`:** updates `ApplicationItemName`; border zone default `Ýok` on create when empty; syncs linked **`TravelHistory`** for check-in/out registration types (see **`docs/REGISTRATION_TRAVEL_HISTORY_SYNC.md`**).
- **`CrossObjectSyncHelper`:** property-change sync for visa, invitation, work permit links (where configured).

---

## 7. UI notes

- **Navigation:** Application group (nested under `Application` detail).
- **Employee vs family member on the line:** `CurrentPositionHistory`, `CurrentSalary`, `CurrentWorkDuty`, and `CurrentWorkPermitItem` are hidden when `Person.IsEmployee` is false (FKs stay null; see `ApplyCurrentFieldsFromSelectedPerson`). On registration applications, `CurrentEducation` is hidden for everyone (employees and family). Family members on registration detail show read-only **`Registration_GelmeginMaksadyTm`** instead of an empty position lookup.
- **Appearance:** Most document and status fields use `[Appearance(..., Criteria = "!Application.ApplicationType.Show…")]`.
- **Detail layout:** `Model.xafml` (Blazor Server) — includes `WorkPermittedLocations` next to other document fields.
- **Border zone on item detail:** gated by `ShowBorderZoneLocation` (same as application header). `ApplicationItemDetailViewBorderZoneController` hides duplicate layout nodes for `Application.BorderZoneLocation` vs item `BorderZoneLocation`.

---

## 8. Related docs

| Topic | Doc |
|-------|-----|
| Parent application | [`APPLICATION.md`](APPLICATION.md) |
| Application type flags | `ApplicationTypeConfigurationCatalog.json`, [`docs/LOOKUP_SEEDING.md`](../../docs/LOOKUP_SEEDING.md) |
| Comma-separated editors | [`docs/COMMA_SEPARATED_MULTI_SELECT.md`](../../docs/COMMA_SEPARATED_MULTI_SELECT.md) |
| Deprecated `Registration` BO | [`docs/DEPRECATED.md`](../../docs/DEPRECATED.md) |
| Person “current” resolution | [`PersonCurrentItems.cs`](PersonCurrentItems.cs) |
| DataImporter columns | `Visa2026.DataImporter/SCENARIO_GUIDE.md` (`Work Permitted Locations`, registration fields on **ApplicationItems**) |
