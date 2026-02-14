# Business Object: WorkPermitLetter

## 1. Purpose

The `WorkPermitLetter` business object represents an official letter of approval from a ministry that authorizes the issuance of one or more work permits. It acts as a parent container, grouping all `WorkPermit` records that were granted under a single official document.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `LetterNumber`| `string`  | The official number of the approval letter. | Required; Unique. |
| `LetterDate`  | `DateTime`| The date the letter was issued. | Required. |
| `Documents`   | `XPCollection<WorkPermitLetterDocument>` | Scanned copies of the work permit letter. | Aggregated. |

---

## 3. Relationships to Other Objects

- **`WorkPermits` (WorkPermit)**: A one-to-many, aggregated relationship to a collection of `WorkPermit` objects. This collection holds all the individual work permits authorized by this letter.
- **`Documents` (WorkPermitLetterDocument)**: A one-to-many, aggregated relationship to a collection of document attachments.

---

## 4. UI & Behavior Notes

- The `WorkPermits` collection should be displayed as a nested List View within the `WorkPermitLetter`'s Detail View.
- This allows for the creation and management of individual work permits directly from the context of their approval letter.