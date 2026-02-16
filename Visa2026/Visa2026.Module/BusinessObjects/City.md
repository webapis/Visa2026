# Business Object: City

## 1. Purpose

The `City` business object is designed to store the names of cities, districts, and other localities. It serves as a lookup for various address fields within the application.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Name`        | `string`  | The name of the city or district. | Required; Unique. |
| `Region`      | `Region`  | The region to which the city belongs. | Required. |

---

## 3. UI & Behavior Notes

- This object is intended to be used as a simple lookup list.
- It should be managed under a "Configuration" or "Lookup" navigation group.