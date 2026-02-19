# Business Object: Application

## 1. Purpose

The `Application` business object is a central entity designed to represent a single, collective request submitted to the Turkmenistan Migration Service. A single `Application` can encompass requests for multiple individuals at once, streamlining the submission process for procedures like visa invitations, extensions, and registrations.

---

---

## 3. Structure and Composition

 - **Multiple Individuals:** Each `Application` can contain one or more individuals, managed through the `ApplicationItems` collection.
- **Separation of Employees and Family Members:** The system enforces a strict separation. An application is designated for either employees or family members via the `IsForFamily` flag, but not both.
- **Project-Based:** Applications are often created in the context of a specific `ProjectContract`.

---

## 4. Properties

| Property Name | Data Type | Description |
|---------------|-----------|-------------|--------------------------------|
| `ApplicationNumber` | `string` | A unique identifier for the application. | - |
| `ApplicationDate` | `DateTime` | The date the application is created or submitted. | Required. | 
| `ApplicationType` | `ApplicationType` (Lookup) | The specific type of application (e.g., `ApplicationForInvitation`). |  |
| `CurrentState` | `ApplicationProgress` | The current state of the application . | Read-only; Calculated. |
| `IsForFamily` | `bool` | A flag to distinguish if the application is for employees (`false`) or family members (`true`). | - |
| `ProjectContract` | `ProjectContract` (Lookup) | A reference to the construction project/contract this application is for. | Conditionally Required; Must belong to the selected Ministry. |
| `Urgency` | `Urgency` (Lookup) | A reference to the processing priority (e.g., `Normal`, `Urgent`). | Conditionally Required. |
| `VisaPeriod` | `VisaPeriod` (Lookup) | A reference to the requested visa duration. | Conditionally Required. |
| `VisaCategory` | `VisaCategory` (Lookup) | A reference to the requested visa category. | Conditionally Required. |

- **`Ministry`**: Represents the government ministry to which an application is submitted.
- **`ProjectContract`**: Stores details about the specific contract with a ministry.
- **`Urgency`**: A lookup object to define processing urgency levels.
- **`VisaPeriod`**: A lookup object for standardized visa durations.