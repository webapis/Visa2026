using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

# Business Object: MaritalStatus

## 1. Purpose

The `MaritalStatus` business object is designed to provide a standardized lookup for marital status options. Its primary purpose is to ensure data consistency when capturing marital status information for other business objects, such as `Person`.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Name`        | `string`  | The name of the marital status (e.g., "Single", "Married", "Divorced"). | Required; Unique; Max 50 chars. |

---

## 3. Business Rules & Logic

- The `Name` property must be unique to prevent duplicate entries.

---

## 4. Relationships to Other Objects

- **Referenced By**: The `Person` business object will reference a `MaritalStatus` object (Many-to-One relationship).

---

## 5. UI & Behavior Notes

- In List Views and lookup editors, the `Name` property should be the default display column.
- It is recommended to pre-populate this object with a list of common marital statuses.
- This object should appear in the navigation menu under the "Lookup/Person" group.