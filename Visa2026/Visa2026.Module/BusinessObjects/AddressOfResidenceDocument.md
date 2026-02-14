# Business Object: AddressOfResidenceDocument

## 1. Purpose

The `AddressOfResidenceDocument` business object stores scanned copies or other file attachments related to an `AddressOfResidence`.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `File`        | `FileData`| The attached file (e.g., a PDF or image). | Required. |
| `Description` | `string`  | A brief description of the file. | Optional; Max 255 chars. |

---

## 3. Relationships to Other Objects

- **`AddressOfResidence` (AddressOfResidence)**: A many-to-one, aggregated relationship to the parent `AddressOfResidence` object.