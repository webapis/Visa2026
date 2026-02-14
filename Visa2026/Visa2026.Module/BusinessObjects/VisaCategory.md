# Business Object: VisaCategory

## 1. Purpose

The `VisaCategory` business object is a lookup entity designed to provide a standardized list of visa categories (e.g., "Single-entry", "Multiple-entry"). It ensures data consistency when classifying a `Visa`.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Name`        | `string`  | The full name of the visa category (e.g., "Single-entry", "Multiple-entry"). | Required; Unique; Max 100 chars. |
| `Code`        | `string`  | An optional short code or abbreviation for the category, used for integration (e.g., "mgCode"). | Optional; Unique; Max 10 chars. |

---

## 3. Business Rules & Logic

- The `Name` property must be unique to prevent duplicate entries.

---

## 4. Relationships to Other Objects

- **Referenced By**: The `Visa` business object will have a many-to-one relationship to `VisaCategory`.

---

## 5. UI & Behavior Notes

- In lookup editors, the `Name` property should be the default display column.
- It is recommended to pre-populate this table with standard visa categories like "Single-entry", "Double-entry", "Triple-entry", and "Multiple-entry".