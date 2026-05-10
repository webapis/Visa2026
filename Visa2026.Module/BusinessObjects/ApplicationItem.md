# Business Object: ApplicationItem

## 1. Purpose

The `ApplicationItem` business object acts as a line item within an `Application`. It links a specific person (either an `Employee` or a `FamilyMember`) to the application process and holds all the relevant, context-specific information for that person, such as the passport and other related documents.

---

## 2. Properties

This section details the data fields of the `ApplicationItem` object as defined in `ApplicationItem.cs`.

| Property Name | Data Type | Description | Constraints / Validation Rules | UI Notes |
|---------------|-----------|-------------|--------------------------------|----------|
| `Application` | `Application` | A required reference to the parent `Application`. | Required. | |
| `Person` | `Person` | The person (Employee or FamilyMember) associated with this application item. | Required. | Read-only (`AllowEdit="False"`). Set programmatically by `Employee` or `FamilyMember` setters. |
| `Employee` | `Employee` | The employee this application item is for. | | `ImmediatePostData` enabled. Hidden if `Application.Category` is FamilyMember. Setting this property also sets `Person`. |
| `FamilyMember` | `FamilyMember` | The family member this application item is for. | | `ImmediatePostData` enabled. Hidden if `Application.Category` is Employee. Setting this property also sets `Person` and `Employee` (from `FamilyMember.Employee`). |
| `CurrentPositionHistory` | `EmployeePositionHistory` | The employee's current position history relevant to this application. | | |
| `PersonName` | `string` | The full name of the associated `Person`. | Read-only; Calculated from `Person.FullName`. | |
| `CurrentPassport` | `Passport` | The passport being used for this application process. | Required. | |
| `PreviousPassport` | `Passport` | The previous passport, used in applications for passport changes. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowPreviousPassport`. |
| `CurrentVisa` | `Visa` | The **target** visa for this application item (extension, cancellation, registration, etc.): the visa **used** by this line. Distinct from **`Visa.IssuingApplicationItem`**, which records the line **under which a visa was issued**. See §3.5. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowCurrentVisa`. Inverse: `Visa.AssociatedApplicationItems`. |
| `CurrentWorkPermitItem` | `WorkPermitItem` | The work permit item being extended or cancelled. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowCurrentWorkPermitItem`. |
| `CurrentInvitationItem` | `InvitationItem` | The invitation item generated for this person. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowCurrentInvitationItem`. |
| `CurrentAddressOfResidence` | `AddressOfResidence` | The address of residence for registration-related applications. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowCurrentAddressOfResidence`. |
| `CurrentRegistration` | `Registration` | Registration information. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowCurrentRegistration`. |
| `CurrentEmployeeContract` | `EmployeeContract` | The current employee contract. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowCurrentEmployeeContract`. |
| `CurrentMedicalRecord` | `MedicalRecord` | The current medical record. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowCurrentMedicalRecord`. |
| `InvitationItemIsIssued` | `bool` | Flag indicating if the invitation item has been issued. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowInvitationItemIsIssued`. |
| `WorkPermitItemIsIssued` | `bool` | Flag indicating if the work permit item has been issued. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowWorkPermitItemIsIssued`. |
| `RejectionIssued` | `bool` | Flag indicating if a rejection has been issued. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowRejectionIssued`. |
| `VisaIssued` | `bool` | Flag indicating if the visa has been issued. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowVisaIssued`. |
| `IsDeleted` | `bool` | Soft-delete flag. | Part of `ISoftDelete`; typically not browsable. | |
| `DateDeleted` | `DateTime?` | When the record was soft-deleted. | Part of `ISoftDelete`. | |
| `DeletedBy` | `ApplicationUser` | User who performed soft delete. | Part of `ISoftDelete`. | |

---

## 3. Business Rules & Logic

- **`Person` Property**: This property is set programmatically. When an `Employee` is assigned, `Person` is set to that `Employee`. When a `FamilyMember` is assigned, `Person` is set to that `FamilyMember`. It is marked as read-only in the UI.
- **`Employee` / `FamilyMember` Visibility**:
    - If `Application.Category` is `FamilyMember`, the `Employee` field is hidden.
    - If `Application.Category` is `Employee`, the `FamilyMember` field is hidden.
- **`Employee` / `FamilyMember` Population**:
    - When `Employee` is set, if `Application.Category` is `Employee` or `Both`, `Person` is set to the assigned `Employee`.
    - When `FamilyMember` is set, `Employee` is set to `FamilyMember.Employee`, and `Person` is set to the assigned `FamilyMember`.

### 3.5 Visa linkage (issuing line vs target visa)

- **`Visa.IssuingApplicationItem` (on `Visa`)**: Points from a **`Visa`** back to the **`ApplicationItem`** row representing **the application procedure that produced this visa** (person + parent **`Application`**). Required when saving a **`Visa`** **unless** **`Visa.HistoricalImport`** is true (legacy / pre-system data with no application on file).
- **`CurrentVisa` (on `ApplicationItem`)**: Points from an **`ApplicationItem`** to the **`Visa`** record **used as input/context** for **this** application (extensions, cancellations, check-out, etc.). Inverse collection: **`Visa.AssociatedApplicationItems`**.

Do not confuse the two directions: **issuing** = “created under this line”; **target** = “this line references that visa.” See **`Visa.md`** §3.

- **Conditional visibility (UI)**: Document-related navigations (`PreviousPassport`, `PreviousVisa`, `CurrentVisa`, `CurrentWorkPermitItem`, `PreviousWorkPermitItem`, `CurrentInvitationItem`, `CurrentAddressOfResidence`, `CurrentRegistration`, `CurrentEmployeeContract`, `CurrentMedicalRecord`, `CurrentEducation`) and several status columns are shown or hidden using **`[Appearance]`** on `ApplicationItem`, driven by **`Application.ApplicationType`** flags such as `ShowPreviousPassport`, `ShowCurrentVisa`, `ShowCurrentWorkPermitItem`, `ShowInvitationItemIsIssued`, etc. (see `ApplicationItem.cs` and **`ApplicationType`** in `LookupBusinessObjects.cs`). **`CurrentPassport`** is always shown when the item is edited (no `ShowCurrentPassport` flag); it is still **required** for a coherent application line.
- **PDF generation (dynamic `PdfFormMapping`)**: `PdfMappingHelper.MapApplicationData` applies the same visibility intent via **`PdfMappingSourceGate`**: a **Property** or **Expression** mapping whose path/text references one of those gated navigations (or gated **`Application.*`** fields such as `Application.Urgency`) is **not evaluated** unless the corresponding **`ApplicationType.Show…`** flag is true and required links are set (and employee-only paths such as **`CurrentPositionHistory`** / **`CurrentEmployeeContract`** require **`Person.IsEmployee`**). **`Constant`** mappings are unaffected. Details: `Services/PDF-Form-Filling.md` §6.2 and `PdfFormMapping.md` §3.

---

## 4. Relationships to Other Objects

- **`Application`**: A required, many-to-one relationship to the parent `Application` object.
- **Lookups**: This object has lookup relationships to `Person`, `Employee`, `FamilyMember`, `EmployeePositionHistory`, `Passport`, `Visa`, `WorkPermitItem`, `InvitationItem`, `AddressOfResidence`, `Registration`, `EmployeeContract`, and `MedicalRecord`.

---


## 5. UI & Behavior Notes

- **Navigation**: This object appears in the navigation menu under the "Application" group.
- **`ImmediatePostData`**: `Employee` and `FamilyMember` properties have `ImmediatePostData` enabled, meaning changes to these properties will immediately trigger server-side logic and UI updates.
- **Read-only Properties**: The `Person` property is explicitly marked as read-only in the UI.
- **Conditional UI**: The visibility of several properties is controlled by `Appearance` rules based on the parent `Application`'s properties.
