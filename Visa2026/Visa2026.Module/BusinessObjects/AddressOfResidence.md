# Business Object: AddressOfResidence

## 1. Purpose

The `AddressOfResidence` business object is designed to store the details of an individual's registered address for a specific period. It creates an auditable history of where a person has lived, which is essential for registration-related applications.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Person` | `Person` (Lookup) | A required, aggregated reference to the parent `Person`. | Required. |
| `Type` | `ResidenceType` (Enum) | The type of accommodation. | Required. |
| `Lodging` | `Lodging` (Lookup) | The specific lodging facility. | Required if Type is Lodging. |
| `FullAddress` | `string` | The complete street address, including building and apartment number. | Required; Max 255 chars. |
| `StartDate` | `DateTime` | The date the residence at this address begins. | Required. |
| `EndDate` | `DateTime` | The date the residence at this address ends. | Required; Must be after `StartDate`. |
| `IsCurrent` | `bool` | A flag indicating if this is the active address. Maps to `IsActive`. | - |
| `Documents` | `XPCollection<AddressOfResidenceDocument>` | A collection of related file attachments. | Aggregated. |
| `LodgingDocuments` | `XPCollection<LodgingDocument>` | A read-only collection of documents from the selected Lodging. | Read-only; Calculated. |

---

## 3. Residence Types (Enum)

- `Lodging`
- `Hotel`
- `PrivateHouse`

---

## 4. Business Rules & Logic

- The `EndDate` must always be later than the `StartDate`.
- The `IsCurrent` property (wrapping `IsActive`) indicates the single active address for the person.
- If `Type` is `Lodging`, the `FullAddress` is automatically derived from the selected `Lodging` object.
- Document attachments are stored with the `Lodging` object if `Type` is `Lodging`; otherwise, they are stored with this `AddressOfResidence` object.

---

## 5. Relationships to Other Objects

- **`Person` (Person)**: A many-to-one, aggregated relationship to the parent `Person` object.
- **`Lodging` (Lodging)**: A many-to-one relationship to a `Lodging` object.
- **`Documents` (AddressOfResidenceDocument)**: A one-to-many, aggregated relationship to a collection of document attachments.

---

## 6. UI & Behavior Notes

- This object should be managed as a nested list view within the `Person`'s Detail View.
- It is also accessible via the "Configuration" navigation group.
- Active addresses are highlighted in **Green/Bold** in List Views.
- **Dynamic Visibility**: 
  - The `Lodging` field is visible only when `Type` is set to `Lodging`. When visible, `FullAddress` should be read-only.
  - The `Documents` tab/collection is visible only when `Type` is **not** `Lodging`. When `Type` is `Lodging`, the UI displays `LodgingDocuments` instead.