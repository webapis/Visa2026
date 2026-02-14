# Business Object: CheckPoint

## 1. Purpose

The `CheckPoint` business object is a lookup entity designed to provide a standardized list of border checkpoints. It is used in registration-related applications to specify an individual's point of entry into the country.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Name`        | `string`  | The full name of the checkpoint (e.g., "Howdan MGP", "Farap MGP"). | Required; Unique; Max 100 chars. |
| `Code`        | `string`  | An optional short code for the checkpoint. | Optional; Unique; Max 10 chars. |

---

## 3. Business Rules & Logic

- The `Name` property must be unique to prevent duplicate entries.

---

## 4. Relationships to Other Objects

- **Referenced By**: The `PersonInApplication` business object will have a many-to-one relationship to `CheckPoint`.

---

## 5. UI & Behavior Notes

- In lookup editors, the `Name` property should be the default display column.
- It is recommended to pre-populate this table with the standard border checkpoints used by the organization.