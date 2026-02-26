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
| `Employee` | `Employee` | The employee this application item is for. | | `ImmediatePostData` enabled. Hidden if `Application.IsForFamily` is true. Setting this property also sets `Person`. |
| `FamilyMember` | `FamilyMember` | The family member this application item is for. | | `ImmediatePostData` enabled. Hidden if `Application.IsForFamily` is false. Setting this property also sets `Person` and `Employee` (from `FamilyMember.Employee`). |
| `CurrentPositionHistory` | `EmployeePositionHistory` | The employee's current position history relevant to this application. | | |
| `PersonName` | `string` | The full name of the associated `Person`. | Read-only; Calculated from `Person.FullName`. | |
| `CurrentPassport` | `Passport` | The passport being used for this application process. | Required. | |
| `PreviousPassport` | `Passport` | The previous passport, used in applications for passport changes. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowPreviousPassport`. |
| `CurrentVisa` | `Visa` | The current visa associated with this application item. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowCurrentVisa`. |
| `CurrentWorkPermitItem` | `WorkPermitItem` | The work permit item being extended or cancelled. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowCurrentWorkPermitItem`. |
| `CurrentInvitationItem` | `InvitationItem` | The invitation item generated for this person. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowCurrentInvitationItem`. |
| `CurrentAddressOfResidence` | `AddressOfResidence` | The address of residence for registration-related applications. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowCurrentAddressOfResidence`. |
| `CurrentRegistration` | `Registration` | Registration information. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowCurrentRegistration`. |
| `CurrentEmployeeContract` | `EmployeeContract` | The current employee contract. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowCurrentEmployeeContract`. |
| `CurrentMedicalRecord` | `MedicalRecord` | The current medical record. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowCurrentMedicalRecord`. |
| `InvitationIssued` | `bool` | Flag indicating if the invitation has been issued. | | Hidden in ListView if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowInvitationIssued`. |
| `WorkPermitIssued` | `bool` | Flag indicating if the work permit has been issued. | | Hidden in ListView if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowWorkPermitIssued`. |
| `RejectionIssued` | `bool` | Flag indicating if a rejection has been issued. | | Hidden in ListView if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowRejectionIssued`. |
| `VisaIssued` | `bool` | Flag indicating if the visa has been issued. | | Hidden in ListView if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowVisaIssued`. |

---

## 3. Business Rules & Logic

- **`Person` Property**: This property is set programmatically. When an `Employee` is assigned, `Person` is set to that `Employee`. When a `FamilyMember` is assigned, `Person` is set to that `FamilyMember`. It is marked as read-only in the UI.
- **`Employee` / `FamilyMember` Visibility**:
    - If `Application.IsForFamily` is `true`, the `Employee` field is hidden.
    - If `Application.IsForFamily` is `false`, the `FamilyMember` field is hidden.
- **`Employee` / `FamilyMember` Population**:
    - When `Employee` is set, if `Application.IsForFamily` is `false`, `Person` is set to the assigned `Employee`.
    - When `FamilyMember` is set, `Employee` is set to `FamilyMember.Employee`, and `Person` is set to the assigned `FamilyMember`.
- **Conditional Visibility of Documents**: The visibility of `PreviousPassport`, `WorkPermit`, `Invitation`, `AddressOfResidence`, and `CurrentRegistration` is dynamically controlled by properties on the `Application.ApplicationType` object (e.g., `ShowPreviousPassport`, `ShowWorkPermit`).
- **Conditional Visibility of Documents**: The visibility of document-related properties (e.g., `CurrentPassport`, `CurrentVisa`, `CurrentWorkPermit`) and status flags (e.g., `InvitationIssued`) is dynamically controlled by boolean properties on the `Application.ApplicationType` object (e.g., `ShowCurrentVisa`, `ShowInvitationIssued`).

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
