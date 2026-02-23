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
| `Passport` | `Passport` | The passport being used for this application process. | Required. | |
| `PreviousPassport` | `Passport` | The previous passport, used in applications for passport changes. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowPreviousPassport`. |
| `WorkPermit` | `WorkPermit` | The work permit being extended or cancelled. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowWorkPermit`. |
| `Invitation` | `Invitation` | The invitation generated for this person. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowInvitation`. |
| `AddressOfResidence` | `AddressOfResidence` | The address of residence for registration-related applications. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowAddressOfResidence`. |
| `CurrentRegistration` | `Registration` | Registration information. | | Hidden if `Application.ApplicationType` is null or `!Application.ApplicationType.ShowRegistration`. |

---

## 3. Business Rules & Logic
- If the parent `Application.IsForFamily` is `false`, the `Employee` field is required, and the `FamilyMember` field is hidden.
- If the parent `Application.IsForFamily` is `true`, the `FamilyMember` field is visible and required. The `Employee` field will be automatically populated from the selected family member's record and will be read-only to prevent inconsistencies.
- The visibility and requirement level of other properties (`Visa`, `WorkPermit`, `Position`, etc.) are dynamically controlled by the `SubType` selected in the parent `Application`.
- **Outcome Exclusivity**: A person cannot be linked to a `Rejection` and a positive outcome (`Invitation`, `IssuedVisa`, `IssuedWorkPermit`) at the same time.
---


## 4. Relationships to Other Objects

- **`Application` (Application)**: A many-to-one, aggregated relationship to the parent `Application` object.
- **Lookups**: This object has non-aggregated lookup relationships to `Employee`, `FamilyMember`, `Passport`, `Visa`, `WorkPermit`, and various other configuration objects.
- **Outcome Relationships**: Associations or lookups to `Invitation`, `Visa` (Issued), `WorkPermit` (Issued), and `Rejection`.
---- **Read-only Properties**: The `Person` property is explicitly marked as read-only in the UI.
- **Conditional UI**: The visibility of several properties is controlled by `Appearance` rules based on the parent `Application`'s properties.
