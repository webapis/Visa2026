# Business Object: LodgingDocument

## 1. Purpose

The `LodgingDocument` business object stores scanned copies or other file attachments related to a `Lodging` facility, such as rental agreements or official permits.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `File`        | `FileData`| The attached file (e.g., a PDF or image). | Required. |
| `Description` | `string`  | A brief description of the file. | Optional; Max 255 chars. |

---

## 3. Relationships to Other Objects

- **`Lodging` (Lodging)**: A many-to-one, aggregated relationship to the parent `Lodging` object.