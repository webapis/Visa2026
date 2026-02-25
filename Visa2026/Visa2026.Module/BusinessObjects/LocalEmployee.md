# Business Object: LocalEmployee

## 1. Purpose

The `LocalEmployee` business object represents an employee who is a local national. It extends the base `Person` object with properties specific to local employees, such as citizenship.

---

## 2. Inheritance

This object inherits from the `Person` business object, inheriting properties like `FirstName`, `LastName`, `DateOfBirth`, etc.

---

## 3. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Company` | `Company` | The company the local employee belongs to. | Required. |
| `Citizenship` | `Country` | The country of citizenship for the local employee. | Required. |

---

## 4. Relationships to Other Objects

- **`Company`**: A many-to-one relationship. Each `LocalEmployee` belongs to one `Company`.

---

## 5. Business Rules & Logic

-  None specified in the provided code. Assumed to inherit standard `Person` business rules.

---

## 6. UI & Behavior Notes

- **Navigation**: This object appears in the navigation menu under the "Employee" group.
- When a new `LocalEmployee` is created, the `Company` property should be set.
- The `Citizenship` property should be a required field.