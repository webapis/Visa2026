using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

# Business Object: Ministry

## 1. Purpose

The `Ministry` business object is a lookup entity that represents a government ministry or agency to which an application is submitted. It stores the ministry's name, contact details, and information about the head of the ministry for use in official correspondence.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Name` | `string` | The official name of the ministry. | Required; Unique; Max 255 chars. |
| `LocalizedName` | `string` | The localized (e.g., Turkmen) name of the ministry. | Optional; Max 255 chars. |
| `MinisterPosition` | `string` | The official title of the head of the ministry (e.g., "Minister"). | Optional; Max 255 chars. |
| `MinisterFullName` | `string` | The full name of the current head of the ministry. | Optional; Max 255 chars. |
| `FormOfAddress` | `string` | The formal way to address the head of the ministry in official letters. | Optional; Max 255 chars. |

---

## 3. Relationships to Other Objects

- **`ProjectContracts` (ProjectContract)**: A one-to-many, aggregated relationship to a collection of `ProjectContract` objects associated with this ministry.
- **Referenced By**: The `Application` business object will have a many-to-one relationship to `Ministry`.

---

## 4. UI & Behavior Notes

- In lookup editors, the `Name` property should be the default display column.
- This table should be pre-populated with the relevant ministries.
- This object should appear in the navigation menu under the "Lookup/Organization" group.