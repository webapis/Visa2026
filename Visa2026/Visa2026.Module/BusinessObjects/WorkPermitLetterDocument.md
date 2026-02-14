# Business Object: WorkPermitLetterDocument

## 1. Purpose

The `WorkPermitLetterDocument` business object stores scanned copies or other file attachments related to a `WorkPermitLetter`.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `File`        | `FileData`| The attached file (e.g., a PDF or image). | Required. |
| `Description` | `string`  | A brief description of the file. | Optional; Max 255 chars. |

---

## 3. Relationships to Other Objects

- **`WorkPermitLetter` (WorkPermitLetter)**: A many-to-one, aggregated relationship to the parent `WorkPermitLetter` object.