# ApplicationItem Property Visibility Matrix

The visibility of properties within the `ApplicationItem` object is dynamically controlled by the `SubType` of the parent `Application`.
This ensures that for any given application process, users are only presented with the fields relevant to the specific individuals involved. The matrix below details which properties are visible for each major application type.

Core properties like `Passport` and `Employee` are generally visible for all types. The `FamilyMember` lookup is visible only when the parent `Application.IsForFamily` flag is set to `true`. When a family member is selected, the `Employee` lookup is automatically populated and made read-only.

| Application Context | Application Type (SubType) | Visible Properties in ApplicationItem |
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