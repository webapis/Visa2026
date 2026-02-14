# Business Object: ProjectContract

## 1. Purpose

The `ProjectContract` business object represents an official contract between the company and a government ministry. It is used to group applications that fall under a specific project or agreement.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Number`      | `string`  | The official number of the contract. | Required; Unique. |
| `Code`        | `string`  | A short code describing the project (3-5 letters). | Required; Unique; Max 5 chars. |
| `Content`     | `string`  | A detailed description or content of the contract. | Optional; Unlimited size. |
| `Ministry`    | `Ministry` (Lookup) | A required, aggregated reference to the parent `Ministry`. | Required. |

---

## 3. Relationships to Other Objects

- **`Ministry` (Ministry)**: A many-to-one, aggregated relationship to the parent `Ministry` object.
- **`WorkPermitLocation` (WorkPermitLocation)**: A reference to the `WorkPermitLocation` associated with this contract.
- **`BorderZone` (BorderZone)**: A reference to the `BorderZone` associated with this contract.
- **Referenced By**: The `Application` business object will have a many-to-one relationship to `ProjectContract`.

---

## 4. UI & Behavior Notes

- This object should be managed as a nested list view within the `Ministry`'s Detail View.
- The `Number` property should be the default display column in lookups.