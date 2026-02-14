# Business Object: PassportType

## 1. Purpose

The `PassportType` business object is a lookup entity designed to provide a standardized list of passport types (e.g., "Regular", "Diplomatic", "Service"). It ensures data consistency when classifying a `Passport`.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Name`        | `string`  | The full name of the passport type. | Required; Unique; Max 100 chars. |
| `Code`        | `string`  | An optional short code for the passport type. | Optional; Unique; Max 10 chars. |

---

## 3. Business Rules & Logic

- The `Name` property must be unique to prevent duplicate entries.

---

## 4. Relationships to Other Objects

- **Referenced By**: The `Passport` business object will have a many-to-one relationship to `PassportType`.

---

## 5. UI & Behavior Notes

- In lookup editors, the `Name` property should be the default display column.
- It is recommended to pre-populate this table with standard passport types.