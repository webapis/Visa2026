# Business Object: Region

## 1. Purpose

The `Region` business object is a lookup entity designed to provide a standardized list of administrative regions (welaýatlar). It is used to group `WorkPermitLocation` records, making them easier to manage and filter.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Name`        | `string`  | The full name of the region (e.g., "Ahal Welaýaty"). | Required; Unique; Max 100 chars. |
| `Code`        | `string`  | An optional short code for the region (e.g., "AH"). | Optional; Unique; Max 10 chars. |

---

## 3. Relationships to Other Objects

- **`WorkPermitLocations` (WorkPermitLocation)**: A one-to-many, aggregated relationship to a collection of `WorkPermitLocation` objects that belong to this region.

---

## 4. UI & Behavior Notes

- In lookup editors, the `Name` property should be the default display column.
- This table should be pre-populated with the standard regions of Turkmenistan: Aşgabat şäheri, Ahal welaýaty, Balkan welaýaty, Daşoguz welaýaty, Lebap welaýaty, and Mary welaýaty.