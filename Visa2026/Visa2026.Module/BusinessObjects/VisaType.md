# Business Object: VisaType

## 1. Purpose

The `VisaType` business object is a lookup entity designed to provide a standardized list of visa types. It ensures data consistency when classifying a `Visa` and can be used to store related codes or alternative names for reporting and integration purposes.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Name`        | `string`  | The full name of the visa type (e.g., "Work Permit", "Business"). | Required; Unique; Max 100 chars. |
| `Code`        | `string`  | A short code or abbreviation for the visa type (e.g., "WP", "BS"). | Required; Unique; Max 10 chars. |
| `Description` | `string`  | An optional description providing more details about the visa type. | Optional. |

---

## 3. Business Rules & Logic

- Both `Name` and `Code` must be unique to prevent duplicate entries.

---

## 4. Relationships to Other Objects

- **Referenced By**: The `Visa` business object will have a many-to-one relationship to `VisaType`.

---

## 5. UI & Behavior Notes

- In lookup editors, the `Name` property should be the default display column.
- It is recommended to pre-populate this table with the standard visa types used by the organization, such as: FM, BS, GL, WP, ÇK, PR.