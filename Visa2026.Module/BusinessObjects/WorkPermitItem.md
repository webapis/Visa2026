# Business Object: WorkPermitItem

## 1. Purpose

The `WorkPermitItem` business object is designed to manage the lifecycle of an employee's work permit. It stores all essential details, including the permit's validity dates, the associated employee and position history, and references to the official documents and application processes that authorized it.

---

## 2. Inheritance

This object inherits from `SingleActiveBaseObject<Employee, WorkPermitItem>` and implements the `IExpirationLogic` interface.

---

## 3. Properties

This section details the data fields of the `WorkPermitItem` object as defined in `WorkPermitItem.cs`.

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Employee` | `Employee` | The employee to whom the work permit is issued. | Required. |
| `Passport` | `Passport` | The passport associated with this work permit. | Required. |
| `CurrentPositionHistory` | `EmployeePositionHistory` | The employee's position history record relevant to this permit. | Required. |
| `StartDate` | `DateTime` | The date the work permit becomes valid. | Required. |
| `ExpirationDate` | `DateTime` | The date the work permit expires. | Required. |
| `WorkPermitNumber` | `string` | The official approval number of the work permit. | Required. |
| `IsCancelled` | `bool` | Indicates the work permit item is cancelled. | Optional (gear); gated by application type when linked. |
| `ASNumber` | `string` | Authorization / tassyk-nama reference number. | Required. |
| `WorkPermittedLocations` | `string` | Comma-separated permitted work locations (catalog multi-select). | Required. |
| `WorkPermit` | `WorkPermit` | A reference to the official letter that granted the permit. | Optional. |
| `IsEmployeeValid` | `bool` | A validation property to ensure the selected employee is part of the parent application. | Read-only. |
| `DaysRemaining` | `int` | A calculated property showing the number of days until the permit expires. | Read-only. |
| `ExpirationState` | `ExpirationState` | A calculated property indicating the status (e.g., Active, Expired, ExpiringSoon). | Read-only. |
| `WorkPermitItemName` | `string` | A calculated display name for the item. | Read-only. |

---

## 4. Business Rules & Logic

- **Single Active Item**: As a `SingleActiveBaseObject`, only one work permit item can be active for an `Employee` at a time. Activating a new item automatically updates the `Employee.CurrentWorkPermitItem` property.
- **Expiration Logic**: The object implements `IExpirationLogic`. The system tracks `ExpirationDate` to determine if the permit is Active, Expiring Soon (<= 30 days), or Expired.
- **Employee Validation**: The `IsEmployeeValid` property ensures that the selected `Employee` is listed in the `ApplicationItems` of the parent `WorkPermit.Application`.
- **Display Name**: `WorkPermitItemName` is automatically formatted as `"{Employee.FullName} - {WorkPermitNumber}"`.

---

## 5. Relationships to Other Objects

- **`Employee`**: A required, many-to-one relationship to the `Employee` object.
- **`Passport`**: A required, many-to-one relationship to the `Passport` object.
- **`CurrentPositionHistory`**: A required, many-to-one relationship to the `EmployeePositionHistory` object.
- **`WorkPermit`**: A lookup relationship to the parent `WorkPermit` object.
- **`Location`**: A lookup relationship to the `WorkPermitLocation` object.

---

## 6. UI & Behavior Notes

- **Navigation**: This object appears in the navigation menu under the "Employee" group.
- **Default Property**: `WorkPermitItemName` is the default property used for display purposes.
- **Calculated Fields**: `DaysRemaining`, `ExpirationState`, and `WorkPermitItemName` are calculated in real-time and are not directly editable.
- **Optional detail fields**: `IsCancelled` is behind the gear toggle; auto-expands when `true`. When linked to an application, it is hidden unless the application type enables `ShowWorkPermitItemIsCancelled`. Change-work-permit workflow uses `ApplicationItem.WorkPermitItemIsChanged`, not columns on `WorkPermitItem`.
- **Appearance Rules**: The work permit item will be grayed out on list views if it has been soft deleted, based on the `IsDeleted` property.
