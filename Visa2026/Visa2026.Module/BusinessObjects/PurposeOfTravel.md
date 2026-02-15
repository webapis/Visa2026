# Business Object: PurposeOfTravel

## 1. Purpose

The `PurposeOfTravel` business object is a lookup entity designed to provide a standardized list of reasons for an individual's travel. It is primarily used in registration-related applications to specify the purpose of entry into the country.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Name`        | `string`  | The full name of the travel purpose (e.g., "Work", "Business Trip", "Family Visit"). | Required; Unique; Max 100 chars. |
| `Code`        | `string`  | An optional short code for the purpose. | Optional; Unique; Max 10 chars. |

---

## 3. Business Rules & Logic

- The `Name` property must be unique to prevent duplicate entries.

---

## 4. Relationships to Other Objects

- **Referenced By**: The `PersonInApplication` business object will have a many-to-one relationship to `PurposeOfTravel`.

---

## 5. UI & Behavior Notes

- In lookup editors, the `Name` property should be the default display column.
- It is recommended to pre-populate this table with the standard travel purposes used by the organization.
- This object should appear in the navigation menu under the "Lookup/Visa" group.