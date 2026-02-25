# Business Object: Employee

## 1. Purpose

The `Employee` business object extends the `Person` object to store information specific to an employee within the organization. It includes details about their employment, company association, and serves as a central point for managing related data such as work permits, contracts, and family members.

---

## 2. Inheritance

This object inherits all properties from the `Person` business object (e.g., `FirstName`, `LastName`, `DateOfBirth`, `Nationality`, etc.).

---

## 3. Properties

This section details the data fields specific to the `Employee` object as defined in `Employee.cs`.

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Company` | `Company` | The company the employee belongs to. | Defaulted to the default company on creation. |
| `IsSubcontractorEmployee` | `bool` | Indicates if the employee works for a subcontractor. | |
| `Subcontractor` | `Subcontractor` | The subcontractor company the employee belongs to. | Visible only if `IsSubcontractorEmployee` is true. |
| `Email` | `string` | The employee's email address. | Max Length: 255. Validated via Regex (`EmployeeEmailFormat`). |
| `CurrentWorkPermitItem` | `WorkPermitItem` | The currently active work permit item. | Read-only in UI (`AllowEdit="False"`). |
| `CurrentPositionHistory` | `EmployeePositionHistory` | The currently active position history record. | Read-only in UI (`AllowEdit="False"`). |
| `CurrentEmployeeContract` | `EmployeeContract` | The currently active employment contract. | Read-only in UI (`AllowEdit="False"`). |
| `CurrentBusinessTrip` | `BusinessTrip` | The currently active business trip. | Read-only in UI (`AllowEdit="False"`). |
| `HireDate` | `DateTime` | The date the employee was hired. | |

---

## 4. Collections (Relationships)

The `Employee` object manages several aggregated collections of related data.

| Collection Name | Item Type | Description |
|-----------------|-----------|-------------|
| `WorkPermitItems` | `WorkPermitItem` | History of work permit items associated with the employee. |
| `FamilyMembers` | `FamilyMember` | List of the employee's family members. |
| `PositionHistory` | `EmployeePositionHistory` | History of positions held by the employee. |
| `EmployeeContracts` | `EmployeeContract` | History of employment contracts. |
| `BusinessTrips` | `BusinessTrip` | History of business trips taken by the employee. |

---

## 5. Behavior & Logic

- **OnCreated**: When a new `Employee` is created, the `Company` property is automatically set to the default company found in the database.
- **UI Appearance**: The `Subcontractor` field is hidden unless `IsSubcontractorEmployee` is checked.
- **Navigation**: This object appears in the navigation menu under the "Employee" group.