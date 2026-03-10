# Business Object: Lodging

## 1. Purpose

The `Lodging` business object represents a company-managed accommodation facility (e.g., a camp, dormitory, or rented apartment complex) where employees can reside. It centralizes address information to ensure consistency across residence records.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Name` | `string` | The name of the lodging facility (e.g., "Camp A", "City Apartment Block 1"). | Required; Unique. |
| `Address` | `string` | The full physical address of the lodging. | Required. |
| `Documents` | `XPCollection<LodgingDocument>` | A collection of related file attachments (e.g., rental agreement). | Aggregated. |

---

## 3. Relationships to Other Objects

- **Referenced By**: `AddressOfResidence` objects reference `Lodging` to associate an employee with a specific facility.
- **`Documents` (LodgingDocument)**: A one-to-many, aggregated relationship to a collection of document attachments.

---

## 4. UI & Behavior Notes

- In lookup editors, the `Name` property should be the default display column.