# Business Object: Employee

## 1. Purpose

The `Employee` business object extends the `Person` object to store information specific to an employee within the organization. It includes details about their employment and serves as a central point for managing related data, such as family members.

---

## 2. Inheritance

This object inherits all properties from the `Person` business object, including `FirstName`, `LastName`, `Email`, `Birthday`, etc.

---

## 3. Properties

This section details the data fields specific to the `Employee` object.

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `EmployeeId`  | `string`  | The unique identifier for the employee. | Optional; Max 20 chars. |
| `Position`    | `Position`| A reference to the employee's current job title. | Required; Read-only in UI. |
| `HireDate`    | `DateTime`| The date the employee was hired. | Optional. |
| `Department`  | `Department`| A reference to the department where the employee works. | Required; Read-only in UI. |
| `CurrentWorkPermit` | `WorkPermit` | The currently active work permit. | Read-only. Managed automatically. |
| `CurrentPositionHistory` | `EmployeePositionHistory` | The currently active position history record. | Read-only. Managed automatically. |
| `IsSubcontractorEmployee` | `bool` | Indicates if the employee works for a subcontractor. | Default: False. |
| `Subcontractor` | `Subcontractor` | The subcontractor company the employee belongs to. | Visible only if IsSubcontractorEmployee is true. |
| `ProjectContract` | `ProjectContract` | The project contract the employee is working on. | Optional. |

---

## 4. Relationships to Other Objects

- **`Department` (Department)**: A many-to-one relationship to the `Department` object.
- **`Position` (Position)**: A many-to-one relationship to the `Position` object, representing the employee's current job title.
- **`Subcontractor` (Subcontractor)**: A many-to-one relationship to the `Subcontractor` object.
- **`ProjectContract` (ProjectContract)**: A many-to-one relationship to the `ProjectContract` object.
- **`FamilyMembers` (FamilyMember)**: A one-to-many relationship to a collection of `FamilyMember` objects. This collection holds all family members associated with this employee. The relationship is aggregated, meaning the lifecycle of a `FamilyMember` is managed by the `Employee`.
- **`PositionHistory` (EmployeePositionHistory)**: A one-to-many, aggregated relationship to track the history of positions held.
- **`WorkPermits` (WorkPermit)**: A one-to-many relationship to a collection of `WorkPermit` objects associated with the employee.

---

## 5. UI & Behavior Notes

- This object appears in the navigation menu under the "HR" group.
- The `FamilyMembers` collection is displayed as a nested List View within the `Employee`'s Detail View, allowing for inline management of family members.
- The `PositionHistory` collection should be displayed as a nested List View.