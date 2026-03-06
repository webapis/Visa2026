# Business Object: EmployeeContract

## 1. Purpose

The `EmployeeContract` business object represents an official employment agreement between the company and an `Employee`. It is designed to track the terms of employment, including the position, duration, and other contractual details for a specific period.

---

## 2. Inheritance

This object inherits from `SingleActiveBaseObject<Employee, EmployeeContract>` and implements the `IExpirationLogic` interface.

---

## 3. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Employee` | `Employee` | The employee to whom the contract applies. | Required. |
| `Person` | `Person` | The person to whom the contract applies. | Required. |
| `Position` | `Position` | The job position covered by this contract. | Required. |
| `ContractStartDate` | `DateTime` | The date the contract becomes effective. | Required. |
| `ExpirationDate` | `DateTime?` | The date the contract expires or is terminated. | Required; Must be later than ContractStartDate. |
| `PositionHistory` | `EmployeePositionHistory` | The job position history record covered by this contract. | Required. |
| `Salary` | `decimal` | The salary specified in the contract. |  |
| `ContractTemplate` | `ContractTemplate` | The contract template used for this contract. |  |
| `DaysRemaining` | `int` | A calculated property showing the number of days until the contract ends. | Read-only; from `IExpirationLogic`. |
| `ExpirationState` | `ExpirationState` | A calculated property indicating the status (e.g., Active, Expired, ExpiringSoon). | Read-only; from `IExpirationLogic`. |

---

## 4. Collections (Relationships)

| Collection Name | Item Type | Description | Aggregation | Inverse Property |
|-----------------|-----------|-------------|-------------|------------------|
| `Documents` | `EmployeeContractDocument` | A collection of scanned copies of the contract and related addendums. | Aggregated | `EmployeeContractDocument.EmployeeContract` |
| `Images` | `EmployeeContractImage` | A collection of scanned images of the contract and related addendums. | Aggregated | `EmployeeContractImage.EmployeeContract` |

---

## 5. Business Rules & Logic

- **Single Active Item**: As a `SingleActiveBaseObject`, only one contract can be active for an `Employee` at a time. Activating a new `EmployeeContract` (by setting `IsActive` to `true`) automatically deactivates the previous one and updates the `Employee.CurrentEmployeeContract` property.
- **Expiration Logic**: The object implements `IExpirationLogic`, using the `ExpirationDate` property for its calculations. This allows for consistent tracking and UI representation of the contract's validity period (Active, Expiring Soon, Expired).

---

## 6. Relationships to Other Objects

- **`Person`**: A required, many-to-one relationship. The `Person` object maintains a collection of all their `EmployeeContracts`.
- **`Employee`**: A required, many-to-one relationship. The `Employee` object maintains a collection of all their `EmployeeContracts`.
- **`Position`**: A many-to-one lookup relationship to the `Position` object.

---

## 7. UI & Behavior Notes

- **Navigation**: This object should appear in the navigation menu, likely under the "Employee" group.
- **Default Property**: A calculated property combining the employee name and contract number would be a suitable default display property.
- **Management**: This object is primarily intended to be managed from within the `Employee`'s Detail View, via the `EmployeeContracts` collection.