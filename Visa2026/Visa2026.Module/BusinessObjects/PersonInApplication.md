# Business Object: PersonInApplication

## 1. Purpose

The `PersonInApplication` business object acts as a line item within an `Application`. It links a specific person (either an `Employee` or a `FamilyMember`) to the application process and holds all the relevant, context-specific information for that person, such as the passport and visa being used or affected.

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
| `IssuedVisa` | `Visa` (Lookup) | The visa issued for this person. | Read-only (System set). |
| `IssuedWorkPermit` | `WorkPermit` (Lookup) | The work permit issued for this person. | Read-only (System set). |
| `Rejection` | `Rejection` (Lookup) | The rejection record if the application was denied. | Read-only (System set). |

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

## 5. Dynamic Property Visibility

The visibility of properties within the `PersonInApplication` object is dynamically controlled by the `SubType` of the parent `Application`. This ensures that for any given application process, users are only presented with the fields relevant to the specific individuals involved. The matrix below details which properties are visible for each major application type.

Core properties like `Passport` and `Employee` are generally visible for all types. The `FamilyMember` lookup is visible only when the parent `Application.IsForFamily` flag is set to `true`. When a family member is selected, the `Employee` lookup is automatically populated and made read-only.

| Application Context | Application Type (SubType) | Visible Properties in PersonInApplication |
| :--- | :--- | :--- |
| **Employee & Family** | `ApplicationForInvitation` | `Passport`, `Position` |
| **Employee** | `ApplicationForChangingInvitation` | `Passport`, `Position` |
| **Employee & Family** | `ApplicationForVisaExtention` | `Passport`, `Visa`, `Position`, `WorkPermit` (Employee only; if `IsWorkPermitRequired`) |
| **Employee** | `ApplicationForChangingVisaCategory` | `Passport`, `Visa`, `Position` |
| **Employee & Family** | `ApplicationForRegistrationUpOnArrival` | `Passport`, `CheckPoint`, `EntryDate`, `VisaIssuedPlace`, `PurposeOfTravel`, `AddressOfResidence` |
| **Employee & Family** | `ApplicationForRegistrationExtention` | `Passport`, `AddressOfResidence` |
| **Employee & Family** | `ApplicationForStrikeOffRegister` | `Passport`, `AddressOfResidence` |
| **Employee** | `ApplicationForRegisteringToANewLocation` | `Passport`, `AddressOfResidence` |
| **Employee** | `ApplicationForChangingPassport` | `Passport`, `PreviousPassport`, `Position` |
| **Employee** | `ApplicationForCancellingVisa` | `Passport`, `Visa`, `Position` |
| **Employee** | `ApplicationForCancellingWorkPermit` | `Passport`, `WorkPermit`, `Position` |
| **Employee** | `ApplicationForCancellingVisaAndWorkpermit` | `Passport`, `Visa`, `WorkPermit`, `Position` |
| **Employee** | `RugsatnamaMöhletineGöräÇakylyk` | `Passport`, `WorkPermit`, `Position` |
| **Employee** | `ApplicationForExtendingWorkPermitLocation` | `Passport`, `WorkPermit`, `Position` |
| **Employee** | `ApplicationForBorderZonePermision` | `Passport`, `Visa`, `Position` |
| **All Others** | *(Default)* | `Passport`, `Position` (if applicable) |

## 6. UI & Behavior Notes
- **Status Color-Coding**: In List Views, the `Status` property is color-coded:
  - **Green**: Invited
  - **Red**: Rejected
  - **Blue**: VisaIssued
  - **Teal**: WorkPermitIssued
  - **Gray/Strikeout**: Cancelled