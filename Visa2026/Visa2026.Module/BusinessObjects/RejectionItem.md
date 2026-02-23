# Business Object: RejectionItem

## 1. Purpose

The `RejectionItem` business object acts as a line item within a `Rejection`. It links a specific person to the rejection record and can store a reason specific to that individual, if different from the main rejection reason.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Rejection` | `Rejection` (Lookup) | A required reference to the parent `Rejection`. | Required. |
| `Person` | `Person` (Lookup) | The person this rejection line is for. | Required. |
| `Reason` | `string` | A specific reason for this person's rejection, if applicable. | Optional. |

---

## 3. Business Rules & Logic

- The `Person` lookup is filtered to only show individuals who were part of the original `Application` associated with the `Rejection`.

---

## 4. UI & Behavior Notes

- **Default Property**: The `RejectionItemName` (Person's Full Name - Rejection Date) is the default display property in lookups and references.
- **Navigation**: This object appears in the navigation menu under the "Application" group.