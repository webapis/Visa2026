# Business Object: WorkPermit

## 1. Purpose

The `WorkPermit` business object represents an official letter of approval from a ministry that authorizes the issuance of one or more work permits. It acts as a parent container, grouping all `WorkPermitItem` records that were granted under a single official document.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Number`      | `string`  | The official number of the approval letter. | Required; Unique. |
| `Date`        | `DateTime`| The date the letter was issued. | Required. |
| `Documents`   | `IList<WorkPermitDocument>` | Scanned copies of the work permit letter. | Aggregated. |

---

## 3. Relationships to Other Objects

- **`Items` (WorkPermitItem)**: A one-to-many, aggregated relationship to a collection of `WorkPermitItem` objects. This collection holds all the individual work permits authorized by this letter.
- **`Documents` (WorkPermitDocument)**: A one-to-many, aggregated relationship to a collection of document attachments.
- **`Application` (Application)**: A many-to-one relationship to the parent `Application`.

---

## 4. UI & Behavior Notes

- The `Items` collection should be displayed as a nested List View within the `WorkPermit`'s Detail View.
- This allows for the creation and management of individual work permits directly from the context of their approval letter.