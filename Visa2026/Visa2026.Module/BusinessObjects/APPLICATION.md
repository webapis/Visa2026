# Business Object: Application

## 1. Purpose

The `Application` business object is a central entity designed to represent a single, collective request submitted to the Turkmenistan Migration Service. A single `Application` can encompass requests for multiple individuals at once, streamlining the submission process for procedures like visa invitations, extensions, and registrations.

---

## 2. Application Types (SubType)

The `Application` object is versatile and handles all types of visa-related procedures by differentiating them through a `SubType` property. This property is a calculated enum that derives its value from either `ApplicationTypeForEmployee` or `ApplicationTypeForFamilyMember`, depending on the context. This provides a granular level of control over the object's behavior and the data it requires.

### ApplicationTypeForEmployee Enum

- `ApplicationForInvitation`
- `ApplicationForChangingInvitation`
- `ApplicationForRegistrationUpOnArrival`
- `ApplicationForRegistrationExtention`
- `ApplicationForRegisteringToANewLocation`
- `ApplicationForChagingInformation`
- `ApplicationForStrikeOffRegister`
- `ApplicationForVisaExtention`
- `ApplicationForChangingVisaCategory`
- `ApplicationForChangingPassport`
- `ApplicationForSevicePasport`
- `ApplicationForBorderZonePermision`
- `ApplicationForCancellingVisaAndWorkpermit`
- `ApplicationForGoBusinessTrip`
- `ApplicationForCameFromBusinessTrip`
- `ApplicationForExtendingWorkPermitLocation`
- `RugsatnamaMöhletineGöräÇakylyk`
- `ApplicationForCancellingVisa`
- `ApplicationForCancellingWorkPermit`

### ApplicationTypeForFamilyMember Enum

- `ApplicationForInvitation`
- `ApplicationForRegistrationUpOnArrival`
- `ApplicationForRegistrationExtention`
- `ApplicationForStrikeOffRegister`
- `ApplicationForVisaExtention`
---

## 3. Structure and Composition

 - **Multiple Individuals:** Each `Application` can contain one or more individuals, managed through the `ApplicationItems` collection.
- **Separation of Employees and Family Members:** The system enforces a strict separation. An application is designated for either employees or family members via the `IsForFamily` flag, but not both.
- **Project-Based:** Applications are often created in the context of a specific `ProjectContract`.

---

## 4. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `ApplicationNumber` | `string` | A unique identifier for the application. | - |
| `ApplicationDate` | `DateTime` | The date the application is created or submitted. | Required. | 
| `SubType` | `enum` | The specific type of application (e.g., `ApplicationForInvitation`). This controls the UI's dynamic behavior. | Required. |
| `Status` | `enum` | The current state of the application in its lifecycle (e.g., `Office`, `ToMinistery`, `Processed`). | Read-only; Calculated. |
| `IsForFamily` | `bool` | A flag to distinguish if the application is for employees (`false`) or family members (`true`). | - |
| `IsWorkPermitRequired` | `bool` | A flag to indicate if a work permit is required. Relevant for Employee applications. | Default: `true`. |
| `Cancelled` | `bool` | A flag to indicate if the application has been cancelled. | - |
| `ProcessDate` | `DateTime` | The date the application was officially processed by the authorities. | Optional; Cannot be earlier than ApplicationDate. |
| `ProcessNumber` | `string` | The official processing number assigned by the ministry. | Optional. |
| `ProjectContract` | `ProjectContract` (Lookup) | A reference to the construction project/contract this application is for. | Conditionally Required; Must belong to the selected Ministry. |
| `Urgency` | `Urgency` (Lookup) | A reference to the processing priority (e.g., `Normal`, `Urgent`). | Conditionally Required. |
| `VisaPeriod` | `VisaPeriod` (Lookup) | A reference to the requested visa duration. | Conditionally Required. |
| `VisaCategory` | `VisaCategory` (Lookup) | A reference to the requested visa category. | Conditionally Required. |
| `Ministry` | `Ministry` (Lookup) | The ministry to which the application is being submitted. | Conditionally Required. |
| `InvitationToBeChanged` | `Invitation` (Lookup) | The existing invitation that is being modified. | Conditionally Required. |
| `NewRegistrationLocation` | `WorkPermitLocation` (Lookup) | The new location for registration. | Conditionally Required; Must be different from PreviousRegistrationLocation. |
| `PreviousRegistrationLocation` | `WorkPermitLocation` (Lookup) | The previous location for registration. | Conditionally Required. |
| `StrikeOffType` | `StrikeOffType` (Enum) | The specific reason or type for striking off the register. | Conditionally Required. |
| `ChangeInfoType` | `ChangeInfoType` (Enum) | The specific type of information being changed. | Conditionally Required. |
| `BusinessTripDestination` | `WorkPermitLocation` (Lookup) | The destination for a business trip application. | Conditionally Required. |
| `DateOfDeparture` | `DateTime` | The departure date for a business trip. | Conditionally Required. |
| `DurationOfStay` | `int` | The duration of the business trip in days. | Conditionally Required. |
| `BorderZonePeriod` | `VisaPeriod` (Lookup) | The requested duration for a border zone permit. | Conditionally Required. |

---

## 5. Relationships to Other Objects

 - **`ApplicationItems` (ApplicationItem)**: A one-to-many, aggregated collection of individuals included in this application.
- **`Invitations` (Invitation)**: A one-to-many, aggregated collection of invitations generated by this application.
- **`Rejections` (Rejection)**: A one-to-many, aggregated collection of rejection records associated with this application.
- **`WorkPermits` (WorkPermit)**: A one-to-many, aggregated collection of work permits generated by this application.
- **`BorderZones` (BorderZone)**: A many-to-many relationship to the `BorderZone` objects requested in the application.

---

## 6. Dynamic UI Behavior

 The user interface for the `Application` object is highly dynamic. The visibility and requirement of many fields are controlled by the selected `SubType`. This design ensures that users only see and fill in the information pertinent to the specific procedure they are performing.

### Visibility Matrix

 The following table details which properties are visible for each Application Type. Note that core properties like `ApplicationNumber`, `ApplicationDate`, `Status`, `Urgency`, `ApplicationItems`, and `ApplicationResults` are generally visible for all types unless otherwise noted.

| Application Context | Application Type (SubType) | Visible Properties |
| :--- | :--- | :--- |
| **Employee** | `ApplicationForInvitation` | `Ministry`, `ProjectContract`, `VisaPeriod`, `VisaCategory` |
| **Employee** | `ApplicationForVisaExtention` | `Ministry`, `ProjectContract`, `VisaPeriod`, `VisaCategory`, `IsWorkPermitRequired` |
| **Employee** | `ApplicationForChangingInvitation` | `Ministry`, `ProjectContract`, `VisaPeriod`, `VisaCategory`, `InvitationToBeChanged` |
| **Employee** | `RugsatnamaMöhletineGöräÇakylyk` | `Ministry`, `ProjectContract`, `VisaPeriod`, `VisaCategory` |
| **Employee** | `ApplicationForBorderZonePermision` | `Ministry`, `ProjectContract`, `BorderZonePeriod`, `BorderZones` |
| **Employee** | `ApplicationForStrikeOffRegister` | `StrikeOffType`<br/>*If StrikeOffType is 'ChangingRegistrationLocation':* `NewRegistrationLocation`, `PreviousRegistrationLocation` |
| **Employee** | `ApplicationForChagingInformation` | `ChangeInfoType` |
| **Employee** | `ApplicationForGoBusinessTrip` | `BusinessTripDestination`, `DateOfDeparture`, `DurationOfStay` |
| **Employee** | `ApplicationForRegisteringToANewLocation` | `NewRegistrationLocation`, `PreviousRegistrationLocation` |
| **Family** | `ApplicationForInvitation` | `Ministry`, `ProjectContract`, `VisaPeriod`, `VisaCategory` |
| **Family** | `ApplicationForVisaExtention` | `Ministry`, `ProjectContract`, `VisaPeriod`, `VisaCategory` |
| **Family** | `ApplicationForStrikeOffRegister` | `StrikeOffType` |
| **All Others** | *(Default)* | Only core application properties are visible. |

---

## 7. Future Business Object Candidates

- **`Ministry`**: Represents the government ministry to which an application is submitted.
- **`ProjectContract`**: Stores details about the specific contract with a ministry.
- **`Urgency`**: A lookup object to define processing urgency levels.
- **`VisaPeriod`**: A lookup object for standardized visa durations.