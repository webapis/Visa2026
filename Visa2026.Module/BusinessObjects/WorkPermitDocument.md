# Business Object: WorkPermitDocument

## 1. Purpose

The `WorkPermitDocument` business object stores scanned copies or other file attachments related to a `WorkPermit`.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `File`        | `FileData`| The attached file (e.g., a PDF or image). | Required. |
| `Description` | `string`  | A brief description of the file. | Optional; Max 255 chars. |

---

## 3. Relationships to Other Objects

- **`WorkPermit` (WorkPermit)**: A many-to-one, aggregated relationship to the parent `WorkPermit` object.