# Business Object: ApplicationType

## 1. Purpose

The `ApplicationType` business object is a lookup entity that defines the different types of applications that can be processed in the system. It plays a crucial role in controlling the UI by determining which fields are visible and required for a given application process.

---

## 2. Inheritance

This object inherits from the `LookupBase` class, which provides the standard `Name` and `Code` properties.

---

## 3. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `LifecycleStage` | `ApplicationLifecycleStage` (Enum) | Specifies the stage of the application lifecycle this type belongs to (e.g., Entry, Stay, Exit). | |
| `Category` | `ApplicationTypeCategory` (Enum) | Specifies if the application type is for an `Employee`, `FamilyMember`, or `Both`. | Required. |
| `OrganizationType` | `OrganizationType` (Lookup) | A required reference to the parent organization type (e.g., "Iş Buýrujy", "Migrasiýa"). | Required. |
| `DurationInDays` | `int` | The number of days used to calculate the default expiration date for an Application. | |
| `ApplicationProgressRoute` | `ApplicationProgressRouteKind` (Enum) | Ministry workflow vs direct to migration service (`ViaMinistries`, `DirectToMigrationService`). Seeded in `ApplicationTypeConfigurationCatalog.json`; drives allowed `ApplicationProgress` state/location codes. | |
| `MinistryReviewDepth` | `MinistryReviewDepth` (Enum) | `None`, `FirstMinistryOnly`, or `FirstAndSecondMinistry` when route is `ViaMinistries`. | |
| `ShowProjectContract` | `bool` | Controls visibility of the `ProjectContract` property in the `Application` Detail View. **Not** used for progress routing at runtime. | |
| `ShowVisaPeriod` | `bool` | Controls visibility of the `VisaPeriod` property in the `Application` Detail View. | |
| `ShowVisaCategory` | `bool` | Controls visibility of the `VisaCategory` property in the `Application` Detail View. | |
| `ShowUrgency` | `bool` | Controls visibility of the `Urgency` property in the `Application` Detail View. | |
| `ShowInvitations` | `bool` | Controls visibility of the `Invitations` collection in the `Application` Detail View. | |
| `ShowRejections` | `bool` | Controls visibility of the `Rejections` collection in the `Application` Detail View. | |
| `ShowWorkPermits` | `bool` | Controls visibility of the `WorkPermits` collection in the `Application` Detail View. | |
| `ShowRegistrations` | `bool` | Controls visibility of the `Registrations` collection in the `Application` Detail View. | |
| `ShowVisas` | `bool` | Controls visibility of the `Visas` collection in the `Application` Detail View. | |
| `ShowApplicationItems` | `bool` | Controls visibility of the `ApplicationItems` collection in the `Application` Detail View. | |
| `ShowApplicationReason` | `bool` | Legacy flag (Application reason field on Application is not used). | |
| `ShowMigrationService` | `bool` | Controls visibility of the `MigrationService` property in the `Application` Detail View. | |
| `ShowBusinessTripPlan` | `bool` | Controls visibility of the `BusinessTripPlan` property in the `Application` Detail View. | |
| `ShowBusinessTrips` | `bool` | Controls visibility of the `BusinessTrips` collection in the `Application` Detail View. | |
| `ShowPreviousPassport` | `bool` | Controls visibility of the `PreviousPassport` property in the `ApplicationItem` Detail View. | |
| `ShowCurrentVisa` | `bool` | Controls visibility of the `CurrentVisa` property in the `ApplicationItem` Detail View. | |
| `ShowCurrentWorkPermit` | `bool` | Controls visibility of the `CurrentWorkPermit` property in the `ApplicationItem` Detail View. | |
| `ShowCurrentInvitation` | `bool` | Controls visibility of the `CurrentInvitation` property in the `ApplicationItem` Detail View. | |
| `ShowCurrentAddressOfResidence` | `bool` | Controls visibility of the `CurrentAddressOfResidence` property in the `ApplicationItem` Detail View. | |
| `ShowCurrentRegistration` | `bool` | Controls visibility of the `CurrentRegistration` property in the `ApplicationItem` Detail View. | |
| `ShowCurrentEmployeeContract` | `bool` | Controls visibility of the `CurrentEmployeeContract` property in the `ApplicationItem` Detail View. | |
| `ShowCurrentMedicalRecord` | `bool` | Controls visibility of the `CurrentMedicalRecord` property in the `ApplicationItem` Detail View. | |
| `ShowInvitationIssued` | `bool` | Controls visibility of the `InvitationIssued` column in the `ApplicationItem` List View. | |
| `ShowWorkPermitIssued` | `bool` | Controls visibility of the `WorkPermitIssued` column in the `ApplicationItem` List View. | |
| `ShowRejectionIssued` | `bool` | Controls visibility of the `RejectionIssued` column in the `ApplicationItem` List View. | |
| `ShowVisaIssued` | `bool` | Controls visibility of the `VisaIssued` column in the `ApplicationItem` List View. | |

---

## 4. Legacy relationships (hidden from UI)

`ApplicationTypeFilter`, `ApplicationTypeFilterNames`, and `ApplicationReasons` remain in the model for import/DB compatibility but are **not shown** on the Application Type detail view. Use **`SelectionCode`**, **`Category`**, and visibility flags instead.

---

## 5. Relationships to Other Objects

- **`OrganizationType` (OrganizationType)**: A many-to-one relationship to the `OrganizationType` object. This creates a hierarchical structure where each `ApplicationType` belongs to a specific `OrganizationType`.

---

## 6. UI & Behavior Notes

- This object is managed under the `Lookup/Application` navigation item.
- The boolean "Show" properties are used in `Appearance` rules on the `Application` and `ApplicationItem` business objects to dynamically control the UI based on the selected `ApplicationType`.

---

## 7. Data Seeding

The data for this object is seeded from JSON files (`applicationtypes_migrasiya.json`, `applicationtypes_isbuyrujy.json`) located in the `Visa2026.Module/DatabaseUpdate` folder. The seeding logic is handled by the `Updater.cs` class.