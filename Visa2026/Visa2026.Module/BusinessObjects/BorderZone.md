# Business Object: BorderZone

## 1. Purpose

The `BorderZone` business object is a lookup entity designed to provide a standardized list of border zones. It is used to specify which restricted border areas a visa holder is permitted to enter when `HasBorderZonePermit` is enabled on a `Visa`.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Name`        | `string`  | The official name of the border zone (e.g., "Daşoguz", "Garabogaz"). | Required; Unique; Max 100 chars. |
| `Code`        | `string`  | An optional short code for the border zone. | Optional; Unique; Max 10 chars. |

---

## 3. Business Rules & Logic

- The `Name` property must be unique to prevent duplicate entries.

---

## 4. Relationships to Other Objects

- **`ProjectContracts` (ProjectContract)**: A one-to-many relationship to a collection of `ProjectContract` objects associated with this border zone.
- **Referenced By**: The `Visa` business object will have a many-to-one relationship to `BorderZone`.

---

## 5. UI & Behavior Notes

- In lookup editors, the `Name` property should be the default display column.
- It is recommended to pre-populate this table with the standard border zones: Daşoguz, Tagtabazar, Serhetabat, Etrek, Sarahs, Garabogaz, Ýolöten, and Farap.