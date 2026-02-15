# Business Object: VisaPeriod

## 1. Purpose

The `VisaPeriod` business object is a lookup entity designed to provide a standardized list of visa durations (e.g., "1 Month", "3 Months", "1 Year"). It is used in visa-related applications to specify the requested validity period.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Name`        | `string`  | The descriptive name of the visa period (e.g., "3 aý"). | Required; Unique; Max 100 chars. |
| `Months`      | `int`     | The duration of the period in months. | Required; Unique. |

---

## 3. Business Rules & Logic

- The `Name` and `Months` properties must be unique to prevent duplicate entries.

---

## 4. Relationships to Other Objects

- **Referenced By**: The `Application` business object will have a many-to-one relationship to `VisaPeriod`.

---

## 5. UI & Behavior Notes

- In lookup editors, the `Name` property should be the default display column.
- It is recommended to pre-populate this table with standard visa periods used by the organization.
- This object should appear in the navigation menu under the "Lookup/Visa" group.