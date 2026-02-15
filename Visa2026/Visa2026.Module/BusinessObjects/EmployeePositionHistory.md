# Business Object: EmployeePositionHistory

## 1. Purpose

The `EmployeePositionHistory` business object is designed to track the history of positions held by an employee within the organization. It allows for a complete audit trail of career progression, including dates and department changes associated with each position.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Employee`    | `Employee`| The employee to whom this record belongs. | Required; Association. |
| `Position`    | `Position`| The position held during this period. | Required. |
| `Department`  | `Department`| The department associated with this position assignment. | Optional. |
| `StartDate`   | `DateTime`| The date the employee started in this position. | Required. |
| `EndDate`     | `DateTime`| The date the employee left this position. | Optional. |
| `IsCurrent`   | `bool`    | Indicates if this is the employee's current position. | Calculated (e.g., EndDate is null). |
| `Documents`   | `XPCollection<EmployeePositionHistoryDocument>` | Copies of documents related to the position assignment (e.g., Order of Assignment). | Aggregated. |

---

## 3. Relationships to Other Objects

- **`Employee` (Employee)**: A many-to-one relationship to the parent `Employee` object.
- **`Position` (Position)**: A reference to the `Position` lookup object.
- **`Department` (Department)**: A reference to the `Department` lookup object.
- **`Documents` (EmployeePositionHistoryDocument)**: A one-to-many, aggregated relationship to a collection of document attachments.

---

## 4. UI & Behavior Notes

- This object appears in the navigation menu under the "Employee" group.
- This object is primarily managed within the `Employee` Detail View via a nested List View.
- Validation should ensure that date ranges for the same employee do not overlap illogically.