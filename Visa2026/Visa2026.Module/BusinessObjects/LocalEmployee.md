# Business Object: LocalEmployee

## 1. Purpose

The `LocalEmployee` business object represents an employee who is a local national. It stores personal and employment information for individuals who are not expatriates. Unlike the `Employee` object, it does not inherit from `Person` but contains its own set of person-related properties.

---

## 2. Inheritance

This object inherits from `BaseObject`.

---

## 3. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Company` | `Company` | The company the local employee belongs to. | |
| `FirstName` | `string` | The employee's first name. | Required; Max 100 chars. |
| `LastName` | `string` | The employee's last name. | Required; Max 100 chars. |
| `MiddleName` | `string` | The employee's middle name or patronymic. | Max 100 chars. |
| `FullName` | `string` | A calculated, read-only field combining the first, middle, and last names. | Read-only; Not Mapped. |
| `BirthDate` | `DateTime` | The employee's date of birth. | |
| `Position` | `Position` | The employee's job position. | |
| `Department` | `Department` | The department the employee works in. | |
| `HireDate` | `DateTime` | The date the employee was hired. | |
| `Email` | `string` | The employee's email address. | Max 255 chars; Validated via Regex. |

---

## 4. Relationships to Other Objects

- **`Company`**: A many-to-one relationship. Each `LocalEmployee` belongs to one `Company`.
- **`Position`**: A many-to-one lookup relationship to the `Position` object.
- **`Department`**: A many-to-one lookup relationship to the `Department` object.
- **Referenced By**: `CompanyHead` and `Representative` objects can link to a `LocalEmployee`.

---

## 5. UI & Behavior Notes

---

- **Navigation**: This object appears in the navigation menu under the "Employee" group.
- **Default Property**: `FullName` is the default property used for display purposes in lookups and references.
- **Validation**: `FirstName` and `LastName` are required fields. The `Email` property has a format validation rule.