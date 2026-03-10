# Business Object: Rejection

## 1. Purpose
The `Rejection` business object represents a negative outcome for an `Application`. It records the official refusal from the State Migration Service, including the date and the reason for rejection.

---

## 2. Inheritance

This object inherits from `BaseObject` and implements the `IPersonLinkParent` interface.

---

## 3. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Application` | `Application` | The parent application that was rejected. | Required. |
| `RejectedDocNumber` | `string` | The official document number of the rejection letter. | Optional; Max 50 chars. |
| `Date` | `DateTime` | The date the rejection decision was issued. | Optional. |
| `Reason` | `string` | The official reason provided for the rejection. | Optional. |
| `RejectionItems` | `IList<RejectionItem>` | A list of individuals included in this rejection. | Aggregated. |
| `File` | `FileData` | A scanned copy of the official rejection letter. | Optional. |
| `AvailablePeople` | `IList<Person>` | A calculated, non-persistent list of people available to be added to this rejection, sourced from the parent `Application`. | Read-only; Not Mapped; Not Browsable. |

---

## 4. Business Rules & Logic

- **`IPersonLinkParent`**: The class implements the `IPersonLinkParent` interface, providing the `Application` and `AvailablePeople` properties. This allows it to be a parent to `PersonLinkedItemBase` objects (`RejectionItem`), which use this information for data source filtering and validation.

---

## 5. UI & Behavior Notes

- **Default Property**: `RejectionTitle` ("Rejection [Doc Number] on [Date]") is the default display property.
- **Icon**: `BO_Validation` or `State_Validation_Invalid`.