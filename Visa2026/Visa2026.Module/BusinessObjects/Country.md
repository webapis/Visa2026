using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

# Business Object: Country

## 1. Purpose

The `Country` business object is designed to store information about countries. Its primary purpose is to provide a standardized lookup for country data that can be referenced by other business objects (e.g., `Person`, `Address`, `Customer`) to ensure data consistency and simplify data entry.

---

## 2. Properties

This section details the data fields of the `Country` object.

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Name`        | `string`  | The official name of the country. | Required; Unique; Max 100 chars. |
| `Code`        | `string`  | The three-letter ISO 3166-1 alpha-3 country code (e.g., "USA", "GBR", "DEU"). | Required; Unique; Fixed 3 chars. |
| `DialingCode` | `string`  | The international dialing code for the country (e.g., "+1", "+44", "+49"). | Optional; Max 10 chars. |

---

## 3. Business Rules & Logic

- The `Name` property must be unique to prevent duplicate country entries.
- The `Code` property must be unique and exactly three characters long, adhering to the ISO 3166-1 alpha-3 standard.

---

## 4. Relationships to Other Objects

- **Referenced By**: Many other business objects (e.g., `Person`, `Customer`, `Address`) will reference a `Country` object (Many-to-One relationship).

---

## 5. UI & Behavior Notes

- In List Views and lookup editors, the `Name` property should be the default display column for identifying a country.
- The `Code` property should be displayed prominently in Detail Views for quick reference.