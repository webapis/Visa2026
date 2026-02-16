# Business Object: Subcontractor

## 1. Purpose

The `Subcontractor` business object is a lookup entity that represents a subcontractor company whose employees work on projects.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Name` | `string` | The official name of the subcontractor company. | Required; Unique; Max 255 chars. |
| `ContactPerson` | `string` | The name of the primary contact person at the company. | Optional; Max 255 chars. |
| `PhoneNumber` | `string` | The contact phone number for the company. | Optional; Max 50 chars. |

---



## 4. UI & Behavior Notes

- In lookup editors, the `Name` property should be the default display column.
- This object should appear in the navigation menu under the "Configuration" or "HR" group.