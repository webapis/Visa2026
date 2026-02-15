# Business Object: Position

## 1. Purpose

The `Position` business object is a lookup entity designed to provide a standardized list of job titles (e.g., "Manager", "Engineer", "Accountant"). It ensures data consistency when defining an employee's current role.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Name`        | `string`  | The full name of the position (e.g., "Senior Software Engineer"). | Required; Unique; Max 100 chars. |
| `Code`        | `string`  | An optional short code for the position. | Optional; Unique; Max 10 chars. |

---

## 3. Business Rules & Logic

- The `Name` property must be unique to prevent duplicate entries.

---

## 4. Relationships to Other Objects

- **Referenced By**: The `Employee` business object will have a many-to-one relationship to `Position` to represent the employee's current role.

---

## 5. UI & Behavior Notes

- In lookup editors, the `Name` property should be the default display column.
- This table should be pre-populated with the standard job titles used by the organization.
- This object should appear in the navigation menu under the "Lookup/Organization" group.