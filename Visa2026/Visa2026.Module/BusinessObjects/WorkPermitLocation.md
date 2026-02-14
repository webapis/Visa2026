# Business Object: WorkPermitLocation

## 1. Purpose

The `WorkPermitLocation` business object is a lookup entity designed to provide a standardized list of geographical locations (cities, districts, etc.) where an employee is permitted to work.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Name`        | `string`  | The full name of the location (e.g., "Aşgabat şäheri"). | Required; Unique; Max 100 chars. |
| `Region`      | `Region` (Lookup) | A reference to the administrative region (welaýat) the location belongs to. | Optional. |

---

## 3. Business Rules & Logic

- The `Name` property must be unique to prevent duplicate entries.

---

## 4. Relationships to Other Objects

- **`ProjectContracts` (ProjectContract)**: A one-to-many relationship to a collection of `ProjectContract` objects associated with this location.
- **Referenced By**: The `WorkPermit` business object will have a relationship to `WorkPermitLocation`. The legacy system allowed multiple locations, so this will likely be a many-to-many relationship.

---

## 5. UI & Behavior Notes

- In lookup editors, the `Name` property should be the default display column.
- This table should be pre-populated with all possible work locations from the legacy system.