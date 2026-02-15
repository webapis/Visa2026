# Business Object: VisaIssuedPlace

## 1. Purpose

The `VisaIssuedPlace` business object is a lookup entity designed to provide a standardized list of visa issuance locations (e.g., "In Turkmenistan", "Abroad"). It ensures data consistency when specifying where a `Visa` was issued.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Name`        | `string`  | The full name of the issuance place category (e.g., "In Turkmenistan", "Abroad"). | Required; Unique; Max 100 chars. |
| `IsDefault`   | `bool`    | Indicates if this is the default selection. | Optional. |

---

## 3. Business Rules & Logic

- The `Name` property must be unique to prevent duplicate entries.

---

## 4. Relationships to Other Objects

- **Referenced By**: The `Visa` business object will have a many-to-one relationship to `VisaIssuedPlace`.

---

## 5. UI & Behavior Notes

- In lookup editors, the `Name` property should be the default display column.
- It is recommended to pre-populate this table with the standard values: "Türkmenistanda" and "Daşary ýurtda".
- This object should appear in the navigation menu under the "Lookup/Visa" group.