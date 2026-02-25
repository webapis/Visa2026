# Business Object: Application

## 1. Purpose

The `Application` business object is a central entity designed to represent a single, collective request submitted to the Turkmenistan Migration Service. A single `Application` can encompass requests for multiple individuals at once, streamlining the submission process for procedures like visa invitations, extensions, and registrations. It also manages the overall state and progress of the application.

---

## 2. Inheritance

This object inherits from `BaseObject`.

---

## 3. Properties

This section details the data fields of the `Application` object as defined in `Application.cs`.

| Property Name | Data Type | Description | Constraints / Validation Rules | UI Notes |
|---------------|-----------|-------------|--------------------------------|----------|
| `ApplicationNumber` | `string` | The sequential part of the unique identifier for the application. | Max Length: 50. | Read-only (`AllowEdit="False"`). Auto-generated on save. |
| `AppNumberPrefix` | `string` | The prefix for the application number, often derived from the `Company`. | | Read-only (`AllowEdit="False"`). Auto-generated on save. |
| `FullApplicationNumber` | `string` | A combined, read-only string of `AppNumberPrefix` and `ApplicationNumber`. | Read-only. | Not Mapped. |
| `Year` | `int` | The year the application was created. | | Read-only (`AllowEdit="False"`). Auto-generated on save. |
| `ApplicationDate` | `DateTime` | The date the application is created or submitted. | Required. | Defaulted to `DateTime.Now` on creation. |
| `IsForFamily` | `bool` | A flag to distinguish if the application is for employees (`false`) or family members (`true`). | | `ImmediatePostData` enabled. Resets `ApplicationType` when changed. |
| `OrganizationType` | `OrganizationType` | The type of organization submitting the application (e.g., Ministry, Company). | | `ImmediatePostData` enabled. Resets `ApplicationType` when changed. |
| `ApplicationType` | `ApplicationType` | The specific type of application (e.g., `ApplicationForInvitation`, `ApplicationForWorkPermit`). | Required. | `ImmediatePostData` enabled. Data source filtered based on `OrganizationType` and `IsForFamily`. |
| `CurrentState` | `ApplicationProgress` | The current state of the application based on its `ProgressHistory`. | | Read-only (`AllowEdit="False"`). Automatically updated. |
| `ProjectContract` | `ProjectContract` | A reference to the construction project/contract this application is for. | | Hidden if `ApplicationType` is null or `!ApplicationType.ShowProjectContract`. |
| `Company` | `Company` | The company associated with the application. | | `ImmediatePostData` enabled. Defaulted to default company on creation. Sets `CompanyHead` and `Representative`. |
| `CompanyHead` | `CompanyHead` | The authorized signatory of the company. | | Data source filtered by `Company`. |
| `Representative` | `Representative` | The representative of the company. | | Data source filtered by `Company`. |
| `Urgency` | `Urgency` | A reference to the processing priority (e.g., `Normal`, `Urgent`). | | |
| `VisaPeriod` | `VisaPeriod` | A reference to the requested visa duration. | | Hidden if `ApplicationType` is null or `!ApplicationType.ShowVisaPeriod`. |
| `VisaCategory` | `VisaCategory` | A reference to the requested visa category. | | Hidden if `ApplicationType` is null or `!ApplicationType.ShowVisaCategory`. |
| `MigrationService` | `MigrationService` | A reference to the specific migration service office. | | Hidden if `ApplicationType` is null or `!ApplicationType.ShowMigrationService`. |
| `BusinessTripPlan` | `BusinessTripPlan` | Aggregated details regarding a business trip plan. | Aggregated. | Hidden if `ApplicationType` is null or `!ApplicationType.ShowBusinessTripPlan`. |
| `ApplicationReason` | `ApplicationReason` | The specific reason for the application. | Required. | Hidden if `ApplicationType` is null or `!ApplicationType.ShowApplicationReason`. Filtered by `ApplicationType`. |

---

## 4. Collections (Relationships)

The `Application` object manages several aggregated collections of related data.

| Collection Name | Item Type | Description | Aggregation | Inverse Property |
|-----------------|-----------|-------------|-------------|------------------|
| `ApplicationItems` | `ApplicationItem` | A collection of line items, each representing a person included in this application. | Aggregated | `ApplicationItem.Application` |
| `Invitations` | `Invitation` | A collection of invitations generated as a result of this application. | Aggregated | `Invitation.Application` |
| `Rejections` | `Rejection` | A collection of rejections issued for this application. | Aggregated | `Rejection.Application` |
| `WorkPermits` | `WorkPermit` | A collection of work permits issued as a result of this application. | Aggregated | `WorkPermit.Application` |
| `Registrations` | `Registration` | A collection of registrations associated with this application. | Aggregated | `Registration.Application` |
| `BusinessTrips` | `BusinessTrip` | A collection of business trips associated with this application. | Aggregated | `BusinessTrip.Application` |
| `Visas` | `Visa` | A collection of visas associated with this application. | | `Visa.Application` |
| `ProgressHistory` | `ApplicationProgress` | A history of all progress updates and status changes for this application. | Aggregated | `ApplicationProgress.Application` |

---

## 5. Business Rules & Logic

- **Application Number Generation (`OnSaving`)**:
    - `Year` is set from `ApplicationDate.Year`.
    - `AppNumberPrefix` is defaulted from `Company.AppNumberPrefix` if not already set.
    - `ApplicationNumber` is auto-generated by finding the highest existing number for the given `AppNumberPrefix` and `Year`, then incrementing it. Padding is applied based on `Company.ApplicationNumberPadding` (defaulting to 4).
- **`CurrentState` Management**:
    - The `ProgressHistory` collection's `CollectionChanged` event is subscribed to, triggering `UpdateCurrentState`.
    - `UpdateCurrentState` identifies the latest `ApplicationProgress` entry by `Date` and sets it as `CurrentState`.
- **`OnCreated`**:
    - `ApplicationDate` is initialized to `DateTime.Now`.
    - `Company` is automatically set to the default company.
- **`IsForFamily` and `OrganizationType` Setters**: Changing these properties will reset the `ApplicationType` to `null`, ensuring consistency.
- **`Company` Setter**: When the `Company` is set, `CompanyHead` and `Representative` are automatically updated to the company's `CurrentAuthorizedSignatory` and `CurrentRepresentative`, respectively.
- **`ApplicationType` Data Source**: The available `ApplicationType` options are filtered based on the selected `OrganizationType` and `IsForFamily` flag.
- **Conditional UI Visibility**: Several properties (`ProjectContract`, `VisaPeriod`, `VisaCategory`, `MigrationService`, `BusinessTripPlan`, `ApplicationReason`) and collections (`ApplicationItems`, `Invitations`, `Rejections`, `WorkPermits`, `Registrations`, `BusinessTrips`, `Visas`) are only visible if the selected `ApplicationType` explicitly enables them (e.g., `ApplicationType.ShowProjectContract`).

---

## 6. UI & Behavior Notes

- **Navigation**: This object appears in the navigation menu under the "Application" group.
- **Default Property**: `ApplicationNumber` is the default property used for display purposes.
- **Read-only Fields**: `ApplicationNumber`, `AppNumberPrefix`, `Year`, and `CurrentState` are marked as read-only in the UI as they are system-generated or managed.
- **Immediate Post Data**: `IsForFamily`, `OrganizationType`, `ApplicationType`, and `Company` have `ImmediatePostData` enabled, meaning changes to these properties will immediately trigger server-side logic and UI updates.
- **Nested Collections**: `ApplicationItems`, `Invitations`, `Rejections`, `WorkPermits`, `Registrations`, `BusinessTrips`, `Visas`, and `ProgressHistory` are typically displayed as nested list views within the `Application`'s detail view.