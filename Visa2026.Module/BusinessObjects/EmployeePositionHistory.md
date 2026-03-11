# Business Object: EmployeePositionHistory

## 1. Purpose

The `EmployeePositionHistory` business object is designed to track the history of positions held by an employee within the organization. It allows for a complete audit trail of career progression, including dates and department changes associated with each position.

---

## 2. Inheritance
This object inherits from `SingleActiveBaseObject<Person, EmployeePositionHistory>`.

---
## 3. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Person`    | `Person`| The person to whom this record belongs. | Required; Association. |
| `Position`    | `Position`| The position held during this period. | Required. |
| `Department`  | `Department`| The department associated with this position assignment. | Optional. |
| `StartDate`   | `DateTime`| The date the employee started in this position. | Required. |
| `EndDate`     | `DateTime`| The date the employee left this position. | Optional. |
| `IsCurrent`   | `bool`    | Indicates if this is the employee's current position. | Calculated (e.g., EndDate is null). |
| `IsDeleted`        | `bool`        | Indicates whether the record has been soft deleted.                         | Browsable(false). Part of `ISoftDelete` interface.                                                                                                                                                                         |
| `DateDeleted`      | `DateTime?`   | The date the record was soft deleted.                                       | Browsable(false). Part of `ISoftDelete` interface.                                                                                                                                                                         |
| `DeletedBy`        | `ApplicationUser`| The user who soft deleted the record.                                      | Browsable(false). Part of `ISoftDelete` interface.                                                                                                                                                                         |

---

## 3. Relationships to Other Objects

- **`Person` (Person)**: A many-to-one relationship to the parent `Person` object.
- **`Position` (Position)**: A reference to the `Position` lookup object.
- **`Department` (Department)**: A reference to the `Department` lookup object.

---

## 4. UI & Behavior Notes

- This object appears in the navigation menu under the "Employee" group.
- `Title` is used as the default display property
- This object is primarily managed within the `Employee` Detail View via a nested List View.
- Validation should ensure that date ranges for the same employee do not overlap illogically.
- The work permit item will be grayed out on list views if it has been soft deleted, based on the `IsDeleted` property.