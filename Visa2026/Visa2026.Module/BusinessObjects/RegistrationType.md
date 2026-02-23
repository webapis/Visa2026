# Business Object: RegistrationType

## 1. Purpose

The `RegistrationType` business object serves as a lookup for the type of registration being performed, such as "Check in" or "Check out". This ensures consistency in categorizing registration events.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Name` | `string` | The descriptive name of the registration type (e.g., "Check in", "Check out"). | Required; Unique; Max 100 chars. |

---

## 3. UI & Behavior Notes

- **Default Property**: The `Name` property is the default display member for lookup editors.
- **Navigation**: This object appears in the navigation menu under the "Lookup/Registration" group.