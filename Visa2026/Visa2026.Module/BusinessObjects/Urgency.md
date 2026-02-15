# Business Object: Urgency

## 1. Purpose

The `Urgency` business object is a lookup entity designed to provide a standardized list of processing priorities for applications (e.g., "Normal", "Urgent", "Highly Urgent"). It helps in categorizing and prioritizing applications based on their required processing speed.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Name`        | `string`  | The full name of the urgency level (e.g., "Adaty", "Gyssagly"). | Required; Unique; Max 100 chars. |
| `Code`        | `string`  | An optional short code for the urgency level. | Optional; Unique; Max 10 chars. |
| `Priority`    | `int`     | An integer value indicating the processing priority (e.g., 1 for Normal, 2 for Urgent). | Required; Unique. |

---

## 3. Business Rules & Logic

- The `Name` and `Priority` properties must be unique to prevent duplicate entries.

---

## 4. Relationships to Other Objects

- **Referenced By**: The `Application` business object will have a many-to-one relationship to `Urgency`.

---

## 5. UI & Behavior Notes

- In lookup editors, the `Name` property should be the default display column.
- It is recommended to pre-populate this table with standard urgency levels: "Adaty", "Gyssagly", and "Örän Gyssagly".
- This object should appear in the navigation menu under the "Lookup/Visa" group.