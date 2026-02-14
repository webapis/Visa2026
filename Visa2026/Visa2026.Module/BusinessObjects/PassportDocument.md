# Business Object: PassportDocument

## 1. Purpose

The `PassportDocument` business object stores scanned copies or other file attachments related to a `Passport`.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `File`        | `FileData`| The attached file (e.g., a PDF or image of the passport). | Required. |
| `Description` | `string`  | A brief description of the file. | Optional; Max 255 chars. |

---

## 3. Relationships to Other Objects

- **`Passport` (Passport)**: A many-to-one, aggregated relationship to the parent `Passport` object.

---