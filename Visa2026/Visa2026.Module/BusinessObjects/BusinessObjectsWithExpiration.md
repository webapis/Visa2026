# Business Objects with Expiration Logic

## Overview
This document identifies Business Objects that contain time-sensitive validity periods. These objects require specific logic to handle validation, notifications, and state transitions as they approach or pass their expiration dates.

## Prerequisites & Active State Logic
To ensure data integrity and correct system behavior, the following rules apply to all Business Objects listed below:

1.  **`IsActive` Property**: Each object must include a boolean property named `IsActive`.
2.  **Priority**: The `IsActive` property has higher priority than the `ExpirationDate` or `EndDate`. An object is considered valid only if `IsActive` is `true`, regardless of the date.
3.  **Default State**: When a new object is created, its `IsActive` property must be set to `true` by default.
4.  **Single Active Instance**: Only one object of the same type belonging to the same **Parent Business Object** can be in the `IsActive = true` state.
    *   *Mechanism*: Activating a new record must automatically deactivate the previous active record for that parent (refer to `SingleActiveBaseObject` pattern). This process should be applied only after the new record is successfully saved to prevent premature deactivation if the user cancels the operation.
5.  **Remaining Days Calculation**: Each object must include a calculated integer property (e.g., `DaysRemaining`) that displays the number of days left until expiration.
    *   *Logic*: `ExpirationDate - DateTime.Today`.
6.  **Expiration State**: Each object must include a calculated property (e.g., `ExpirationState`) that returns a status enumeration based on `IsActive` and `DaysRemaining`.
    *   *Suggested Values*: `Active`, `Expiring Soon` (e.g., < 30 days), `Expired`, `Archived` (if `IsActive` is false).
7.  **Parent Object Structure (History vs. Current)**:
    *   **Collection (History)**: The Parent BO must maintain a One-to-Many collection of these objects to store the full history (e.g., `Person.Passports`).
    *   **Current Reference (Active Item)**: The Parent BO must have a One-to-One property that references the specific object where `IsActive` is `true` (e.g., `Person.ActivePassport`).

---

## 1. Objects with `ExpirationDate`
These objects explicitly define an expiration date, representing a hard deadline for validity.

| Business Object | Parent BO | Expiration Property | Parent Collection (History) | Parent Active Reference | Other Usage |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Visa** | `Passport` | `ExpirationDate` | `Passport.Visas` | `Person.CurrentVisa`* | • **PersonInApplication** (`Visa`, `IssuedVisa`) |
| **WorkPermit** | `Employee` | `ExpirationDate` | `Employee.WorkPermits` | `Employee.CurrentWorkPermit` | • **PersonInApplication** (`WorkPermit`, `IssuedWorkPermit`)<br>• **WorkPermitLetter** (Collection) |
| **Passport** | `Person` | `ExpirationDate` | `Person.Passports` | `Person.ActivePassport` | • **PersonInApplication** (`Passport`, `PreviousPassport`)<br>• **Visa** (Parent)<br>• **WorkPermit** (Linked Doc) |
| **Invitation** | `Application` | `ExpirationDate` | `Application.Invitations` | N/A | • **Application** (`InvitationToBeChanged`)<br>• **PersonInApplication** (`Invitation`) |

*\*Note: While Visa is a child of Passport, the active reference is maintained on the Person object for easier access.*

---

## 2. Objects with `EndDate` (Validity Periods)
These objects track a history or a specific period of validity using an end date. While not strictly "expiration," the logic is similar.

| Business Object | Parent BO | Expiration Property | Parent Collection (History) | Parent Active Reference | Other Usage |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **AddressOfResidence** | `Person` | `EndDate` | `Person.AddressesOfResidence` | `Person.CurrentAddressOfResidence` | • **PersonInApplication** (`AddressOfResidence`) |
| **EmployeePositionHistory** | `Employee` | `EndDate` | `Employee.PositionHistory` | `Employee.CurrentPositionHistory` | - |
| **Education** | `Person` | `EducationEndDate` | `Person.Educations` | `Person.CurrentEducation` | - |