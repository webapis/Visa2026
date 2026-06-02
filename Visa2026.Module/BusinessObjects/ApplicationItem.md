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
| `CurrentPositionHistory` | `EmployeePositionHistory` | Employee lines; populated from person when applicable. |

### Registration / travel (flattened on the line)

Former `Registration` child data lives on `ApplicationItem` when `ApplicationType.ShowRegistrations` is true:

| Property | Type | UI gate |
|----------|------|---------|
| `TravelDate` | `DateTime?` | `ShowRegistrations` |
| `TravelType` | `TravelType?` | `ShowRegistrations` |
| `MovementType` | `MovementType?` | `ShowRegistrations` |
| `CheckPoint` | `CheckPoint` | `ShowRegistrations` + external travel |
| `PurposeOfTravel` | `PurposeOfTravel` | `ShowRegistrations` |
| `TravelNotes` | `string` | `ShowRegistrations` |
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
| `BorderZoneLocation` | `BorderZoneName` | `BorderZoneMultiSelect` | Always on item detail (not gated by `Show*`); default `Ýok` on create. See [`docs/COMMA_SEPARATED_MULTI_SELECT.md`](../../docs/COMMA_SEPARATED_MULTI_SELECT.md). |
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
| `CurrentMedicalRecord` | `ShowCurrentMedicalRecord` |
| `CurrentEducation` | `CurrentEducation` → `ShowCurrentEducation` |

Catalog flags are defined on [`ApplicationType`](LookupBusinessObjects.cs) and seeded from `DatabaseUpdate/LookupCatalogs/ApplicationTypeConfigurationCatalog.json` (deploy sync via `ApplicationTypeConfigurationUpdater`).

**Examples (not exhaustive):** `ShowWorkPermittedLocations` and `ShowCurrentWorkDuty` are enabled for types such as `App_Inv_And_WP`, `App_Visa_and_WP_Ext`, and `App_WP_Ext` (work permitted locations); see the catalog for the full matrix.

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

- **Unique person per application:** `IsPersonUniqueInApplication`.
- **`OnSaving`:** updates `ApplicationItemName`; border zone default `Ýok` on create when empty.
- **`CrossObjectSyncHelper`:** property-change sync for visa, invitation, work permit links (where configured).

---

## 7. UI notes

- **Navigation:** Application group (nested under `Application` detail).
- **Appearance:** Most document and status fields use `[Appearance(..., Criteria = "!Application.ApplicationType.Show…")]`.
- **Detail layout:** `Model.xafml` (Blazor Server) — includes `WorkPermittedLocations` next to other document fields.
- **Border zone on item detail:** `ApplicationItemDetailViewBorderZoneController` hides duplicate layout nodes for `Application.BorderZoneLocation` vs item `BorderZoneLocation`.

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
