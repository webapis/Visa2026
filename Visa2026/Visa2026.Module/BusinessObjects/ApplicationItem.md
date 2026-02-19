# Business Object: ApplicationItem

## 1. Purpose

The `ApplicationItem` business object acts as a line item within an `Application`. It links a specific person (either an `Employee` or a `FamilyMember`) to the application process and holds all the relevant, context-specific information for that person, such as the passport and visa being used or affected.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Application` | `Application` (Lookup) | A required, aggregated reference to the parent `Application`. | Required. |
| `Employee` | `Employee` (Lookup) | The employee this application line is for. | Conditionally Required. |
| `FamilyMember` | `FamilyMember` (Lookup) | The family member this application line is for. | Conditionally Required. |
| `PersonName` | `string` | The name of the person (Employee or Family Member). | Read-only; Calculated. |
| `Passport` | `Passport` (Lookup) | The passport being used for this application process. | Required. |
| `PreviousPassport` | `Passport` (Lookup) | The previous passport, used in applications for passport changes. | Optional; Conditionally Required. |
| `Visa` | `Visa` (Lookup) | The visa being extended, changed, or cancelled. | Optional; Conditionally Required. |
| `WorkPermit` | `WorkPermit` (Lookup) | The work permit being extended or cancelled. | Optional; Conditionally Required. |
| `Position` | `Position` (Lookup) | The employee's current position relevant to this application. | Optional; Conditionally Required. |
| `AddressOfResidence` | `AddressOfResidence` (Lookup) | The address of residence for registration-related applications. | Optional; Conditionally Required. |
| `CheckPoint` | `CheckPoint` (Lookup) | The border checkpoint used for entry. | Optional; Conditionally Required. |
| `EntryDate` | `DateTime` | The date of entry into the country. | Optional; Conditionally Required. |
| `VisaIssuedPlace` | `VisaIssuedPlace` (Lookup) | The location where the visa was issued. | Optional; Conditionally Required. |
| `PurposeOfTravel` | `PurposeOfTravel` (Lookup) | The stated purpose of travel for registration. | Optional; Conditionally Required. |
| `RegistrationNumber` | `string` | The official registration number assigned after processing. | Optional. |
| `RegistrationDate` | `DateTime` | The date the registration was officially completed. | Optional. |
| `Status` | `string` | The current status of the person's application line (e.g., Invited, Rejected). | Read-only; Calculated. |
| `Cancelled` | `bool` | Indicates if this specific line item has been cancelled. | - |
| `Invitation` | `Invitation` (Lookup) | The invitation generated for this person. | Read-only (System set). |
---

## 3. Business Rules & Logic
- The `Employee` field is always visible to provide context for the individual in the application.
- If the parent `Application.IsForFamily` is `false`, the `Employee` field is required, and the `FamilyMember` field is hidden.
- If the parent `Application.IsForFamily` is `true`, the `FamilyMember` field is visible and required. The `Employee` field will be automatically populated from the selected family member's record and will be read-only to prevent inconsistencies.
- The visibility and requirement level of other properties (`Visa`, `WorkPermit`, `Position`, etc.) are dynamically controlled by the `SubType` selected in the parent `Application`.
- **Outcome Exclusivity**: A person cannot be linked to a `Rejection` and a positive outcome (`Invitation`, `IssuedVisa`, `IssuedWorkPermit`) at the same time.
---

## 4. Relationships to Other Objects

- **`Application` (Application)**: A many-to-one, aggregated relationship to the parent `Application` object.
- **Lookups**: This object has non-aggregated lookup relationships to `Employee`, `FamilyMember`, `Passport`, `Visa`, `WorkPermit`, and various other configuration objects.
- **Outcome Relationships**: Associations or lookups to `Invitation`, `Visa` (Issued), `WorkPermit` (Issued), and `Rejection`.
---